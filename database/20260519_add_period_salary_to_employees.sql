-- Agrega columna period_salary a hr.hr_employees
-- Corresponde al campo PeriodSalary del commit fcdae40

ALTER TABLE hr.hr_employees
    ADD COLUMN IF NOT EXISTS period_salary NUMERIC(18,4) NOT NULL DEFAULT 0;
