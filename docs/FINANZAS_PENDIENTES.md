# Finanzas — Seguimiento (rama `finanzas-fase1`)

Documento de seguimiento para el módulo financiero moderno de Nanchesoft ERP.
Refleja el alcance de la **Fase 1** (2026-05-19) y la **Fase 2** (2026-05-19,
mismo día) con todos los frentes ejecutados.

## -1. Avance fase 3 (completado el 2026-05-20)

### Central de Pagos multiempresa
- Entidades `PaymentBatch`, `PaymentBatchLine`, `PaymentBatchAudit` (esquema
  `finance`). Soportan multiempresa, multibanco y multicuenta sin cambiar de
  contexto.
- Endpoint `GET /api/payment-central/pending` consolida todas las facturas
  pendientes de pago a nivel tenant, calcula vencimiento real, prioridad y
  cantidad ya comprometida en lotes vivos.
- Endpoint `GET /api/payment-central/lookups` devuelve empresas, cuentas
  bancarias/caja con saldos, proveedores, monedas y tipos de pago con prefijos
  de folio (TR, CH, SPEI, DEP, EF, TJ, COMP, OTR).
- Endpoints CRUD `/api/payment-central/batches`:
  - `POST /` genera folio `LOTE-YYYY-NNNNN`, registra auditoría.
  - `POST /{id}/authorize` autoriza líneas (total o parcial con overrides).
  - `POST /{id}/reject` rechazo de lote entero con motivo.
  - `POST /{id}/cancel` cancela lote no ejecutado.
  - `POST /{id}/execute` aplica pago a la factura, genera `Payment`,
    `BankMovement`, `Check` (si tipo cheque) y descuenta saldo bancario / caja.
  - `GET /{id}/audit` bitácora completa.
- Endpoint `GET /api/payment-central/executive` con pendientes, autorizados,
  ejecutados 30d, flujo comprometido vs disponible, razón de riesgo, bancos
  con menor saldo y vencimientos en 14 días.

### UI Blazor
- `/finance/payment-central` — vista global tipo ERP premium con
  selección masiva, edición inline (cuenta, tipo, monto, prioridad),
  asignación rápida y generación del lote.
- `/finance/payment-batches` — listado de lotes con filtros por estatus.
- `/finance/payment-batches/{id}` — detalle del lote con autorización,
  rechazo, cancelación, ejecución, bitácora e impresión (window.print).
- `/finance/payment-executive` — pantalla ejecutiva con tarjetas, bancos
  con menor saldo y próximos vencimientos.

### Navegación
- En grupo **CxP** se agregaron: "Central de pagos", "Lotes de pre-pago" y
  "Pantalla ejecutiva pagos".

### Migración SQL fase 3
- Archivo `database/20260520_payment_central.sql` idempotente con tablas
  `finance.payment_batches`, `finance.payment_batch_lines`,
  `finance.payment_batch_audits` con sus índices y FKs.

---

## 0. Avance fase 2 (completado)

### Cheques y chequeras
- Nuevas entidades `Check`, `CheckBook` con esquema `finance`.
- Endpoints `POST/PUT/GET/DELETE /api/finance/check-books` para administrar
  chequeras (rango de folios, serie, próximo folio).
- Endpoints `POST/PUT/GET /api/finance/checks` para captura, edición y consulta.
- Acciones: `POST /api/finance/checks/{id}/issue` (afecta saldo y genera
  movimiento bancario), `/cash` (marca como cobrado), `/cancel` (reversa el
  movimiento), `/print` (registra impresión).
- Endpoint `GET /api/finance/checks/summary` para tarjetas KPI.
- UI Blazor: `/finance/checks` (grid + acciones) y `/finance/check-books`.

### Catálogo de tipos de movimiento y conceptos financieros
- Entidad `FinanceMovementType` con `Code`, `Name`, `Direction`, `Nature`,
  `AffectsBalance` y referencia opcional a cuenta contable.
- Entidad `FinanceConcept` con `Category` (payroll/purchase/sales/tax/service/
  transfer/loan/other) y dirección.
- Seeds automáticos para los tipos solicitados (Cargo, Abono, Transferencia,
  Comisión, Interés, Ajuste, Traspaso) y conceptos base (Nómina, Compra, Venta,
  Impuestos, Servicios, Transferencia, Préstamo, Otros).
- UI Blazor: `/finance/movement-types` y `/finance/concepts`.

### Indicadores financieros y proyección
- `GET /api/finance/indicators` calcula saldos en bancos/caja, cuentas por
  cobrar/pagar, inventario, capital de trabajo, razón circulante, prueba ácida,
  razón de efectivo, endeudamiento y flujo operativo a 30 días.
- `GET /api/finance/projection?weeks=N` proyecta flujo de efectivo semanal con
  perfiles de cobranza y pagos y considerando cheques pendientes.
- UI Blazor: `/finance/financial-indicators` y `/finance/financial-projection`
  (gráfico semanal con semáforos).

### Catálogo de bancos ampliado
- Tabla `catalog.banks` con `swift_code`, `currency_id`, `logo_url`,
  `contact_name/phone/email`, `customer_service_phone`, `website`.
- Reutiliza el CRUD genérico `CatalogCrudPage` (con quick-create modal,
  permisos y refresco automático).

### Navegación
- Nuevo grupo lateral **Bancos** con: Catálogo de bancos, Cuentas bancarias,
  Movimientos, Transferencias, Estado de cuenta, Conciliación, Chequeras,
  Cheques, Tipos de movimiento y Conceptos financieros.
- En **Finanzas** se agregaron entradas para "Indicadores financieros" y
  "Proyección financiera" como complemento al centro financiero.

### Migración SQL fase 2
- Archivo `database/20260520_finance_phase2.sql` idempotente
  (`ADD COLUMN IF NOT EXISTS`, `CREATE TABLE IF NOT EXISTS`, seeds con
  `WHERE NOT EXISTS`). Seguro re-aplicarlo.

---



## 1. Avance fase 1 (completado)

### Catálogo y cuentas bancarias
- Entidad `BankAccount` ampliada con: `BankBranch`, `AccountExecutive`,
  `InitialBalance`, `ReconciledBalance`.
- DTOs, endpoint y editor de Tesorería sincronizados con los nuevos campos.
- Tarjeta de saldo conciliado disponible en la pantalla
  `/finance/bank-movements`.

### Movimientos bancarios
- Nueva tabla de endpoints `/api/finance/bank-movements`.
  - `GET` listado con filtros por cuenta y rango de fechas.
  - `POST` captura manual (depósito, retiro, comisión, interés, cargo, ajuste).
  - `PUT` edita movimientos manuales no conciliados.
  - `DELETE` reversa el efecto en saldo si el movimiento es manual y no
    conciliado.
  - `GET /account/{id}/statement` devuelve el estado de cuenta (libros) con
    saldo inicial, actual, conciliado y totales del periodo.
- UI Blazor: `/finance/bank-movements`.
- Los movimientos generados por ingresos, egresos, pagos y recibos siguen
  reflejándose en este listado, pero solo los manuales se pueden editar.

### Transferencias internas
- Endpoint `POST /api/finance/internal-transfers` crea dos movimientos
  vinculados (uno por cada cuenta) y actualiza los saldos.
- Soporta combinaciones banco↔banco, banco↔caja y caja↔caja.
- UI Blazor: `/finance/bank-transfers`.
- Tabla `finance.internal_transfers` con auditoría y referencias a los
  movimientos generados.

### Estado de cuenta
- Entidades `BankStatement` y `BankStatementEntry` para capturar o importar
  partidas del estado de cuenta bancario sin afectar saldos de libros.
- `POST /api/finance/bank-statements` captura manual.
- `POST /api/finance/bank-statements/import-csv` parsea CSV (delimitador y
  encabezado configurables).
- UI Blazor: `/finance/bank-statements` con modal de captura manual y de
  importación CSV.

### Conciliación asistida
- `GET /api/finance/reconciliation/suggestions` cruza partidas del estado
  de cuenta contra movimientos no conciliados, sugiere coincidencias por
  importe y fecha (con tolerancia configurable) y devuelve un score de
  confianza.
- `POST /api/finance/reconciliation/apply-matches` marca movimientos como
  conciliados, asocia las partidas y actualiza el saldo conciliado de la
  cuenta.
- UI Blazor: `/finance/reconciliation-suggestions`.

### Navegación y permisos
- Las nuevas pantallas se agregaron al grupo **Tesorería** del menú
  lateral. Reutilizan la sesión activa y los servicios HTTP existentes.

## 2. Pendientes priorizados (fase 2+)

### Catálogo de bancos
- [ ] Vista de mantenimiento del catálogo `finance.banks` con CRUD propio
      (actualmente se administra como catálogo maestro).
- [ ] Permitir capturar logotipo y datos de contacto del banco.

### Movimientos bancarios
- [ ] Importación masiva de movimientos manuales (CSV / Excel) con
      conciliación automática inmediata.
- [ ] Plantillas de cargos automáticos recurrentes (comisiones, intereses)
      con cédula mensual.

### Estado de cuenta
- [ ] Importadores específicos por banco (BBVA, Banorte, Banamex, Santander)
      para parsear sus formatos nativos.
- [ ] Soporte para Excel (.xlsx) además de CSV.
- [ ] Vista de partidas pendientes de conciliar agrupadas por estado de
      cuenta.

### Conciliación bancaria
- [ ] Pantalla unificada que combine el `Reconciliation` clásico con las
      sugerencias de la fase 1.
- [ ] Marcar partidas con diferencias contables (montos parciales,
      desfases) y generar póliza automática.
- [ ] Reporte de antigüedad de partidas sin conciliar.

### Cuentas por cobrar / por pagar
- [ ] Reforzar dashboard de antigüedad con cortes a 30/60/90/120 días.
- [ ] Programar abonos parciales con flujo de aprobación.
- [ ] Vincular pagos a múltiples facturas con prorrateo.

### Flujo de efectivo
- [ ] Forecast por semana con curva proyectada vs. real.
- [ ] Captura manual de ingresos/egresos esperados fuera del CRM/CxP/CxC.

### Configuración financiera
- [ ] Catálogo de tipos de movimiento parametrizable por empresa (hoy es
      una enum implícita en la API).
- [ ] Asociación contable por tipo de movimiento para auto-pólizas.

### Reportes y exportación
- [ ] PDF del estado de cuenta interno por cuenta y periodo.
- [ ] Exportar movimientos filtrados a Excel.

## 3. Notas técnicas

- Migración SQL: `database/20260519_finance_phase1.sql`. Se ejecuta de
  forma idempotente con `ALTER TABLE IF EXISTS ... ADD COLUMN IF NOT EXISTS`
  y `CREATE TABLE IF NOT EXISTS`, por lo que es seguro re-aplicarla.
- Las nuevas entidades se mapean al esquema `finance` vía
  `NanchesoftSchemaCatalog`.
- En desarrollo, `EnsureCreated()` crea las tablas nuevas automáticamente
  para bases vacías. Para BD productivas, ejecutar el SQL de migración.
- Build verificado en `Nanchesoft.Api` y `Nanchesoft.Web` (release, sin
  errores; warnings preexistentes en otros módulos).

## 4. Convenciones reutilizadas

- Schema `finance` para todas las tablas (`finance.bank_accounts`,
  `finance.bank_movements`, etc.).
- Snake case en BD vía `UseNanchesoftSnakeCaseNames`.
- DTOs JSON con propiedades pascal case (sin atributos).
- HttpClient con factory nombrado `Nanchesoft.Api`.
- Patrón Blazor: páginas en `/Pages/Finance/`, servicios en
  `/Services/Finance/`.
