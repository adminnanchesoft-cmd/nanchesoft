# 05 · API REST y contratos

> Base: `/api/v1/`  ·  Auth: `Bearer JWT`  ·  Tenant: claim `company_id` (override opcional `X-Company-Id`).

## Catálogo de endpoints

### Autenticación

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/auth/login` | Email + password → access + refresh |
| POST | `/auth/refresh` | Rotación de refresh token |
| POST | `/auth/logout` | Revoca refresh token |
| POST | `/auth/mfa/challenge` | TOTP / SMS |
| POST | `/auth/password-reset/request` | — |
| POST | `/auth/password-reset/confirm` | — |
| GET | `/auth/me` | Perfil + empresas accesibles + permisos efectivos |
| POST | `/auth/switch-company` | Cambia `company_id` activo y devuelve nuevo JWT |

### Multiempresa / setup

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/companies/accessible` | Empresas a las que el usuario tiene acceso |
| GET | `/companies/{id}/branding` | Branding del portal B2B |

### Sync móvil

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/sync/changes?since={iso8601}` | Cambios desde la última sync (incremental) |
| POST | `/sync/push` | Push de operaciones pendientes (batch idempotente) |
| GET | `/sync/manifest` | Catálogos esenciales para arranque offline (productos, clientes, precios) |
| GET | `/sync/conflicts` | Conflictos no resueltos del usuario |
| POST | `/sync/conflicts/{id}/resolve` | Resolución manual de conflicto |

### CRM — Leads

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/crm/leads` | Listar con filtros (`status`, `owner`, `tag`, `q`, `cursor`) |
| POST | `/crm/leads` | Crear |
| GET | `/crm/leads/{id}` | Detalle |
| PATCH | `/crm/leads/{id}` | Actualizar parcial |
| POST | `/crm/leads/{id}/convert` | Convierte a Customer + Opportunity |
| POST | `/crm/leads/{id}/score/recompute` | Recalcula score |
| POST | `/crm/leads/import` | CSV / Excel |

### CRM — Opportunities

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/crm/opportunities` | Listar |
| GET | `/crm/opportunities/pipeline?pipelineId=` | Kanban data |
| POST | `/crm/opportunities` | Crear |
| PATCH | `/crm/opportunities/{id}` | Editar |
| POST | `/crm/opportunities/{id}/stage` | Cambiar etapa con razón |
| POST | `/crm/opportunities/{id}/won` | Cerrar como ganada (+ generar SO opcional) |
| POST | `/crm/opportunities/{id}/lost` | Cerrar como perdida con motivo |

### CRM — Activities

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/crm/activities/my-day?date=` | Agenda del día del usuario |
| GET | `/crm/activities` | Listar (filtros: relación, estatus, fecha, owner) |
| POST | `/crm/activities` | Crear (visita, llamada, etc.) |
| POST | `/crm/activities/{id}/check-in` | Geolocalizar entrada |
| POST | `/crm/activities/{id}/check-out` | Salida + outcome + nota |
| POST | `/crm/activities/{id}/complete` | Marcar como hecha |

### Catálogo / productos

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/catalog/products?q=&category=&priceListId=&cursor=` | Lista con búsqueda full-text |
| GET | `/catalog/products/{id}` | Detalle + variantes + assets |
| GET | `/catalog/products/{id}/availability?warehouseId=` | Stock por almacén |
| GET | `/catalog/categories` | Árbol |
| GET | `/catalog/featured?audience=b2b` | Destacados por canal |
| GET | `/catalog/recently-viewed` | Por usuario |

### Listas de precios

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/pricing/quote?productId=&qty=&customerId=` | Cotización rápida con reglas aplicadas |
| GET | `/pricing/price-lists` | Listar |
| GET | `/pricing/price-lists/{id}/details` | Reglas |

### Carrito B2B

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/commerce/cart` | Carrito activo del usuario actual |
| POST | `/commerce/cart/lines` | Agregar producto |
| PATCH | `/commerce/cart/lines/{lineId}` | Cambiar cantidad / comentario |
| DELETE | `/commerce/cart/lines/{lineId}` | Quitar |
| POST | `/commerce/cart/clear` | Vaciar |
| POST | `/commerce/cart/apply-coupon` | Cupón |
| POST | `/commerce/cart/save-as-template` | Plantilla recurrente |
| GET | `/commerce/cart/templates` | Listar plantillas |
| POST | `/commerce/cart/from-template/{id}` | Recompra |

### Checkout

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/commerce/checkout/start` | Inicia y devuelve resumen (totales, validaciones, alerts) |
| POST | `/commerce/checkout/confirm` | Confirma → crea `SalesOrder` |
| GET | `/commerce/checkout/{salesOrderId}/status` | Polling alterno a SignalR |

### Pedidos (B2B / portal)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/orders` | Listar (filtros por estado, fecha, monto) |
| GET | `/orders/{id}` | Detalle + líneas + estado + tracking |
| GET | `/orders/{id}/timeline` | Eventos cronológicos |
| GET | `/orders/{id}/tracking` | Tracking en vivo |
| POST | `/orders/{id}/cancel-request` | Solicitar cancelación |
| POST | `/orders/{id}/reorder` | Recompra (genera carrito) |
| GET | `/orders/{id}/invoice` | Descarga PDF / XML |

### Pedidos (vendedor móvil)

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/sales/orders/quick` | Crear pedido rápido (vendedor en ruta) |
| POST | `/sales/orders/{id}/confirm` | Confirmar (workflow) |
| POST | `/sales/orders/{id}/lines` | Agregar línea inline |
| POST | `/sales/orders/{id}/payment` | Registrar pago (efectivo, transferencia) |
| POST | `/sales/orders/{id}/photo-proof` | Adjuntar foto evidencia |

### Cliente / portal — estado de cuenta

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/portal/me/account` | Resumen: saldo, crédito, próximos vencimientos |
| GET | `/portal/me/invoices?status=` | Facturas |
| GET | `/portal/me/payments` | Historial de pagos |
| POST | `/portal/me/payments/intent` | Iniciar pago (Stripe/SPEI/Mercado Pago) |
| GET | `/portal/me/documents` | Documentos publicados (catálogos, contratos) |
| GET | `/portal/me/notifications` | Notificaciones in-app |

### Dashboards

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/dashboards/sales/overview?period=` | KPIs ventas |
| GET | `/dashboards/seller/{userId}` | Personal del vendedor |
| GET | `/dashboards/customer/{customerId}` | Para CRM |
| GET | `/dashboards/exec` | Ejecutivo (director) |
| GET | `/dashboards/warehouse` | Almacén |

### Búsqueda global

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/search?q=` | Resultados unificados (productos, clientes, pedidos, leads) |
| GET | `/search/suggest?q=` | Autocompletado |

---

## DTOs clave (ejemplos)

### `CartLineDto`

```json
{
  "id": "f5...",
  "productId": 123,
  "variantId": 456,
  "productName": "Bota Industrial NS-1200",
  "sku": "NS1200-26",
  "thumbnailUrl": "https://cdn.../...",
  "qty": 12,
  "unitPriceBase": 850.00,
  "unitPriceApplied": 765.00,
  "discountPct": 10,
  "lineTotal": 9180.00,
  "tax": 1468.80,
  "appliedRules": [
    { "id": 22, "label": "Lista mayoreo – Zapaterías El Sol", "type": "priceList" }
  ],
  "availability": { "onHand": 240, "available": 198, "warehouseId": 1 },
  "comment": "Entrega antes del viernes",
  "requestedDeliveryDate": "2026-05-23"
}
```

### `OrderTimelineEvent`

```json
{ "ts": "2026-05-20T14:32:11Z",
  "type": "state.changed",
  "fromState": "confirmed",
  "toState": "picked",
  "actor": { "id": 88, "name": "Almacén — Roberto Méndez" },
  "note": "Surtido completo, esperando ruta" }
```

### `DashboardKpi`

```json
{
  "label": "Ventas del día",
  "value": 182540,
  "currency": "MXN",
  "period": "today",
  "trend": { "delta": 12.4, "direction": "up" },
  "sparkline": [120, 130, 115, 150, 170, 162, 182],
  "drilldown": "/orders?date=today"
}
```

### `SyncDeltaResponse`

```json
{
  "serverTime": "2026-05-20T18:00:00Z",
  "since": "2026-05-20T12:00:00Z",
  "entities": {
    "customers": { "upserts": [...], "tombstones": [...] },
    "products":  { "upserts": [...], "tombstones": [...] },
    "priceLists":{ "upserts": [...], "tombstones": [...] },
    "activities":{ "upserts": [...], "tombstones": [...] }
  },
  "stats": { "totalUpserts": 542, "totalTombstones": 9 }
}
```

## Convenciones de errores

```json
{
  "error": {
    "code": "credit_limit_exceeded",
    "message": "El cliente excede su crédito disponible.",
    "details": { "creditAvailable": 12000, "orderTotal": 18500 },
    "traceId": "01H..."
  }
}
```

Códigos de error consistentes en `snake_case`. Mapeo a HTTP:
- `400` validation_failed, business_rule_failed
- `401` auth_required, token_expired
- `403` insufficient_permissions, tenant_mismatch
- `404` not_found
- `409` conflict, version_mismatch
- `422` business_rule_failed
- `429` rate_limited
- `5xx` server_error

## Versionado y deprecación

- Cada endpoint deprecado regresa header `Deprecation: true` y `Sunset: <fecha>`.
- Soporte mínimo 6 meses tras anunciar deprecación.
- `/api/v2/` correrá en paralelo con `/api/v1/` mientras hay clientes móviles antiguos.

## Documentación

- **OpenAPI 3.1** autogenerado, accesible en `/api/docs`.
- **Postman collection** publicada en `docs/api/postman/`.
- **Tipos TypeScript / Dart** generados automáticamente con `openapi-generator` en CI.
