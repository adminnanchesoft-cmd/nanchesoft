-- Nanchesoft - catálogos operativos Orange/Silvasoft normalizados
-- Agrega tablas para capturar colores, manufacturas, cascos y catálogos relacionados.
CREATE SCHEMA IF NOT EXISTS product;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS product.product_colors (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id uuid NOT NULL,
    company_id uuid NOT NULL,
    code varchar(40) NOT NULL,
    name varchar(160) NOT NULL,
    description varchar(500) NOT NULL DEFAULT '',
    sequence integer NOT NULL DEFAULT 0,
    is_active boolean NOT NULL DEFAULT true,
    created_at timestamptz NOT NULL DEFAULT now(),
    created_by varchar(120) NOT NULL DEFAULT '',
    updated_at timestamptz NULL,
    updated_by varchar(120) NOT NULL DEFAULT ''
);

CREATE TABLE IF NOT EXISTS product.product_manufacturing_types (LIKE product.product_colors INCLUDING DEFAULTS INCLUDING CONSTRAINTS INCLUDING INDEXES);
CREATE TABLE IF NOT EXISTS product.product_toe_caps (LIKE product.product_colors INCLUDING DEFAULTS INCLUDING CONSTRAINTS INCLUDING INDEXES);
CREATE TABLE IF NOT EXISTS product.product_sole_colors (LIKE product.product_colors INCLUDING DEFAULTS INCLUDING CONSTRAINTS INCLUDING INDEXES);
CREATE TABLE IF NOT EXISTS product.product_dies (LIKE product.product_colors INCLUDING DEFAULTS INCLUDING CONSTRAINTS INCLUDING INDEXES);
CREATE TABLE IF NOT EXISTS product.quality_control_dies (LIKE product.product_colors INCLUDING DEFAULTS INCLUDING CONSTRAINTS INCLUDING INDEXES);
CREATE TABLE IF NOT EXISTS product.product_folio_patterns (LIKE product.product_colors INCLUDING DEFAULTS INCLUDING CONSTRAINTS INCLUDING INDEXES);

CREATE UNIQUE INDEX IF NOT EXISTS ux_product_colors_company_code ON product.product_colors(company_id, code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_manufacturing_types_company_code ON product.product_manufacturing_types(company_id, code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_toe_caps_company_code ON product.product_toe_caps(company_id, code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_sole_colors_company_code ON product.product_sole_colors(company_id, code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_dies_company_code ON product.product_dies(company_id, code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_quality_control_dies_company_code ON product.quality_control_dies(company_id, code);
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_folio_patterns_company_code ON product.product_folio_patterns(company_id, code);

CREATE INDEX IF NOT EXISTS ix_product_colors_company_active ON product.product_colors(company_id, is_active);
CREATE INDEX IF NOT EXISTS ix_product_manufacturing_types_company_active ON product.product_manufacturing_types(company_id, is_active);
CREATE INDEX IF NOT EXISTS ix_product_toe_caps_company_active ON product.product_toe_caps(company_id, is_active);
CREATE INDEX IF NOT EXISTS ix_product_sole_colors_company_active ON product.product_sole_colors(company_id, is_active);
CREATE INDEX IF NOT EXISTS ix_product_dies_company_active ON product.product_dies(company_id, is_active);
CREATE INDEX IF NOT EXISTS ix_quality_control_dies_company_active ON product.quality_control_dies(company_id, is_active);
CREATE INDEX IF NOT EXISTS ix_product_folio_patterns_company_active ON product.product_folio_patterns(company_id, is_active);
