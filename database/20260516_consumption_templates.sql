-- =============================================================
-- Consumption Templates
-- Reemplaza la lógica de ProductConsumptionProfile.
-- Consumo definido a nivel estilo + corrida (no producto terminado).
-- Columnas en snake_case para producción local (dev Azure usa PascalCase vía EF convention).
-- =============================================================

CREATE TABLE IF NOT EXISTS product.consumption_templates (
    id                   UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    company_id           UUID          NOT NULL REFERENCES core.companies(id) ON DELETE RESTRICT,
    product_style_id     UUID          NOT NULL REFERENCES product.product_styles(id) ON DELETE RESTRICT,
    product_size_run_id  UUID          NOT NULL REFERENCES product.product_size_runs(id) ON DELETE RESTRICT,
    is_active            BOOLEAN       NOT NULL DEFAULT TRUE,
    is_authorized        BOOLEAN       NOT NULL DEFAULT FALSE,
    created_at           TIMESTAMPTZ   NOT NULL DEFAULT now(),
    created_by           VARCHAR(120),
    updated_at           TIMESTAMPTZ,
    updated_by           VARCHAR(120),
    authorized_at        TIMESTAMPTZ,
    authorized_by        VARCHAR(120),
    notes                VARCHAR(1200) NOT NULL DEFAULT ''
);

-- Una sola plantilla activa por empresa + estilo + corrida
CREATE UNIQUE INDEX IF NOT EXISTS ix_consumption_templates_active_style_run
    ON product.consumption_templates (company_id, product_style_id, product_size_run_id)
    WHERE is_active = true;

-- =============================================================

CREATE TABLE IF NOT EXISTS product.consumption_template_details (
    id                        UUID          NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    consumption_template_id   UUID          NOT NULL REFERENCES product.consumption_templates(id) ON DELETE CASCADE,
    product_component_id      UUID          NOT NULL REFERENCES product.product_components(id) ON DELETE RESTRICT,
    pieces                    INTEGER       NOT NULL DEFAULT 1,
    is_active                 BOOLEAN       NOT NULL DEFAULT TRUE,
    created_at                TIMESTAMPTZ   NOT NULL DEFAULT now(),
    created_by                VARCHAR(120),
    updated_at                TIMESTAMPTZ,
    updated_by                VARCHAR(120),
    notes                     VARCHAR(1200) NOT NULL DEFAULT '',
    UNIQUE (consumption_template_id, product_component_id)
);

CREATE INDEX IF NOT EXISTS ix_ctd_template_id   ON product.consumption_template_details (consumption_template_id);
CREATE INDEX IF NOT EXISTS ix_ctd_component_id  ON product.consumption_template_details (product_component_id);

-- =============================================================

CREATE TABLE IF NOT EXISTS product.consumption_template_sizes (
    id                             UUID           NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    consumption_template_detail_id UUID           NOT NULL REFERENCES product.consumption_template_details(id) ON DELETE CASCADE,
    product_size_run_size_id       UUID           NOT NULL REFERENCES product.product_size_run_sizes(id) ON DELETE RESTRICT,
    consumption                    NUMERIC(18,4)  NOT NULL DEFAULT 0,
    is_active                      BOOLEAN        NOT NULL DEFAULT TRUE,
    created_at                     TIMESTAMPTZ    NOT NULL DEFAULT now(),
    created_by                     VARCHAR(120),
    updated_at                     TIMESTAMPTZ,
    updated_by                     VARCHAR(120),
    UNIQUE (consumption_template_detail_id, product_size_run_size_id)
);

CREATE INDEX IF NOT EXISTS ix_cts_detail_id        ON product.consumption_template_sizes (consumption_template_detail_id);
CREATE INDEX IF NOT EXISTS ix_cts_size_run_size_id ON product.consumption_template_sizes (product_size_run_size_id);
