-- ============================================================
-- Migration: Material Characteristics and Sizes
-- Date: 2026-05-16
-- Description:
--   1. Creates product.material_characteristics catalog
--   2. Creates product.material_sizes catalog
--   3. Adds material_characteristic_id and material_size_id to
--      product.material_items with auto-name generation support
--   4. Adds unique index on (company_id, name) to material_items
--   5. Adds unique partial index on (company_id, characteristic, size)
-- ============================================================

-- 1. material_characteristics
CREATE TABLE IF NOT EXISTS product.material_characteristics (
    id                  uuid            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id           uuid            NOT NULL,
    company_id          uuid            NOT NULL,
    code                varchar(60)     NOT NULL,
    name                varchar(140)    NOT NULL,
    description         varchar(600)    NOT NULL DEFAULT '',
    is_active           boolean         NOT NULL DEFAULT true,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          varchar(120),
    updated_at          timestamptz,
    updated_by          varchar(120),
    CONSTRAINT pk_material_characteristics PRIMARY KEY (id),
    CONSTRAINT uq_material_characteristics_company_code UNIQUE (company_id, code)
);

-- 2. material_sizes
CREATE TABLE IF NOT EXISTS product.material_sizes (
    id                  uuid            NOT NULL DEFAULT gen_random_uuid(),
    tenant_id           uuid            NOT NULL,
    company_id          uuid            NOT NULL,
    code                varchar(60)     NOT NULL,
    name                varchar(80)     NOT NULL,
    description         varchar(600)    NOT NULL DEFAULT '',
    sort_order          integer         NOT NULL DEFAULT 0,
    is_active           boolean         NOT NULL DEFAULT true,
    created_at          timestamptz     NOT NULL DEFAULT now(),
    created_by          varchar(120),
    updated_at          timestamptz,
    updated_by          varchar(120),
    CONSTRAINT pk_material_sizes PRIMARY KEY (id),
    CONSTRAINT uq_material_sizes_company_code UNIQUE (company_id, code)
);

-- 3. Add FK columns to material_items
ALTER TABLE product.material_items
    ADD COLUMN IF NOT EXISTS material_characteristic_id uuid,
    ADD COLUMN IF NOT EXISTS material_size_id           uuid;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_material_items_characteristic'
          AND table_schema = 'product' AND table_name = 'material_items'
    ) THEN
        ALTER TABLE product.material_items
            ADD CONSTRAINT fk_material_items_characteristic
            FOREIGN KEY (material_characteristic_id)
            REFERENCES product.material_characteristics (id)
            ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_material_items_size'
          AND table_schema = 'product' AND table_name = 'material_items'
    ) THEN
        ALTER TABLE product.material_items
            ADD CONSTRAINT fk_material_items_size
            FOREIGN KEY (material_size_id)
            REFERENCES product.material_sizes (id)
            ON DELETE RESTRICT;
    END IF;
END $$;

-- 4. Unique index on (company_id, name) for material_items
CREATE UNIQUE INDEX IF NOT EXISTS ix_material_items_company_name
    ON product.material_items (company_id, name);

-- 5. Unique partial index on (company_id, characteristic_id, size_id)
--    Only applies when both FK columns are set (not null)
CREATE UNIQUE INDEX IF NOT EXISTS ix_material_items_characteristic_size
    ON product.material_items (company_id, material_characteristic_id, material_size_id)
    WHERE material_characteristic_id IS NOT NULL
      AND material_size_id IS NOT NULL;
