-- ===========================================================================
-- Nanchesoft ERP — Central de Pagos (Finanzas Fase 3)
-- Lotes de pre-pago multiempresa, autorización y ejecución consolidada.
-- Migración idempotente.
-- ===========================================================================

BEGIN;

CREATE SCHEMA IF NOT EXISTS finance;

-- ----------------------------------------------------------------------------
-- finance.payment_batches
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS finance.payment_batches (
    id                      uuid PRIMARY KEY,
    tenant_id               uuid NOT NULL,
    folio                   varchar(40)  NOT NULL DEFAULT '',
    batch_date              date         NOT NULL DEFAULT current_date,
    scheduled_date          date         NULL,
    status                  varchar(32)  NOT NULL DEFAULT 'draft',
    line_count              integer      NOT NULL DEFAULT 0,
    company_count           integer      NOT NULL DEFAULT 0,
    supplier_count          integer      NOT NULL DEFAULT 0,
    total_amount            numeric(18,2) NOT NULL DEFAULT 0,
    authorized_amount       numeric(18,2) NOT NULL DEFAULT 0,
    executed_amount         numeric(18,2) NOT NULL DEFAULT 0,
    notes                   varchar(500) NOT NULL DEFAULT '',
    priority                varchar(20)  NOT NULL DEFAULT 'normal',
    requested_by_user_id    uuid         NULL,
    requested_by_name       varchar(200) NOT NULL DEFAULT '',
    authorized_by_user_id   uuid         NULL,
    authorized_by_name      varchar(200) NOT NULL DEFAULT '',
    authorized_at           timestamp    NULL,
    rejected_reason         varchar(500) NOT NULL DEFAULT '',
    rejected_at             timestamp    NULL,
    rejected_by_user_id     uuid         NULL,
    rejected_by_name        varchar(200) NOT NULL DEFAULT '',
    executed_at             timestamp    NULL,
    executed_by_user_id     uuid         NULL,
    executed_by_name        varchar(200) NOT NULL DEFAULT '',
    is_active               boolean      NOT NULL DEFAULT true,
    created_at              timestamp    NOT NULL DEFAULT now(),
    created_by              varchar(64)  NULL,
    updated_at              timestamp    NULL,
    updated_by              varchar(64)  NULL
);
CREATE INDEX IF NOT EXISTS ix_payment_batches_tenant ON finance.payment_batches(tenant_id);
CREATE INDEX IF NOT EXISTS ix_payment_batches_status ON finance.payment_batches(status);
CREATE INDEX IF NOT EXISTS ix_payment_batches_date ON finance.payment_batches(batch_date);

-- ----------------------------------------------------------------------------
-- finance.payment_batch_lines
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS finance.payment_batch_lines (
    id                      uuid PRIMARY KEY,
    tenant_id               uuid NOT NULL,
    payment_batch_id        uuid NOT NULL,
    company_id              uuid NOT NULL,
    supplier_id             uuid NULL,
    supplier_name           varchar(200) NOT NULL DEFAULT '',
    purchase_invoice_id     uuid NULL,
    invoice_folio           varchar(60)  NOT NULL DEFAULT '',
    invoice_date            date         NULL,
    due_date                date         NULL,
    days_overdue            integer      NOT NULL DEFAULT 0,
    currency_id             uuid         NULL,
    currency_code           varchar(10)  NOT NULL DEFAULT '',
    exchange_rate           numeric(18,6) NOT NULL DEFAULT 1,
    original_amount         numeric(18,2) NOT NULL DEFAULT 0,
    amount_due              numeric(18,2) NOT NULL DEFAULT 0,
    amount_to_pay           numeric(18,2) NOT NULL DEFAULT 0,
    priority                varchar(20)  NOT NULL DEFAULT 'normal',
    payment_type            varchar(20)  NOT NULL DEFAULT 'transfer',
    bank_account_id         uuid         NULL,
    cash_account_id         uuid         NULL,
    check_book_id           uuid         NULL,
    scheduled_date          date         NULL,
    reference               varchar(120) NOT NULL DEFAULT '',
    notes                   varchar(500) NOT NULL DEFAULT '',
    line_status             varchar(20)  NOT NULL DEFAULT 'pending',
    payment_id              uuid         NULL,
    check_id                uuid         NULL,
    bank_movement_id        uuid         NULL,
    executed_folio          varchar(40)  NOT NULL DEFAULT '',
    executed_at             timestamp    NULL,
    rejected_reason         varchar(500) NOT NULL DEFAULT '',
    is_active               boolean      NOT NULL DEFAULT true,
    created_at              timestamp    NOT NULL DEFAULT now(),
    created_by              varchar(64)  NULL,
    updated_at              timestamp    NULL,
    updated_by              varchar(64)  NULL,
    CONSTRAINT fk_payment_batch_lines_batch
        FOREIGN KEY (payment_batch_id) REFERENCES finance.payment_batches(id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_payment_batch_lines_batch ON finance.payment_batch_lines(payment_batch_id);
CREATE INDEX IF NOT EXISTS ix_payment_batch_lines_status ON finance.payment_batch_lines(line_status);
CREATE INDEX IF NOT EXISTS ix_payment_batch_lines_invoice ON finance.payment_batch_lines(purchase_invoice_id);
CREATE INDEX IF NOT EXISTS ix_payment_batch_lines_company ON finance.payment_batch_lines(company_id);

-- ----------------------------------------------------------------------------
-- finance.payment_batch_audits
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS finance.payment_batch_audits (
    id                       uuid PRIMARY KEY,
    tenant_id                uuid NOT NULL,
    payment_batch_id         uuid NOT NULL,
    payment_batch_line_id    uuid NULL,
    user_id                  uuid NULL,
    user_name                varchar(200) NOT NULL DEFAULT '',
    action                   varchar(40)  NOT NULL DEFAULT '',
    previous_value           text         NOT NULL DEFAULT '',
    new_value                text         NOT NULL DEFAULT '',
    ip_address               varchar(64)  NOT NULL DEFAULT '',
    notes                    varchar(500) NOT NULL DEFAULT '',
    is_active                boolean      NOT NULL DEFAULT true,
    created_at               timestamp    NOT NULL DEFAULT now(),
    created_by               varchar(64)  NULL,
    updated_at               timestamp    NULL,
    updated_by               varchar(64)  NULL,
    CONSTRAINT fk_payment_batch_audits_batch
        FOREIGN KEY (payment_batch_id) REFERENCES finance.payment_batches(id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS ix_payment_batch_audits_batch ON finance.payment_batch_audits(payment_batch_id);
CREATE INDEX IF NOT EXISTS ix_payment_batch_audits_action ON finance.payment_batch_audits(action);

COMMIT;
