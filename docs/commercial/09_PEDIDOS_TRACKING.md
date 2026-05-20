# 09 · Pedidos y seguimiento

## Ciclo de vida de un pedido

```
Borrador ─► Confirmado ─► Surtido (picking) ─► Embarcado ─► En ruta ─► Entregado
   │                                                                       │
   └──► Cancelado (con razón)                                               └──► Facturado
                                                                                   │
                                                                                   └──► Pagado
```

- Cada transición:
  - Validaciones (crédito, stock, datos CFDI).
  - Permisos por rol.
  - Notificaciones automáticas (push al cliente, push al vendedor, push al almacén).
  - Registro en `OrderStateLog`.
  - Evento SignalR `order.state.changed`.

## Estados detallados

| Estado | Quien lo provoca | Validaciones | Notifica |
|--------|------------------|-------------|----------|
| `draft` | Vendedor / B2B user | Mínimo 1 línea con qty > 0 | — |
| `confirmed` | Vendedor / B2B user / Auto desde checkout | Crédito o forma de pago válida, datos CFDI, stock asignable | Cliente + supervisor + almacén |
| `picking` | Almacén | Surtido iniciado | Cliente (opcional) |
| `picked` | Almacén | Todas las líneas surtidas o faltante explícito | Cliente + logística |
| `shipped` | Logística | Guía generada | Cliente + vendedor |
| `inTransit` | Carrier/manual | Ubicación periódica | Cliente |
| `delivered` | Repartidor / cliente | Firma o foto | Cliente + cobranza |
| `cancelled` | Cualquiera con permiso | Razón obligatoria | Todos los involucrados |

## Tracking en vivo

- **Repartidor:** abre la app, marca pedidos a entregar, GPS actualiza posición cada 30 s.
- **Cliente:** ve mapa con punto en movimiento + ETA.
- **Vendedor / supervisor:** ven todos sus pedidos en mapa, colores por estado.
- Implementación:
  - Hub `DeliveryTrackingHub` con grupos por `orderId` y por `routeId`.
  - Posiciones guardadas en Redis stream (TTL 24 h) y compactadas a `DeliveryStop` al final del día.

## Backorders y partials

- Si stock insuficiente al confirmar:
  - Opción A: dividir pedido en dos (lo que sí + backorder).
  - Opción B: mantener pedido entero con `expectedFulfillmentDate`.
- Pedidos parciales: `SalesShipment` por envío, cada uno con tracking propio.
- Vista del pedido muestra "X de Y unidades enviadas".

## Cancelaciones

- Cliente solicita cancelación → no se cancela automáticamente.
- Genera `CancellationRequest` que el vendedor o supervisor aprueba.
- Si ya embarcado → workflow especial (devolución / reembolso).

## Notas y comentarios (chatter)

- Cada pedido tiene `OrderNote(orderId, userId, text, attachment?, visibility: internal|customer)`.
- Comentarios "internos" solo los ven empleados; "customer-facing" se muestran en el portal.
- Menciones `@persona` notifican al usuario.

## Comprobantes de entrega

- Foto desde repartidor o cliente.
- Firma capacitiva.
- QR para que cliente confirme con su app.
- Almacenado en MinIO con URL firmada.
- Visible permanentemente en detalle del pedido.

## Devoluciones

- `SalesReturn` (ya existe en domain) se conecta a pedido origen.
- Razones tipificadas: defecto, equivocación, cliente cambió de opinión, mercancía dañada.
- Genera nota de crédito si aplica.

## Reorder / recompra

- Cualquier pedido finalizado tiene botón "Recomprar".
- Genera `CartSession` con las mismas líneas (ajustadas a precios y stock actuales).
- Si productos descontinuados → propone sustitutos.

## Plantillas recurrentes

- Pedido marcado como plantilla puede generar carritos automáticamente:
  - Frecuencia: semanal / quincenal / mensual.
  - Notificación 2 días antes con "Revisa tu pedido recurrente".
  - Si el cliente no actúa, el sistema crea borrador (no confirma).

## Integraciones de paquetería

- DHL, FedEx, Estafeta, 99 Minutos, Treggo (modular).
- `IShippingProvider` con `CreateGuide`, `GetStatus`, `Cancel`.
- Webhooks de carriers se reciben en `/webhooks/shipping/{carrier}` y actualizan estado.

## SLA y alertas

- Pedido confirmado sin surtir en 24h → alerta a almacén.
- Pedido surtido sin embarcar en 48h → alerta a logística.
- Pedido entregado sin pagar (PUE) → alerta a cobranza después de N días.
- Configurable por empresa.

## Métricas clave

- Lead time (confirmado → entregado).
- Cycle time por estado.
- Tasa de cancelación.
- On-time delivery rate.
- Backorder rate.
- Ticket promedio.
- Pedidos por canal (B2B, móvil, POS).

Cada métrica se expone como tile en el dashboard de Operaciones.
