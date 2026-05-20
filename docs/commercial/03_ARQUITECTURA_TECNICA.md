# 03 · Arquitectura técnica

## Stack confirmado

| Capa | Tecnología | Versión objetivo |
|------|-----------|------------------|
| API | ASP.NET Core | 10 (LTS cuando salga) |
| ORM | EF Core + Npgsql | 10 |
| BD | PostgreSQL | 16 |
| Cache | Redis | 7 |
| Real-time | SignalR | nativo |
| Web admin & B2B | Blazor Server | 10 + MudBlazor 7 |
| Mobile | Flutter | 3.x estable |
| Auth | JWT (Access + Refresh) + OIDC opcional | — |
| Búsqueda | PostgreSQL FTS + pg_trgm (fase 2: OpenSearch) | — |
| Files | MinIO / S3 compatible | — |
| Observabilidad | Serilog + OpenTelemetry → Grafana/Loki/Tempo | — |
| CI/CD | GitHub Actions + Docker + systemd | — |

## Diagrama de alto nivel

```
┌─────────────────────────────────────────────────────────────┐
│                  CLIENTES (frontends)                        │
│  Flutter app vendedores  ·  Blazor admin  ·  Blazor B2B     │
│  Blazor portal clientes  ·  Webhooks  ·  Integraciones      │
└─────────────────────────────────────────────────────────────┘
                │                  │                  │
                │ HTTPS/JWT        │ SignalR          │ Webhooks
                ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────┐
│                   API GATEWAY  (ASP.NET Core 10)             │
│  ─ Middleware: TenantResolver, Auth, RateLimit, RequestLog  │
│  ─ Hubs: NotificationsHub, OrdersHub, PresenceHub           │
│  ─ Endpoints versionados:  /api/v1/...                       │
└─────────────────────────────────────────────────────────────┘
                │             │             │
        Application       Infrastructure   Persistence
        (CQRS lite)       (Auth, Storage,  (EF Core)
                           CFDI, Email)
                │             │             │
                └──────┬──────┴─────────────┘
                       ▼
            ┌────────────────────────┐
            │      PostgreSQL 16     │   Redis (cache, presence)
            │  schema = nanchesoft   │   MinIO (archivos, fotos)
            │  RLS por company_id    │
            └────────────────────────┘
```

## Estructura de soluciones (extensión)

```
src/
├── Nanchesoft.Domain/                  (existente)
│   ├── Entities/
│   │   ├── Sales/                       — Customer, SalesOrder, etc. (existentes)
│   │   ├── Crm/                  ★ NUEVO  Lead, Opportunity, Activity, CampaignTouch
│   │   ├── Commerce/             ★ NUEVO  B2BAccount, B2BUser, CartSession, CartLine
│   │   ├── Fulfillment/          ★ NUEVO  OrderStateLog, ShipmentTracking
│   │   └── Portal/               ★ NUEVO  CustomerPortalUser, Notification
│   └── Common/
│       └── Workflow/                    ★ NUEVO  StateMachine<T>, IStatefulEntity
│
├── Nanchesoft.Application/
│   ├── Commerce/                  ★ NUEVO  CartService, CheckoutService, PricingService
│   ├── Crm/                       ★ NUEVO  PipelineService, ActivityService, LeadService
│   ├── Sales/                            (extender SalesOrderService)
│   └── Sync/                      ★ NUEVO  DeltaSyncService (offline mobile)
│
├── Nanchesoft.Api/
│   ├── Endpoints/
│   │   ├── CommerceEndpoints.cs   ★ NUEVO
│   │   ├── CrmEndpoints.cs        ★ NUEVO
│   │   ├── B2BPortalEndpoints.cs  ★ NUEVO
│   │   ├── MobileSyncEndpoints.cs ★ NUEVO
│   │   └── ...
│   └── Hubs/                      ★ NUEVO
│       ├── NotificationsHub.cs
│       ├── OrdersHub.cs
│       └── PresenceHub.cs
│
├── Nanchesoft.Web/                     (Blazor existente — extender)
│   └── Components/
│       ├── Crm/                   ★ NUEVO
│       ├── B2B/                   ★ NUEVO  Catálogo, carrito, checkout
│       └── Portal/                ★ NUEVO  Estado de cuenta, mis pedidos
│
├── Nanchesoft.Mobile/             ★ NUEVO PROYECTO  (Flutter)
│   ├── lib/
│   │   ├── app/
│   │   ├── features/
│   │   │   ├── auth/
│   │   │   ├── customers/
│   │   │   ├── orders/
│   │   │   ├── crm/
│   │   │   ├── route/
│   │   │   └── dashboard/
│   │   ├── core/
│   │   │   ├── api/
│   │   │   ├── sync/
│   │   │   ├── db/  (Drift / SQLite)
│   │   │   └── design/
│   │   └── main.dart
│
└── Nanchesoft.Shared/                  (DTOs comunes)
    └── Commerce/                  ★ NUEVO  CartDto, CheckoutDto, etc.
```

## Patrones arquitectónicos

### 1. CQRS-lite con MediatR

- Comandos (`CreateOrderCommand`) y queries (`GetOrdersByCustomerQuery`) explícitos.
- Handlers pequeños y testeables.
- Validación con FluentValidation por comando.

### 2. Tenant resolver

```csharp
public sealed class TenantResolverMiddleware
{
    public async Task InvokeAsync(HttpContext ctx, ITenantContext tenant, ...)
    {
        var companyId = ResolveFromJwtOrHeader(ctx);
        tenant.SetCompany(companyId);
        await _next(ctx);
    }
}
```

- El `companyId` viaja en el JWT como claim.
- Todas las queries de EF Core aplican filtro global: `modelBuilder.Entity<T>().HasQueryFilter(e => e.CompanyId == _tenant.CompanyId);`
- Postgres RLS como segunda línea: `CREATE POLICY tenant_isolation ON sales_order USING (company_id = current_setting('app.company_id')::int);`

### 3. State machine declarativa

```csharp
public sealed class SalesOrderStateMachine : StateMachine<SalesOrder, OrderState>
{
    public SalesOrderStateMachine()
    {
        Allow(Draft, Confirmed,  requires: Role.Seller);
        Allow(Confirmed, Picked, requires: Role.Warehouse);
        Allow(Picked, Shipped,   requires: Role.Warehouse);
        Allow(Shipped, Delivered,requires: Role.Logistics);
        AllowAny(_, Cancelled,   requires: Role.Manager, beforeState: NotIn(Delivered));
    }
}
```

### 4. Real-time con SignalR

| Hub | Eventos | Receptores |
|-----|---------|-----------|
| `OrdersHub` | `order.created`, `order.state.changed`, `order.line.added` | Vendedores del cliente, admin, almacén |
| `NotificationsHub` | `notification.new`, `notification.read` | Usuario |
| `PresenceHub` | `user.online`, `user.typing`, `cart.viewing` | Equipos compartidos |
| `CrmHub` | `lead.assigned`, `activity.due-soon` | Vendedor / supervisor |

- Grupos: `tenant:{companyId}`, `role:{role}`, `user:{userId}`, `customer:{customerId}`.
- Reconexión automática, mensajes garantizados con replay (last 50 desde Redis stream).

### 5. Sync delta offline (móvil)

```
GET /api/v1/sync/changes?since=2026-05-19T12:00:00Z
   → { customers: [...], products: [...], priceLists: [...],
        tombstones: [{type, id, deletedAt}], serverTime: ... }

POST /api/v1/sync/push
   { operations: [
       { id: "client-uuid", op: "create", entity: "order", payload: {...} },
       { id: "client-uuid", op: "update", entity: "activity", payload: {...} }
   ] }
   → { results: [{ clientId, status, serverId, conflict? }] }
```

- Idempotencia por `operationId` (UUID generado en cliente).
- Conflictos: last-write-wins por campo + bandera para revisión del usuario.
- Tombstones para eliminaciones.
- SQLite local con Drift en Flutter; tablas espejo + cola `pending_operations`.

### 6. Pricing engine

`PricingService` aplica en este orden:

1. Lista de precios del cliente (override).
2. Lista de precios por categoría / canal.
3. Reglas: cantidad mínima, fecha, combo, descuento global.
4. IVA / IEPS según producto.

Devuelve `LinePricing { Base, Applied, Tax, Total, RuleSourceId, RuleLabel }`.

### 7. Validación CFDI 4.0 al confirmar pedido

- Pre-validación del RFC del cliente vía cache (consultas a SAT con throttle).
- `UsoCfdi`, `MetodoPago`, `FormaPago` requeridos al confirmar B2B.
- Stamp PAC se difiere a facturación, pero la captura es desde el pedido.

## Convenciones API

- Base: `https://api.nanchesoft.mx/api/v1/`
- Auth: `Authorization: Bearer <jwt>`
- Tenant: claim `company_id` en el JWT; opcional header `X-Company-Id` para multiempresa por usuario.
- Versionado por URL (`/v1`, `/v2`); breaking changes solo entre majors.
- Paginación: cursor-based `?cursor=...&limit=50` (más estable que offset bajo cambios concurrentes).
- Filtros tipo OData light: `?filter=state eq 'confirmed' and total gt 1000&sort=-createdAt`.
- Respuestas siempre envueltas:

```json
{
  "data": [ ... ],
  "meta": { "cursor": "abc", "hasMore": true, "count": 50, "tookMs": 38 },
  "errors": []
}
```

- ETags y `If-None-Match` para listas grandes (productos, clientes).
- Rate limit por usuario / empresa, con cabeceras `RateLimit-*`.

## Seguridad

- **Claims del JWT:**
  - `sub` (userId), `company_id`, `role` (multi-valor), `scopes`, `tenant`, `iat`, `exp`.
- **Refresh tokens** rotatorios, con detección de reuse.
- **MFA opcional** TOTP para usuarios administrativos.
- **OIDC** opcional para clientes empresariales (Azure AD, Google Workspace).
- **Roles base:** SuperAdmin, CompanyAdmin, SalesManager, Seller, WarehouseManager, WarehouseOp, Customer (B2B), Cashier, Accountant, Auditor.
- **Permisos granulares:** matriz `Role × Module × Action` en `RolePermission`.
- **Auditoría:** todo mutación significativa pasa por `AuditLog` (ya existe).
- **PII:** datos personales con cifrado columna (`pgcrypto` para CURP/teléfonos) opcional.

## Observabilidad

- **Logs estructurados** con Serilog, salida JSON a Loki.
- **Trazas distribuidas** OpenTelemetry → Tempo. Span por endpoint + por handler de MediatR + por consulta EF.
- **Métricas Prometheus:** request rate, latency P50/P95/P99, errors, cart_conversion, orders_per_minute.
- **Dashboards Grafana** preconstruidos:
  - "Salud API" (latencia, errores, throughput).
  - "Negocio" (pedidos/hora, conversión, ticket medio).
  - "Móvil" (versiones, crashes, sync conflicts).

## Despliegue

- **Docker Compose** para dev local (Postgres + Redis + MinIO + API + Web).
- **Producción:** systemd units en VPS dedicado (alineado con setup actual) con opción a contenerizar.
- **Migraciones:** EF Core migrations + script de RLS en `scripts/sql/`.
- **Estrategia blue-green** para deploys sin downtime (api + web detrás de Nginx).
- **Builds Flutter:**
  - Android: Play Store (track interno → cerrado → producción).
  - iOS: TestFlight → App Store.
  - Codepush opcional vía Shorebird para hotfixes de Dart.
