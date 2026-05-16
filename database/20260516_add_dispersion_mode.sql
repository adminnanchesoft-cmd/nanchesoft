-- Agrega dispersion_mode a consumption_template_details
-- "paired" = empareja tallas (Silvasoft Orange), "linear" = progresión lineal pura
ALTER TABLE product.consumption_template_details
    ADD COLUMN IF NOT EXISTS dispersion_mode VARCHAR(20) NOT NULL DEFAULT 'paired';
