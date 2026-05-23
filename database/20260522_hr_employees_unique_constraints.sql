-- ============================================================================
-- 20260522_hr_employees_unique_constraints.sql
--
-- Endurece la unicidad de colaboradores a nivel BD para que el importador
-- (ni cualquier otra ruta) pueda volver a insertar duplicados, aun si el
-- código de aplicación falla.
--
-- Las restricciones (company_id, code) y (company_id, employee_number) ya
-- existen creadas por EF. Aquí se agregan:
--
--   * UNIQUE parcial (company_id, tax_id)  WHERE tax_id  <> ''
--   * UNIQUE parcial (company_id, curp)    WHERE curp    <> ''
--   * UNIQUE parcial (company_id, nss)     WHERE nss     <> ''
--   * UNIQUE parcial (tenant_id,  employee_number)  -- protege multi-empresa
--
-- Si en producción hubiera valores duplicados, la creación fallará y se debe
-- limpiar antes (ver 20260522_hr_employees_dedup_analyze.sql).
-- ============================================================================

\set ON_ERROR_STOP on

begin;

-- Normalizar strings que deberían ser únicos antes de aplicar índices:
update hr.hr_employees set tax_id = upper(trim(tax_id)) where tax_id is not null;
update hr.hr_employees set curp   = upper(trim(curp))   where curp   is not null;
update hr.hr_employees set nss    = trim(nss)           where nss    is not null;
update hr.hr_employees set employee_number = trim(employee_number) where employee_number is not null;
update hr.hr_employees set code            = upper(trim(code))     where code            is not null;

create unique index if not exists ux_hr_employees_company_tax_id
    on hr.hr_employees (company_id, tax_id)
    where tax_id is not null and tax_id <> '';

create unique index if not exists ux_hr_employees_company_curp
    on hr.hr_employees (company_id, curp)
    where curp is not null and curp <> '';

create unique index if not exists ux_hr_employees_company_nss
    on hr.hr_employees (company_id, nss)
    where nss is not null and nss <> '';

create unique index if not exists ux_hr_employees_tenant_employee_number
    on hr.hr_employees (tenant_id, employee_number);

commit;

\echo 'OK: índices únicos parciales creados sobre hr.hr_employees.'
