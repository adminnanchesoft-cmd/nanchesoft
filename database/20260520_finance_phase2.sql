-- ===========================================================================
-- Nanchesoft ERP — Finanzas Fase 2
-- Cheques, chequeras, tipos de movimiento, conceptos financieros y catálogo
-- ampliado de bancos. Migración idempotente (segura para re-aplicar).
-- ===========================================================================

BEGIN;

CREATE SCHEMA IF NOT EXISTS catalog;
CREATE SCHEMA IF NOT EXISTS finance;

-- ----------------------------------------------------------------------------
-- catalog.banks: campos ampliados (logo, contacto, swift, moneda, etc.)
-- ----------------------------------------------------------------------------
ALTER TABLE IF EXISTS catalog.banks
    ADD COLUMN IF NOT EXISTS swift_code           varchar(32)  NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS currency_id          uuid         NULL,
    ADD COLUMN IF NOT EXISTS logo_url             varchar(500) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS contact_name         varchar(120) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS contact_phone        varchar(60)  NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS contact_email        varchar(120) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS customer_service_phone varchar(60) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS website              varchar(200) NOT NULL DEFAULT '';

-- ----------------------------------------------------------------------------
-- finance.movement_types
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS finance.movement_types (
    id                   uuid PRIMARY KEY,
    tenant_id            uuid NOT NULL,
    company_id           uuid NULL,
    code                 varchar(40)  NOT NULL DEFAULT '',
    name                 varchar(120) NOT NULL,
    direction            varchar(16)  NOT NULL DEFAULT 'neutral',
    nature               varchar(32)  NOT NULL DEFAULT '',
    affects_balance      boolean      NOT NULL DEFAULT true,
    is_system            boolean      NOT NULL DEFAULT false,
    accounting_account_id uuid        NULL,
    notes                varchar(500) NOT NULL DEFAULT '',
    is_active            boolean      NOT NULL DEFAULT true,
    created_at           timestamp    NOT NULL DEFAULT now(),
    created_by           varchar(64)  NULL,
    updated_at           timestamp    NULL,
    updated_by           varchar(64)  NULL
);
CREATE INDEX IF NOT EXISTS ix_movement_types_tenant ON finance.movement_types(tenant_id);
CREATE INDEX IF NOT EXISTS ix_movement_types_direction ON finance.movement_types(direction);

-- ----------------------------------------------------------------------------
-- finance.concepts
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS finance.concepts (
    id                   uuid PRIMARY KEY,
    tenant_id            uuid NOT NULL,
    company_id           uuid NULL,
    code                 varchar(40)  NOT NULL DEFAULT '',
    name                 varchar(150) NOT NULL,
    category             varchar(32)  NOT NULL DEFAULT 'other',
    direction            varchar(16)  NOT NULL DEFAULT 'neutral',
    accounting_account_id uuid        NULL,
    is_system            boolean      NOT NULL DEFAULT false,
    notes                varchar(500) NOT NULL DEFAULT '',
    is_active            boolean      NOT NULL DEFAULT true,
    created_at           timestamp    NOT NULL DEFAULT now(),
    created_by           varchar(64)  NULL,
    updated_at           timestamp    NULL,
    updated_by           varchar(64)  NULL
);
CREATE INDEX IF NOT EXISTS ix_concepts_tenant ON finance.concepts(tenant_id);
CREATE INDEX IF NOT EXISTS ix_concepts_category ON finance.concepts(category);

-- ----------------------------------------------------------------------------
-- finance.check_books (chequeras)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS finance.check_books (
    id                  uuid PRIMARY KEY,
    tenant_id           uuid NOT NULL,
    company_id          uuid NOT NULL,
    bank_account_id     uuid NOT NULL,
    code                varchar(40)  NOT NULL DEFAULT '',
    name                varchar(120) NOT NULL DEFAULT '',
    series              varchar(20)  NOT NULL DEFAULT '',
    folio_start         integer      NOT NULL DEFAULT 1,
    folio_end           integer      NOT NULL DEFAULT 1,
    next_folio          integer      NOT NULL DEFAULT 1,
    notes               varchar(500) NOT NULL DEFAULT '',
    is_active           boolean      NOT NULL DEFAULT true,
    created_at          timestamp    NOT NULL DEFAULT now(),
    created_by          varchar(64)  NULL,
    updated_at          timestamp    NULL,
    updated_by          varchar(64)  NULL
);
CREATE INDEX IF NOT EXISTS ix_check_books_account ON finance.check_books(bank_account_id);

-- ----------------------------------------------------------------------------
-- finance.checks (cheques)
-- ----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS finance.checks (
    id                   uuid PRIMARY KEY,
    tenant_id            uuid NOT NULL,
    company_id           uuid NOT NULL,
    bank_account_id      uuid NOT NULL,
    check_book_id        uuid NULL,
    supplier_id          uuid NULL,
    employee_id          uuid NULL,
    folio                varchar(40)  NOT NULL DEFAULT '',
    issue_date           date         NOT NULL DEFAULT current_date,
    posting_date         date         NULL,
    cashed_date          date         NULL,
    cancel_date          date         NULL,
    beneficiary_type     varchar(20)  NOT NULL DEFAULT 'other',
    beneficiary_name     varchar(200) NOT NULL DEFAULT '',
    amount               numeric(18,2) NOT NULL DEFAULT 0,
    concept              varchar(300) NOT NULL DEFAULT '',
    reference            varchar(120) NOT NULL DEFAULT '',
    status               varchar(20)  NOT NULL DEFAULT 'pending',
    is_printed           boolean      NOT NULL DEFAULT false,
    printed_at           timestamp    NULL,
    bank_movement_id     uuid         NULL,
    notes                varchar(500) NOT NULL DEFAULT '',
    is_active            boolean      NOT NULL DEFAULT true,
    created_at           timestamp    NOT NULL DEFAULT now(),
    created_by           varchar(64)  NULL,
    updated_at           timestamp    NULL,
    updated_by           varchar(64)  NULL
);
CREATE INDEX IF NOT EXISTS ix_checks_account ON finance.checks(bank_account_id);
CREATE INDEX IF NOT EXISTS ix_checks_status ON finance.checks(status);
CREATE INDEX IF NOT EXISTS ix_checks_issue_date ON finance.checks(issue_date);
CREATE INDEX IF NOT EXISTS ix_checks_supplier ON finance.checks(supplier_id) WHERE supplier_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_checks_employee ON finance.checks(employee_id) WHERE employee_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_checks_check_book ON finance.checks(check_book_id) WHERE check_book_id IS NOT NULL;

-- ----------------------------------------------------------------------------
-- Seed inicial: tipos de movimiento estándar (uno por tenant)
-- ----------------------------------------------------------------------------
INSERT INTO finance.movement_types (id, tenant_id, code, name, direction, nature, affects_balance, is_system)
SELECT gen_random_uuid(), t.id, mt.code, mt.name, mt.direction, mt.nature, mt.affects_balance, true
FROM core.tenants t
CROSS JOIN (VALUES
    ('CHRG', 'Cargo',         'out',     'bank',       true),
    ('CREDIT', 'Abono',       'in',      'bank',       true),
    ('TRANSF', 'Transferencia','neutral','transfer',   true),
    ('FEE', 'Comisión',       'out',     'fee',        true),
    ('INT', 'Interés',        'in',      'interest',   true),
    ('ADJ', 'Ajuste',         'neutral', 'adjustment', true),
    ('TRSP', 'Traspaso',      'neutral', 'transfer',   true)
) AS mt(code, name, direction, nature, affects_balance)
WHERE NOT EXISTS (
    SELECT 1 FROM finance.movement_types m
    WHERE m.tenant_id = t.id AND m.code = mt.code
);

-- ----------------------------------------------------------------------------
-- Seed inicial: conceptos financieros estándar
-- ----------------------------------------------------------------------------
INSERT INTO finance.concepts (id, tenant_id, code, name, category, direction, is_system)
SELECT gen_random_uuid(), t.id, c.code, c.name, c.category, c.direction, true
FROM core.tenants t
CROSS JOIN (VALUES
    ('PAYROLL',  'Nómina',         'payroll',  'out'),
    ('PURCHASE', 'Compra',         'purchase', 'out'),
    ('SALES',    'Venta',          'sales',    'in'),
    ('TAX',      'Impuestos',      'tax',      'out'),
    ('SERVICE',  'Servicios',      'service',  'out'),
    ('TRANSFER', 'Transferencia',  'transfer', 'neutral'),
    ('LOAN',     'Préstamo',       'loan',     'neutral'),
    ('OTHER',    'Otros',          'other',    'neutral')
) AS c(code, name, category, direction)
WHERE NOT EXISTS (
    SELECT 1 FROM finance.concepts f
    WHERE f.tenant_id = t.id AND f.code = c.code
);

COMMIT;
