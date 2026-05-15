-- Nanchesoft Cloud - Corridas normalizadas tipo Orange/Silvasoft
-- Versión corregida para schemas y snake_case.
-- Ejecutar después de respaldar y después de crear las tablas base.

ALTER TABLE product.product_size_runs
    ADD COLUMN IF NOT EXISTS legacy_key varchar(20) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS secondary_key varchar(20) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS consumption_mode varchar(1) NOT NULL DEFAULT 'I',
    ADD COLUMN IF NOT EXISTS middle_point integer NULL;

ALTER TABLE product.product_size_run_sizes
    ADD COLUMN IF NOT EXISTS factor_label varchar(30) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS proportion numeric(18,6) NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS is_visible boolean NOT NULL DEFAULT true;

UPDATE product.product_size_run_sizes
SET barcode_label = size_code
WHERE COALESCE(barcode_label, '') = '';

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
