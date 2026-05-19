-- ============================================================================
-- Finanzas FASE 1
-- Agrega columnas faltantes a cuentas bancarias y crea tablas auxiliares para
-- movimientos bancarios manuales, importacion de estados de cuenta y
-- transferencias internas entre cuentas.
-- ============================================================================

-- 1) Cuentas bancarias: nuevas columnas
ALTER TABLE IF EXISTS finance.bank_accounts
    ADD COLUMN IF NOT EXISTS bank_branch       VARCHAR(120) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS account_executive VARCHAR(160) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS initial_balance   NUMERIC(18,2) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS reconciled_balance NUMERIC(18,2) NOT NULL DEFAULT 0;

-- 2) Estados de cuenta importados (cabecera)
CREATE TABLE IF NOT EXISTS finance.bank_statements (
    id                  uuid PRIMARY KEY,
    tenant_id           uuid NOT NULL,
    company_id          uuid NOT NULL,
    bank_account_id     uuid NOT NULL,
    statement_date      date NOT NULL,
    period_start        date NULL,
    period_end          date NULL,
    opening_balance     NUMERIC(18,2) NOT NULL DEFAULT 0,
    closing_balance     NUMERIC(18,2) NOT NULL DEFAULT 0,
    source              VARCHAR(40) NOT NULL DEFAULT 'manual',
    reference           VARCHAR(160) NOT NULL DEFAULT '',
    notes               VARCHAR(500) NOT NULL DEFAULT '',
    is_active           boolean NOT NULL DEFAULT TRUE,
    created_at          timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    created_by          VARCHAR(80) NULL,
    updated_at          timestamp without time zone NULL,
    updated_by          VARCHAR(80) NULL
);

CREATE INDEX IF NOT EXISTS ix_bank_statements_company_account ON finance.bank_statements (company_id, bank_account_id, statement_date);

-- 3) Partidas de estados de cuenta (detalle importado)
CREATE TABLE IF NOT EXISTS finance.bank_statement_entries (
    id                  uuid PRIMARY KEY,
    tenant_id           uuid NOT NULL,
    company_id          uuid NOT NULL,
    bank_statement_id   uuid NOT NULL,
    bank_account_id     uuid NOT NULL,
    entry_date          date NOT NULL,
    description         VARCHAR(255) NOT NULL DEFAULT '',
    reference           VARCHAR(160) NOT NULL DEFAULT '',
    amount_in           NUMERIC(18,2) NOT NULL DEFAULT 0,
    amount_out          NUMERIC(18,2) NOT NULL DEFAULT 0,
    balance_after       NUMERIC(18,2) NULL,
    matched_movement_id uuid NULL,
    is_matched          boolean NOT NULL DEFAULT FALSE,
    is_active           boolean NOT NULL DEFAULT TRUE,
    created_at          timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    created_by          VARCHAR(80) NULL,
    updated_at          timestamp without time zone NULL,
    updated_by          VARCHAR(80) NULL,
    CONSTRAINT fk_bank_statement_entries_statement
        FOREIGN KEY (bank_statement_id) REFERENCES finance.bank_statements(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_bank_statement_entries_statement ON finance.bank_statement_entries (bank_statement_id);
CREATE INDEX IF NOT EXISTS ix_bank_statement_entries_account ON finance.bank_statement_entries (bank_account_id, entry_date);

-- 4) Transferencias internas (cabecera de la operacion)
CREATE TABLE IF NOT EXISTS finance.internal_transfers (
    id                       uuid PRIMARY KEY,
    tenant_id                uuid NOT NULL,
    company_id               uuid NOT NULL,
    transfer_date            date NOT NULL,
    source_account_type      VARCHAR(20) NOT NULL,
    source_account_id        uuid NOT NULL,
    destination_account_type VARCHAR(20) NOT NULL,
    destination_account_id   uuid NOT NULL,
    amount                   NUMERIC(18,2) NOT NULL,
    reference                VARCHAR(160) NOT NULL DEFAULT '',
    notes                    VARCHAR(500) NOT NULL DEFAULT '',
    status                   VARCHAR(40) NOT NULL DEFAULT 'posted',
    source_movement_id       uuid NULL,
    destination_movement_id  uuid NULL,
    is_active                boolean NOT NULL DEFAULT TRUE,
    created_at               timestamp without time zone NOT NULL DEFAULT (now() at time zone 'utc'),
    created_by               VARCHAR(80) NULL,
    updated_at               timestamp without time zone NULL,
    updated_by               VARCHAR(80) NULL
);

CREATE INDEX IF NOT EXISTS ix_internal_transfers_company_date ON finance.internal_transfers (company_id, transfer_date);

-- Inicializa initial_balance con el saldo actual para registros existentes
UPDATE finance.bank_accounts
SET initial_balance = current_balance
WHERE initial_balance = 0 AND current_balance <> 0;
