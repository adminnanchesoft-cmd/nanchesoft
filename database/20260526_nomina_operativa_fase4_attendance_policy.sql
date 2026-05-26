-- ============================================================
-- Nómina Operativa – Fase 4: AttendancePolicy y AttendancePolicyRule
-- Idempotente: seguro re-aplicarlo
-- ============================================================

CREATE TABLE IF NOT EXISTS hr.attendance_policies (
    id                      uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id               uuid         NOT NULL,
    company_id              uuid         NOT NULL REFERENCES organization.companies(id) ON DELETE CASCADE,
    work_shift_id           uuid         NULL REFERENCES hr.work_shifts(id) ON DELETE SET NULL,
    code                    varchar(40)  NOT NULL,
    name                    varchar(120) NOT NULL,
    description             varchar(300) NOT NULL DEFAULT '',
    tolerance_minutes       integer      NOT NULL DEFAULT 0,
    min_overtime_minutes    integer      NOT NULL DEFAULT 15,
    requires_punch_in       boolean      NOT NULL DEFAULT true,
    requires_punch_out      boolean      NOT NULL DEFAULT true,
    is_default              boolean      NOT NULL DEFAULT false,
    notes                   varchar(500) NOT NULL DEFAULT '',
    is_deleted              boolean      NOT NULL DEFAULT false,
    is_active               boolean      NOT NULL DEFAULT true,
    created_at              timestamptz  NOT NULL DEFAULT now(),
    created_by              varchar(100) NULL,
    updated_at              timestamptz  NULL,
    updated_by              varchar(100) NULL
);

CREATE INDEX IF NOT EXISTS ix_attendance_policies_tenant_company
    ON hr.attendance_policies(tenant_id, company_id)
    WHERE NOT is_deleted;

CREATE TABLE IF NOT EXISTS hr.attendance_policy_rules (
    id                      uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id               uuid         NOT NULL,
    company_id              uuid         NOT NULL REFERENCES organization.companies(id) ON DELETE CASCADE,
    attendance_policy_id    uuid         NOT NULL REFERENCES hr.attendance_policies(id) ON DELETE CASCADE,
    code                    varchar(40)  NOT NULL,
    name                    varchar(120) NOT NULL,
    rule_type               varchar(40)  NOT NULL DEFAULT '',
    condition_type          varchar(40)  NOT NULL DEFAULT 'GreaterThan',
    threshold_minutes       integer      NULL,
    threshold_days          numeric(8,2) NULL,
    action_type             varchar(40)  NOT NULL DEFAULT 'CreateIncident',
    action_value            numeric(14,4) NOT NULL DEFAULT 0,
    incident_type_code      varchar(40)  NULL,
    sort_order              integer      NOT NULL DEFAULT 0,
    notes                   varchar(500) NOT NULL DEFAULT '',
    is_deleted              boolean      NOT NULL DEFAULT false,
    is_active               boolean      NOT NULL DEFAULT true,
    created_at              timestamptz  NOT NULL DEFAULT now(),
    created_by              varchar(100) NULL,
    updated_at              timestamptz  NULL,
    updated_by              varchar(100) NULL
);

CREATE INDEX IF NOT EXISTS ix_attendance_policy_rules_policy
    ON hr.attendance_policy_rules(attendance_policy_id)
    WHERE NOT is_deleted;
