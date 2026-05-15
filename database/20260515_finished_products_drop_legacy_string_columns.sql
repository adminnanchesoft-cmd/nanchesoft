-- Migration: drop legacy string columns from product.finished_products
-- Removes: color_name, model_name, folio_pattern
-- These were placeholder string fields replaced by FK columns added in
-- 20260514_finished_products_add_fk_columns.sql (product_color_id, item_model_id,
-- product_folio_pattern_id). model_name and folio_pattern were NOT NULL without
-- a default value, causing EF Core INSERTs to fail with a 500 error.
-- Safe to run: table has 0 rows at time of this migration.
-- Idempotent.

DO $$
BEGIN

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='color_name') THEN
        ALTER TABLE product.finished_products DROP COLUMN color_name;
        RAISE NOTICE 'Dropped column color_name from product.finished_products';
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='model_name') THEN
        ALTER TABLE product.finished_products DROP COLUMN model_name;
        RAISE NOTICE 'Dropped column model_name from product.finished_products';
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='folio_pattern') THEN
        ALTER TABLE product.finished_products DROP COLUMN folio_pattern;
        RAISE NOTICE 'Dropped column folio_pattern from product.finished_products';
    END IF;

    RAISE NOTICE 'finished_products legacy string columns migration complete.';
END;
$$;
