# 08 · CRM móvil

> El CRM **no** es una pantalla separada en la app: es una capa que vive sobre clientes, leads, oportunidades y actividades, accesible desde toda la app.

## Conceptos centrales

- **Lead** — interesado aún no convertido en cliente.
- **Customer** — cliente con `Customer` existente.
- **Opportunity** — venta potencial con monto, fase, probabilidad.
- **Activity** — visita, llamada, reunión, tarea, nota.
- **Pipeline** — secuencia configurable de fases.
- **Feed** — historial cronológico unificado por cliente/oportunidad.

## Flujo "Día del vendedor"

```
06:30  Notificación push: "Tu ruta de hoy está lista (8 visitas)"
07:45  Abre app → Inicio: KPIs + lista del día
07:50  Toca "Iniciar ruta"  → GPS arranca, primera parada visible
08:20  Llega → app detecta geofence → modal "¿Hacer check-in en El Sol?"
08:21  Toca "Sí" → check-in con foto, ubicación
08:25  Mientras platica con el cliente, crea pedido directo
08:55  Termina → "Check-out" → outcome "Pedido cerrado, interés alto"
       app sugiere siguiente parada y muestra mapa
09:30  Llamada perdida marcada como actividad pendiente
10:00  Llega a Lead nuevo → la app abre ficha + cuestionario calificación
       Notas dictadas con voz → transcripción + tags automáticos
12:30  Comida → modo pausa (no cuenta tiempo de ruta)
17:00  Cierre del día → resumen automático: visitas, pedidos, km, próximos pasos
```

## Pipeline kanban

```
┌─────────┬─────────┬──────────┬───────────┬─────────┬─────────┐
│ Prospec │ Calific │ Propuesta│ Negociac. │ Ganada  │ Perdida │
├─────────┼─────────┼──────────┼───────────┼─────────┼─────────┤
│ ▓▓▓▓▓▓  │ ▓▓▓▓    │ ▓▓▓      │ ▓         │ ▓▓      │ ▓       │
│ El Sol  │ Maravilla │ Don Pepe │ Súper Mty │ Boticas │ Cano    │
│ $45,000 │ $80,000  │ $32,000  │ $120,000  │ $24,000 │ —       │
│ 20%     │ 35%      │ 55%      │ 75%       │ 100%    │ 0%      │
│ Visita  │ Cotiza   │ Esperan  │ Firma     │ Cerrada │ Precio  │
│         │ enviada  │ OK Gte   │ contrato  │         │ alto    │
│ ...     │ ...      │ ...      │ ...       │ ...     │ ...     │
└─────────┴─────────┴──────────┴───────────┴─────────┴─────────┘
```

- Scroll horizontal por etapas.
- Drag entre etapas para cambiar fase (con razón requerida al pasar a "perdida").
- Pinch para vista "mini" (sólo conteos).
- Tap larga → menú: asignar, agregar nota, ver historial, eliminar.

## Activity feed por cliente

Cada interacción se registra automáticamente:

```
HOY
  09:42  ✚ Visita registrada – check-in en El Sol (Calle X)
         Outcome: interés alto. Nota: "Cliente quiere precios mayoreo casco SP-1"
         + Foto + ubicación + duración 35 min  · por Carlos Ruiz

AYER
  17:15  ⊕ Pedido #4521 – $11,920 – Confirmado
  14:30  ☎ Llamada – 2:12 min – sin respuesta
  09:00  📧 Email automatizado: "Recordatorio de pago Factura A-983"

LUN 18 MAY
  ...
```

Filtros: tipo, vendedor, rango de fecha.

## Lead scoring

Reglas declarativas (configurables por empresa):

| Atributo / evento | Puntos |
|--------------------|--------|
| RFC válido | +20 |
| Email corporativo (no gmail/hotmail) | +10 |
| Estimado de venta > 100k | +25 |
| Visitó catálogo > 3 veces (B2B) | +15 |
| Respondió email | +10 |
| Sin actividad por 14 días | -15 |

Score se recalcula en background; el lead muestra barra de 0-100 con color.

## Calificación de leads — formulario rápido

Al primer contacto, el vendedor puede llenar checklist:
- [ ] ¿Es decisor?
- [ ] ¿Tiene presupuesto?
- [ ] ¿Tiene urgencia?
- [ ] ¿Encaja con nuestra oferta?
- Próximos pasos: [campo libre con voz].
- Re-contacto: [fecha].

Cierre del lead:
- Convertir a Customer + Opportunity en un solo tap.
- Marcar como "no interesado" con motivo.

## Notificaciones inteligentes

- **Push** cuando una oportunidad lleva 7 días sin movimiento.
- **Push** cuando cae un lead asignado al vendedor.
- **Push** al cliente cuando su pedido cambia de estado (configurable).
- **Email + push** al supervisor cuando una oportunidad de monto > X entra a "Negociación".
- **WhatsApp** opcional (Twilio) para recordatorios al cliente.

## Cuotas y reportes

- Cuota mensual por vendedor (monto + unidades + clientes nuevos).
- Tile en inicio con avance vs. meta y proyección.
- Reporte semanal automático por email al supervisor.

## Modo supervisor

- Filtros adicionales: ver todos los vendedores del equipo.
- Mapa de calor con visitas vs. cuotas.
- Reasignar leads y oportunidades.
- Aprobar descuentos solicitados por vendedor (workflow).

## Integraciones desde CRM

- **Click-to-call** con tracking de duración.
- **WhatsApp business** para enviar cotización con link.
- **Calendarios externos** (Google, Outlook) por iCalendar.
- **Email marketing** simple: campañas a leads con tag.
- **Lecturas de tarjeta de presentación** con OCR (`text_recognition`).

## Reglas declarativas de workflow

```yaml
# /docs/commercial/crm_rules.example.yaml
when:
  entity: opportunity
  event: stage_changed
  to: negotiation
do:
  - notify: { who: supervisor_of_owner, channel: [push, email] }
  - task: { title: "Revisar precios con descuento", owner: owner, dueIn: 1d }
```

Almacenadas como `WorkflowRule` en BD; ejecutadas por un `WorkflowEngine` (MediatR + Quartz).

## Privacidad y trazabilidad

- Toda actividad sensible (visita con ubicación) firmada con `userId`, `timestamp`, `geo` y hash inmutable.
- El vendedor puede ver lo que se reporta sobre él en su "Mi perfil → Auditoría".
- Política clara: ubicación solo durante horario laboral (config por empresa).
