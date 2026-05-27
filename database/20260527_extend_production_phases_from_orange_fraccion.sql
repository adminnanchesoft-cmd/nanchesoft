-- ─────────────────────────────────────────────────────────────────────────────
-- Nanchesoft - Extensión de production_phases con atributos de Orange Fraccion
-- Migra los conceptos de dbo.Fraccion y dbo.Fraccion_Cadena de Silvasoft Orange
-- a la entidad ProductionPhase de Nanchesoft.
-- Incluye regla 1:1 de auto-replicación con detección de ciclos por trigger.
-- ─────────────────────────────────────────────────────────────────────────────

CREATE SCHEMA IF NOT EXISTS product;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ─────────────────────────────────────────────────────────────────────────────
-- 1. EXTENDER product.production_phases
-- ─────────────────────────────────────────────────────────────────────────────

ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS company_id uuid NULL;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS clave_number integer NOT NULL DEFAULT 0;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS base_cost numeric(18,4) NOT NULL DEFAULT 0;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS cost_type varchar(20) NOT NULL DEFAULT 'fixed';
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS phase_group_id uuid NULL;

ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS show_on_production_card boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS print_barcode boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS generate_for_all_products boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS registers_progress boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS is_payable boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS is_piece_work_payable boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS generate_from_transfer_out boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS applies_to_all_products boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS tracks_produced_quantity boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS include_in_projection boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS affects_inventory boolean NOT NULL DEFAULT false;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS tracks_quantity_in_transfer_out boolean NOT NULL DEFAULT false;

ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS branch_key integer NOT NULL DEFAULT 0;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS piece_work_location_id uuid NULL;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS factory_branch_id uuid NULL;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS pre_payroll_classification_id uuid NULL;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS manufacturing_type_id uuid NULL;
ALTER TABLE product.production_phases ADD COLUMN IF NOT EXISTS silvasoft_fraccion_id uuid NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- 2. FOREIGN KEYS de production_phases
-- ─────────────────────────────────────────────────────────────────────────────

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_production_phases_tenant') THEN
        ALTER TABLE product.production_phases
            ADD CONSTRAINT fk_production_phases_tenant
            FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_production_phases_company') THEN
        ALTER TABLE product.production_phases
            ADD CONSTRAINT fk_production_phases_company
            FOREIGN KEY (company_id) REFERENCES core.companies(id) ON DELETE RESTRICT;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_production_phases_group') THEN
        ALTER TABLE product.production_phases
            ADD CONSTRAINT fk_production_phases_group
            FOREIGN KEY (phase_group_id) REFERENCES product.production_phases(id) ON DELETE SET NULL;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='inventory' AND table_name='warehouses')
       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_production_phases_piece_work_location') THEN
        ALTER TABLE product.production_phases
            ADD CONSTRAINT fk_production_phases_piece_work_location
            FOREIGN KEY (piece_work_location_id) REFERENCES inventory.warehouses(id) ON DELETE SET NULL;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='core' AND table_name='branches')
       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_production_phases_factory_branch') THEN
        ALTER TABLE product.production_phases
            ADD CONSTRAINT fk_production_phases_factory_branch
            FOREIGN KEY (factory_branch_id) REFERENCES core.branches(id) ON DELETE SET NULL;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='product' AND table_name='product_manufacturing_types')
       AND NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_production_phases_manufacturing_type') THEN
        ALTER TABLE product.production_phases
            ADD CONSTRAINT fk_production_phases_manufacturing_type
            FOREIGN KEY (manufacturing_type_id) REFERENCES product.product_manufacturing_types(id) ON DELETE SET NULL;
    END IF;
END $$;

-- ─────────────────────────────────────────────────────────────────────────────
-- 3. ÍNDICES de production_phases
-- ─────────────────────────────────────────────────────────────────────────────

CREATE UNIQUE INDEX IF NOT EXISTS ux_production_phases_company_code
    ON product.production_phases(company_id, code)
    WHERE company_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_production_phases_company_active
    ON product.production_phases(company_id, is_active);

CREATE INDEX IF NOT EXISTS ix_production_phases_clave_number
    ON product.production_phases(company_id, clave_number);

CREATE INDEX IF NOT EXISTS ix_production_phases_silvasoft
    ON product.production_phases(silvasoft_fraccion_id)
    WHERE silvasoft_fraccion_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS ix_production_phases_phase_group
    ON product.production_phases(phase_group_id)
    WHERE phase_group_id IS NOT NULL;

-- ─────────────────────────────────────────────────────────────────────────────
-- 4. CREAR product.production_phase_auto_links
--    (regla de auto-replicación de destajos, equivalente a Fraccion_Cadena)
-- ─────────────────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS product.production_phase_auto_links (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   uuid NOT NULL,
    company_id                  uuid NOT NULL,

    -- from_phase_id: la fracción que DISPARA la replicación
    from_phase_id               uuid NOT NULL,

    -- to_phase_id: la fracción que se GENERA automáticamente con la misma cantidad
    to_phase_id                 uuid NOT NULL,

    -- Célula de producción default a asignar al destajo auto-generado
    default_production_cell_id  uuid NULL,

    -- Célula que captura el destajo (opcional, sobrescribe la default si se especifica)
    capture_production_cell_id  uuid NULL,

    -- Mapeo opcional con Silvasoft (para sincronización vía SilvaSoftAgent)
    silvasoft_chain_id          uuid NULL,

    is_active                   boolean NOT NULL DEFAULT true,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  varchar(120) NOT NULL DEFAULT '',
    updated_at                  timestamptz NULL,
    updated_by                  varchar(120) NOT NULL DEFAULT ''
);

-- ─────────────────────────────────────────────────────────────────────────────
-- 5. CONSTRAINTS DE production_phase_auto_links
-- ─────────────────────────────────────────────────────────────────────────────

-- UNIQUE en (company_id, from_phase_id): FUERZA la regla 1:1
-- Una fracción solo puede tener UNA fracción destino de auto-replicación.
CREATE UNIQUE INDEX IF NOT EXISTS ux_phase_auto_links_company_from
    ON product.production_phase_auto_links(company_id, from_phase_id);

CREATE INDEX IF NOT EXISTS ix_phase_auto_links_to
    ON product.production_phase_auto_links(to_phase_id);

CREATE INDEX IF NOT EXISTS ix_phase_auto_links_silvasoft
    ON product.production_phase_auto_links(silvasoft_chain_id)
    WHERE silvasoft_chain_id IS NOT NULL;

ALTER TABLE product.production_phase_auto_links
    ADD CONSTRAINT fk_phase_auto_links_tenant
    FOREIGN KEY (tenant_id) REFERENCES core.tenants(id) ON DELETE RESTRICT;

ALTER TABLE product.production_phase_auto_links
    ADD CONSTRAINT fk_phase_auto_links_company
    FOREIGN KEY (company_id) REFERENCES core.companies(id) ON DELETE RESTRICT;

ALTER TABLE product.production_phase_auto_links
    ADD CONSTRAINT fk_phase_auto_links_from_phase
    FOREIGN KEY (from_phase_id) REFERENCES product.production_phases(id) ON DELETE CASCADE;

ALTER TABLE product.production_phase_auto_links
    ADD CONSTRAINT fk_phase_auto_links_to_phase
    FOREIGN KEY (to_phase_id) REFERENCES product.production_phases(id) ON DELETE CASCADE;

DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='production' AND table_name='production_cells') THEN
        ALTER TABLE product.production_phase_auto_links
            ADD CONSTRAINT fk_phase_auto_links_default_cell
            FOREIGN KEY (default_production_cell_id) REFERENCES production.production_cells(id) ON DELETE SET NULL;
        ALTER TABLE product.production_phase_auto_links
            ADD CONSTRAINT fk_phase_auto_links_capture_cell
            FOREIGN KEY (capture_production_cell_id) REFERENCES production.production_cells(id) ON DELETE SET NULL;
    END IF;
END $$;

-- No-self-loop básico (A → A no tiene sentido)
ALTER TABLE product.production_phase_auto_links
    ADD CONSTRAINT chk_phase_auto_links_no_self_loop
    CHECK (from_phase_id <> to_phase_id);

-- ─────────────────────────────────────────────────────────────────────────────
-- 6. TRIGGER de DETECCIÓN DE CICLOS
--    Previene A→B→C→A en INSERT/UPDATE recorriendo la cadena hacia adelante.
-- ─────────────────────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION product.check_phase_auto_link_no_cycle()
RETURNS TRIGGER AS $$
DECLARE
    cursor_phase uuid;
    next_phase uuid;
    depth integer := 0;
    max_depth integer := 100; -- safety cap (no debería pasar de 10-15 en la práctica)
BEGIN
    -- Empezamos en el destino del nuevo enlace y seguimos la cadena.
    -- Si en algún punto encontramos el ORIGEN, hay un ciclo.
    cursor_phase := NEW.to_phase_id;

    WHILE cursor_phase IS NOT NULL AND depth < max_depth LOOP
        -- ¿La cadena nos lleva de vuelta al origen?
        IF cursor_phase = NEW.from_phase_id THEN
            RAISE EXCEPTION 'Ciclo detectado en production_phase_auto_links: la cadena que inicia en % regresa al origen (intentando insertar % → %)',
                NEW.from_phase_id, NEW.from_phase_id, NEW.to_phase_id
                USING ERRCODE = '23514';
        END IF;

        -- ¿La fracción actual tiene a su vez una regla de auto-replicación?
        SELECT to_phase_id INTO next_phase
        FROM product.production_phase_auto_links
        WHERE from_phase_id = cursor_phase
          AND company_id = NEW.company_id
          AND id <> COALESCE(NEW.id, '00000000-0000-0000-0000-000000000000'::uuid);

        cursor_phase := next_phase;
        depth := depth + 1;
    END LOOP;

    IF depth >= max_depth THEN
        RAISE EXCEPTION 'Cadena de auto-replicación demasiado profunda (>= % niveles) iniciando en %',
            max_depth, NEW.from_phase_id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_phase_auto_links_no_cycle ON product.production_phase_auto_links;

CREATE TRIGGER trg_phase_auto_links_no_cycle
    BEFORE INSERT OR UPDATE ON product.production_phase_auto_links
    FOR EACH ROW
    EXECUTE FUNCTION product.check_phase_auto_link_no_cycle();

-- ─────────────────────────────────────────────────────────────────────────────
-- 7. COMENTARIOS de documentación
-- ─────────────────────────────────────────────────────────────────────────────

COMMENT ON TABLE product.production_phase_auto_links IS
'Regla de auto-replicación de destajos entre fases productivas.
Cuando se captura un destajo en from_phase_id, el sistema DEBE generar
automáticamente otro destajo en to_phase_id con la MISMA cantidad,
asignado a default_production_cell_id (o capture_production_cell_id si
se especifica). La regla es 1:1 (UNIQUE en company_id+from_phase_id)
y es recursiva (A→B, B→C, etc.) pero NO permite ciclos (trigger).
Equivalente a dbo.Fraccion_Cadena de Silvasoft Orange.';

COMMENT ON COLUMN product.production_phase_auto_links.from_phase_id IS
'Fase principal cuya captura de destajo DISPARA la auto-replicación.';

COMMENT ON COLUMN product.production_phase_auto_links.to_phase_id IS
'Fase secundaria que se auto-genera con la misma cantidad cuando se captura un destajo en from_phase_id.';

COMMENT ON COLUMN product.production_phases.silvasoft_fraccion_id IS
'FraccionID original de Silvasoft Orange para sincronización con SilvaSoftAgent.';

COMMENT ON COLUMN product.production_phases.clave_number IS
'Clave numérica corta visible al usuario (equivalente a Fraccion.Clave en Orange).';
