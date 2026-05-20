# 11 · Rendimiento, tiempo real y offline

## Presupuesto de performance

| Métrica | Meta | Crítico |
|--------|------|---------|
| API P50 | < 90 ms | < 200 ms |
| API P95 | < 300 ms | < 600 ms |
| API P99 | < 700 ms | < 1.2 s |
| Blazor TTI mobile 4G | < 1.5 s | < 3 s |
| Flutter cold start (gama media) | < 1.5 s | < 2.5 s |
| Búsqueda de productos (debounce 250 ms incluido) | < 300 ms | < 700 ms |
| Confirmar pedido (E2E) | < 800 ms | < 1.5 s |
| SignalR roundtrip | < 80 ms | < 200 ms |

## Tácticas back-end

### Base de datos
- Índices específicos (ver doc 04).
- Vistas materializadas para dashboards pesados (`mv_sales_today_by_seller`).
- `pg_stat_statements` activo + revisión semanal.
- Conexiones pooled con Npgsql, max pool 200; el manager por tenant resetea `app.company_id`.
- Query timeout 3 s default; 8 s en reportes.

### Cache
- **Redis** distribuido. Niveles:
  - L1: in-memory por proceso (catálogos casi inmutables).
  - L2: Redis (datos por tenant, TTL 30 s – 24 h según naturaleza).
- Patrón: `cache-aside` con `IDistributedCache` envuelto en `NsCache<T>(key, ttl, factory)`.
- Invalidación dirigida desde handlers de eventos (`OrderConfirmed` invalida `dashboard:sales:today:{company}`).

### Compresión y transporte
- **Brotli** + gzip en respuestas.
- **HTTP/2** end-to-end.
- Headers `Cache-Control` agresivos para assets versionados (`/_content/*`).

### Async + parallel
- Handlers de MediatR siempre `async`.
- Operaciones pesadas (export, sync masivo) en `Hangfire` o background channels.
- `Parallel.ForEachAsync` para procesar lotes con concurrencia controlada.

### N+1 y proyecciones
- EF Core con `AsNoTracking()` por default en queries de lectura.
- `Select` proyectando a DTO directo (no traer entidad completa).
- Inclusión explícita: `Include` solo cuando aplica.

## Tácticas front-end web (Blazor)

- `RenderMode.InteractiveServer` con WebSocket persistente.
- **Compresión SignalR** + retentions cortas.
- **Stream rendering** para páginas con consulta pesada (`@attribute [StreamRendering]`).
- Carga progresiva: skeleton primero, datos después.
- Virtualización (`<Virtualize>`).
- `OnAfterRenderAsync(firstRender)` para inicializar JS pesado.
- Service Worker para activos + offline shell del portal.

## Tácticas Flutter

- **AOT release** + `--split-debug-info`.
- **Lazy routes** con `go_router`.
- **Imágenes**:
  - `cached_network_image` con disk + memory cache.
  - Tamaños responsivos (`?w=320` parametrizado).
  - Placeholder y blur hash.
- **Listas**:
  - `ListView.builder` siempre.
  - `AutomaticKeepAliveClientMixin` solo donde realmente se necesita.
- **DB local**:
  - Drift con índices.
  - Streams reactivos en vez de polling.

## Tiempo real

### SignalR — Hubs

- **NotificationsHub** — notificaciones generales por usuario.
- **OrdersHub** — eventos de pedidos (estado, líneas, comentarios).
- **CrmHub** — eventos CRM (asignaciones, vencimientos).
- **DeliveryTrackingHub** — posiciones GPS de repartidores.
- **PresenceHub** — usuarios en línea, "viendo este pedido".
- **SupportHub** — chat de soporte.

### Patrón de grupos

```
tenant:{companyId}                       — toda la empresa
tenant:{companyId}:role:{role}           — todos los almacenistas
tenant:{companyId}:user:{userId}         — un usuario
tenant:{companyId}:order:{orderId}       — observadores de pedido
tenant:{companyId}:customer:{customerId} — interesados en cliente
tenant:{companyId}:route:{routeId}       — observadores de ruta
```

### Replay y entrega garantizada

- Cada evento crítico se almacena en Redis Stream por grupo con TTL 24 h.
- Cliente al reconectar envía `lastEventId`; servidor reenvía los faltantes.
- Eventos idempotentes: `eventId` UUID; cliente desduplica.

### Backpressure

- Eventos de baja prioridad (presencia, typing) descartables.
- Eventos críticos (estado de pedido) garantizados.
- Throttling por usuario (max 50 msg/s).

## Offline robusto (Flutter)

### Tablas locales (Drift)
- `customers`, `products`, `price_lists`, `activities`, `orders`, `cart_sessions`, `entity_change_log`, `pending_operations`.
- Versionadas con migraciones; corruption recovery con `rebuild`.

### Engine de sync

```
class SyncEngine {
  Future<void> bootstrap()        // primera vez: descarga catálogos completos
  Future<void> deltaPull()         // periódico: cambios desde last cursor
  Future<void> pushOperations()    // sube cola con idempotencia
  Stream<SyncState> state          // ui status
  Future<void> resolveConflict(c)  // manual
}
```

### Política de conflictos

- Pedido borrador con misma línea editada en dos dispositivos: merge por última edición de cada campo (`updatedAt`).
- Línea borrada en uno + editada en otro: ganador = más reciente; el otro queda en "Conflictos para revisar".
- Stock inválido al subir pedido: pedido queda en estado `serverRejected` con razón.

### UX offline

- Banner persistente: "Offline · 3 cambios pendientes de subir".
- Cada item recién creado offline tiene badge "⏳ Pendiente".
- "Reintentar ahora" disponible siempre.
- En zonas grises (señal intermitente): la app prefiere offline para evitar timeouts.

## Métricas de salud en tiempo real

- Dashboard `/admin/ops` para SRE:
  - Latencia API en vivo.
  - Sync queue length por dispositivo.
  - Conexiones SignalR activas.
  - Tasa de error 5xx.
  - Salud Postgres / Redis / MinIO.
- Alertas Prometheus → PagerDuty / Telegram.

## Test de carga

- Plan k6 mensual con escenarios:
  - 100 vendedores creando pedidos en paralelo.
  - 1000 clientes B2B navegando catálogo.
  - 500 conexiones SignalR sostenidas.
- Métricas auditadas y comparadas con la línea base.
