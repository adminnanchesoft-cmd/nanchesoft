-- Agrega FK product_manufacturing_type_id a finished_products (columna faltante en DB local)
ALTER TABLE product.finished_products
    ADD COLUMN IF NOT EXISTS product_manufacturing_type_id uuid NULL;

ALTER TABLE product.finished_products
    DROP CONSTRAINT IF EXISTS fk_fp_manufacturing_type;

ALTER TABLE product.finished_products
    ADD CONSTRAINT fk_fp_manufacturing_type
    FOREIGN KEY (product_manufacturing_type_id)
    REFERENCES product.product_manufacturing_types(id)
    ON DELETE RESTRICT;
