-- ============================================================
-- Nómina Operativa – Fase 7: Captura manual de incidencias
-- Agrega Origin y ManuallyEdited a hr.employee_incidents
-- Idempotente: seguro re-aplicarlo
-- ============================================================

ALTER TABLE hr.employee_incidents
    ADD COLUMN IF NOT EXISTS origin          varchar(30)  NOT NULL DEFAULT 'manual',
    ADD COLUMN IF NOT EXISTS manually_edited boolean      NOT NULL DEFAULT false;

CREATE INDEX IF NOT EXISTS ix_employee_incidents_period_origin
    ON hr.employee_incidents(tenant_id, company_id, payroll_period_id, origin)
    WHERE NOT is_deleted;
