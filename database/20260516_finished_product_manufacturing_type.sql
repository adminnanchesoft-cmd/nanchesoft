-- ============================================================
-- FASE 11-18 — Migración completa del esquema de productos
-- Ejecutar UNA SOLA VEZ. Compatible con EnsureCreated.
-- ============================================================

-- 1. Agregar columnas Description y Sequence faltantes en tablas legacy
ALTER TABLE product.colors
    ADD COLUMN IF NOT EXISTS "Description" character varying(600) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "Sequence"    integer                NOT NULL DEFAULT 0;

ALTER TABLE product.manufacture_types
    ADD COLUMN IF NOT EXISTS "Description" character varying(600) NOT NULL DEFAULT '';

ALTER TABLE product.sole_colors
    ADD COLUMN IF NOT EXISTS "Description" character varying(600) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "Sequence"    integer                NOT NULL DEFAULT 0;

ALTER TABLE product.toe_caps
    ADD COLUMN IF NOT EXISTS "Description" character varying(600) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "Sequence"    integer                NOT NULL DEFAULT 0;

ALTER TABLE product.product_foliations
    ADD COLUMN IF NOT EXISTS "Description" character varying(600) NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS "Sequence"    integer                NOT NULL DEFAULT 0;

-- 2. Crear tabla product_leather_types
CREATE TABLE IF NOT EXISTS product.product_leather_types (
    "Id"          uuid                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "TenantId"    uuid                     NOT NULL,
    "CompanyId"   uuid                     NOT NULL,
    "Code"        character varying(40)    NOT NULL,
    "Name"        character varying(160)   NOT NULL,
    "Description" character varying(600)   NOT NULL DEFAULT '',
    "Sequence"    integer                  NOT NULL DEFAULT 0,
    "IsActive"    boolean                  NOT NULL DEFAULT true,
    "CreatedAt"   timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"   text,
    "UpdatedAt"   timestamp with time zone,
    "UpdatedBy"   text
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_product_leather_types_company_code
    ON product.product_leather_types("CompanyId", "Code");

-- 3. Crear tabla product_soles
CREATE TABLE IF NOT EXISTS product.product_soles (
    "Id"          uuid                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "TenantId"    uuid                     NOT NULL,
    "CompanyId"   uuid                     NOT NULL,
    "Code"        character varying(40)    NOT NULL,
    "Name"        character varying(160)   NOT NULL,
    "Description" character varying(600)   NOT NULL DEFAULT '',
    "Sequence"    integer                  NOT NULL DEFAULT 0,
    "IsActive"    boolean                  NOT NULL DEFAULT true,
    "CreatedAt"   timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"   text,
    "UpdatedAt"   timestamp with time zone,
    "UpdatedBy"   text
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_product_soles_company_code
    ON product.product_soles("CompanyId", "Code");

-- 4. Crear tabla quality_control_dies
CREATE TABLE IF NOT EXISTS product.quality_control_dies (
    "Id"          uuid                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "TenantId"    uuid                     NOT NULL,
    "CompanyId"   uuid                     NOT NULL,
    "Code"        character varying(40)    NOT NULL,
    "Name"        character varying(160)   NOT NULL,
    "Description" character varying(600)   NOT NULL DEFAULT '',
    "Sequence"    integer                  NOT NULL DEFAULT 0,
    "IsActive"    boolean                  NOT NULL DEFAULT true,
    "CreatedAt"   timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"   text,
    "UpdatedAt"   timestamp with time zone,
    "UpdatedBy"   text
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_quality_control_dies_company_code
    ON product.quality_control_dies("CompanyId", "Code");

-- 5. Agregar FK columns faltantes a finished_products
ALTER TABLE product.finished_products
    ADD COLUMN IF NOT EXISTS "ItemModelId"               uuid REFERENCES product.item_models("Id")           ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ItemBrandId"               uuid REFERENCES product.item_brands("Id")           ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ProductLeatherTypeId"      uuid REFERENCES product.product_leather_types("Id") ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ProductColorId"            uuid REFERENCES product.colors("Id")                ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ProductToeCapId"           uuid REFERENCES product.toe_caps("Id")              ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ProductSoleId"             uuid REFERENCES product.product_soles("Id")         ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ProductSoleColorId"        uuid REFERENCES product.sole_colors("Id")           ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ProductFolioPatternId"     uuid REFERENCES product.product_foliations("Id")    ON DELETE RESTRICT,
    ADD COLUMN IF NOT EXISTS "ProductManufacturingTypeId" uuid REFERENCES product.manufacture_types("Id")   ON DELETE RESTRICT;

-- 6. Crear tablas para insumos por producto (Fase 12)
CREATE TABLE IF NOT EXISTS product.finished_product_supplies (
    "Id"                 uuid                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "TenantId"           uuid                     NOT NULL,
    "CompanyId"          uuid                     NOT NULL,
    "FinishedProductId"  uuid                     NOT NULL REFERENCES product.finished_products("Id") ON DELETE CASCADE,
    "ProductComponentId" uuid                     NOT NULL REFERENCES product.product_components("Id") ON DELETE RESTRICT,
    "IsAuthorized"       boolean                  NOT NULL DEFAULT false,
    "AuthorizedAt"       timestamp with time zone,
    "AuthorizedBy"       character varying(120),
    "Notes"              character varying(1200)  NOT NULL DEFAULT '',
    "IsActive"           boolean                  NOT NULL DEFAULT true,
    "CreatedAt"          timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"          text,
    "UpdatedAt"          timestamp with time zone,
    "UpdatedBy"          text
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_finished_product_supplies_product_component
    ON product.finished_product_supplies("FinishedProductId", "ProductComponentId");

CREATE TABLE IF NOT EXISTS product.finished_product_supply_sizes (
    "Id"                       uuid                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "FinishedProductSupplyId"  uuid                     NOT NULL REFERENCES product.finished_product_supplies("Id") ON DELETE CASCADE,
    "ProductSizeRunSizeId"     uuid                     NOT NULL REFERENCES product.product_size_run_sizes("Id") ON DELETE RESTRICT,
    "MaterialItemId"           uuid                     REFERENCES product.material_items("Id") ON DELETE RESTRICT,
    "Notes"                    character varying(1200)  NOT NULL DEFAULT '',
    "IsActive"                 boolean                  NOT NULL DEFAULT true,
    "CreatedAt"                timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"                text,
    "UpdatedAt"                timestamp with time zone,
    "UpdatedBy"                text
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_finished_product_supply_sizes_supply_size
    ON product.finished_product_supply_sizes("FinishedProductSupplyId", "ProductSizeRunSizeId");

-- 7. Crear tablas de distribución de tallas por material
CREATE TABLE IF NOT EXISTS product.material_size_distributions (
    "Id"                   uuid                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "TenantId"             uuid                     NOT NULL,
    "CompanyId"            uuid                     NOT NULL,
    "MaterialSubfamilyId"  uuid                     NOT NULL REFERENCES product.material_subfamilies("Id") ON DELETE RESTRICT,
    "ProductSizeRunId"     uuid                     NOT NULL REFERENCES product.product_size_runs("Id") ON DELETE RESTRICT,
    "ProductLastId"        uuid                     REFERENCES product.product_lasts("Id") ON DELETE RESTRICT,
    "Notes"                character varying(1200)  NOT NULL DEFAULT '',
    "IsActive"             boolean                  NOT NULL DEFAULT true,
    "CreatedAt"            timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"            text,
    "UpdatedAt"            timestamp with time zone,
    "UpdatedBy"            text
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_material_size_dist_company_subfamily_run_last
    ON product.material_size_distributions("CompanyId","MaterialSubfamilyId","ProductSizeRunId","ProductLastId");

CREATE TABLE IF NOT EXISTS product.material_size_distribution_details (
    "Id"                          uuid                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "MaterialSizeDistributionId"  uuid                     NOT NULL REFERENCES product.material_size_distributions("Id") ON DELETE CASCADE,
    "ProductSizeRunSizeId"        uuid                     NOT NULL REFERENCES product.product_size_run_sizes("Id") ON DELETE RESTRICT,
    "MaterialItemId"              uuid                     REFERENCES product.material_items("Id") ON DELETE RESTRICT,
    "Notes"                       character varying(1200)  NOT NULL DEFAULT '',
    "IsActive"                    boolean                  NOT NULL DEFAULT true,
    "CreatedAt"                   timestamp with time zone NOT NULL DEFAULT now(),
    "CreatedBy"                   text,
    "UpdatedAt"                   timestamp with time zone,
    "UpdatedBy"                   text
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_material_size_distribution_details_dist_size
    ON product.material_size_distribution_details("MaterialSizeDistributionId","ProductSizeRunSizeId");
