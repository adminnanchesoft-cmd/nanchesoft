-- ============================================================
-- Nómina Operativa – Fase 8: Prenómina Operativa
-- Agrega payroll_concept_id a hr.nom_payroll_incident_types
-- para mapear tipos de incidencia a conceptos de nómina.
-- Idempotente: seguro re-aplicarlo
-- ============================================================

ALTER TABLE hr.nom_payroll_incident_types
    ADD COLUMN IF NOT EXISTS payroll_concept_id uuid NULL
        REFERENCES payroll.payroll_concepts(id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS ix_nom_payroll_incident_types_concept
    ON hr.nom_payroll_incident_types(payroll_concept_id)
    WHERE payroll_concept_id IS NOT NULL;
