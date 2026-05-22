create extension if not exists pgcrypto;

create table if not exists payroll.nom_payroll_incident_types (
    id uuid primary key default gen_random_uuid(),
    tenant_id uuid not null,
    company_id uuid not null,
    branch_id uuid null,
    code varchar(40) not null,
    name varchar(180) not null,
    description varchar(1000) not null default '',
    incident_category varchar(30) not null,
    affect_type varchar(30) not null,
    payroll_concept_type varchar(40) not null,
    sat_code varchar(30) not null default '',
    color varchar(20) not null default '',
    icon varchar(60) not null default '',
    sort_order integer not null default 0,
    is_discount boolean not null default false,
    is_perception boolean not null default false,
    is_informative boolean not null default false,
    requires_amount boolean not null default false,
    requires_quantity boolean not null default false,
    requires_authorization boolean not null default false,
    applies_to_payroll boolean not null default true,
    is_system boolean not null default false,
    is_active boolean not null default true,
    created_at timestamp with time zone not null default now(),
    created_by text null,
    updated_at timestamp with time zone null,
    updated_by text null,
    is_deleted boolean not null default false,
    deleted_at timestamp with time zone null,
    deleted_by varchar(120) null,
    constraint fk_nom_payroll_incident_types_tenant foreign key (tenant_id) references core.tenants(id) on delete restrict,
    constraint fk_nom_payroll_incident_types_company foreign key (company_id) references core.companies(id) on delete restrict,
    constraint fk_nom_payroll_incident_types_branch foreign key (branch_id) references core.branches(id) on delete restrict,
    constraint ck_nom_payroll_incident_types_category check (incident_category in ('DEDUCCION', 'PERCEPCION', 'INFORMATIVA')),
    constraint ck_nom_payroll_incident_types_affect check (affect_type in ('SUMA', 'RESTA', 'NO_AFECTA')),
    constraint ck_nom_payroll_incident_types_concept_type check (payroll_concept_type in ('FALTA', 'RETARDO', 'HORAS_EXTRA', 'BONO', 'COMISION', 'VACACIONES', 'INCAPACIDAD', 'PRESTAMO', 'DESCUENTO_DANOS', 'OTRO')),
    constraint ck_nom_payroll_incident_types_color check (color = '' or color ~ '^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{3})$'),
    constraint ck_nom_payroll_incident_types_deduction_affect check (incident_category <> 'DEDUCCION' or affect_type = 'RESTA'),
    constraint ck_nom_payroll_incident_types_perception_affect check (incident_category <> 'PERCEPCION' or affect_type = 'SUMA')
);

create unique index if not exists ux_nom_payroll_incident_types_tenant_company_code
    on payroll.nom_payroll_incident_types (tenant_id, company_id, upper(code));

create index if not exists ix_nom_payroll_incident_types_scope
    on payroll.nom_payroll_incident_types (tenant_id, company_id, branch_id, is_active, is_deleted);

create index if not exists ix_nom_payroll_incident_types_category_sort
    on payroll.nom_payroll_incident_types (company_id, incident_category, sort_order);

alter table hr.hr_employee_incidents
    add column if not exists branch_id uuid null,
    add column if not exists nom_payroll_incident_type_id uuid null,
    add column if not exists is_deleted boolean not null default false,
    add column if not exists deleted_at timestamp with time zone null,
    add column if not exists deleted_by varchar(120) null;

do $$
begin
    if not exists (
        select 1
        from pg_constraint
        where conname = 'fk_hr_employee_incidents_branch'
    ) then
        alter table hr.hr_employee_incidents
            add constraint fk_hr_employee_incidents_branch
            foreign key (branch_id) references core.branches(id) on delete restrict;
    end if;

    if not exists (
        select 1
        from pg_constraint
        where conname = 'fk_hr_employee_incidents_nom_payroll_incident_type'
    ) then
        alter table hr.hr_employee_incidents
            add constraint fk_hr_employee_incidents_nom_payroll_incident_type
            foreign key (nom_payroll_incident_type_id) references payroll.nom_payroll_incident_types(id) on delete restrict;
    end if;
end $$;

create index if not exists ix_hr_employee_incidents_company_incident_type_date
    on hr.hr_employee_incidents (company_id, nom_payroll_incident_type_id, incident_date);

create index if not exists ix_hr_employee_incidents_scope
    on hr.hr_employee_incidents (tenant_id, company_id, branch_id, is_active, is_deleted);

insert into payroll.nom_payroll_incident_types (
    tenant_id, company_id, code, name, description, incident_category, affect_type, payroll_concept_type,
    sat_code, color, icon, sort_order, is_discount, is_perception, is_informative,
    requires_amount, requires_quantity, requires_authorization, applies_to_payroll, is_system, created_by
)
select c.tenant_id, c.id, seed.code, seed.name, seed.description, seed.incident_category, seed.affect_type, seed.payroll_concept_type,
       seed.sat_code, seed.color, seed.icon, seed.sort_order, seed.is_discount, seed.is_perception, seed.is_informative,
       seed.requires_amount, seed.requires_quantity, seed.requires_authorization, seed.applies_to_payroll, true, 'migration'
from core.companies c
cross join (
    values
        ('DESCUENTO_DANOS', 'Descuento por danos', 'Descuento por reposicion o dano de herramienta/material.', 'DEDUCCION', 'RESTA', 'DESCUENTO_DANOS', '', '#DC2626', 'triangle-alert', 10, true, false, false, true, false, true, true),
        ('REPOSICION_TIJERAS', 'Reposicion de tijeras', 'Reposicion de herramienta asignada al colaborador.', 'DEDUCCION', 'RESTA', 'DESCUENTO_DANOS', '', '#DC2626', 'scissors', 20, true, false, false, true, false, true, true),
        ('HORAS_EXTRA', 'Horas extra', 'Tiempo extraordinario autorizado.', 'PERCEPCION', 'SUMA', 'HORAS_EXTRA', '', '#16A34A', 'clock-plus', 30, false, true, false, true, true, true, true),
        ('BONO_PRODUCTIVIDAD', 'Bono de productividad', 'Bono operativo por desempeno o productividad.', 'PERCEPCION', 'SUMA', 'BONO', '', '#16A34A', 'badge-dollar-sign', 40, false, true, false, true, false, true, true),
        ('FALTA_INJUSTIFICADA', 'Falta injustificada', 'Ausencia no justificada con afectacion a nomina.', 'DEDUCCION', 'RESTA', 'FALTA', '', '#DC2626', 'calendar-x', 50, true, false, false, false, true, false, true),
        ('RETARDO', 'Retardo', 'Retardo operativo registrado por asistencia.', 'DEDUCCION', 'RESTA', 'RETARDO', '', '#DC2626', 'clock-alert', 60, true, false, false, false, true, false, true),
        ('INFORMATIVA_ASISTENCIA', 'Informativa asistencia', 'Incidencia informativa sin afectacion directa.', 'INFORMATIVA', 'NO_AFECTA', 'OTRO', '', '#2563EB', 'info', 70, false, false, true, false, false, false, false),
        ('OTRO', 'Otro', 'Incidencia general clasificada por autorizacion.', 'INFORMATIVA', 'NO_AFECTA', 'OTRO', '', '#2563EB', 'circle-ellipsis', 999, false, false, true, false, false, false, false)
) as seed (
    code, name, description, incident_category, affect_type, payroll_concept_type, sat_code, color, icon, sort_order,
    is_discount, is_perception, is_informative, requires_amount, requires_quantity, requires_authorization, applies_to_payroll
)
where not exists (
    select 1
    from payroll.nom_payroll_incident_types existing
    where existing.tenant_id = c.tenant_id
      and existing.company_id = c.id
      and upper(existing.code) = seed.code
);

update hr.hr_employee_incidents i
set nom_payroll_incident_type_id = t.id
from payroll.nom_payroll_incident_types t
where t.company_id = i.company_id
  and t.tenant_id = i.tenant_id
  and t.code = case
      when lower(coalesce(i.incident_type, '')) in ('falta', 'falta_injustificada', 'absence') then 'FALTA_INJUSTIFICADA'
      when lower(coalesce(i.incident_type, '')) in ('retardo', 'delay') then 'RETARDO'
      when lower(coalesce(i.incident_type, '')) in ('hora_extra', 'horas_extra', 'overtime') then 'HORAS_EXTRA'
      when lower(coalesce(i.incident_type, '')) in ('bonus', 'bono', 'bono_productividad') then 'BONO_PRODUCTIVIDAD'
      when lower(coalesce(i.incident_type, '')) in ('attendance_review', 'informativa_asistencia') then 'INFORMATIVA_ASISTENCIA'
      else 'OTRO'
  end
  and i.nom_payroll_incident_type_id is null;
