-- Phase 3: finished_product_supplies + finished_product_supply_sizes
-- Phase 6: material_size_distributions + material_size_distribution_details

CREATE TABLE IF NOT EXISTS product.finished_product_supplies (
    id                   UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    is_active            BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by           VARCHAR(120),
    updated_at           TIMESTAMPTZ,
    updated_by           VARCHAR(120),
    tenant_id            UUID        NOT NULL,
    company_id           UUID        NOT NULL,
    finished_product_id  UUID        NOT NULL REFERENCES product.finished_products(id) ON DELETE CASCADE,
    product_component_id UUID        NOT NULL REFERENCES product.product_components(id) ON DELETE RESTRICT,
    is_authorized        BOOLEAN     NOT NULL DEFAULT FALSE,
    authorized_at        TIMESTAMPTZ,
    authorized_by        VARCHAR(120),
    notes                VARCHAR(1200) NOT NULL DEFAULT '',
    CONSTRAINT uq_fps_product_component UNIQUE (finished_product_id, product_component_id)
);

CREATE TABLE IF NOT EXISTS product.finished_product_supply_sizes (
    id                          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    is_active                   BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by                  VARCHAR(120),
    updated_at                  TIMESTAMPTZ,
    updated_by                  VARCHAR(120),
    finished_product_supply_id  UUID        NOT NULL REFERENCES product.finished_product_supplies(id) ON DELETE CASCADE,
    product_size_run_size_id    UUID        NOT NULL REFERENCES product.product_size_run_sizes(id) ON DELETE RESTRICT,
    material_item_id            UUID        REFERENCES product.material_items(id) ON DELETE RESTRICT,
    notes                       VARCHAR(1200) NOT NULL DEFAULT '',
    CONSTRAINT uq_fpss_supply_size UNIQUE (finished_product_supply_id, product_size_run_size_id)
);

CREATE TABLE IF NOT EXISTS product.material_size_distributions (
    id                      UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    is_active               BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by              VARCHAR(120),
    updated_at              TIMESTAMPTZ,
    updated_by              VARCHAR(120),
    tenant_id               UUID        NOT NULL,
    company_id              UUID        NOT NULL,
    material_subfamily_id   UUID        NOT NULL REFERENCES product.material_subfamilies(id) ON DELETE RESTRICT,
    product_size_run_id     UUID        NOT NULL REFERENCES product.product_size_runs(id) ON DELETE RESTRICT,
    product_last_id         UUID        REFERENCES product.product_lasts(id) ON DELETE RESTRICT,
    notes                   VARCHAR(1200) NOT NULL DEFAULT '',
    CONSTRAINT uq_msd_company_subfamily_run_last UNIQUE (company_id, material_subfamily_id, product_size_run_id, product_last_id)
);

CREATE TABLE IF NOT EXISTS product.material_size_distribution_details (
    id                          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    is_active                   BOOLEAN     NOT NULL DEFAULT TRUE,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by                  VARCHAR(120),
    updated_at                  TIMESTAMPTZ,
    updated_by                  VARCHAR(120),
    material_size_distribution_id UUID      NOT NULL REFERENCES product.material_size_distributions(id) ON DELETE CASCADE,
    product_size_run_size_id    UUID        NOT NULL REFERENCES product.product_size_run_sizes(id) ON DELETE RESTRICT,
    material_item_id            UUID        REFERENCES product.material_items(id) ON DELETE RESTRICT,
    notes                       VARCHAR(1200) NOT NULL DEFAULT '',
    CONSTRAINT uq_msdd_dist_size UNIQUE (material_size_distribution_id, product_size_run_size_id)
);
