-- ============================================================
-- Nómina Operativa – Fase 1: extensiones de catálogos
-- Idempotente: seguro re-aplicarlo
-- ============================================================

-- ── PayrollPeriodType: campos operativos ──────────────────────
-- Nota: is_active ya existe vía BaseEntity en todas las tablas
ALTER TABLE payroll.payroll_period_types
    ADD COLUMN IF NOT EXISTS payment_days          integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS working_days          integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS adjust_to_calendar_month boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS quincena_adjust_type  varchar(40) NOT NULL DEFAULT 'LaborDays',
    ADD COLUMN IF NOT EXISTS seventh_day_position  integer NULL,
    ADD COLUMN IF NOT EXISTS payment_day_position  integer NOT NULL DEFAULT 0;

-- ── PayrollPeriod: campos operativos ─────────────────────────
ALTER TABLE payroll.payroll_periods
    ADD COLUMN IF NOT EXISTS payroll_period_type_id uuid NULL
        REFERENCES payroll.payroll_period_types(id) ON DELETE SET NULL,
    ADD COLUMN IF NOT EXISTS fiscal_year           integer NULL,
    ADD COLUMN IF NOT EXISTS period_number         integer NULL,
    ADD COLUMN IF NOT EXISTS is_start_of_month     boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS is_end_of_month       boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS is_start_of_year      boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS is_end_of_year        boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS is_bimester_start     boolean NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS is_bimester_end       boolean NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS ix_payroll_periods_period_type
    ON payroll.payroll_periods(payroll_period_type_id)
    WHERE payroll_period_type_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_payroll_periods_fiscal_year
    ON payroll.payroll_periods(fiscal_year, period_number)
    WHERE fiscal_year IS NOT NULL;

-- ── WorkShift: campos operativos ──────────────────────────────
ALTER TABLE hr.work_shifts
    ADD COLUMN IF NOT EXISTS hours_per_shift       decimal(5,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS minutes_for_lateness  integer NOT NULL DEFAULT 10,
    ADD COLUMN IF NOT EXISTS rest_days             varchar(100) NOT NULL DEFAULT 'Saturday,Sunday';

-- ── Department: campo operativo ───────────────────────────────
ALTER TABLE hr.departments
    ADD COLUMN IF NOT EXISTS number                integer NULL;

-- ── Position: campo operativo ─────────────────────────────────
ALTER TABLE hr.positions
    ADD COLUMN IF NOT EXISTS number                integer NULL;
