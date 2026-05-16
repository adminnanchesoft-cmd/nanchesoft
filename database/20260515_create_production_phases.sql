-- ============================================================
-- Migración: Catálogo de Fases de Producción
-- Fecha: 2026-05-15
-- ============================================================

-- 1. Crear tabla production_phases en el schema product
CREATE TABLE IF NOT EXISTS product.production_phases (
    "Id"          UUID                     NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    "TenantId"    UUID                     NOT NULL REFERENCES core.tenants("Id") ON DELETE RESTRICT,
    "Code"        CHARACTER VARYING(40)    NOT NULL,
    "Name"        CHARACTER VARYING(140)   NOT NULL,
    "Description" CHARACTER VARYING(600)   NULL DEFAULT '',
    "Sequence"    INTEGER                  NOT NULL DEFAULT 0,
    "IsActive"    BOOLEAN                  NOT NULL DEFAULT TRUE,
    "CreatedAt"   TIMESTAMPTZ              NOT NULL DEFAULT NOW(),
    "CreatedBy"   CHARACTER VARYING(120)   NULL,
    "UpdatedAt"   TIMESTAMPTZ              NULL,
    "UpdatedBy"   CHARACTER VARYING(120)   NULL,
    CONSTRAINT "UQ_production_phases_TenantId_Code" UNIQUE ("TenantId", "Code")
);

-- 2. Agregar FK ProductionPhaseId en product_components
ALTER TABLE product.product_components
    ADD COLUMN IF NOT EXISTS "ProductionPhaseId" UUID NULL
        REFERENCES product.production_phases("Id") ON DELETE RESTRICT;

-- 3. Eliminar columnas de texto legacy
ALTER TABLE product.product_components
    DROP COLUMN IF EXISTS "ProductionPhase",
    DROP COLUMN IF EXISTS "WarehouseDeliveryRole";

-- 4. Seed de fases típicas de producción
DO $$
DECLARE
    v_tenant_id UUID;
BEGIN
    SELECT "Id" INTO v_tenant_id FROM core.tenants ORDER BY "CreatedAt" LIMIT 1;

    IF v_tenant_id IS NOT NULL THEN
        INSERT INTO product.production_phases
            ("Id", "TenantId", "Code", "Name", "Description", "Sequence", "IsActive", "CreatedAt", "CreatedBy")
        VALUES
            (gen_random_uuid(), v_tenant_id, 'CORTE',     'Corte',              'Corte de materiales y piezas según patrones.',         10, TRUE, NOW(), 'seed'),
            (gen_random_uuid(), v_tenant_id, 'COSTURA',   'Costura',            'Unión de piezas mediante costura manual o mecánica.',  20, TRUE, NOW(), 'seed'),
            (gen_random_uuid(), v_tenant_id, 'MONTADO',   'Montado',            'Armado y conformado del producto sobre la horma.',     30, TRUE, NOW(), 'seed'),
            (gen_random_uuid(), v_tenant_id, 'TERMINADO', 'Terminado',          'Acabado, limpieza y detallado final del producto.',    40, TRUE, NOW(), 'seed'),
            (gen_random_uuid(), v_tenant_id, 'C-CALIDAD', 'Control de calidad', 'Revisión e inspección de calidad antes del empaque.', 50, TRUE, NOW(), 'seed'),
            (gen_random_uuid(), v_tenant_id, 'EMPAQUE',   'Empaque',            'Empaque y etiquetado del producto terminado.',         60, TRUE, NOW(), 'seed')
        ON CONFLICT ("TenantId", "Code") DO NOTHING;
    END IF;
END;
$$;
