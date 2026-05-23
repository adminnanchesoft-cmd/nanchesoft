-- ============================================================================
-- 20260522_hr_employees_dedup_analyze.sql
--
-- ANÁLISIS (solo lectura) de empleados duplicados en hr.hr_employees.
-- No realiza cambios; produce los reportes que el operador deberá revisar
-- antes de ejecutar 20260522_hr_employees_dedup_apply.sql.
--
-- Ejecutar:   psql -d nanchesoftdb -f 20260522_hr_employees_dedup_analyze.sql
--
-- Bloques que devuelve:
--   1. Resumen global por tenant/empresa.
--   2. Duplicados por (tenant_id, company_id, employee_number).
--   3. Duplicados por (tenant_id, rfc) y (tenant_id, curp) y (tenant_id, nss).
--   4. Tabla de "ganadores vs perdedores" con criterio:
--        a) más antiguo por created_at;
--        b) en empate, el que tenga más conteo de hijos relacionados.
--   5. Conteos de FK (hijos) por cada empleado duplicado, agrupados por tabla.
-- ============================================================================

\echo '== 1) Resumen global de duplicados por (tenant, empresa, no.empleado) =='

select
    e.tenant_id,
    e.company_id,
    e.employee_number,
    count(*) as total_filas,
    min(e.created_at) as primer_registro,
    max(e.created_at) as ultimo_registro
from hr.hr_employees e
where coalesce(e.is_deleted, false) = false
group by e.tenant_id, e.company_id, e.employee_number
having count(*) > 1
order by total_filas desc, e.tenant_id, e.company_id, e.employee_number;

\echo '== 2) Detalle de filas duplicadas (mismo tenant + empresa + no.empleado) =='

with dups as (
    select tenant_id, company_id, employee_number
    from hr.hr_employees
    where coalesce(is_deleted, false) = false
    group by tenant_id, company_id, employee_number
    having count(*) > 1
)
select
    e.id,
    e.tenant_id,
    e.company_id,
    e.employee_number,
    e.code,
    e.first_name,
    e.last_name,
    e.second_last_name,
    e.tax_id,
    e.curp,
    e.nss,
    e.hire_date,
    e.status,
    e.is_active,
    e.created_at,
    e.created_by
from hr.hr_employees e
join dups d
  on d.tenant_id = e.tenant_id
 and d.company_id = e.company_id
 and d.employee_number = e.employee_number
where coalesce(e.is_deleted, false) = false
order by e.tenant_id, e.company_id, e.employee_number, e.created_at;

\echo '== 3a) Duplicados por (tenant_id, tax_id/RFC) (ignora vacíos) =='

select e.tenant_id, e.tax_id, count(*) as total
from hr.hr_employees e
where coalesce(e.is_deleted, false) = false
  and coalesce(nullif(trim(e.tax_id), ''), null) is not null
group by e.tenant_id, e.tax_id
having count(*) > 1
order by total desc;

\echo '== 3b) Duplicados por (tenant_id, curp) =='

select e.tenant_id, e.curp, count(*) as total
from hr.hr_employees e
where coalesce(e.is_deleted, false) = false
  and coalesce(nullif(trim(e.curp), ''), null) is not null
group by e.tenant_id, e.curp
having count(*) > 1
order by total desc;

\echo '== 3c) Duplicados por (tenant_id, nss) =='

select e.tenant_id, e.nss, count(*) as total
from hr.hr_employees e
where coalesce(e.is_deleted, false) = false
  and coalesce(nullif(trim(e.nss), ''), null) is not null
group by e.tenant_id, e.nss
having count(*) > 1
order by total desc;

\echo '== 4) Mapa "ganador vs perdedor" propuesto por (tenant, empresa, no.empleado) =='

-- Conteo de referencias FK por empleado: usado como segundo criterio de desempate.
-- Se ven todas las tablas hijas que apuntan a hr.hr_employees(id).
create temp table tmp_emp_child_counts on commit drop as
with counts as (
    select employee_id, count(*) as c from hr.hr_employee_certifications group by employee_id
    union all
    select employee_id, count(*) from hr.hr_employee_competency_assessments group by employee_id
    union all
    select employee_id, count(*) from hr.hr_employee_documents group by employee_id
    union all
    select employee_id, count(*) from hr.hr_employee_incidents group by employee_id
    union all
    select employee_id, count(*) from hr.hr_employee_movements group by employee_id
    union all
    select employee_id, count(*) from hr.hr_employee_performance_reviews group by employee_id
    union all
    select employee_id, count(*) from hr.hr_recurring_incident_rules group by employee_id
    union all
    select employee_id, count(*) from hr.hr_attendance_punches group by employee_id
    union all
    select employee_id, count(*) from hr.hr_onboarding_checklists group by employee_id
    union all
    select employee_id, count(*) from hr.hr_vacation_requests group by employee_id
    union all
    select employee_id, count(*) from hr.employee_contracts group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_attendance_daily_summaries group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_daily_entries group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_dispersion_lines group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_global_movement_lines group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_prepayroll_adjustments group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_receipt_controls group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_recurring_movements group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_run_line_details group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_run_lines group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_source_applications group by employee_id
    union all
    select employee_id, count(*) from payroll.payroll_tax_accumulators group by employee_id
    union all
    select employee_id, count(*) from payroll.employee_loans group by employee_id
    union all
    select employee_id, count(*) from payroll.employee_loan_deductions group by employee_id
    union all
    select employee_id, count(*) from production.production_cell_employees group by employee_id
    union all
    select employee_id, count(*) from production.piece_work_records group by employee_id
    union all
    select employee_id, count(*) from production.production_voucher_details group by employee_id
    union all
    -- relaciones especiales con nombres distintos
    select incumbent_employee_id as employee_id, count(*) from hr.hr_succession_plan_records group by incumbent_employee_id
    union all
    select successor_employee_id as employee_id, count(*) from hr.hr_succession_plan_records group by successor_employee_id
    union all
    select hired_employee_id as employee_id, count(*) from hr.hr_candidate_applications where hired_employee_id is not null group by hired_employee_id
)
select employee_id, sum(c)::bigint as total_hijos
from counts
group by employee_id;

with dup_keys as (
    select tenant_id, company_id, employee_number
    from hr.hr_employees
    where coalesce(is_deleted, false) = false
    group by tenant_id, company_id, employee_number
    having count(*) > 1
),
ranked as (
    select
        e.id,
        e.tenant_id,
        e.company_id,
        e.employee_number,
        e.code,
        e.first_name || ' ' || e.last_name || ' ' || coalesce(e.second_last_name,'') as nombre_completo,
        e.created_at,
        coalesce(c.total_hijos, 0) as total_hijos,
        row_number() over (
            partition by e.tenant_id, e.company_id, e.employee_number
            order by coalesce(c.total_hijos,0) desc, e.created_at asc, e.id asc
        ) as rn
    from hr.hr_employees e
    join dup_keys d
      on d.tenant_id = e.tenant_id
     and d.company_id = e.company_id
     and d.employee_number = e.employee_number
    left join tmp_emp_child_counts c on c.employee_id = e.id
    where coalesce(e.is_deleted, false) = false
)
select
    r.tenant_id,
    r.company_id,
    r.employee_number,
    r.id as employee_id,
    r.code,
    r.nombre_completo,
    r.created_at,
    r.total_hijos,
    case when r.rn = 1 then 'WINNER' else 'LOSER' end as papel
from ranked r
order by r.tenant_id, r.company_id, r.employee_number, r.rn;

\echo '== 5) Conteo de hijos por tabla, para todos los empleados duplicados =='

with dup_emp as (
    select e.id
    from hr.hr_employees e
    join (
        select tenant_id, company_id, employee_number
        from hr.hr_employees
        where coalesce(is_deleted, false) = false
        group by tenant_id, company_id, employee_number
        having count(*) > 1
    ) d
      on d.tenant_id = e.tenant_id
     and d.company_id = e.company_id
     and d.employee_number = e.employee_number
    where coalesce(e.is_deleted, false) = false
)
select 'hr_employee_certifications' as tabla, count(*) as filas from hr.hr_employee_certifications where employee_id in (select id from dup_emp)
union all select 'hr_employee_competency_assessments', count(*) from hr.hr_employee_competency_assessments where employee_id in (select id from dup_emp)
union all select 'hr_employee_documents', count(*) from hr.hr_employee_documents where employee_id in (select id from dup_emp)
union all select 'hr_employee_incidents', count(*) from hr.hr_employee_incidents where employee_id in (select id from dup_emp)
union all select 'hr_employee_movements', count(*) from hr.hr_employee_movements where employee_id in (select id from dup_emp)
union all select 'hr_employee_performance_reviews', count(*) from hr.hr_employee_performance_reviews where employee_id in (select id from dup_emp)
union all select 'hr_recurring_incident_rules', count(*) from hr.hr_recurring_incident_rules where employee_id in (select id from dup_emp)
union all select 'hr_attendance_punches', count(*) from hr.hr_attendance_punches where employee_id in (select id from dup_emp)
union all select 'hr_onboarding_checklists', count(*) from hr.hr_onboarding_checklists where employee_id in (select id from dup_emp)
union all select 'hr_vacation_requests', count(*) from hr.hr_vacation_requests where employee_id in (select id from dup_emp)
union all select 'employee_contracts', count(*) from hr.employee_contracts where employee_id in (select id from dup_emp)
union all select 'payroll_attendance_daily_summaries', count(*) from payroll.payroll_attendance_daily_summaries where employee_id in (select id from dup_emp)
union all select 'payroll_daily_entries', count(*) from payroll.payroll_daily_entries where employee_id in (select id from dup_emp)
union all select 'payroll_dispersion_lines', count(*) from payroll.payroll_dispersion_lines where employee_id in (select id from dup_emp)
union all select 'payroll_global_movement_lines', count(*) from payroll.payroll_global_movement_lines where employee_id in (select id from dup_emp)
union all select 'payroll_prepayroll_adjustments', count(*) from payroll.payroll_prepayroll_adjustments where employee_id in (select id from dup_emp)
union all select 'payroll_receipt_controls', count(*) from payroll.payroll_receipt_controls where employee_id in (select id from dup_emp)
union all select 'payroll_recurring_movements', count(*) from payroll.payroll_recurring_movements where employee_id in (select id from dup_emp)
union all select 'payroll_run_line_details', count(*) from payroll.payroll_run_line_details where employee_id in (select id from dup_emp)
union all select 'payroll_run_lines', count(*) from payroll.payroll_run_lines where employee_id in (select id from dup_emp)
union all select 'payroll_source_applications', count(*) from payroll.payroll_source_applications where employee_id in (select id from dup_emp)
union all select 'payroll_tax_accumulators', count(*) from payroll.payroll_tax_accumulators where employee_id in (select id from dup_emp)
union all select 'payroll_employee_loans', count(*) from payroll.employee_loans where employee_id in (select id from dup_emp)
union all select 'payroll_employee_loan_deductions', count(*) from payroll.employee_loan_deductions where employee_id in (select id from dup_emp)
union all select 'production_cell_employees', count(*) from production.production_cell_employees where employee_id in (select id from dup_emp)
union all select 'production_piece_work_records', count(*) from production.piece_work_records where employee_id in (select id from dup_emp)
union all select 'production_voucher_details', count(*) from production.production_voucher_details where employee_id in (select id from dup_emp)
union all select 'hr_succession_plan_records_incumbent', count(*) from hr.hr_succession_plan_records where incumbent_employee_id in (select id from dup_emp)
union all select 'hr_succession_plan_records_successor', count(*) from hr.hr_succession_plan_records where successor_employee_id in (select id from dup_emp)
union all select 'hr_candidate_applications_hired', count(*) from hr.hr_candidate_applications where hired_employee_id in (select id from dup_emp)
order by filas desc, tabla;

\echo '== Fin del análisis. =='
