# Nanchesoft Commercial Platform — Propuesta Maestra

> **Plataforma comercial táctil, industrial, multiempresa, optimizada para México.**
> Combina la **UX de Shopify**, el **ERP de Odoo**, los **dashboards de NetSuite** y el **CRM móvil de Salesforce** — sin copiar visualmente a ninguno.

## Índice de documentos

| # | Documento | Propósito |
|---|-----------|-----------|
| 01 | [Análisis de plataformas líderes](01_ANALISIS_BENCHMARK.md) | Qué tomar de Shopify / Odoo / NetSuite / Salesforce Mobile |
| 02 | [Visión de diseño y sistema visual](02_VISION_DISENO.md) | Principios UX, design system, tipografía, color, motion |
| 03 | [Arquitectura técnica](03_ARQUITECTURA_TECNICA.md) | Stack, capas, módulos, multiempresa, JWT, SignalR |
| 04 | [Modelo de datos comercial](04_MODELO_DATOS.md) | Nuevas entidades: Lead, Opportunity, CartSession, B2BAccount, etc. |
| 05 | [API REST y contratos](05_API_REST.md) | Endpoints, DTOs, versionado, paginación, OData parcial |
| 06 | [App móvil para vendedores (Flutter)](06_APP_VENDEDORES.md) | Pantallas, flujos, modo offline, ruteo, captura táctil |
| 07 | [Ecommerce B2B y portal de clientes (Blazor)](07_ECOMMERCE_B2B_PORTAL.md) | Catálogo, carrito, checkout, listas de precios, autoservicio |
| 08 | [CRM móvil](08_CRM_MOVIL.md) | Pipeline, leads, actividades, agenda, voz a texto |
| 09 | [Pedidos y seguimiento](09_PEDIDOS_TRACKING.md) | Ciclo completo, estados, timeline, push, mapa |
| 10 | [Dashboards y KPIs](10_DASHBOARDS_KPIS.md) | Vistas ejecutivas tipo NetSuite, tiles, alertas |
| 11 | [Rendimiento, tiempo real y offline](11_RENDIMIENTO.md) | SignalR, caching, latencia, sync, optimización móvil |
| 12 | [Seguridad y multitenancy](12_SEGURIDAD.md) | JWT, roles, scopes por empresa, RLS en Postgres |
| 13 | [Roadmap por fases](13_ROADMAP.md) | 6 fases trimestrales con entregables medibles |

## Productos a construir

1. **App móvil vendedores** — Flutter (iOS + Android) — toma de pedidos, CRM, ruta.
2. **Ecommerce B2B web** — Blazor Server + MudBlazor — catálogo mayorista, carrito, checkout.
3. **Portal de clientes** — Blazor — estado de cuenta, pedidos, seguimiento, recompra.
4. **CRM móvil** — Flutter (compartido con app vendedores) — pipeline, leads, agenda.
5. **API comercial** — ASP.NET Core 10 — base unificada para todos los frontends.
6. **Dashboards comerciales** — Blazor — ejecutivos, supervisores, vendedores.

## Premisas estratégicas

- **México primero:** CFDI 4.0, RFC, NOM-151, PUE/PPD, formas de pago SAT, monedas, IEPS.
- **Multiempresa:** un solo deploy, datos aislados por `companyId` y RLS.
- **Industrial:** lecturas con escáner, lotes/tallas, listas de precios por canal, vendedores en ruta.
- **Táctil:** targets ≥ 48 px, gestos, sin hovers críticos, modo una mano.
- **Rápido:** P95 < 300 ms en API, < 1 s primer pintado móvil, offline-first en captura.
- **Elegante:** sistema de diseño coherente, motion sutil, jerarquía clara.

## Métricas de éxito (Norte estrella)

| KPI | Meta a 12 meses |
|-----|-----------------|
| Tiempo medio para capturar un pedido | < 90 s (vs. 5+ min en sistemas legacy) |
| Pedidos B2B autoservicio | ≥ 35 % del volumen mayorista |
| NPS de vendedores | ≥ 60 |
| NPS de clientes B2B | ≥ 50 |
| Latencia P95 de API | < 300 ms |
| Disponibilidad mensual | ≥ 99.9 % |
| Tasa de adopción móvil | ≥ 90 % de la fuerza de ventas activa |
