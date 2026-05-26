# Prompts por Fase — Módulo de Nómina Operativa para NancheSoft
# VERSIÓN CORREGIDA (conflictos resueltos 2026-05-26)

> **Documento 2 de 2.** Cada bloque marcado con `=== PROMPT FASE N ===` se copia
> completo y se pega en Claude Code dentro de VS Code, una fase a la vez.
> Espera a que termine cada fase (incluido su commit + push) antes de pasar a la siguiente.
> Si encuentras un conflicto no previsto, detente y avísame.

---

## Stack real del proyecto (corrección aplicada a todos los prompts)

- **UI:** Blazor Server — páginas en `src/Nanchesoft.Web/Pages/` como archivos `.razor`.
  Los servicios llaman la API vía `HttpClient` inyectado (`factory.CreateClient("Nanchesoft.Api")`).
- **API:** Minimal API en `src/Nanchesoft.Api/Endpoints/` — archivos `*Endpoints.cs` con
  métodos de extensión `Map*Endpoints`.
- **ORM:** EF Core con `DbContext` en `src/Nanchesoft.Persistence/Context/NanchesoftDbContext.cs`.
- **Esquema:** **No hay EF Migrations.** El proyecto usa `EnsureCreated()` en desarrollo
  (crea tablas nuevas automáticamente) y scripts `ALTER TABLE IF NOT EXISTS` para producción.
  Para cada tabla nueva: agrega la entidad al DbContext + crea un script SQL en `database/`.
- **Naming:** snake_case en BD vía `UseNanchesoftSnakeCaseNames`. PascalCase en C#.
- **Multi-tenant:** columnas `TenantId` + `CompanyId` en toda tabla, filtradas en cada query.
- **DevExtreme:** se usan componentes `<DxGrid>`, `<DxFormLayout>`, etc. en los `.razor`.

---

## Mapa de entidades que YA EXISTEN (referencia rápida antes de crear nada)

| Concepto del plan | Entidad real | Estado |
|---|---|---|
| TipoPeriodo | `PayrollPeriodType` | Existe — faltan campos operativos |
| Periodo | `PayrollPeriod` | Existe — `PeriodType` es string, no FK |
| Departamento | `Department` | Existe — faltan `Number` e `IsActive` |
| Puesto | `Position` | Existe — faltan `Number` e `IsActive` |
| Turno | `WorkShift` | Existe — faltan `HoursPerShift`, `MinutesForLateness`, `RestDays`, `IsActive` |
| Horario detallado | `WorkSchedule` | Existe — horarios por día de la semana, asignado al empleado |
| Empleado | `Employee` | Existe — ya tiene `ClockKey`, `DepartmentId`, `PositionId`, `WorkScheduleId` |
| Marcas reloj | `AttendancePunch` | Existe — con `PunchType`, `Source`, `WorkDate`, `PunchDateTime` |
| Importador CSV | `POST /api/hr/time-clock/import/csv` | Ya implementado en `PayrollMvpEndpoints.cs` |
| Concepto operativo | `PayrollConcept` | Existe pero es **FISCAL** (tiene SatCode/Agrupador) — crear entidad nueva aparte |
| Incidencia empleado | `EmployeeIncident` | Existe pero simplificada — ampliar, no recrear |
| Corte de prenómina | `PrePayrollCutoff` | Existe con `WorkedDaysTotal`, `OvertimeHoursTotal`, `Status` |
| Movimiento prenómina | `PrePayrollAdjustment` | Existe pero apunta a `PayrollConcept` fiscal |

---

=== PROMPT FASE 0 — EXPLORACIÓN Y DIAGNÓSTICO ===

Estoy trabajando en **NancheSoft**, un ERP en **Blazor Server + Minimal API + DevExtreme**,
base de datos **PostgreSQL**, arquitectura **multi-tenant** (TenantId + CompanyId). Vamos
a agregar la **parte operativa del módulo de nómina** (reloj checador, incidencias,
políticas de retardos, prenómina). En esta fase **NO escribas ni modifiques código**.
Solo exploración y diagnóstico.

Stack del proyecto:
- UI: Blazor Server. Páginas en `src/Nanchesoft.Web/Pages/` como `.razor`.
- API: Minimal API en `src/Nanchesoft.Api/Endpoints/`.
- ORM: EF Core, DbContext en `src/Nanchesoft.Persistence/Context/NanchesoftDbContext.cs`.
- Esquema: NO hay EF Migrations. Usa `EnsureCreated()` + scripts `ALTER TABLE IF NOT EXISTS`.
- Naming: snake_case en BD, PascalCase en C#.
- DevExtreme para todos los componentes UI.

Contexto importante:
- El proyecto YA tiene módulo de nómina fiscal. **No se toca en todo el proyecto.**
  Nada de CFDI, ISR, IMSS, timbrado, claves SAT, SatCode, SatAgrupador, UUID.
- Manual de referencia: `docs/NOMINAS_Elemental.pdf` (CONTPAQi Nóminas) — solo referencia
  funcional.

Tu tarea en Fase 0:

1. **Reporte del stack** (confirma lo que ya sé, corrígeme si hay diferencias):
   - Versiones exactas de Blazor, DevExtreme, EF Core.
   - Cómo funciona el multi-tenancy (cómo se resuelve TenantId/CompanyId en los endpoints).
   - Cómo se obtiene el usuario actual en los endpoints.
   - Patrón de una pantalla Blazor completa: página razor → servicio HTTP → endpoint API.

2. **Inventario de estas entidades específicas** — para cada una dime si existe, con qué
   campos, y si le faltan campos del plan:
   - `PayrollPeriodType` (TipoPeriodo)
   - `PayrollPeriod` (Periodo)
   - `Department` (Departamento)
   - `Position` (Puesto)
   - `WorkShift` (Turno/Jornada)
   - `WorkSchedule` (Horario por día)
   - `Employee` — campos: ClockKey, DepartmentId, PositionId, WorkScheduleId
   - `AttendancePunch` (marcas del reloj)
   - El endpoint CSV de checadas en `PayrollMvpEndpoints.cs` — qué formatos soporta,
     qué validaciones hace, si ya guarda en `AttendancePunch`
   - `EmployeeIncident` — campos actuales
   - `PayrollConcept` — confirmar que tiene campos SAT y es fiscal
   - `PrePayrollCutoff` y `PrePayrollAdjustment` — campos y propósito actual
   - Páginas Blazor existentes de nómina operativa (HumanResources/, Payroll/)

3. **Punto de decisión** — hazme esta lista antes de cerrar el diagnóstico:
   - ¿Qué entidades propones **extender** (ALTER TABLE) vs **crear nuevas**?
   - Para `EmployeeIncident`: ¿tiene sentido agregarle FK al nuevo catálogo de incidencias
     + campo `Origin` + `ManuallyEdited`, o es mejor una tabla nueva?
   - Para el importador CSV: ¿el que ya existe es suficiente base o hay que rehacerlo?
   - Para la prenómina: ¿`PrePayrollCutoff` puede ser el "corte operativo" o necesita
     tabla nueva?
   Hazme la lista "propongo extender / propongo crear" y **espera mi respuesta** antes
   de planear las siguientes fases.

4. **Riesgos**: ¿algo que debas ajustar antes de empezar?

NO hagas commit (no hay cambios de código). Entrega el reporte y espera mi respuesta.

=== FIN PROMPT FASE 0 ===

---

=== PROMPT FASE 1 — CATÁLOGOS DE APOYO OPERATIVOS ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Ya hicimos Fase 0.

REGLAS (aplican siempre en todas las fases):
- No romper nada existente. No tocar NADA fiscal (CFDI, ISR, IMSS, SAT, timbrado).
- Multi-tenant: TenantId + CompanyId en toda tabla y query nuevos.
- Esquema: agregar entidad al DbContext (EnsureCreated la crea en dev) + script SQL
  `ALTER TABLE IF NOT EXISTS` en `database/` para producción. **No usar EF Migrations.**
- Seguir convenciones: snake_case BD, PascalCase C#, Minimal API en Endpoints/, páginas
  Blazor en Pages/, servicios HTTP en Services/.
- Manual referencia: `docs/NOMINAS_Elemental.pdf`.
- Para entidades que ya existan: **extiéndelas** (ALTER TABLE), no dupliques.

OBJETIVO: dejar listos los catálogos de apoyo operativos.

**1. PayrollPeriodType (ya existe) — solo agrega campos faltantes:**
   Campos a agregar con `ALTER TABLE IF NOT EXISTS`:
   - `payment_days` int, `working_days` int, `adjust_to_calendar_month` bool,
   - `quincena_adjust_type` varchar (valores: "LaborDays" / "NonLaborDays"),
   - `seventh_day_position` int nullable, `payment_day_position` int, `is_active` bool.
   Agrega una función/endpoint `POST /api/payroll/period-types/{id}/generate-periods`
   que genere automáticamente los `PayrollPeriod` de un ejercicio dado una fecha de inicio.

**2. PayrollPeriod (ya existe) — extiende y corrige:**
   - Agrega FK `payroll_period_type_id` uuid nullable (no rompe nada al ser nullable).
   - Agrega: `fiscal_year` int, `period_number` int, `is_start_of_month` bool,
     `is_end_of_month` bool, `is_start_of_year` bool, `is_end_of_year` bool,
     `is_bimester_start` bool, `is_bimester_end` bool.
   - El campo `period_type` (string) que ya existe, consérvalo por compatibilidad.

**3. Department (ya existe) — agrega:**
   - `number` int nullable, `is_active` bool not null default true.

**4. Position (ya existe) — agrega:**
   - `number` int nullable, `is_active` bool not null default true.

**5. WorkShift (ya existe, = Turno) — agrega:**
   - `hours_per_shift` decimal(5,2), `minutes_for_lateness` int default 0,
   - `rest_days` varchar (lista de días: "Saturday,Sunday" o similar),
   - `is_active` bool not null default true.
   Nota: `WorkSchedule` ya existe con horarios por día y está asignada al empleado —
   no la dupliques. `WorkShift` es la jornada/turno general; `WorkSchedule` es el
   horario detallado.

**6. Employee — NO agregar campos:** ya tiene `ClockKey` (= NumeroGafete), `DepartmentId`,
   `PositionId`, `WorkScheduleId`. Si en Fase 0 encontraste que falta algo específico,
   agrégalo aquí; de lo contrario no toques la entidad.

Para cada entidad extendida:
- Script SQL `ALTER TABLE IF NOT EXISTS ... ADD COLUMN IF NOT EXISTS` en `database/`.
- Actualiza la clase C# con los nuevos campos.
- Actualiza la configuración EF en `Configurations/` si aplica.
- Actualiza los endpoints existentes para exponer los campos nuevos (GET/PUT).
- Si la pantalla Blazor del catálogo ya existe, agrégale los campos nuevos; si no existe,
  crea grid DevExtreme CRUD filtrado por tenant + empresa.

Al terminar: script SQL aplicado, compila, pantallas cargan. `git commit` "Fase 1:
extender catálogos de apoyo operativos de nómina" + `git push`.

=== FIN PROMPT FASE 1 ===

---

=== PROMPT FASE 2 — CONCEPTOS OPERATIVOS ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 2.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant en todo lo nuevo, script SQL
en `database/` (no EF Migrations), seguir convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

OBJETIVO: crear `OperationalConcept` — catálogo de conceptos operativos de nómina
(percepciones y deducciones **no fiscales**). Es distinto y separado de `PayrollConcept`
que ya existe y es fiscal (tiene SatCode, SatAgrupador) — **no modificar PayrollConcept**.

Crea la entidad `OperationalConcept`:
- `Id`, `TenantId`, `CompanyId`.
- `Number` (int) — número de concepto, único por empresa.
- `Description` (string).
- `ConceptType` (string: "perception" / "deduction").
- `CalculationRule` (string: "fixed_amount" / "per_day" / "per_hour" / "formula" / "manual").
- `Formula` (string nullable) — expresión cuando CalculationRule = formula.
- `BaseValue` (decimal nullable) — importe fijo / valor por día / valor por hora.
- `IsGlobalAutomatic` (bool) — se incluye por defecto a todos los empleados.
- `PrintOnReceipt` (bool).
- `IsActive` (bool).
- `AttendanceCatalogId` (Guid nullable) — FK preparada para `AttendanceCatalog` de Fase 3;
  crea la columna ahora, la FK se activa en Fase 3.

Script SQL en `database/20260526_operational_concepts.sql`.
Endpoint Minimal API: CRUD en `/api/payroll/operational-concepts`.
Seed inicial de 6 conceptos (percepciones + deducciones típicas operativas).
Pantalla Blazor CRUD con DevExtreme en `Pages/Payroll/OperationalConcepts.razor`.
Agrégala al menú lateral en el grupo Nómina.

Al terminar: script aplicado, compila, pantalla carga. `git commit` "Fase 2: catálogo de
conceptos operativos de nómina" + `git push`.

=== FIN PROMPT FASE 2 ===

---

=== PROMPT FASE 3 — CATÁLOGO DE INCIDENCIAS ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 3.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, script SQL en `database/`
(no EF Migrations), convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

OBJETIVO: crear el catálogo de tipos de incidencia y extender `EmployeeIncident`.

**Parte A — nueva entidad `AttendanceCatalog`** (catálogo de claves de incidencia):
- `Id`, `TenantId`, `CompanyId`.
- `Code` (string corto, ej. "RET", "FINJ") — único por empresa.
- `Description` (string).
- `UnitType` (string: "days" / "hours").
- `Nature` (string: "absence" / "lateness" / "overtime" / "leave" / "disability" /
  "attendance" / "other").
- `PayEffect` (string: "deducts" / "pays" / "neutral").
- `OperationalConceptId` (Guid nullable FK a `OperationalConcept` de Fase 2).
- `IsSystem` (bool) — claves base no se pueden eliminar, solo editar parcialmente.
- `IsActive` (bool).

Script SQL en `database/20260526_attendance_catalog.sql`.

Seed con estas 16 claves base (`IsSystem = true`):

| Code | Description | UnitType | Nature |
|------|-------------|----------|--------|
| TRAB | Días trabajados | days | attendance |
| FINJ | Faltas injustificadas | days | absence |
| CAST | Días de castigo | days | absence |
| ENFG | Enfermedad general | days | disability |
| ATRB | Accidente de trabajo | days | disability |
| ATRY | Accidente de trayecto | days | disability |
| INC  | Incapacidad pagada empresa | days | disability |
| MAT  | Incapacidad maternidad | days | disability |
| PCS  | Permiso con goce | days | leave |
| PSS  | Permiso sin goce | days | leave |
| HE1  | Horas extra sencillas | hours | overtime |
| HE2  | Horas extra dobles | hours | overtime |
| HE3  | Horas extra triples | hours | overtime |
| HE4  | Horas extra nivel 4 | hours | overtime |
| HE5  | Horas extra nivel 5 | hours | overtime |
| RET  | Retardos | hours | lateness |

**Parte B — extender `EmployeeIncident`** (ya existe, solo agregar columnas):
- `attendance_catalog_id` uuid nullable FK a `AttendanceCatalog`.
- `origin` varchar default 'manual' (valores: "clock" / "manual" / "policy").
- `clock_import_id` uuid nullable (FK preparada para Fase 5).
- `manually_edited` bool default false.
El campo `IncidentType` (string) que ya existe consérvalo por compatibilidad.

Script: `database/20260526_employee_incident_extend.sql` con `ADD COLUMN IF NOT EXISTS`.

**Parte C — conectar FK en OperationalConcept:**
Activa la FK `attendance_catalog_id` que dejaste preparada en Fase 2.

Endpoints: CRUD `/api/payroll/attendance-catalog`. Las claves con `IsSystem = true` no
se pueden eliminar (devolver 400).
Pantalla Blazor CRUD: `Pages/Payroll/AttendanceCatalog.razor`. Claves del sistema se
distinguen visualmente (badge o icono de candado).

Al terminar: scripts aplicados, compila, pantalla carga. `git commit` "Fase 3: catálogo
de incidencias y extensión de EmployeeIncident" + `git push`.

=== FIN PROMPT FASE 3 ===

---

=== PROMPT FASE 4 — MOTOR DE POLÍTICAS DE ASISTENCIA Y RETARDOS ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 4.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, script SQL en `database/`
(no EF Migrations), convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

OBJETIVO: motor de políticas de asistencia — configuración de reglas que, en Fase 6,
convertirán las marcas del reloj en incidencias.

Crea dos entidades nuevas:

**`AttendancePolicy`** (cabecera):
- `Id`, `TenantId`, `CompanyId`.
- `Name` (string).
- `Scope` (string: "company" / "department" / "shift").
- `DepartmentId` (Guid nullable FK).
- `WorkShiftId` (Guid nullable FK).
- `Priority` (int).
- `IsActive` (bool).

**`AttendancePolicyRule`** (reglas de la política — diseño extensible):
- `Id`, `TenantId`, `CompanyId`.
- `AttendancePolicyId` (Guid FK).
- `RuleType` (string enum):
  `tolerance_minutes` / `lateness_range` / `latenesses_equal_absence` /
  `lateness_discount` / `absence_discount` / `overtime_rounding` /
  `overtime_threshold_minutes` / `sunday_premium` / `rest_day_worked`.
- `IntValue1`, `IntValue2` (int nullable) — umbrales, cantidades.
- `DecimalValue1` (decimal nullable) — importes o fracciones.
- `StringValue1` (string nullable) — valores de texto o código de incidencia.
- `AttendanceCatalogId` (Guid nullable FK) — incidencia a generar.
- `OperationalConceptId` (Guid nullable FK) — concepto a generar.
- `Notes` (string nullable).

Usa JSONB de PostgreSQL NO es necesario: las columnas genéricas son suficientes y más
fáciles de consultar. Si en el futuro se necesitan más parámetros, se agregan columnas.

Script SQL: `database/20260526_attendance_policies.sql`.

Seed: una política de ejemplo "Política General" con valores razonables (10 min
tolerancia, 3 retardos = 1 falta, descuento por falta = 1 día, umbral horas extra = 15
min).

Servicio en la API: `AttendancePolicyResolver` — dado un `EmployeeId`, devuelve la
política que aplica (por turno primero, luego departamento, luego empresa, según
prioridad). Este servicio lo consumirá el procesamiento en Fase 6.

Endpoints:
- CRUD `/api/payroll/attendance-policies` (políticas).
- CRUD `/api/payroll/attendance-policy-rules` (reglas de una política).

Pantalla Blazor maestro-detalle: `Pages/Payroll/AttendancePolicies.razor` — lista de
políticas arriba, editor de reglas de la política seleccionada abajo (DevExtreme).

Al terminar: script aplicado, compila, pantalla carga. `git commit` "Fase 4: motor de
políticas de asistencia y retardos" + `git push`.

=== FIN PROMPT FASE 4 ===

---

=== PROMPT FASE 5 — IMPORTADOR DE CSV DEL RELOJ CHECADOR ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 5.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, script SQL en `database/`
(no EF Migrations), convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

CONTEXTO IMPORTANTE — lee esto antes de programar:
El proyecto YA tiene:
- Entidad `AttendancePunch` con campos: `EmployeeId`, `WorkDate`, `PunchDateTime`,
  `PunchType` ("entry"/"exit"), `Source`, `DeviceName`, `ExternalReference`, `Status`.
- Endpoint `POST /api/hr/time-clock/import/csv` en `PayrollMvpEndpoints.cs` que ya
  importa CSV en dos formatos (columna `fechahora`+`tipo` o columnas `fecha`+`horaentrada`+`horasalida`).
- Endpoint `POST /api/hr/time-clock/preview/csv` para vista previa.
Revisa ese código antes de empezar. Tu trabajo es **extenderlo**, no rehacerlo.

OBJETIVO: agregar trazabilidad de importaciones y mapeos configurables, sin tirar el
importador existente.

**Parte A — nuevas entidades de trazabilidad:**

`ClockImportMapping` (plantilla de mapeo reutilizable):
- `Id`, `TenantId`, `CompanyId`, `Name`.
- `HasHeaders` (bool), `Separator` (char), `Encoding` (string).
- `ColumnIndexEmployee` (int), `ColumnIndexDateTime` (int nullable),
  `ColumnIndexDate` (int nullable), `ColumnIndexTimeEntry` (int nullable),
  `ColumnIndexTimeExit` (int nullable), `ColumnIndexPunchType` (int nullable).
- `DateFormat` (string), `TimeFormat` (string).
- `EntryValue` (string), `ExitValue` (string) — qué valor en la columna tipo = entrada/salida.
- `IsActive` (bool).

`ClockImport` (bitácora de cada importación):
- `Id`, `TenantId`, `CompanyId`.
- `FileName`, `ImportedAt` (datetime), `UserId` (string).
- `MappingId` (Guid nullable FK), `PayrollPeriodId` (Guid nullable FK).
- `TotalRows`, `ValidRows`, `ErrorRows` (int).
- `Status` (string: "loaded" / "processed" / "cancelled").
- `Notes` (string nullable).

Extiende `AttendancePunch` agregando:
- `clock_import_id` uuid nullable FK a `ClockImport`.
- `raw_employee_key` varchar nullable — gafete crudo del CSV para diagnóstico.
- `read_error` varchar nullable — error si no se pudo cruzar el gafete.

PUNTO DE DECISIÓN — pregúntame esto antes de programar la pantalla:
- ¿Qué columnas trae tu CSV de reloj? ¿Trae encabezados?
- ¿La fecha y hora vienen juntas o separadas?
- ¿Cómo distingue entrada de salida?
- ¿Qué separador y codificación usa?
Pídeme un CSV de ejemplo (10-20 filas). Diseña el mapeo en torno a esa respuesta.
Si no te respondo, usa el formato que ya maneja el importador existente como default.

Pantalla Blazor `Pages/Payroll/ClockImport.razor`:
- Subir CSV, elegir o crear mapeo, vista previa del resultado (grid con empleado resuelto,
  fecha/hora, tipo, errores resaltados), asociar a periodo, confirmar importación.
- Bitácora de importaciones pasadas con opción de cancelar las no procesadas.
- Reutiliza/adapta el endpoint existente; extiéndelo para que cree el registro `ClockImport`
  y vincule los `AttendancePunch` al mismo.

Script SQL: `database/20260526_clock_import.sql` (tablas nuevas + ALTER TABLE en
AttendancePunch).

Al terminar: script aplicado, compila, pantalla carga. `git commit` "Fase 5: trazabilidad
y mapeo configurable del importador de checadas" + `git push`.

=== FIN PROMPT FASE 5 ===

---

=== PROMPT FASE 6 — PROCESAMIENTO DE MARCAS → INCIDENCIAS ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 6.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, script SQL en `database/`
(no EF Migrations), convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

CONTEXTO: `EmployeeIncident` ya existe y fue extendida en Fase 3 con `AttendanceCatalogId`,
`Origin`, `ClockImportId`, `ManuallyEdited`. Las marcas están en `AttendancePunch` con
`ClockImportId`. Las políticas están en `AttendancePolicy` / `AttendancePolicyRule`.

OBJETIVO: el servicio que procesa marcas → incidencias.

Crea el servicio `ClockProcessingService` (en la API, no en Web) con esta lógica:

1. Recibe un `ClockImportId`. Lee todos los `AttendancePunch` de esa importación.
2. Agrupa por empleado + día.
3. Para cada empleado resuelve su política con `AttendancePolicyResolver` (Fase 4).
4. Por cada día: empareja entradas y salidas (si PunchType es indeterminado, infiere por
   orden cronológico). Calcula hora real entrada, hora real salida, horas trabajadas.
5. Aplica reglas de la política:
   - Tolerancia → retardo (RET) si se excede.
   - Si tardanza supera límite → falta (FINJ).
   - Sin marcas en día laborable → falta (FINJ).
   - Con marcas → día trabajado (TRAB).
   - Tiempo post-salida > umbral → horas extra (HE1/HE2/HE3…) con redondeo.
   - Domingo / día de descanso → prima dominical según regla.
6. Acumula retardos del periodo; si llegan al umbral configurado → genera falta por retardos.
7. Guarda en `EmployeeIncident` con `Origin = "clock"` o `"policy"` según corresponda.
8. Procesa solo incidencias con `ManuallyEdited = false` — las editadas a mano no se pisan.
9. Marca `ClockImport.Status = "processed"`.

Idempotente: procesar dos veces no duplica — elimina las de `Origin = "clock"/"policy"`
de esa importación y regenera (respeta las de `Origin = "manual"`).

Endpoint: `POST /api/payroll/clock-imports/{id}/process` — devuelve resumen
(retardos, faltas, horas extra generados). Incluye `GET` para vista previa sin guardar.

Pantalla Blazor: desde la bitácora de la Fase 5, botón **Procesar** que muestra la vista
previa en un grid y luego confirma. Permitir re-procesar con aviso si ya fue procesado.

Al terminar: script si aplica, compila, pantalla carga. `git commit` "Fase 6:
procesamiento de marcas del checador a incidencias" + `git push`.

=== FIN PROMPT FASE 6 ===

---

=== PROMPT FASE 7 — CAPTURA MANUAL DE INCIDENCIAS ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 7.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, script SQL en `database/`
(no EF Migrations), convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

OBJETIVO: captura manual de incidencias. No se crean tablas nuevas — se usa
`EmployeeIncident` con `Origin = "manual"` y `ManuallyEdited = true`.

Puede que necesites una entidad de configuración de vistas de captura — créala si hace
falta con su script SQL.

Construye tres modos de captura:

**1. Captura tabular tipo prenómina** (la principal):
Pantalla `Pages/Payroll/ManualIncidents.razor`:
- Grid DevExtreme donde filas = empleados y columnas = días del periodo o conceptos de
  incidencia. Configurable (qué columnas mostrar, al estilo F7 del manual).
- Captura inline en celdas. Celdas con valor manual en color distinto.
- "Aplicar valor a toda la columna" — misma incidencia a todos los empleados visibles.
- Guarda configuración de vista como predeterminada por usuario.
- Filtros: periodo, departamento, turno.

**2. Captura masiva por tipo de incidencia:**
Pantalla `Pages/Payroll/BulkIncidents.razor`:
- El usuario elige una clave de `AttendanceCatalog`, una cantidad y una fecha, y selecciona
  varios empleados para aplicarles la incidencia de golpe.

**3. Captura individual por empleado:**
Pantalla `Pages/Payroll/EmployeeIncidents.razor`:
- Selecciona empleado, muestra todas sus incidencias del periodo, permite editar/agregar.

Reglas de negocio:
- Toda captura manual: `Origin = "manual"`, `ManuallyEdited = true`.
- Si el usuario edita una incidencia del checador: `ManuallyEdited = true` — no se pisará
  al re-procesar la Fase 6.
- Validar que la unidad (días/horas) coincida con `AttendanceCatalog.UnitType`.
- No permitir captura en periodos cerrados (`PayrollPeriod.IsClosed = true`).

Endpoints necesarios (CRUD de `EmployeeIncident` filtrado por periodo/empleado/origin).

Al terminar: compila, pantallas cargan. `git commit` "Fase 7: captura manual de
incidencias" + `git push`.

=== FIN PROMPT FASE 7 ===

---

=== PROMPT FASE 8 — PRENÓMINA OPERATIVA Y ENLACE AL CÁLCULO ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 8.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, script SQL en `database/`
(no EF Migrations), convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

CONTEXTO IMPORTANTE — lee antes de programar:
Ya existen:
- `PrePayrollCutoff`: corte de prenómina con `PayrollPeriodId`, `WorkedDaysTotal`,
  `OvertimeHoursTotal`, `Status` ("draft"/"closed"), `IsClosed`.
- `PrePayrollAdjustment`: movimiento de prenómina con `PayrollConceptId` (FISCAL),
  `Quantity`, `Amount`, `TaxableAmount`, `ExemptAmount`, `Status`.
- El cálculo fiscal en `PayrollMvpEndpoints.cs` / `PayrollCalculatedEndpoints.cs` ya
  consume `PrePayrollAdjustment`.

PUNTO DE DECISIÓN — investiga y dime:
1. ¿Cómo lee exactamente el cálculo fiscal los datos de prenómina?
   ¿Lee `PrePayrollAdjustment`? ¿Qué campos usa? ¿Por periodo + empleado?
2. ¿Quieres que la prenómina operativa **genere `PrePayrollAdjustment`** (para que el
   cálculo fiscal los lea igual que siempre), o prefieres una tabla de movimientos
   operativos separada?
Dependiendo de tu respuesta, ajusta. Preferencia sugerida: generar `PrePayrollAdjustment`
con `OperationalConcept` mapeado, para no modificar el cálculo fiscal.

**Parte A — nueva entidad `OperationalPayrollMovement`** (movimientos intermedios):
Crea esta entidad solo si el punto de decisión lo requiere. Si el plan es generar
`PrePayrollAdjustment` directamente, omítela.
- `Id`, `TenantId`, `CompanyId`, `EmployeeId`, `PayrollPeriodId`.
- `OperationalConceptId` (FK), `Quantity` (decimal), `Amount` (decimal nullable).
- `SourceIncidentId` (Guid nullable FK a `EmployeeIncident`).
- `Status` (string: "draft" / "confirmed" / "sent_to_payroll").

**Parte B — servicio `OperationalPrePayrollService`:**
Para un `PayrollPeriodId` dado:
1. Lee todas las `EmployeeIncident` del periodo.
2. Las convierte en movimientos usando `AttendanceCatalog → OperationalConcept → regla
   de cálculo`.
3. Consolida por empleado.
4. Guarda en `OperationalPayrollMovement` o en `PrePayrollAdjustment` según lo acordado.

**Parte C — pantalla prenómina operativa:**
`Pages/Payroll/OperationalPrePayroll.razor`:
- Selección de periodo.
- Grid consolidado: por empleado, días trabajados, faltas, retardos, horas extra, importes.
- Botón **Generar**: recalcula desde incidencias (reemplaza borradores sin pisar
  confirmados/enviados).
- Botón **Enviar al cálculo**: marca como EnviadoACalculo / envía a PrePayrollAdjustment.
- Trazabilidad: desde un importe, ver las incidencias origen.
- Re-ejecutable e idempotente.

Script SQL: `database/20260526_operational_prepayroll.sql` (si se crean tablas nuevas).

Al terminar: script si aplica, compila, pantalla carga. `git commit` "Fase 8: prenómina
operativa y enlace al cálculo" + `git push`.

=== FIN PROMPT FASE 8 ===

---

=== PROMPT FASE 9 — REPORTES OPERATIVOS ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 9.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, script SQL si aplica,
convenciones. Manual: `docs/NOMINAS_Elemental.pdf`.

OBJETIVO: reportes operativos. Revisa primero cómo exportan a Excel/PDF los módulos
existentes (contabilidad, compras) y usa el mismo patrón.

Reportes a construir (cada uno en su página Blazor con filtros + grid + exportar):

1. **Reporte de asistencia por periodo** (`Pages/Payroll/Reports/AttendanceReport.razor`):
   Días trabajados, faltas, retardos, permisos, incapacidades, horas extra por empleado
   o departamento. Filtros: periodo, departamento, empleado, turno.

2. **Reporte de retardos y faltas** (`Pages/Payroll/Reports/LatenessReport.razor`):
   Detalle por empleado con fechas y acumulado del periodo. Filtros: periodo,
   departamento, empleado, rango de fechas.

3. **Reporte de horas extra** (`Pages/Payroll/Reports/OvertimeReport.razor`):
   Horas extra por empleado/departamento separadas por tipo (HE1/HE2/HE3…), con
   totales. Filtros: periodo, departamento, empleado.

4. **Bitácora de importaciones** (`Pages/Payroll/Reports/ClockImportLog.razor`):
   Lista de `ClockImport` con quién importó, cuándo, archivo, totales, estado. Con
   opción de ver el detalle de marcas de una importación.

5. **Reporte de prenómina operativa** (`Pages/Payroll/Reports/OperationalPrePayrollReport.razor`):
   Consolidado de la Fase 8 en formato imprimible/exportable.

Cada reporte: filtros → grid DevExtreme → exportar Excel y PDF.
Todos filtrados por TenantId + CompanyId.
Integrados al menú lateral en el grupo Nómina, subgrupo Reportes.

Al terminar: compila, reportes cargan y exportan. `git commit` "Fase 9: reportes
operativos de nómina" + `git push`.

=== FIN PROMPT FASE 9 ===

---

=== PROMPT FASE 10 — PULIDO, PRUEBAS Y DOCUMENTACIÓN ===

Continuamos nómina operativa de **NancheSoft** (Blazor Server + Minimal API + DevExtreme +
PostgreSQL, multi-tenant). Fase 10, la última.

REGLAS: no romper nada, no tocar nada fiscal, multi-tenant, convenciones del proyecto.

OBJETIVO: cerrar el proyecto con pulido, pruebas y documentación.

Tareas:

1. **Prueba del flujo completo** (corrige lo que falle):
   - Crear tipo de periodo → generar periodos.
   - Extender departamentos, puestos, WorkShift con campos nuevos.
   - Asignar turno y ClockKey al empleado.
   - Crear conceptos operativos e incidencias.
   - Configurar política de asistencia.
   - Importar CSV de checadas (usa CSV de prueba), revisar vista previa, confirmar.
   - Procesar importación → ver incidencias generadas.
   - Capturar alguna incidencia manual.
   - Generar prenómina operativa, enviar al cálculo.
   - Sacar los 5 reportes.

2. **No-regresión**: verifica que el módulo de nómina fiscal existente sigue igual.
   Corre los tests de integración del proyecto si existen.

3. **Validaciones y errores**: pantallas nuevas manejan bien casos vacíos, datos inválidos
   y errores con mensajes claros en DevExtreme.

4. **Permisos**: si el proyecto tiene roles/permisos, integra todas las pantallas nuevas.

5. **Rendimiento**: revisa queries pesados (procesamiento, prenómina). Agrega índices en
   `EmployeeId`, `PayrollPeriodId`, `WorkDate`, `ClockImportId` donde falten. Script SQL
   en `database/20260526_nomina_operativa_indexes.sql`.

6. **Documentación**: crea `docs/MODULO_NOMINA_OPERATIVA.md` con:
   - Qué hace el módulo y cómo se relaciona con el módulo fiscal.
   - Flujo paso a paso.
   - Tablas nuevas y extendidas con propósito.
   - Cómo configurar una política de asistencia.
   - Cómo configurar el mapeo de un CSV de reloj.

7. Si el proyecto tiene tests de integración, agrega tests para la lógica crítica:
   procesamiento de marcas, cálculo de retardos, generación de prenómina.

Al terminar: compila, flujo completo funciona, módulo fiscal intacto. `git commit`
"Fase 10: pulido, pruebas y documentación del módulo de nómina operativa" + `git push`.

=== FIN PROMPT FASE 10 ===

---

## Cierre

Con esto el módulo de nómina operativa queda completo: catálogos extendidos, conceptos
operativos, incidencias, políticas de retardos, importación de CSV, procesamiento
automático, captura manual, prenómina y reportes — todo conectado al cálculo fiscal
existente sin haberlo tocado.

**Una fase a la vez.** Revisa el resultado, confirma el commit + push, y recién entonces
pasa a la siguiente.
