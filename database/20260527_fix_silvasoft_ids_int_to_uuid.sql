-- ─────────────────────────────────────────────────────────────────────────────
-- Corrección: silvasoft_composicion_id y silvasoft_clase_id eran INT pero
-- los PKs de SilvaSoft Orange son uniqueidentifier (GUID). Se reconvierten a uuid.
-- Los datos previos en esas columnas son inválidos (INT no puede almacenar GUIDs)
-- por lo que se descartan con DROP + ADD.
-- ─────────────────────────────────────────────────────────────────────────────

-- Eliminar índices que dependen de las columnas INT
DROP INDEX IF EXISTS product.ix_material_families_silvasoft_id;
DROP INDEX IF EXISTS product.ix_material_subfamilies_silvasoft_clase_id;

-- material_families: recrear silvasoft_composicion_id como uuid
ALTER TABLE product.material_families DROP COLUMN IF EXISTS silvasoft_composicion_id;
ALTER TABLE product.material_families ADD COLUMN silvasoft_composicion_id uuid NULL;

-- material_subfamilies: recrear ambas columnas como uuid
ALTER TABLE product.material_subfamilies DROP COLUMN IF EXISTS silvasoft_clase_id;
ALTER TABLE product.material_subfamilies DROP COLUMN IF EXISTS silvasoft_composicion_id;
ALTER TABLE product.material_subfamilies ADD COLUMN silvasoft_clase_id uuid NULL;
ALTER TABLE product.material_subfamilies ADD COLUMN silvasoft_composicion_id uuid NULL;

-- Recrear índices
CREATE INDEX IF NOT EXISTS ix_material_families_silvasoft_id
    ON product.material_families(company_id, silvasoft_composicion_id)
    WHERE silvasoft_composicion_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_material_subfamilies_silvasoft_clase_id
    ON product.material_subfamilies(company_id, silvasoft_clase_id)
    WHERE silvasoft_clase_id IS NOT NULL;
