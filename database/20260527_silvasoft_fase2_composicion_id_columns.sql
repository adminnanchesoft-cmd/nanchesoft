-- ─────────────────────────────────────────────────────────────────────────────
--  SilvaSoft Fase 2 — columnas de trazabilidad con SilvaSoft
--
--  material_families:    + silvasoft_composicion_id INT NULL
--  material_subfamilies: + silvasoft_clase_id INT NULL
--                        + silvasoft_composicion_id INT NULL
--
--  Estas columnas almacenan las PKs de SilvaSoft para:
--   · Detectar duplicados por ID (más confiable que por código)
--   · Vincular subfamilias con su familia padre vía composicionid
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE product.material_families
    ADD COLUMN IF NOT EXISTS silvasoft_composicion_id INT NULL;

ALTER TABLE product.material_subfamilies
    ADD COLUMN IF NOT EXISTS silvasoft_clase_id INT NULL,
    ADD COLUMN IF NOT EXISTS silvasoft_composicion_id INT NULL;

-- Índices para búsquedas rápidas por SilvaSoft ID (multi-tenant: por company)
CREATE INDEX IF NOT EXISTS ix_material_families_silvasoft_id
    ON product.material_families (company_id, silvasoft_composicion_id)
    WHERE silvasoft_composicion_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_material_subfamilies_silvasoft_clase_id
    ON product.material_subfamilies (company_id, silvasoft_clase_id)
    WHERE silvasoft_clase_id IS NOT NULL;
