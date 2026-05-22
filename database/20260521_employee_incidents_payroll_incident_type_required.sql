create extension if not exists pgcrypto;

alter table hr.hr_employee_incidents
    add column if not exists payroll_incident_type_id uuid null;

insert into payroll.nom_payroll_incident_types (
    tenant_id, company_id, code, name, description, incident_category, affect_type, payroll_concept_type,
    sat_code, color, icon, sort_order, is_discount, is_perception, is_informative,
    requires_amount, requires_quantity, requires_authorization, applies_to_payroll, is_system, is_active, created_by
)
select c.tenant_id, c.id, seed.code, seed.name, seed.description, seed.incident_category, seed.affect_type, seed.payroll_concept_type,
       '', seed.color, seed.icon, seed.sort_order, seed.is_discount, seed.is_perception, seed.is_informative,
       seed.requires_amount, seed.requires_quantity, seed.requires_authorization, seed.applies_to_payroll, true, true, 'migration'
from core.companies c
cross join (
    values
        ('FALTA', 'Falta', 'Falta registrada para nomina.', 'DEDUCCION', 'RESTA', 'FALTA', '#DC2626', 'calendar-x', 5, true, false, false, false, true, false, true),
        ('RETARDO', 'Retardo', 'Retardo registrado para nomina.', 'DEDUCCION', 'RESTA', 'RETARDO', '#DC2626', 'clock-alert', 10, true, false, false, false, true, false, true),
        ('HORAS_EXTRA', 'Horas extra', 'Tiempo extraordinario autorizado.', 'PERCEPCION', 'SUMA', 'HORAS_EXTRA', '#16A34A', 'clock-plus', 20, false, true, false, true, true, true, true),
        ('BONO', 'Bono', 'Bono capturable para nomina.', 'PERCEPCION', 'SUMA', 'BONO', '#16A34A', 'badge-dollar-sign', 30, false, true, false, true, false, true, true),
        ('DESCUENTO_DANOS', 'Descuento por danos', 'Descuento por reposicion o dano de herramienta/material.', 'DEDUCCION', 'RESTA', 'DESCUENTO_DANOS', '#DC2626', 'triangle-alert', 40, true, false, false, true, false, true, true),
        ('PRESTAMO', 'Prestamo', 'Descuento recurrente o capturable por prestamo.', 'DEDUCCION', 'RESTA', 'PRESTAMO', '#DC2626', 'hand-coins', 50, true, false, false, true, false, true, true),
        ('VACACIONES', 'Vacaciones', 'Incidencia informativa o de control para vacaciones.', 'INFORMATIVA', 'NO_AFECTA', 'VACACIONES', '#2563EB', 'plane', 60, false, false, true, false, true, false, false)
) as seed (
    code, name, description, incident_category, affect_type, payroll_concept_type, color, icon, sort_order,
    is_discount, is_perception, is_informative, requires_amount, requires_quantity, requires_authorization, applies_to_payroll
)
where not exists (
    select 1
    from payroll.nom_payroll_incident_types t
    where t.tenant_id = c.tenant_id
      and t.company_id = c.id
      and upper(t.code) = seed.code
      and not t.is_deleted
);

update hr.hr_employee_incidents i
set payroll_incident_type_id = coalesce(i.payroll_incident_type_id, i.nom_payroll_incident_type_id)
where i.payroll_incident_type_id is null
  and i.nom_payroll_incident_type_id is not null;

update hr.hr_employee_incidents i
set payroll_incident_type_id = t.id
from payroll.nom_payroll_incident_types t
where i.payroll_incident_type_id is null
  and t.tenant_id = i.tenant_id
  and t.company_id = i.company_id
  and not t.is_deleted
  and (
      upper(t.code) = upper(coalesce(i.incident_type, ''))
      or upper(t.name) = upper(coalesce(i.incident_type, ''))
  );

insert into payroll.nom_payroll_incident_types (
    tenant_id, company_id, branch_id, code, name, description, incident_category, affect_type, payroll_concept_type,
    sat_code, color, icon, sort_order, is_discount, is_perception, is_informative,
    requires_amount, requires_quantity, requires_authorization, applies_to_payroll, is_system, is_active, created_by
)
select distinct on (i.tenant_id, i.company_id, legacy_name)
       i.tenant_id,
       i.company_id,
       i.branch_id,
       left(regexp_replace(upper(legacy_name), '[^A-Z0-9]+', '_', 'g'), 40),
       legacy_name,
       'Creado automaticamente desde incidencia legacy.',
       'INFORMATIVA',
       'NO_AFECTA',
       'OTRO',
       '',
       '#2563EB',
       'info',
       999,
       false,
       false,
       true,
       false,
       false,
       false,
       false,
       false,
       true,
       'migration'
from (
    select *, nullif(trim(coalesce(incident_type, '')), '') as legacy_name
    from hr.hr_employee_incidents
    where payroll_incident_type_id is null
) i
where legacy_name is not null
  and not exists (
      select 1
      from payroll.nom_payroll_incident_types t
      where t.tenant_id = i.tenant_id
        and t.company_id = i.company_id
        and (upper(t.code) = left(regexp_replace(upper(legacy_name), '[^A-Z0-9]+', '_', 'g'), 40)
             or upper(t.name) = upper(legacy_name))
        and not t.is_deleted
  )
order by i.tenant_id, i.company_id, legacy_name, i.branch_id nulls last;

update hr.hr_employee_incidents i
set payroll_incident_type_id = t.id
from payroll.nom_payroll_incident_types t
where i.payroll_incident_type_id is null
  and t.tenant_id = i.tenant_id
  and t.company_id = i.company_id
  and not t.is_deleted
  and (
      upper(t.code) = left(regexp_replace(upper(trim(coalesce(i.incident_type, 'OTRO'))), '[^A-Z0-9]+', '_', 'g'), 40)
      or upper(t.name) = upper(trim(coalesce(i.incident_type, 'OTRO')))
  );

update hr.hr_employee_incidents i
set payroll_incident_type_id = t.id
from payroll.nom_payroll_incident_types t
where i.payroll_incident_type_id is null
  and t.tenant_id = i.tenant_id
  and t.company_id = i.company_id
  and t.code = 'VACACIONES'
  and not t.is_deleted;

do $$
begin
    if exists (select 1 from hr.hr_employee_incidents where payroll_incident_type_id is null) then
        raise exception 'No se pudo asignar payroll_incident_type_id a todas las incidencias.';
    end if;
end $$;

do $$
begin
    if not exists (
        select 1 from pg_constraint where conname = 'fk_hr_employee_incidents_payroll_incident_type'
    ) then
        alter table hr.hr_employee_incidents
            add constraint fk_hr_employee_incidents_payroll_incident_type
            foreign key (payroll_incident_type_id) references payroll.nom_payroll_incident_types(id) on delete restrict;
    end if;
end $$;

alter table hr.hr_employee_incidents
    alter column payroll_incident_type_id set not null;

create index if not exists ix_hr_employee_incidents_company_payroll_incident_type_date
    on hr.hr_employee_incidents (company_id, payroll_incident_type_id, incident_date);
