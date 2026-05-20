# 01 · Análisis profundo de plataformas líderes

> Objetivo: extraer **principios** y **patrones**, no copiar pantallas. Cada sección termina con un cuadro de **decisiones para Nanchesoft**.

---

## 1. Shopify — UX comercial

### Qué hace excepcionalmente bien

- **Tiempo cero a la primera acción.** El admin abre directo en un dashboard con "ventas de hoy" + "pedidos por procesar" arriba; sin árboles de menús.
- **Tarjetas con jerarquía emocional.** Números grandes, comparativo vs. ayer, micro-tendencia como chip de color (`+12.4 %`).
- **Búsqueda como columna vertebral.** `Ctrl+K` global → productos, pedidos, clientes, ajustes. La búsqueda es navegación.
- **Acciones primarias siempre flotantes en móvil.** "Crear pedido" como FAB persistente.
- **Empty states con CTA.** Nunca una lista vacía sin acción sugerida.
- **Skeleton loading** en lugar de spinners — la pantalla "ya existe" antes de los datos.
- **Listas con multi-selección + barra de acciones contextual** que aparece debajo del header.
- **Mobile parity:** todo lo que se hace en desktop se puede hacer en el celular; el admin móvil es de primera clase.
- **Flujo de pedido manual** — un único viewport: cliente, productos buscables inline, total dinámico, descuentos chip, pago, envío. Sin tabs.
- **Pagos como objetos editables después del hecho.** Mark as paid, refund, partial — todo desde la línea del pedido.
- **Notificaciones contextuales** (toast inferior con acción "Deshacer" 6 s).

### Qué evitar

- Demasiada densidad en escritorio para usuarios industriales — el balance debe ser distinto.
- Tipografía muy ligera; en bodegas se necesita contraste mayor.

### Decisiones para Nanchesoft

| Práctica | Cómo aplicamos |
|----------|----------------|
| Búsqueda global `Ctrl+K` / "Buscar todo" en móvil | Implementar `GlobalSearchService` con índice unificado (productos, clientes, pedidos, vendedores) |
| Acciones flotantes (FAB) | Mostrar FAB **solo en pantallas de lista táctil**, nunca en formularios |
| Skeleton loading | `<NsSkeleton>` reusable en Blazor + `Shimmer` en Flutter |
| Empty states con CTA | Toda lista vacía debe ofrecer 1-2 acciones |
| Toast con "Deshacer" | `NsSnackbar` con callback, default 6 s |
| Pedido en una sola vista | Editor de pedido scroll-friendly, sin tabs |

---

## 2. Odoo — ERP modular

### Qué hace excepcionalmente bien

- **Vista kanban configurable** para casi todo — pedidos, leads, tareas. Drag-and-drop entre columnas de estado.
- **Modo de vista intercambiable:** lista ↔ kanban ↔ pivot ↔ gráfica ↔ calendario, sin cambiar de URL.
- **Filtros guardados y compartibles.** Los chips de filtro son ciudadanos de primera clase.
- **Mensajería en cada documento (chatter).** Cada pedido, lead, factura tiene timeline de eventos, comentarios @menciones, archivos.
- **Modelo de datos uniforme.** Toda entidad tiene `display_name`, `state`, `tags`, `responsible_user`, `company_id` — el ERP es predecible.
- **Listas precios con reglas** (por cliente, categoría, cantidad, fecha).
- **Workflow declarativo:** botones de transición arriba ("Confirmar", "Validar", "Cancelar") con permisos por rol.
- **Modo developer** que muestra técnicos campos — útil para tener "ver IDs / técnicos" oculto pero accesible.

### Qué evitar

- UI cargada visualmente; en móvil se siente lento.
- Mezcla inconsistente de fuentes y estilos entre módulos viejos y nuevos.

### Decisiones para Nanchesoft

| Práctica | Cómo aplicamos |
|----------|----------------|
| Multi-vista (lista/kanban/calendario/mapa) | `NsViewSwitcher` con persistencia por usuario |
| Chatter por documento | `DocumentTimeline` (auditoría + comentarios + adjuntos + estados) |
| Workflow declarativo | Atributo `[Workflow("draft→confirmed→shipped")]` en entidad + barra de acciones primarias arriba |
| Pricelists complejas | Reuso de `ItemPriceList` actual + reglas por cliente/categoría/volumen/fecha |
| Filtros guardados | Tabla `UserSavedFilter` por entidad + chips persistentes |
| Tags multi-uso | Tabla `Tag` global con many-to-many polimórfico |

---

## 3. NetSuite — dashboards ejecutivos

### Qué hace excepcionalmente bien

- **Dashboards por rol** preconfigurados (CFO, COO, Sales VP, Account Manager).
- **Reminders + KPIs + Trends + Reports** como bloques arrastrables.
- **Drill-down hasta la transacción.** Click en un KPI → lista filtrada → documento.
- **Periodicidad explícita.** Cada tile dice "Hoy", "MTD", "YTD", "vs. año anterior" con sparkline.
- **Saved searches** componibles — los usuarios construyen su dashboard.
- **Modo presentación** (board mode) para reuniones — sin distracciones.

### Qué evitar

- Diseño visual de 2008. Bordes, gradientes, iconos pixelados.
- Configuración lentísima (50 clicks para agregar un tile).

### Decisiones para Nanchesoft

| Práctica | Cómo aplicamos |
|----------|----------------|
| Dashboards por rol | 6 dashboards preconfigurados (Director, Gerente Ventas, Vendedor, Cliente B2B, Almacén, Cobranza) |
| Tiles con drill-down | Cada `KpiTile` lleva a una vista filtrada de su entidad |
| Periodicidad y sparkline | Componente `<NsKpi value period trend />` |
| Saved searches | Tabla `SavedDashboardLayout` por usuario |
| Modo presentación | Botón "Modo TV" — full screen + auto-refresh 60 s + tipografía gigante |

---

## 4. Salesforce Mobile — CRM móvil

### Qué hace excepcionalmente bien

- **Feed unificado** — actividades, tareas, llamadas en un timeline.
- **Quick actions persistentes** abajo (log a call, new task, new event, send email).
- **Modo offline con sync delta.** El vendedor trabaja en avión y al reconectar resuelve conflictos.
- **Einstein search** — "muéstrame mis oportunidades por cerrar este mes" en lenguaje natural.
- **Geolocalización para visitas.** Check-in con geofence, ruteo del día, optimización.
- **Voz a texto** para notas de visita.
- **Tarjetas verticales scrolleables** con la información más importante arriba (cliente, monto, fase, próximo paso).
- **Botón flotante de "lista de tareas del día"** colapsable.

### Qué evitar

- Profundidad excesiva de menús, complejidad de configuración admin.
- Iconografía corporativa pesada.

### Decisiones para Nanchesoft

| Práctica | Cómo aplicamos |
|----------|----------------|
| Feed unificado por cliente | `CustomerActivityFeed` (visitas, llamadas, pedidos, pagos, comentarios) |
| Quick actions en bottom nav móvil | 4 acciones: Nueva visita, Nuevo pedido, Llamar, Notas de voz |
| Offline-first | SQLite local + cola de operaciones + sync delta vía `/api/v1/sync/changes?since=...` |
| Búsqueda en lenguaje natural | Fase 2: integración con LLM para "ventas de junio del cliente X" |
| Check-in geolocalizado | Geofence opcional, captura lat/lng en cada visita |
| Voz a texto | Plugin Flutter `speech_to_text` para notas |
| Tarjetas verticales | `CustomerCard` con `nombre`, `saldo`, `último pedido`, `próxima visita` |
| FAB del día | "Mi día" — agenda + ruta optimizada + pedidos pendientes |

---

## Síntesis comparativa

| Dimensión | Shopify | Odoo | NetSuite | Salesforce | **Nanchesoft** |
|-----------|---------|------|----------|------------|----------------|
| Curva de aprendizaje | Baja | Media | Alta | Media | **Baja-media** |
| Densidad visual | Media | Alta | Muy alta | Media | **Media-baja, industrial** |
| Móvil first-class | ✅ | ⚠️ | ❌ | ✅ | **✅** |
| Multiempresa | Solo Plus | ✅ | ✅ | ✅ | **✅** |
| Offline robusto | ❌ | ❌ | ❌ | ✅ | **✅** |
| Localización México (CFDI) | Plugin | Plugin | Costoso | Plugin | **Nativo** |
| Touch-first | Parcial | No | No | Sí | **Sí** |
| Velocidad percibida | ⚡⚡⚡ | ⚡⚡ | ⚡ | ⚡⚡ | **⚡⚡⚡** |
| Sistema de diseño coherente | ✅ Polaris | ⚠️ | ❌ | ✅ Lightning | **✅ NS Design System** |

---

## La fórmula Nanchesoft

```
UX = Shopify.simplicidad × Odoo.modularidad × NetSuite.profundidad × Salesforce.movilidad
```

- Tomamos de **Shopify** la velocidad y el "tiempo cero a la primera acción".
- Tomamos de **Odoo** la modularidad, los workflows declarativos y el chatter por documento.
- Tomamos de **NetSuite** los dashboards drill-downables por rol.
- Tomamos de **Salesforce Mobile** la experiencia móvil offline-first y el feed unificado.

Y agregamos lo que ninguno tiene de forma nativa: **México**, **industrial mayorista**, **tallas/lotes/corridas**, **CFDI 4.0**, **multiempresa real**, **vendedores en ruta con pedidos en 90 segundos**.
