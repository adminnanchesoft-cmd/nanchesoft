create extension if not exists pgcrypto;

create table if not exists hr.hr_recurring_incident_rules (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    company_id uuid not null,
    branch_id uuid null,
    employee_id uuid not null,
    nom_payroll_incident_type_id uuid not null,
    amount numeric(18,2) not null default 0,
    quantity numeric(18,4) not null default 1,
    start_date timestamp with time zone not null,
    end_date timestamp with time zone null,
    frequency varchar(30) not null default 'cada_periodo',
    notes varchar(1000) not null default '',
    requires_authorization boolean not null default false,
    authorized_by varchar(120) null,
    authorized_at timestamp with time zone null,
    is_active boolean not null default true,
    created_at timestamp with time zone not null default now(),
    created_by text null,
    updated_at timestamp with time zone null,
    updated_by text null,
    is_deleted boolean not null default false,
    deleted_at timestamp with time zone null,
    deleted_by varchar(120) null,
    constraint fk_hr_recurring_incident_rules_tenant foreign key (tenant_id) references core.tenants(id) on delete restrict,
    constraint fk_hr_recurring_incident_rules_company foreign key (company_id) references core.companies(id) on delete restrict,
    constraint fk_hr_recurring_incident_rules_branch foreign key (branch_id) references core.branches(id) on delete restrict,
    constraint fk_hr_recurring_incident_rules_employee foreign key (employee_id) references hr.hr_employees(id) on delete restrict,
    constraint fk_hr_recurring_incident_rules_type foreign key (nom_payroll_incident_type_id) references payroll.nom_payroll_incident_types(id) on delete restrict,
    constraint ck_hr_recurring_incident_rules_frequency check (frequency in ('semanal', 'quincenal', 'mensual', 'cada_periodo'))
);

alter table hr.hr_employee_incidents
    add column if not exists recurrent_rule_id uuid null;

do $$
begin
    if not exists (
        select 1 from pg_constraint where conname = 'fk_hr_employee_incidents_recurrent_rule'
    ) then
        alter table hr.hr_employee_incidents
            add constraint fk_hr_employee_incidents_recurrent_rule
            foreign key (recurrent_rule_id) references hr.hr_recurring_incident_rules(id) on delete restrict;
    end if;
end $$;

create index if not exists ix_hr_recurring_incident_rules_scope
    on hr.hr_recurring_incident_rules (tenant_id, company_id, branch_id, is_active, is_deleted);

create index if not exists ix_hr_recurring_incident_rules_employee_type_start
    on hr.hr_recurring_incident_rules (company_id, employee_id, nom_payroll_incident_type_id, start_date);

create unique index if not exists ux_hr_employee_incidents_recurrent_rule_period
    on hr.hr_employee_incidents (recurrent_rule_id, payroll_period_id)
    where recurrent_rule_id is not null and payroll_period_id is not null;
