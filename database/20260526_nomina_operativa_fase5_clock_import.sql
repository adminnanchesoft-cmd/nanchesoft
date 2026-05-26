-- ============================================================
-- Nómina Operativa – Fase 5: ClockImport y ClockImportMapping
-- Idempotente: seguro re-aplicarlo
-- ============================================================

CREATE TABLE IF NOT EXISTS hr.clock_import_mappings (
    id                      uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id               uuid         NOT NULL,
    company_id              uuid         NOT NULL REFERENCES organization.companies(id) ON DELETE CASCADE,
    code                    varchar(40)  NOT NULL,
    name                    varchar(120) NOT NULL,
    device_code             varchar(80)  NOT NULL DEFAULT '',
    employee_number_column  varchar(80)  NOT NULL DEFAULT 'NoEmpleado',
    date_time_column        varchar(80)  NOT NULL DEFAULT '',
    date_column             varchar(80)  NOT NULL DEFAULT '',
    time_in_column          varchar(80)  NOT NULL DEFAULT '',
    time_out_column         varchar(80)  NOT NULL DEFAULT '',
    punch_type_column       varchar(80)  NOT NULL DEFAULT '',
    default_punch_type      varchar(20)  NOT NULL DEFAULT 'entry',
    date_format             varchar(30)  NOT NULL DEFAULT 'yyyy-MM-dd',
    time_format             varchar(30)  NOT NULL DEFAULT 'HH:mm:ss',
    delimiter               varchar(5)   NOT NULL DEFAULT ',',
    is_default              boolean      NOT NULL DEFAULT false,
    notes                   varchar(500) NOT NULL DEFAULT '',
    is_active               boolean      NOT NULL DEFAULT true,
    created_at              timestamptz  NOT NULL DEFAULT now(),
    created_by              varchar(100) NULL,
    updated_at              timestamptz  NULL,
    updated_by              varchar(100) NULL
);

CREATE TABLE IF NOT EXISTS hr.clock_imports (
    id                      uuid         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    tenant_id               uuid         NOT NULL,
    company_id              uuid         NOT NULL REFERENCES organization.companies(id) ON DELETE CASCADE,
    clock_import_mapping_id uuid         NULL REFERENCES hr.clock_import_mappings(id) ON DELETE SET NULL,
    file_name               varchar(260) NOT NULL DEFAULT '',
    file_size_bytes         bigint       NOT NULL DEFAULT 0,
    file_format             varchar(20)  NOT NULL DEFAULT '',
    imported_at             timestamptz  NOT NULL DEFAULT now(),
    imported_by             varchar(100) NOT NULL DEFAULT '',
    rows_read               integer      NOT NULL DEFAULT 0,
    rows_created            integer      NOT NULL DEFAULT 0,
    rows_skipped            integer      NOT NULL DEFAULT 0,
    rows_error              integer      NOT NULL DEFAULT 0,
    status                  varchar(20)  NOT NULL DEFAULT 'Done',
    error_summary           varchar(2000) NOT NULL DEFAULT '',
    notes                   varchar(500) NOT NULL DEFAULT '',
    is_active               boolean      NOT NULL DEFAULT true,
    created_at              timestamptz  NOT NULL DEFAULT now(),
    created_by              varchar(100) NULL,
    updated_at              timestamptz  NULL,
    updated_by              varchar(100) NULL
);

CREATE INDEX IF NOT EXISTS ix_clock_imports_tenant_company
    ON hr.clock_imports(tenant_id, company_id, imported_at DESC);
