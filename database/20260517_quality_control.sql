-- Quality Control module tables
-- Migrated from Guava: PControlDeCalidad, PDD_ControlDeCalidad_Inspector, PDD_Defecto

CREATE TABLE IF NOT EXISTS quality_control_records (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               UUID NOT NULL REFERENCES tenants(id) ON DELETE RESTRICT,
    company_id              UUID NOT NULL REFERENCES companies(id) ON DELETE RESTRICT,
    production_order_id     UUID NOT NULL REFERENCES production_orders(id) ON DELETE RESTRICT,

    folio                   VARCHAR(20) NOT NULL,
    inspection_date         DATE NOT NULL,
    inspector_name          VARCHAR(200) NOT NULL,

    status                  VARCHAR(20) NOT NULL DEFAULT 'pending',  -- pending|approved|rejected|on_hold
    result                  VARCHAR(20) NOT NULL DEFAULT '',          -- approved|rejected|conditional

    total_units_inspected   INT NOT NULL DEFAULT 0,
    total_units_approved    INT NOT NULL DEFAULT 0,
    total_units_rejected    INT NOT NULL DEFAULT 0,

    notes                   VARCHAR(2000) NOT NULL DEFAULT '',

    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by              VARCHAR(120),
    updated_at              TIMESTAMPTZ,
    updated_by              VARCHAR(120),
    closed_at               TIMESTAMPTZ,
    closed_by               VARCHAR(120),

    CONSTRAINT uq_qcrecord_folio UNIQUE (tenant_id, company_id, folio)
);

CREATE INDEX IF NOT EXISTS ix_qcrecord_company_status  ON quality_control_records(tenant_id, company_id, status);
CREATE INDEX IF NOT EXISTS ix_qcrecord_production_order ON quality_control_records(tenant_id, production_order_id);

-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS quality_defects (
    id                          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    quality_control_record_id   UUID NOT NULL REFERENCES quality_control_records(id) ON DELETE CASCADE,

    defect_code                 VARCHAR(30) NOT NULL,
    defect_description          VARCHAR(500) NOT NULL DEFAULT '',
    severity                    VARCHAR(20) NOT NULL DEFAULT 'low',  -- low|medium|high|critical
    quantity_affected           INT NOT NULL DEFAULT 0,
    resolution_notes            VARCHAR(1000) NOT NULL DEFAULT '',
    is_resolved                 BOOLEAN NOT NULL DEFAULT FALSE,

    is_active                   BOOLEAN NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by                  VARCHAR(120),
    updated_at                  TIMESTAMPTZ,
    updated_by                  VARCHAR(120),

    CONSTRAINT uq_defect_code_per_record UNIQUE (quality_control_record_id, defect_code)
);

CREATE INDEX IF NOT EXISTS ix_quality_defects_record ON quality_defects(quality_control_record_id);
