-- Migration: fix timestamp type mismatch in product.product_variants
-- Columns created_at and updated_at were created as "timestamp without time zone"
-- while BaseEntity.CreatedAt writes DateTime.UtcNow (Kind=Utc).
-- Npgsql 6+ rejects writing UTC DateTime to timestamp without time zone,
-- causing HTTP 500 on any SaveChanges that touches ProductVariant rows.
-- All other product.* tables already use timestamptz. This aligns the schema.
-- Idempotent: checks column type before altering.

DO $$
BEGIN

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'product'
          AND table_name   = 'product_variants'
          AND column_name  = 'created_at'
          AND data_type    = 'timestamp without time zone'
    ) THEN
        ALTER TABLE product.product_variants
            ALTER COLUMN created_at TYPE timestamp with time zone
            USING created_at AT TIME ZONE 'UTC';
        RAISE NOTICE 'product_variants.created_at migrated to timestamptz';
    END IF;

    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'product'
          AND table_name   = 'product_variants'
          AND column_name  = 'updated_at'
          AND data_type    = 'timestamp without time zone'
    ) THEN
        ALTER TABLE product.product_variants
            ALTER COLUMN updated_at TYPE timestamp with time zone
            USING updated_at AT TIME ZONE 'UTC';
        RAISE NOTICE 'product_variants.updated_at migrated to timestamptz';
    END IF;

    RAISE NOTICE 'product_variants timestamp migration complete.';
END;
$$;
