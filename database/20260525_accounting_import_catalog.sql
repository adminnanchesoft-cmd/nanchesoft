-- Importación de catálogo de cuentas contables multiempresa
-- 2026-05-25

-- Grupos de empresas que comparten un catálogo contable
CREATE TABLE IF NOT EXISTS accounting_group_companies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    name VARCHAR(128) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by VARCHAR(128)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_acc_group_companies_tenant_name
    ON accounting_group_companies(tenant_id, name);

-- Empresas que pertenecen a cada grupo
CREATE TABLE IF NOT EXISTS accounting_group_company_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    group_company_id UUID NOT NULL REFERENCES accounting_group_companies(id),
    company_name VARCHAR(128) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_acc_group_company_members_unique
    ON accounting_group_company_members(group_company_id, company_name);

-- Columna group_company_id en accounting_accounts (catálogo compartido)
ALTER TABLE accounting_accounts
    ADD COLUMN IF NOT EXISTS group_company_id UUID REFERENCES accounting_group_companies(id);

-- Relación cuenta contable ↔ empresa (SI / NO por empresa del grupo)
CREATE TABLE IF NOT EXISTS accounting_account_companies (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    account_id UUID NOT NULL REFERENCES accounting_accounts(id),
    company_name VARCHAR(128) NOT NULL,
    applies BOOLEAN,            -- TRUE=SI, FALSE=NO, NULL=no definido
    import_source VARCHAR(32),
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_acc_account_companies_unique
    ON accounting_account_companies(account_id, company_name);

-- Registro de importaciones de catálogos
CREATE TABLE IF NOT EXISTS accounting_imports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID NOT NULL,
    group_company_id UUID REFERENCES accounting_group_companies(id),
    file_name VARCHAR(256) NOT NULL,
    user_id VARCHAR(128),
    status VARCHAR(32) NOT NULL DEFAULT 'pending',
    total_rows INTEGER NOT NULL DEFAULT 0,
    valid_rows INTEGER NOT NULL DEFAULT 0,
    error_rows INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_acc_imports_tenant_id ON accounting_imports(tenant_id);

-- Detalle fila a fila de cada importación
CREATE TABLE IF NOT EXISTS accounting_import_details (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    import_id UUID NOT NULL REFERENCES accounting_imports(id),
    excel_row INTEGER NOT NULL,
    account_code VARCHAR(64),
    account_name VARCHAR(256),
    company VARCHAR(128),
    applies BOOLEAN,
    status VARCHAR(32) NOT NULL DEFAULT 'pending',
    error_message TEXT
);

CREATE INDEX IF NOT EXISTS ix_acc_import_details_import_id ON accounting_import_details(import_id);
