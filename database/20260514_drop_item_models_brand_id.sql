-- Migration: remove brand_id from item_models
-- Reason: ItemModel no longer references ItemBrand; the unique index is (company_id, code).
-- Run once in pgAdmin. Idempotent: checks existence before dropping.

DO $$
BEGIN
    -- Drop the foreign-key constraint if it still exists
    IF EXISTS (
        SELECT 1
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage kcu
            ON tc.constraint_name = kcu.constraint_name
            AND tc.table_schema   = kcu.table_schema
        WHERE tc.constraint_type = 'FOREIGN KEY'
          AND tc.table_schema    = 'product'
          AND tc.table_name      = 'item_models'
          AND kcu.column_name    = 'brand_id'
    ) THEN
        -- Find and drop the actual constraint name dynamically
        DECLARE v_constraint text;
        BEGIN
            SELECT tc.constraint_name
              INTO v_constraint
              FROM information_schema.table_constraints tc
              JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
               AND tc.table_schema   = kcu.table_schema
             WHERE tc.constraint_type = 'FOREIGN KEY'
               AND tc.table_schema    = 'product'
               AND tc.table_name      = 'item_models'
               AND kcu.column_name    = 'brand_id'
             LIMIT 1;

            EXECUTE format('ALTER TABLE product.item_models DROP CONSTRAINT %I', v_constraint);
        END;
    END IF;

    -- Drop the column if it still exists
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'product'
          AND table_name   = 'item_models'
          AND column_name  = 'brand_id'
    ) THEN
        ALTER TABLE product.item_models DROP COLUMN brand_id;
        RAISE NOTICE 'Column brand_id dropped from product.item_models';
    ELSE
        RAISE NOTICE 'Column brand_id does not exist in product.item_models — nothing to do';
    END IF;
END;
$$;
