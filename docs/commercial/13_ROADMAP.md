# 13 · Roadmap por fases

> 6 fases trimestrales. Cada fase tiene entregables medibles, dueño, dependencias y métricas de salida.

## Resumen visual

```
T1 2026-06 ─► T2 2026-09 ─► T3 2026-12 ─► T4 2027-03 ─► T5 2027-06 ─► T6 2027-09
   Cimiento     App        Ecommerce      CRM móvil     Dashboards   Crecimiento
   API+Auth     pedidos    B2B + portal   pipeline +    ejecutivos   integraciones
   Diseño       mobile     checkout +     ruta + voz +  + analítica  + automatiza-
   sistema      MVP        crédito +      offline       avanzada     ciones
                           CFDI                                      + IA
```

---

## FASE 1 — Cimientos (T1, ~10 sem)

**Objetivo:** habilitar a vendedores con la app móvil más simple posible.

### Entregables
- ✅ NS Design System v1 (tokens + componentes base) en Blazor y Flutter.
- ✅ API `/auth/*` con JWT + refresh.
- ✅ Tenant resolver + RLS en Postgres.
- ✅ Endpoints `/sync/manifest` + `/sync/changes` + `/sync/push`.
- ✅ Flutter shell: login, navegación, modo offline básico.
- ✅ Pantallas: Inicio (KPIs cosméticos), Clientes (lista + detalle), Pedidos (lista + detalle existente).
- ✅ Conexión a entidades existentes (`Customer`, `SalesOrder`).

### Métricas de salida
- 5 vendedores piloto usando la app a diario.
- Cold start < 2 s.
- 0 fugas de datos entre empresas en pruebas RLS.
- Cobertura tests unitarios API ≥ 60 %.

---

## FASE 2 — Pedidos móviles MVP (T2, ~12 sem)

**Objetivo:** un pedido en 90 segundos, online y offline.

### Entregables
- ✅ Editor de pedido Flutter (una vista).
- ✅ Buscador productos + escaneo de código de barras.
- ✅ Pricing engine con listas y reglas básicas.
- ✅ Validación crédito + alertas.
- ✅ Modo offline funcional con cola y reintentos.
- ✅ Sync push con resolución básica de conflictos.
- ✅ Workflow `draft → confirmed → picking → shipped → delivered` en API.
- ✅ Notificaciones push (FCM/APNs).
- ✅ Hub SignalR `OrdersHub`.

### Métricas de salida
- Tiempo medio de captura de pedido ≤ 120 s.
- Tasa de éxito sync ≥ 99 %.
- 30 vendedores activos.

---

## FASE 3 — Ecommerce B2B + portal (T3, ~14 sem)

**Objetivo:** clientes mayoristas hacen pedidos por web sin llamar al vendedor.

### Entregables
- ✅ `B2BAccount`, `B2BUser`, `CartSession`, `CartLine` (entidades + migraciones).
- ✅ Blazor B2B: catálogo, detalle producto, carrito, checkout.
- ✅ Estado de cuenta (saldo, vencidos, facturas, pagos).
- ✅ CFDI 4.0 al confirmar pedido (datos obligatorios).
- ✅ Pasarela de pago Stripe + SPEI (referencia única).
- ✅ Portal de notificaciones in-app + email.
- ✅ Plantillas recurrentes / recompra.
- ✅ Documentos publicados (PDFs).

### Métricas de salida
- ≥ 15 % del volumen mayorista capturado en autoservicio.
- Lighthouse ≥ 90 mobile.
- 100 clientes B2B activos con login.

---

## FASE 4 — CRM móvil + ruta (T4, ~12 sem)

**Objetivo:** convertir al vendedor en un gestor de cuentas con CRM serio.

### Entregables
- ✅ Lead / Opportunity / Activity (entidades + API).
- ✅ Pipeline kanban en móvil y web.
- ✅ Feed unificado por cliente.
- ✅ Visitas con check-in geolocalizado.
- ✅ Ruta diaria optimizada (TSP heurístico).
- ✅ Voz a texto para notas.
- ✅ Escáner de tarjetas de presentación (OCR).
- ✅ Lead scoring declarativo.
- ✅ Reglas de workflow básicas (`WorkflowEngine`).
- ✅ Quartz jobs para alertas (oportunidades sin movimiento, leads viejos).

### Métricas de salida
- 100 % de visitas con check-in.
- Conversion rate lead → cliente medible y > 12 %.
- 50 % de leads con score > 60 calificados a customers.

---

## FASE 5 — Dashboards y analítica (T5, ~10 sem)

**Objetivo:** decisiones basadas en datos para todos los roles.

### Entregables
- ✅ 6 dashboards por rol con tiles drag-and-drop.
- ✅ `<NsKpiTile>` (5 variantes) en Blazor y Flutter.
- ✅ Vistas materializadas para KPIs pesados.
- ✅ Saved searches y dashboards personalizados.
- ✅ Modo TV (full screen, auto-refresh).
- ✅ Alertas por umbral.
- ✅ Reportes programados (email PDF).
- ✅ Export a Excel / PDF estándar.

### Métricas de salida
- TTI dashboard < 1.2 s.
- 80 % de usuarios entran al dashboard al menos 1 vez/día.

---

## FASE 6 — Integraciones, IA y crecimiento (T6, ~12 sem)

**Objetivo:** plataforma extensible, conectada, con asistencia inteligente.

### Entregables
- ✅ Webhooks salientes (catálogo de eventos).
- ✅ Integración paqueterías (DHL, FedEx, Estafeta, 99 Min).
- ✅ Integración WhatsApp Business (Twilio).
- ✅ Conectores contables (CONTPAQi, Aspel) — export/import.
- ✅ Asistente IA en CRM (consulta NL: "muéstrame mis oportunidades de junio").
- ✅ Sugerencias de cross-sell / up-sell.
- ✅ Resumen automático de visitas (LLM).
- ✅ App marketplace interno (extensiones por empresa).
- ✅ App pública API para clientes y partners.

### Métricas de salida
- 5 integraciones externas en producción.
- 30 % de pedidos B2B con cross-sell sugerido aceptado.

---

## Hitos transversales (en todas las fases)

- **Auditoría de accesibilidad** WCAG 2.2 AA cada fase.
- **Pentest** externo al final de cada fase con módulos sensibles.
- **Load test k6** mensual + fix.
- **Onboarding y soporte** documentado (videos cortos por flujo).
- **Telemetría de adopción** revisada semanalmente.

## Riesgos identificados

| Riesgo | Mitigación |
|--------|------------|
| Adopción móvil baja entre vendedores tradicionales | Piloto guiado + incentivo por uso + interfaz nativa-MX |
| CFDI 4.0 cambios SAT | Capa de adaptador PAC, contratos con 2 PACs (failover) |
| Conflictos de sync offline | UI de conflictos clara + reglas defaults conservadoras |
| Latencia en redes lentas (zonas rurales) | Sync compresión + delta + assets locales |
| Multitenancy fuga de datos | RLS + filtro EF + tests específicos + alertas |
| Sobrecarga de configuración | Defaults razonables + asistente onboarding |

## Métricas norte estrella (revisión trimestral)

| KPI | Inicio | Meta T6 |
|-----|--------|---------|
| Tiempo medio captura pedido | ~5 min | < 90 s |
| % pedidos B2B autoservicio | 0 % | ≥ 35 % |
| NPS vendedores | — | ≥ 60 |
| NPS clientes B2B | — | ≥ 50 |
| Latencia P95 API | — | < 300 ms |
| Disponibilidad | — | ≥ 99.9 % |
| Adopción móvil | — | ≥ 90 % |
| Conversion rate CRM | — | ≥ 12 % |
