-- Nanchesoft Cloud
-- Convierte columnas/tablas/índices/constraints existentes a snake_case en PostgreSQL.
-- IMPORTANTE: Ejecutar primero en una copia o respaldar la base.
-- Uso: pgAdmin -> nanchesoftdb1 -> Query Tool -> ejecutar completo.

BEGIN;

CREATE SCHEMA IF NOT EXISTS accounting;
CREATE SCHEMA IF NOT EXISTS auth;
CREATE SCHEMA IF NOT EXISTS catalog;
CREATE SCHEMA IF NOT EXISTS config;
CREATE SCHEMA IF NOT EXISTS core;
CREATE SCHEMA IF NOT EXISTS finance;
CREATE SCHEMA IF NOT EXISTS hr;
CREATE SCHEMA IF NOT EXISTS inventory;
CREATE SCHEMA IF NOT EXISTS org;
CREATE SCHEMA IF NOT EXISTS payroll;
CREATE SCHEMA IF NOT EXISTS product;
CREATE SCHEMA IF NOT EXISTS purchase;
CREATE SCHEMA IF NOT EXISTS sales;
CREATE SCHEMA IF NOT EXISTS subscription;

CREATE OR REPLACE FUNCTION public.nanchesoft_to_snake_case(input_name text)
RETURNS text
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT lower(
        regexp_replace(
            regexp_replace(
                regexp_replace(input_name, '[\s\-]+', '_', 'g'),
                '([a-z0-9])([A-Z])', '\1_\2', 'g'
            ),
            '([A-Z]+)([A-Z][a-z])', '\1_\2', 'g'
        )
    );
$$;

-- 1) Renombrar tablas que todavía estén en PascalCase o mixtas.
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_type = 'BASE TABLE'
          AND table_schema IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY table_schema, table_name
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.table_name);

        IF r.table_name <> new_name
           AND NOT EXISTS (
                SELECT 1
                FROM information_schema.tables t
                WHERE t.table_schema = r.table_schema
                  AND t.table_name = new_name
           ) THEN
            EXECUTE format('ALTER TABLE %I.%I RENAME TO %I;', r.table_schema, r.table_name, new_name);
        END IF;
    END LOOP;
END $$;

-- 2) Renombrar columnas como "TenantId" -> tenant_id, "CreatedAt" -> created_at, etc.
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT table_schema, table_name, column_name
        FROM information_schema.columns
        WHERE table_schema IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY table_schema, table_name, ordinal_position
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.column_name);

        IF r.column_name <> new_name
           AND NOT EXISTS (
                SELECT 1
                FROM information_schema.columns c
                WHERE c.table_schema = r.table_schema
                  AND c.table_name = r.table_name
                  AND c.column_name = new_name
           ) THEN
            EXECUTE format('ALTER TABLE %I.%I RENAME COLUMN %I TO %I;', r.table_schema, r.table_name, r.column_name, new_name);
        END IF;
    END LOOP;
END $$;

-- 3) Renombrar constraints a snake_case.
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT tc.table_schema, tc.table_name, tc.constraint_name
        FROM information_schema.table_constraints tc
        WHERE tc.table_schema IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY tc.table_schema, tc.table_name, tc.constraint_name
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.constraint_name);

        IF r.constraint_name <> new_name
           AND NOT EXISTS (
                SELECT 1
                FROM information_schema.table_constraints tc2
                WHERE tc2.table_schema = r.table_schema
                  AND tc2.constraint_name = new_name
           ) THEN
            EXECUTE format('ALTER TABLE %I.%I RENAME CONSTRAINT %I TO %I;', r.table_schema, r.table_name, r.constraint_name, new_name);
        END IF;
    END LOOP;
END $$;

-- 4) Renombrar índices a snake_case.
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT schemaname, indexname
        FROM pg_indexes
        WHERE schemaname IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY schemaname, indexname
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.indexname);

        IF r.indexname <> new_name
           AND NOT EXISTS (
                SELECT 1
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = r.schemaname
                  AND c.relname = new_name
           ) THEN
            EXECUTE format('ALTER INDEX %I.%I RENAME TO %I;', r.schemaname, r.indexname, new_name);
        END IF;
    END LOOP;
END $$;

-- 5) Columnas enterprise para corridas tipo Orange, ya en snake_case.
ALTER TABLE IF EXISTS product.product_size_runs
    ADD COLUMN IF NOT EXISTS legacy_key varchar(20) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS secondary_key varchar(20) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS consumption_mode varchar(1) NOT NULL DEFAULT 'I',
    ADD COLUMN IF NOT EXISTS middle_point integer NULL;

ALTER TABLE IF EXISTS product.product_size_run_sizes
    ADD COLUMN IF NOT EXISTS factor_label varchar(30) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS proportion numeric(18,6) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS is_visible boolean NOT NULL DEFAULT true;

UPDATE product.product_size_run_sizes
SET barcode_label = size_code
WHERE COALESCE(barcode_label, '') = '';

-- 6) Variantes/SKU por talla, usando schemas reales y nombres snake_case.
CREATE TABLE IF NOT EXISTS product.product_variants (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    finished_product_id uuid NOT NULL,
    product_size_run_id uuid NOT NULL,
    product_size_run_size_id uuid NOT NULL,
    sequence integer NOT NULL DEFAULT 0,
    size_code varchar(20) NOT NULL DEFAULT '',
    display_label varchar(30) NOT NULL DEFAULT '',
    sku varchar(80) NOT NULL DEFAULT '',
    barcode varchar(80) NOT NULL DEFAULT '',
    notes varchar(500) NOT NULL DEFAULT '',
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamp without time zone NOT NULL DEFAULT now(),
    created_by text NULL,
    updated_at timestamp without time zone NULL,
    updated_by text NULL,
    CONSTRAINT fk_product_variants_tenant FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT,
    CONSTRAINT fk_product_variants_company FOREIGN KEY (company_id) REFERENCES core.companies(id) ON DELETE RESTRICT,
    CONSTRAINT fk_product_variants_finished_product FOREIGN KEY (finished_product_id) REFERENCES product.finished_products(id) ON DELETE CASCADE,
    CONSTRAINT fk_product_variants_size_run FOREIGN KEY (product_size_run_id) REFERENCES product.product_size_runs(id) ON DELETE RESTRICT,
    CONSTRAINT fk_product_variants_size_run_size FOREIGN KEY (product_size_run_size_id) REFERENCES product.product_size_run_sizes(id) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_product_variants_company_sku ON product.product_variants(company_id, sku);
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_variants_product_size ON product.product_variants(finished_product_id, product_size_run_size_id);
CREATE INDEX IF NOT EXISTS ix_product_size_run_sizes_run_size_code ON product.product_size_run_sizes(product_size_run_id, size_code);

COMMIT;

-- Verificación rápida:
SELECT table_schema, table_name, column_name
FROM information_schema.columns
WHERE table_schema IN ('core','product')
  AND table_name IN ('tenants','companies','product_size_runs','product_size_run_sizes','product_variants')
ORDER BY table_schema, table_name, ordinal_position;
