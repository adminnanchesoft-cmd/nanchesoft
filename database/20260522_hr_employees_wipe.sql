-- ============================================================================
-- 20260522_hr_employees_wipe.sql
--
-- Borra TODOS los empleados y sus datos derivados (nómina, incidencias,
-- asistencias, préstamos, producción, contratos, etc.) en una sola transacción.
--
-- Se usa cuando se quiere reimportar desde cero la base de colaboradores.
--
-- Antes de borrar:
--   1) Crea respaldo en hr.hr_employees_backup_<ts> con todos los registros.
--   2) Loguea conteos previos en hr.employees_wipe_log.
--
-- Si cualquier sentencia falla, hace ROLLBACK automático (todo o nada).
--
-- Ejecutar:   psql -d nanchesoftdb1 -f 20260522_hr_employees_wipe.sql
-- ============================================================================

\set ON_ERROR_STOP on
\timing on

BEGIN;

-- ---------------------------------------------------------------------------
-- 0) Tabla de bitácora de wipes (si no existe).
-- ---------------------------------------------------------------------------
create schema if not exists hr;

create table if not exists hr.employees_wipe_log (
    id           uuid primary key default gen_random_uuid(),
    executed_at  timestamptz not null default now(),
    executed_by  text not null default current_user,
    tenants      integer not null,
    companies    integer not null,
    employees    integer not null,
    children     jsonb not null,
    backup_table text not null
);

-- ---------------------------------------------------------------------------
-- 1) Crear respaldo dinámico con timestamp.
-- ---------------------------------------------------------------------------
do $$
declare
    backup_name text := 'hr_employees_backup_' || to_char(now(),'YYYYMMDD_HH24MISS');
    sql_create  text;
    total_emp   integer;
    total_ten   integer;
    total_co    integer;
    children    jsonb;
begin
    sql_create := format('create table hr.%I as table hr.hr_employees', backup_name);
    execute sql_create;

    select count(*), count(distinct tenant_id), count(distinct company_id)
      into total_emp, total_ten, total_co
      from hr.hr_employees;

    select jsonb_build_object(
        'payroll_run_line_details',           (select count(*) from payroll.payroll_run_line_details),
        'payroll_run_lines',                  (select count(*) from payroll.payroll_run_lines),
        'payroll_runs',                       (select count(*) from payroll.payroll_runs),
        'payroll_tax_accumulators',           (select count(*) from payroll.payroll_tax_accumulators),
        'payroll_source_applications',        (select count(*) from payroll.payroll_source_applications),
        'payroll_receipt_controls',           (select count(*) from payroll.payroll_receipt_controls),
        'payroll_dispersion_lines',           (select count(*) from payroll.payroll_dispersion_lines),
        'payroll_dispersion_batches',         (select count(*) from payroll.payroll_dispersion_batches),
        'payroll_fiscal_reconciliations',     (select count(*) from payroll.payroll_fiscal_reconciliations),
        'payroll_employer_obligations',       (select count(*) from payroll.payroll_employer_obligations),
        'payroll_accounting_postings',        (select count(*) from payroll.payroll_accounting_postings),
        'payroll_run_closings',               (select count(*) from payroll.payroll_run_closings),
        'payroll_global_movement_lines',      (select count(*) from payroll.payroll_global_movement_lines),
        'payroll_prepayroll_adjustments',     (select count(*) from payroll.payroll_prepayroll_adjustments),
        'payroll_daily_entries',              (select count(*) from payroll.payroll_daily_entries),
        'payroll_attendance_daily_summaries', (select count(*) from payroll.payroll_attendance_daily_summaries),
        'payroll_recurring_movements',        (select count(*) from payroll.payroll_recurring_movements),
        'employee_loan_deductions',           (select count(*) from payroll.employee_loan_deductions),
        'employee_loans',                     (select count(*) from payroll.employee_loans),
        'hr_attendance_punches',              (select count(*) from hr.hr_attendance_punches),
        'hr_employee_incidents',              (select count(*) from hr.hr_employee_incidents),
        'hr_recurring_incident_rules',        (select count(*) from hr.hr_recurring_incident_rules),
        'hr_employee_certifications',         (select count(*) from hr.hr_employee_certifications),
        'hr_employee_competency_assessments', (select count(*) from hr.hr_employee_competency_assessments),
        'hr_employee_documents',              (select count(*) from hr.hr_employee_documents),
        'hr_employee_movements',              (select count(*) from hr.hr_employee_movements),
        'hr_employee_performance_reviews',    (select count(*) from hr.hr_employee_performance_reviews),
        'hr_succession_plan_records',         (select count(*) from hr.hr_succession_plan_records),
        'hr_vacation_requests',               (select count(*) from hr.hr_vacation_requests),
        'hr_onboarding_checklists',           (select count(*) from hr.hr_onboarding_checklists),
        'employee_contracts',                 (select count(*) from hr.employee_contracts),
        'hr_candidate_applications_hired',    (select count(*) from hr.hr_candidate_applications where hired_employee_id is not null),
        'production_cell_employees',          (select count(*) from production.production_cell_employees),
        'piece_work_records',                 (select count(*) from production.piece_work_records),
        'production_voucher_details',         (select count(*) from production.production_voucher_details where employee_id is not null)
    ) into children;

    insert into hr.employees_wipe_log (tenants, companies, employees, children, backup_table)
    values (total_ten, total_co, total_emp, children, 'hr.'||backup_name);

    raise notice 'Respaldo creado: hr.%', backup_name;
    raise notice 'Empleados a borrar: %, tenants: %, empresas: %', total_emp, total_ten, total_co;
end $$;

-- ---------------------------------------------------------------------------
-- 2) Borrado en orden topológico (hijas primero).
--
-- Reglas de orden:
--   * Todo lo que apunta a payroll_run_lines.id se borra ANTES de run_lines.
--   * Todo lo que apunta a payroll_runs.id se borra ANTES de runs.
--   * Todo lo que apunta a payroll_dispersion_batches.id se borra antes.
--   * Al final se borra hr.hr_employees.
-- ---------------------------------------------------------------------------

-- 2.1 Dependientes de payroll_run_lines
delete from payroll.payroll_run_line_details;
delete from payroll.payroll_dispersion_lines;
delete from payroll.payroll_source_applications;
delete from payroll.payroll_tax_accumulators;
delete from payroll.payroll_receipt_controls;
delete from payroll.employee_loan_deductions;

-- 2.2 payroll_run_lines, ya sin dependientes
delete from payroll.payroll_run_lines;

-- 2.3 Dependientes de payroll_runs / payroll_dispersion_batches
delete from payroll.payroll_fiscal_reconciliations;
delete from payroll.payroll_dispersion_batches;
delete from payroll.payroll_employer_obligations;
delete from payroll.payroll_accounting_postings;
delete from payroll.payroll_run_closings;

-- 2.4 payroll_runs
delete from payroll.payroll_runs;

-- 2.5 Otros datos de payroll/HR ligados a employee_id
delete from payroll.payroll_global_movement_lines;
delete from payroll.payroll_prepayroll_adjustments;
delete from payroll.payroll_daily_entries;
delete from payroll.payroll_attendance_daily_summaries;
delete from payroll.payroll_recurring_movements;
delete from payroll.employee_loans;
delete from hr.hr_attendance_punches;
delete from hr.hr_employee_incidents;
delete from hr.hr_recurring_incident_rules;
delete from hr.hr_employee_certifications;
delete from hr.hr_employee_competency_assessments;
delete from hr.hr_employee_documents;
delete from hr.hr_employee_movements;
delete from hr.hr_employee_performance_reviews;
delete from hr.hr_onboarding_checklists;
delete from hr.hr_vacation_requests;
delete from hr.hr_succession_plan_records;
delete from hr.employee_contracts;

-- 2.6 Candidatos contratados: poner hired_employee_id en null (FK nullable)
update hr.hr_candidate_applications set hired_employee_id = null where hired_employee_id is not null;

-- 2.7 Producción
delete from production.production_cell_employees;
delete from production.piece_work_records;
update production.production_voucher_details set employee_id = null where employee_id is not null;

-- 2.8 Por último, los empleados.
delete from hr.hr_employees;

-- ---------------------------------------------------------------------------
-- 3) Verificación (debe quedar todo en 0).
-- ---------------------------------------------------------------------------
do $$
declare
    leftover integer;
begin
    select
        (select count(*) from hr.hr_employees)
      + (select count(*) from hr.hr_attendance_punches)
      + (select count(*) from hr.hr_employee_incidents)
      + (select count(*) from hr.hr_recurring_incident_rules)
      + (select count(*) from hr.hr_employee_certifications)
      + (select count(*) from hr.hr_employee_competency_assessments)
      + (select count(*) from hr.hr_employee_documents)
      + (select count(*) from hr.hr_employee_movements)
      + (select count(*) from hr.hr_employee_performance_reviews)
      + (select count(*) from hr.hr_onboarding_checklists)
      + (select count(*) from hr.hr_vacation_requests)
      + (select count(*) from hr.hr_succession_plan_records)
      + (select count(*) from hr.employee_contracts)
      + (select count(*) from payroll.payroll_run_line_details)
      + (select count(*) from payroll.payroll_run_lines)
      + (select count(*) from payroll.payroll_runs)
      + (select count(*) from payroll.payroll_tax_accumulators)
      + (select count(*) from payroll.payroll_source_applications)
      + (select count(*) from payroll.payroll_receipt_controls)
      + (select count(*) from payroll.payroll_dispersion_lines)
      + (select count(*) from payroll.payroll_global_movement_lines)
      + (select count(*) from payroll.payroll_prepayroll_adjustments)
      + (select count(*) from payroll.payroll_daily_entries)
      + (select count(*) from payroll.payroll_attendance_daily_summaries)
      + (select count(*) from payroll.payroll_recurring_movements)
      + (select count(*) from payroll.employee_loan_deductions)
      + (select count(*) from payroll.employee_loans)
      + (select count(*) from production.production_cell_employees)
      + (select count(*) from production.piece_work_records)
    into leftover;

    if leftover <> 0 then
        raise exception 'Quedaron % filas relacionadas. Abortando.', leftover;
    end if;
    raise notice 'OK: hr.hr_employees y todas las tablas hijas quedaron vacías.';
end $$;

COMMIT;
