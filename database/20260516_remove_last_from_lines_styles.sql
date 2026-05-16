-- ============================================================
-- Migration: Remove product_last_id from product_lines and product_styles
-- Date: 2026-05-16
-- Description:
--   La horma (ProductLast) ya no se gestiona en líneas ni estilos.
--   Solo aplica a producto terminado (finished_products).
-- ============================================================

ALTER TABLE product.product_lines  DROP COLUMN IF EXISTS product_last_id;
ALTER TABLE product.product_styles DROP COLUMN IF EXISTS product_last_id;
