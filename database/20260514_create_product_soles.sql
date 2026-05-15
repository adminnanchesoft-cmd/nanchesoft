-- Migration: create product.product_soles table
-- New entity ProductSole (IOrangeSimpleCatalogEntity) for footwear sole catalog.
-- Run once in pgAdmin. Idempotent.

CREATE TABLE IF NOT EXISTS product.product_soles (
    id              uuid            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id       uuid            NOT NULL,
    company_id      uuid            NOT NULL,
    code            varchar(40)     NOT NULL,
    name            varchar(160)    NOT NULL,
    description     varchar(500)    NOT NULL DEFAULT '',
    sequence        integer         NOT NULL DEFAULT 0,
    is_active       boolean         NOT NULL DEFAULT true,
    created_by      varchar(160)    NOT NULL DEFAULT '',
    updated_by      varchar(160)    NOT NULL DEFAULT '',
    created_at      timestamptz     NOT NULL DEFAULT now(),
    updated_at      timestamptz,
    CONSTRAINT pk_product_soles PRIMARY KEY (id),
    CONSTRAINT fk_product_soles_tenant  FOREIGN KEY (tenant_id)  REFERENCES core.tenants  (id) ON DELETE RESTRICT,
    CONSTRAINT fk_product_soles_company FOREIGN KEY (company_id) REFERENCES core.companies (id) ON DELETE RESTRICT,
    CONSTRAINT uq_product_soles_company_code UNIQUE (company_id, code)
);
