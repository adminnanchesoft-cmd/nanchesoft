# Módulo: Nómina Operativa

**Versión:** Fase E completa  
**Branch:** finanzas-fase1  
**Última actualización:** 2026-05-26

---

## Descripción general

Módulo para gestión operativa de nómina sin fiscalización (sin CFDI, ISR, IMSS, timbrado). Orientado a empresas que necesitan control de asistencia, incidencias y cálculo de neto estimado antes de la dispersión formal.

> **RESTRICCIÓN PERMANENTE:** Este módulo NO toca el módulo fiscal. Sin claves SAT, sin timbrado, sin UUID, sin cálculo de ISR/IMSS.

---

## Arquitectura

- **Frontend:** Blazor Server (`src/Nanchesoft.Web/Pages/Payroll/`)
- **API:** Minimal API (`src/Nanchesoft.Api/Endpoints/HumanResourcesEndpoints.cs`)
- **DB:** PostgreSQL — esquema `hr.*` y `payroll.*`
- **Multi-tenant:** Todas las tablas tienen `TenantId` + `CompanyId`
- **Migrations:** No EF Migrations; se usa `EnsureCreated` + `ALTER TABLE IF NOT EXISTS` manual

---

## Páginas disponibles

| Ruta | Descripción |
|------|-------------|
| `/payroll/periods` | Gestión de periodos (crear, editar, cerrar, reabrir) |
| `/payroll/operational-prepayroll` | Prenómina operativa: captura, incidencias, ajustes, cierre |
| `/payroll/employee-incidents` | Incidencias por empleado (captura directa) |
| `/payroll/finiquito` | Cálculo de finiquito/liquidación LFT 2023 |
| `/payroll/reports/operational-prepayroll` | Reporte detallado de prenómina (exportar CSV, imprimir) |
| `/payroll/reports/lista-de-raya` | Lista de raya operativa (firma, exportar CSV, imprimir) |

---

## Entidades de base de datos

### `hr.hr_employee_incidents`
Incidencias por empleado (faltas, retardos, horas extra).

| Columna clave | Tipo | Descripción |
|---|---|---|
| `Origin` | text | `manual` / `clock` / `policy` / `recurring` |
| `ManuallyEdited` | bool | Protege el registro de regeneración automática |
| `PayrollPeriodId` | uuid | FK al periodo |
| `Status` | text | `draft` / `confirmed` / `cancelled` |

### `hr.hr_employee_terminations`
Cálculo de finiquito y liquidación.

| Columna clave | Tipo | Descripción |
|---|---|---|
| `TerminationType` | text | `voluntary` / `justified` / `unjustified` / `restructuring` |
| `Status` | text | `draft` / `approved` |
| `TotalGross` | numeric | Total bruto calculado |
| `IndemnizationDays` | numeric | 90 días (solo unjustified/restructuring) |
| `SeniorityBonusDays` | numeric | 20 días/año (solo unjustified/restructuring) |

### `payroll.payroll_periods`
Periodos de nómina.

| Columna clave | Tipo | Descripción |
|---|---|---|
| `IsClosed` | bool | Periodo cerrado (no permite nuevas incidencias) |
| `Status` | text | `captura` / `calculado` / `cerrado` |

### `hr.hr_attendance_policies`
Políticas de asistencia (reglas automáticas de incidencias).

| Columna clave | Tipo | Descripción |
|---|---|---|
| `Scope` | text | `company` / `department` / `shift` |
| `DepartmentId` | uuid? | FK para scope=department |
| `Priority` | int | Orden de aplicación (mayor prioridad = menor número) |

---

## Flujo de trabajo de un periodo

```
1. Crear periodo (Periods.razor)
2. Generar prenómina (OperationalPrePayroll.razor → botón "Generar")
3. Capturar incidencias
   a. Automáticas: reloj checador (importar punch) → generar resúmenes → generar incidencias
   b. Por política: "Aplicar políticas de asistencia"
   c. Manuales: EmployeeIncidents.razor o modal en prenómina
4. Agregar ajustes (bonos, descuentos manuales)
5. Revisar prenómina y neto estimado
6. Cerrar periodo (botón "Cerrar periodo" en OperationalPrePayroll o Periods)
7. Imprimir lista de raya (PayrollListReport.razor)
```

---

## Cálculo de finiquito (LFT 2023)

### Tabla de días de vacaciones

| Años de antigüedad | Días |
|---|---|
| 1 | 12 |
| 2 | 14 |
| 3 | 16 |
| 4 | 18 |
| 5 | 20 |
| 6–10 | 22 |
| 11–15 | 24 |
| 16–20 | 26 |
| 21–25 | 28 |
| 26–30 | 30 |
| 31–35 | 32 |
| 36+ | 34 |

### Componentes del finiquito

| Concepto | Aplica a | Fórmula |
|---|---|---|
| Vacaciones proporcionales | Todos | `(diasVac × diasPeriodo / 365) × salarioDiario` |
| Prima vacacional | Todos | `vacProporcional × 25%` |
| Aguinaldo proporcional | Todos | `15 × diasDesdeEnero / 365 × salarioDiario` |
| Prima de antigüedad | Voluntaria ≥15 años o forzosa | `12 × años × min(SDI, 2×SMG)` |
| Indemnización 90 días | Unjustified / Restructuring | `90 × SDI` |
| 20 días/año | Unjustified / Restructuring | `20 × años × SDI` |

> **SMG de referencia:** $278.80 MXN/día (2023). Actualizar en `HumanResourcesEndpoints.cs → CalculateTerminationAsync`.

---

## API endpoints de nómina operativa

```
GET    /api/payroll/periods                  Lista de todos los periodos
POST   /api/payroll/periods/{id}/close       Cerrar periodo
POST   /api/payroll/periods/{id}/reopen      Reabrir periodo

GET    /api/hr/terminations                  Lista de finiquitos
POST   /api/hr/terminations/calculate        Calcular finiquito
POST   /api/hr/terminations/{id}/approve     Aprobar finiquito
DELETE /api/hr/terminations/{id}             Eliminar finiquito (draft)
```

---

## Despliegue

```bash
cd /opt/nanchesoft/nanchesoft
dotnet publish src/Nanchesoft.Api -c Release -o /opt/nanchesoft/publish/api
dotnet publish src/Nanchesoft.Web -c Release -o /opt/nanchesoft/publish/web

sudo systemctl restart nanchesoft-api nanchesoft-web

# IMPORTANTE: Siempre restaurar appsettings después del deploy
# El publish sobreescribe con Host=localhost que no conecta
sudo tee /opt/nanchesoft/publish/api/appsettings.json > /dev/null << 'EOF'
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=nanchesoftdb1;Username=nancheadmin;Password=Sandokan89240371"
  }
}
EOF
```

---

## Notas de desarrollo

- El `OperationalPrePayrollEmployeeRow` es el DTO central que agrega sueldo + incidencias + ajustes por empleado/periodo.
- `PayrollListReport.razor` reutiliza `GetOperationalPrePayrollAsync` — misma fuente de datos que el reporte de prenómina, presentación diferente.
- El campo `IsClosed` en `payroll_periods` es la única barrera de integridad para evitar modificaciones en periodos ya pagados.
- La prima de antigüedad topa en `2 × SMG` sobre el salario diario integrado, no sobre el neto.
