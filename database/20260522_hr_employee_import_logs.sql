-- ============================================================================
-- 20260522_hr_employee_import_logs.sql
--
-- Bitácora de cada importación de colaboradores desde Excel. Permite auditar
-- quién importó, cuándo, qué archivo, totales y errores.
-- ============================================================================

\set ON_ERROR_STOP on

create table if not exists hr.hr_employee_import_logs (
    id              uuid primary key default gen_random_uuid(),
    tenant_id       uuid not null,
    company_id      uuid not null,
    branch_id       uuid null,
    file_name       varchar(260) not null,
    file_size_bytes bigint not null default 0,
    conflict_mode   varchar(20) not null default 'update',
    total_rows      integer not null default 0,
    created_count   integer not null default 0,
    updated_count   integer not null default 0,
    skipped_count   integer not null default 0,
    duplicate_count integer not null default 0,
    error_count     integer not null default 0,
    errors          jsonb not null default '[]'::jsonb,
    duplicates      jsonb not null default '[]'::jsonb,
    success         boolean not null default false,
    rolled_back     boolean not null default false,
    executed_by     varchar(180) not null default 'sistema',
    executed_at     timestamptz not null default now(),
    duration_ms     integer not null default 0,
    constraint fk_hr_employee_import_logs_tenant  foreign key (tenant_id)  references core.tenants(id)   on delete restrict,
    constraint fk_hr_employee_import_logs_company foreign key (company_id) references core.companies(id) on delete restrict,
    constraint ck_hr_employee_import_logs_mode    check (conflict_mode in ('update','skip','error'))
);

create index if not exists ix_hr_employee_import_logs_tenant  on hr.hr_employee_import_logs (tenant_id, executed_at desc);
create index if not exists ix_hr_employee_import_logs_company on hr.hr_employee_import_logs (company_id, executed_at desc);

\echo 'OK: hr.hr_employee_import_logs lista.'
