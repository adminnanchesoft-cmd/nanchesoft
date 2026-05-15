-- Migration: add catalog FK columns to product.finished_products
-- Adds: item_model_id, item_brand_id, product_leather_type_id, product_color_id,
--       product_toe_cap_id, product_sole_id, product_sole_color_id, product_folio_pattern_id
-- Also: make name nullable (was NOT NULL with default '').
-- Removes: color_name, model_name, folio_pattern string columns (ADD ONLY this run — DROP in separate migration once UI is validated).
-- Run once in pgAdmin. Idempotent.

DO $$
BEGIN

    -- Add new FK columns (nullable, no default needed)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='item_model_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN item_model_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_item_model FOREIGN KEY (item_model_id) REFERENCES product.item_models(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='item_brand_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN item_brand_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_item_brand FOREIGN KEY (item_brand_id) REFERENCES product.item_brands(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='product_leather_type_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN product_leather_type_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_leather_type FOREIGN KEY (product_leather_type_id) REFERENCES product.product_leather_types(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='product_color_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN product_color_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_color FOREIGN KEY (product_color_id) REFERENCES product.product_colors(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='product_toe_cap_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN product_toe_cap_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_toe_cap FOREIGN KEY (product_toe_cap_id) REFERENCES product.product_toe_caps(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='product_sole_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN product_sole_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_sole FOREIGN KEY (product_sole_id) REFERENCES product.product_soles(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='product_sole_color_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN product_sole_color_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_sole_color FOREIGN KEY (product_sole_color_id) REFERENCES product.product_sole_colors(id) ON DELETE RESTRICT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema='product' AND table_name='finished_products' AND column_name='product_folio_pattern_id') THEN
        ALTER TABLE product.finished_products ADD COLUMN product_folio_pattern_id uuid;
        ALTER TABLE product.finished_products ADD CONSTRAINT fk_fp_folio_pattern FOREIGN KEY (product_folio_pattern_id) REFERENCES product.product_folio_patterns(id) ON DELETE RESTRICT;
    END IF;

    -- Make name nullable (was NOT NULL '' — drop the NOT NULL constraint)
    BEGIN
        ALTER TABLE product.finished_products ALTER COLUMN name DROP NOT NULL;
    EXCEPTION WHEN OTHERS THEN
        NULL; -- already nullable, ignore
    END;

    RAISE NOTICE 'finished_products FK columns migration complete.';
END;
$$;
