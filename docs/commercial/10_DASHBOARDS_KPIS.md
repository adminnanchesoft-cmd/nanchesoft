# 10 · Dashboards y KPIs

> Inspiración: NetSuite (drill-down + roles) + Shopify (densidad balanceada) + diseño moderno (sin estética 2008).

## Dashboards por rol

### A. Director / dueño
Foco: rentabilidad, tendencia, cobranza, riesgo.

| Tile | Periodo | Drilldown |
|------|---------|-----------|
| Ventas MTD vs. mes anterior | MTD | `/orders?period=mtd` |
| Margen bruto MTD | MTD | reporte margen |
| Cartera vencida | Hoy | `/portal/admin/aging` |
| Pedidos pendientes | Hoy | `/orders?state=confirmed,picked` |
| Cuotas equipo de ventas | MTD | matriz vendedores |
| Top 10 clientes | MTD | lista clientes |
| Productos sin venta 60d | Histórico | inventario muerto |
| Flujo proyectado 30d | 30d | proyección |

### B. Gerente de ventas
Foco: equipo, pipeline, conversión, agenda.

| Tile | Notas |
|------|-------|
| Pipeline (monto por etapa) | Funnel visual |
| Vendedores por cumplimiento | Lista con %, semaforizado |
| Visitas hoy vs. plan | KPI grande |
| Leads sin asignar | Cola |
| Conversion rate | Lead → Cliente |
| Mapa territorio | Heatmap visitas |

### C. Vendedor
Foco: su día, sus números, sus alertas.

| Tile | |
|------|--|
| Cuota mes (gauge) | |
| Ventas hoy | |
| Visitas hoy: completadas / planeadas | |
| Clientes nuevos del mes | |
| Pedidos pendientes de surtir | |
| Top 5 clientes que no compran hace 30d | |
| Saldo vencido de mi cartera | |

### D. Almacén
Foco: surtido, embarques, inventario.

| Tile | |
|------|--|
| Pedidos por surtir (cola priorizada) | |
| Embarques de hoy | |
| Productos bajo punto de reorden | |
| Faltantes en picking | |
| SLA promedio de surtido | |
| Movimientos del día | |

### E. Cliente B2B (en portal)
| Tile | |
|------|--|
| Saldo total / vencido | |
| Crédito disponible (barra) | |
| Próximos vencimientos (lista) | |
| Pedidos en curso | |
| Recompra rápida | |
| Promociones aplicables | |

### F. Cobranza / Tesorería
| Tile | |
|------|--|
| Cobrado hoy / semana / mes | |
| Aging (1-30 / 31-60 / 61-90 / 90+) | |
| Días promedio de cobro | |
| Cheques recibidos | |
| Conciliación pendiente | |
| Top deudores | |

## Componente `<NsKpiTile>`

Cubre 5 variantes:
1. **Número** (valor + trend + sparkline).
2. **Gauge** (progreso vs. meta).
3. **Funnel** (etapas con conteo y conversión).
4. **Lista** (top N con avatar/icono + cifra).
5. **Mapa** (calor / pines).

API Razor (esquema):

```razor
<NsKpiTile Label="Ventas hoy"
           Value="@todayRevenue"
           Currency="MXN"
           Period="@KpiPeriod.Today"
           Comparison="@yesterdayRevenue"
           Trend="@trend"
           Sparkline="@last7days"
           DrillTo="/orders?date=today" />
```

API Flutter:

```dart
NsKpiTile(
  label: 'Ventas hoy',
  value: 182540,
  currency: 'MXN',
  period: KpiPeriod.today,
  trend: Trend(delta: 12.4, direction: TrendDirection.up),
  sparkline: [120, 130, 115, 150, 170, 162, 182],
  onTap: () => context.push('/orders?date=today'),
)
```

## Layout configurable

- Grid 12 columnas en escritorio, 4 en tablet, 1 en móvil.
- Drag-and-drop para reordenar (Blazor: `MudDropZone`; Flutter: `reorderables`).
- Cada tile puede colapsarse a "mini" (sólo cifra).
- "Modo TV" toggle: full screen, fuente 1.6×, auto-refresh 60 s, sin tooltips.

## Persistencia de layout

```
SavedDashboardLayout {
   Id, CompanyId, UserId, DashboardKey,
   LayoutJson:
     [{ tileId: "sales.today", x:0, y:0, w:3, h:2 }, ...],
   IsDefault, UpdatedAt
}
```

## Periodos estándar

`Today`, `Yesterday`, `WTD`, `LastWeek`, `MTD`, `LastMonth`, `QTD`, `LastQuarter`, `YTD`, `LastYear`, `Custom(from, to)`.

Cada query del dashboard recibe `period` y devuelve `value`, `previousValue`, `series` (puntos para sparkline).

## Implementación de cómputo

- Capa `DashboardService` con cache Redis por (companyId, role, period).
- TTL corto (60 s) para tiles de "Hoy"; más largo para periodos cerrados.
- Re-cálculo proactivo cuando se publican eventos: `OrderConfirmed`, `PaymentReceived`, etc.
- Para grandes volúmenes: vistas materializadas refrescadas asíncronamente.

## Alertas y reglas

- Reglas declarativas:
  ```yaml
  - metric: cartera_vencida
    threshold: '> 250000'
    severity: warning
    notify: ['gerente_ventas', 'cobranza']
  ```
- Las alertas se visualizan como banner sobre el dashboard y se envían por push/email.

## Reportes guardados

- Cualquier filtro de lista puede guardarse como "Vista".
- Vistas compartidas a nivel rol o usuario.
- Reportes programados: "Cada lunes 8 am → enviarme PDF de pipeline".

## Densidad y motion

- Tiles entran con stagger 40 ms entre filas.
- Sparkline anima desde 0 a su forma final en 600 ms (easeOutCubic).
- Cambios de número con tween (efecto "odómetro") para diferencias < 50%.

## Accesibilidad de dashboards

- Cada tile tiene `aria-label` descriptivo: "Ventas del día: 182,540 MXN, sube 12.4% vs ayer".
- Modo "Solo cifras" sin gráficas, para lectores de pantalla.
- Atajos: `g d` → ir a dashboard; `t v` → modo TV.
