-- =============================================================================
-- MIGRACIÓN: Módulo de Programación de Producción
-- Fecha:     2026-05-17
-- Schema:    production.*
-- =============================================================================

CREATE SCHEMA IF NOT EXISTS "production";

-- ---------------------------------------------------------------------------
-- 1. Celdas de producción (líneas / estaciones de trabajo)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_cells (
    id                      uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid            NOT NULL REFERENCES core.tenants(id),
    company_id              uuid            NOT NULL REFERENCES core.companies(id),
    branch_id               uuid            NOT NULL REFERENCES core.branches(id),
    production_phase_id     uuid            NOT NULL REFERENCES product.production_phases(id),
    code                    varchar(20)     NOT NULL,
    name                    varchar(120)    NOT NULL,
    capacity_per_day        int             NOT NULL DEFAULT 0,
    capacity_per_week       int             NOT NULL DEFAULT 0,
    notes                   varchar(1200),
    is_active               boolean         NOT NULL DEFAULT true,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120),
    CONSTRAINT uq_production_cells_code UNIQUE (tenant_id, company_id, code)
);

CREATE INDEX IF NOT EXISTS idx_production_cells_phase     ON production.production_cells(production_phase_id);
CREATE INDEX IF NOT EXISTS idx_production_cells_branch    ON production.production_cells(branch_id);

-- ---------------------------------------------------------------------------
-- 2. Empleados por celda
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_cell_employees (
    id                      uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    production_cell_id      uuid            NOT NULL REFERENCES production.production_cells(id) ON DELETE CASCADE,
    employee_id             uuid            NOT NULL REFERENCES hr.hr_employees(id),
    role                    varchar(30)     NOT NULL DEFAULT 'operator',
    assigned_date           date            NOT NULL,
    end_date                date,
    is_active               boolean         NOT NULL DEFAULT true,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_prod_cell_emp_cell     ON production.production_cell_employees(production_cell_id);
CREATE INDEX IF NOT EXISTS idx_prod_cell_emp_employee ON production.production_cell_employees(employee_id);

-- ---------------------------------------------------------------------------
-- 3. Órdenes de producción
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_orders (
    id                      uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid            NOT NULL REFERENCES core.tenants(id),
    company_id              uuid            NOT NULL REFERENCES core.companies(id),
    branch_id               uuid            NOT NULL REFERENCES core.branches(id),
    folio                   varchar(20)     NOT NULL,
    week_code               varchar(10)     NOT NULL,
    start_date              date            NOT NULL,
    end_date                date            NOT NULL,
    delivery_date           date            NOT NULL,
    status                  varchar(30)     NOT NULL DEFAULT 'draft',
    priority                smallint        NOT NULL DEFAULT 1,
    notes                   text,
    total_units_planned     int             NOT NULL DEFAULT 0,
    total_units_produced    int             NOT NULL DEFAULT 0,
    total_units_shipped     int             NOT NULL DEFAULT 0,
    explosion_status        varchar(20)     NOT NULL DEFAULT 'pending',
    approved_at             timestamptz,
    approved_by             varchar(120),
    closed_at               timestamptz,
    closed_by               varchar(120),
    is_active               boolean         NOT NULL DEFAULT true,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120),
    CONSTRAINT uq_production_orders_folio UNIQUE (tenant_id, company_id, folio)
);

CREATE INDEX IF NOT EXISTS idx_prod_orders_week      ON production.production_orders(tenant_id, company_id, week_code);
CREATE INDEX IF NOT EXISTS idx_prod_orders_status    ON production.production_orders(tenant_id, company_id, status);
CREATE INDEX IF NOT EXISTS idx_prod_orders_delivery  ON production.production_orders(tenant_id, company_id, delivery_date);

-- ---------------------------------------------------------------------------
-- 4. Líneas de la orden de producción
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_order_lines (
    id                              uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    production_order_id             uuid            NOT NULL REFERENCES production.production_orders(id) ON DELETE CASCADE,
    line_number                     smallint        NOT NULL,
    sales_order_id                  uuid            REFERENCES sales.sales_orders(id),
    sales_order_line_id             uuid            REFERENCES sales.sales_order_lines(id),
    finished_product_id             uuid            NOT NULL REFERENCES product.finished_products(id),
    product_style_id                uuid            REFERENCES product.product_styles(id),
    product_size_run_id             uuid            REFERENCES product.product_size_runs(id),
    product_last_id                 uuid            REFERENCES product.product_lasts(id),
    product_color_id                uuid            REFERENCES product.product_colors(id),
    product_sole_id                 uuid            REFERENCES product.product_soles(id),
    product_manufacturing_type_id   uuid            REFERENCES product.product_manufacturing_types(id),
    customer_id                     uuid            REFERENCES org.customers(id),
    quantities_per_size             jsonb           NOT NULL DEFAULT '{}',
    total_units_planned             int             NOT NULL DEFAULT 0,
    total_units_produced            int             NOT NULL DEFAULT 0,
    total_units_shipped             int             NOT NULL DEFAULT 0,
    total_units_pending             int             NOT NULL DEFAULT 0,
    status                          varchar(20)     NOT NULL DEFAULT 'pending',
    delivery_date                   date,
    priority                        smallint        NOT NULL DEFAULT 1,
    notes                           text,
    is_active                       boolean         NOT NULL DEFAULT true,
    created_at                      timestamptz     NOT NULL DEFAULT now(),
    created_by                      varchar(120),
    updated_at                      timestamptz,
    updated_by                      varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_prod_order_lines_order    ON production.production_order_lines(production_order_id);
CREATE INDEX IF NOT EXISTS idx_prod_order_lines_product  ON production.production_order_lines(finished_product_id);
CREATE INDEX IF NOT EXISTS idx_prod_order_lines_sale     ON production.production_order_lines(sales_order_id);
CREATE INDEX IF NOT EXISTS idx_prod_order_lines_qty_gin  ON production.production_order_lines USING gin(quantities_per_size);

-- ---------------------------------------------------------------------------
-- 5. Programación semanal
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_schedules (
    id                      uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid            NOT NULL REFERENCES core.tenants(id),
    company_id              uuid            NOT NULL REFERENCES core.companies(id),
    branch_id               uuid            NOT NULL REFERENCES core.branches(id),
    week_code               varchar(10)     NOT NULL,
    week_start_date         date            NOT NULL,
    week_end_date           date            NOT NULL,
    status                  varchar(20)     NOT NULL DEFAULT 'open',
    total_capacity_units    int             NOT NULL DEFAULT 0,
    total_scheduled_units   int             NOT NULL DEFAULT 0,
    total_produced_units    int             NOT NULL DEFAULT 0,
    load_percentage         numeric(5,2)    NOT NULL DEFAULT 0,
    notes                   text,
    locked_at               timestamptz,
    locked_by               varchar(120),
    closed_at               timestamptz,
    closed_by               varchar(120),
    is_active               boolean         NOT NULL DEFAULT true,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120),
    CONSTRAINT uq_production_schedules_week UNIQUE (tenant_id, company_id, branch_id, week_code)
);

-- ---------------------------------------------------------------------------
-- 6. Líneas de programación semanal
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_schedule_lines (
    id                          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    production_schedule_id      uuid        NOT NULL REFERENCES production.production_schedules(id) ON DELETE CASCADE,
    production_order_id         uuid        NOT NULL REFERENCES production.production_orders(id),
    production_order_line_id    uuid        NOT NULL REFERENCES production.production_order_lines(id),
    production_cell_id          uuid        REFERENCES production.production_cells(id),
    production_phase_id         uuid        NOT NULL REFERENCES product.production_phases(id),
    scheduled_date              date        NOT NULL,
    units_scheduled             int         NOT NULL DEFAULT 0,
    units_produced              int         NOT NULL DEFAULT 0,
    shift                       varchar(20) NOT NULL DEFAULT 'morning',
    notes                       varchar(1200),
    is_active                   boolean     NOT NULL DEFAULT true,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  varchar(120),
    updated_at                  timestamptz,
    updated_by                  varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_prod_sched_lines_schedule  ON production.production_schedule_lines(production_schedule_id);
CREATE INDEX IF NOT EXISTS idx_prod_sched_lines_order     ON production.production_schedule_lines(production_order_id);
CREATE INDEX IF NOT EXISTS idx_prod_sched_lines_phase_dt  ON production.production_schedule_lines(production_phase_id, scheduled_date);

-- ---------------------------------------------------------------------------
-- 7. Avance por fase (tracking)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_phase_progress (
    id                          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    production_order_id         uuid        NOT NULL REFERENCES production.production_orders(id) ON DELETE CASCADE,
    production_order_line_id    uuid        NOT NULL REFERENCES production.production_order_lines(id),
    production_phase_id         uuid        NOT NULL REFERENCES product.production_phases(id),
    units_planned               int         NOT NULL DEFAULT 0,
    units_in_progress           int         NOT NULL DEFAULT 0,
    units_completed             int         NOT NULL DEFAULT 0,
    units_rejected              int         NOT NULL DEFAULT 0,
    units_pending               int         NOT NULL DEFAULT 0,
    status                      varchar(20) NOT NULL DEFAULT 'pending',
    started_at                  timestamptz,
    completed_at                timestamptz,
    last_updated_at             timestamptz NOT NULL DEFAULT now(),
    rescheduled_count           smallint    NOT NULL DEFAULT 0,
    last_reschedule_reason      varchar(1000),
    is_active                   boolean     NOT NULL DEFAULT true,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  varchar(120),
    updated_at                  timestamptz,
    updated_by                  varchar(120),
    CONSTRAINT uq_phase_progress UNIQUE (production_order_id, production_order_line_id, production_phase_id)
);

CREATE INDEX IF NOT EXISTS idx_phase_progress_order ON production.production_phase_progress(production_order_id);

-- ---------------------------------------------------------------------------
-- 8. Vales de proceso (tarjetas de producción)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_vouchers (
    id                          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   uuid        NOT NULL REFERENCES core.tenants(id),
    company_id                  uuid        NOT NULL REFERENCES core.companies(id),
    production_order_id         uuid        NOT NULL REFERENCES production.production_orders(id),
    production_order_line_id    uuid        NOT NULL REFERENCES production.production_order_lines(id),
    production_phase_id         uuid        NOT NULL REFERENCES product.production_phases(id),
    production_cell_id          uuid        REFERENCES production.production_cells(id),
    folio                       varchar(30) NOT NULL,
    lot_number                  varchar(30),
    batch_size                  int         NOT NULL DEFAULT 0,
    status                      varchar(20) NOT NULL DEFAULT 'issued',
    issued_date                 date        NOT NULL,
    issued_by                   varchar(120),
    completed_date              date,
    completed_by                varchar(120),
    cancelled_date              date,
    cancelled_reason            varchar(500),
    printed                     boolean     NOT NULL DEFAULT false,
    printed_at                  timestamptz,
    print_count                 smallint    NOT NULL DEFAULT 0,
    notes                       text,
    is_active                   boolean     NOT NULL DEFAULT true,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  varchar(120),
    updated_at                  timestamptz,
    updated_by                  varchar(120),
    CONSTRAINT uq_production_vouchers_folio UNIQUE (tenant_id, company_id, folio)
);

CREATE INDEX IF NOT EXISTS idx_prod_vouchers_order    ON production.production_vouchers(production_order_id);
CREATE INDEX IF NOT EXISTS idx_prod_vouchers_phase_dt ON production.production_vouchers(production_phase_id, issued_date);

-- ---------------------------------------------------------------------------
-- 9. Detalles del vale de proceso
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_voucher_details (
    id                      uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    production_voucher_id   uuid        NOT NULL REFERENCES production.production_vouchers(id) ON DELETE CASCADE,
    employee_id             uuid        REFERENCES hr.hr_employees(id),
    size_run_size_id        uuid        REFERENCES product.product_size_run_sizes(id),
    quantity_assigned       int         NOT NULL DEFAULT 0,
    quantity_produced       int         NOT NULL DEFAULT 0,
    quantity_rejected       int         NOT NULL DEFAULT 0,
    operation_code          varchar(30),
    notes                   varchar(1200),
    is_active               boolean     NOT NULL DEFAULT true,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_prod_voucher_details_voucher  ON production.production_voucher_details(production_voucher_id);
CREATE INDEX IF NOT EXISTS idx_prod_voucher_details_emp      ON production.production_voucher_details(employee_id);

-- ---------------------------------------------------------------------------
-- 10. Tarifas de destajo por fase
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.piece_work_rates (
    id                      uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid            NOT NULL REFERENCES core.tenants(id),
    company_id              uuid            NOT NULL REFERENCES core.companies(id),
    production_phase_id     uuid            NOT NULL REFERENCES product.production_phases(id),
    effective_date          date            NOT NULL,
    price_per_unit          numeric(12,4)   NOT NULL DEFAULT 0,
    notes                   varchar(1200),
    is_active               boolean         NOT NULL DEFAULT true,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_piece_work_rates_phase ON production.piece_work_rates(company_id, production_phase_id, effective_date);

-- ---------------------------------------------------------------------------
-- 11. Registros de destajo (piecework)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.piece_work_records (
    id                      uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid            NOT NULL REFERENCES core.tenants(id),
    company_id              uuid            NOT NULL REFERENCES core.companies(id),
    employee_id             uuid            NOT NULL REFERENCES hr.hr_employees(id),
    production_voucher_id   uuid            REFERENCES production.production_vouchers(id),
    production_order_id     uuid            NOT NULL REFERENCES production.production_orders(id),
    production_phase_id     uuid            NOT NULL REFERENCES product.production_phases(id),
    payroll_period_id       uuid            REFERENCES payroll.payroll_periods(id),
    work_date               date            NOT NULL,
    units_produced          int             NOT NULL DEFAULT 0,
    units_rejected          int             NOT NULL DEFAULT 0,
    unit_price              numeric(12,4)   NOT NULL DEFAULT 0,
    gross_amount            numeric(12,4)   NOT NULL DEFAULT 0,
    quality_deduction       numeric(12,4)   NOT NULL DEFAULT 0,
    net_amount              numeric(12,4)   NOT NULL DEFAULT 0,
    status                  varchar(20)     NOT NULL DEFAULT 'pending',
    approved_by             varchar(120),
    approved_at             timestamptz,
    processed_by            varchar(120),
    processed_at            timestamptz,
    notes                   text,
    is_active               boolean         NOT NULL DEFAULT true,
    created_at              timestamptz     NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_piecework_emp_date   ON production.piece_work_records(employee_id, work_date);
CREATE INDEX IF NOT EXISTS idx_piecework_order      ON production.piece_work_records(production_order_id);
CREATE INDEX IF NOT EXISTS idx_piecework_period     ON production.piece_work_records(payroll_period_id);
CREATE INDEX IF NOT EXISTS idx_piecework_tenant_dt  ON production.piece_work_records(tenant_id, company_id, work_date);

-- ---------------------------------------------------------------------------
-- 12. Requerimientos de materiales (explosión guardada)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.material_requirements (
    id                      uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid        NOT NULL REFERENCES core.tenants(id),
    company_id              uuid        NOT NULL REFERENCES core.companies(id),
    production_order_id     uuid        REFERENCES production.production_orders(id),
    calculated_at           timestamptz NOT NULL DEFAULT now(),
    calculated_by           varchar(120),
    status                  varchar(20) NOT NULL DEFAULT 'draft',
    total_lines             int         NOT NULL DEFAULT 0,
    lines_with_shortage     int         NOT NULL DEFAULT 0,
    lines_fuly_covered      int         NOT NULL DEFAULT 0,
    notes                   text,
    is_active               boolean     NOT NULL DEFAULT true,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_mat_req_order  ON production.material_requirements(production_order_id);
CREATE INDEX IF NOT EXISTS idx_mat_req_status ON production.material_requirements(tenant_id, company_id, status);

-- ---------------------------------------------------------------------------
-- 13. Líneas del requerimiento de materiales
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.material_requirement_lines (
    id                          uuid            PRIMARY KEY DEFAULT gen_random_uuid(),
    material_requirement_id     uuid            NOT NULL REFERENCES production.material_requirements(id) ON DELETE CASCADE,
    production_order_line_id    uuid            REFERENCES production.production_order_lines(id),
    product_component_id        uuid            REFERENCES product.product_components(id),
    material_item_id            uuid            REFERENCES product.material_items(id),
    unit_id                     uuid            REFERENCES catalog.units(id),
    supplier_id                 uuid            REFERENCES org.suppliers(id),
    purchase_requisition_id     uuid            REFERENCES purchase.purchase_requisitions(id),
    purchase_order_id           uuid            REFERENCES purchase.purchase_orders(id),
    component_code              varchar(60),
    component_name              varchar(160),
    material_name               varchar(220),
    quantity_required           numeric(14,4)   NOT NULL DEFAULT 0,
    quantity_on_hand            numeric(14,4)   NOT NULL DEFAULT 0,
    quantity_reserved           numeric(14,4)   NOT NULL DEFAULT 0,
    quantity_to_reserve         numeric(14,4)   NOT NULL DEFAULT 0,
    quantity_shortage           numeric(14,4)   NOT NULL DEFAULT 0,
    quantity_on_order           numeric(14,4)   NOT NULL DEFAULT 0,
    unit_cost                   numeric(14,4)   NOT NULL DEFAULT 0,
    total_cost                  numeric(14,4)   NOT NULL DEFAULT 0,
    coverage_status             varchar(20)     NOT NULL DEFAULT 'unknown',
    reserved_at                 timestamptz,
    reserved_by                 varchar(120),
    is_active                   boolean         NOT NULL DEFAULT true,
    created_at                  timestamptz     NOT NULL DEFAULT now(),
    created_by                  varchar(120),
    updated_at                  timestamptz,
    updated_by                  varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_mat_req_lines_req      ON production.material_requirement_lines(material_requirement_id);
CREATE INDEX IF NOT EXISTS idx_mat_req_lines_item     ON production.material_requirement_lines(material_item_id);
CREATE INDEX IF NOT EXISTS idx_mat_req_lines_component ON production.material_requirement_lines(product_component_id);

-- ---------------------------------------------------------------------------
-- 14. En proceso (tracking en tiempo real)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.production_in_process (
    id                          uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id                   uuid        NOT NULL REFERENCES core.tenants(id),
    company_id                  uuid        NOT NULL REFERENCES core.companies(id),
    production_order_id         uuid        NOT NULL REFERENCES production.production_orders(id),
    production_order_line_id    uuid        NOT NULL REFERENCES production.production_order_lines(id),
    production_phase_id         uuid        NOT NULL REFERENCES product.production_phases(id),
    production_cell_id          uuid        REFERENCES production.production_cells(id),
    entry_date                  date        NOT NULL,
    units_entered               int         NOT NULL DEFAULT 0,
    units_exited                int         NOT NULL DEFAULT 0,
    units_rejected              int         NOT NULL DEFAULT 0,
    units_current               int         NOT NULL DEFAULT 0,
    entered_by                  varchar(120),
    notes                       varchar(1200),
    is_active                   boolean     NOT NULL DEFAULT true,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  varchar(120),
    updated_at                  timestamptz,
    updated_by                  varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_in_process_order_phase ON production.production_in_process(production_order_id, production_phase_id);
CREATE INDEX IF NOT EXISTS idx_in_process_phase_date  ON production.production_in_process(production_phase_id, entry_date);

-- ---------------------------------------------------------------------------
-- 15. Excedentes de producción
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.surplus_records (
    id                      uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid        NOT NULL REFERENCES core.tenants(id),
    company_id              uuid        NOT NULL REFERENCES core.companies(id),
    production_order_id     uuid        NOT NULL REFERENCES production.production_orders(id),
    finished_product_id     uuid        NOT NULL REFERENCES product.finished_products(id),
    size_run_size_id        uuid        REFERENCES product.product_size_run_sizes(id),
    units_planned           int         NOT NULL DEFAULT 0,
    units_produced          int         NOT NULL DEFAULT 0,
    units_surplus           int         NOT NULL DEFAULT 0,
    disposition             varchar(20) NOT NULL DEFAULT 'pending',
    assigned_order_id       uuid        REFERENCES production.production_orders(id),
    warehouse_entry_id      uuid,
    notes                   text,
    resolved_at             timestamptz,
    resolved_by             varchar(120),
    is_active               boolean     NOT NULL DEFAULT true,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_surplus_order   ON production.surplus_records(production_order_id);
CREATE INDEX IF NOT EXISTS idx_surplus_product ON production.surplus_records(finished_product_id);

-- ---------------------------------------------------------------------------
-- 16. Restricciones de fase (NoPasa)
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS production.phase_restrictions (
    id                      uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id               uuid        NOT NULL REFERENCES core.tenants(id),
    company_id              uuid        NOT NULL REFERENCES core.companies(id),
    finished_product_id     uuid        REFERENCES product.finished_products(id),
    product_style_id        uuid        REFERENCES product.product_styles(id),
    production_phase_id     uuid        NOT NULL REFERENCES product.production_phases(id),
    restriction_type        varchar(30) NOT NULL DEFAULT 'style_phase',
    reason                  varchar(500),
    is_active               boolean     NOT NULL DEFAULT true,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              varchar(120),
    updated_at              timestamptz,
    updated_by              varchar(120)
);

CREATE INDEX IF NOT EXISTS idx_phase_restrictions_phase ON production.phase_restrictions(company_id, production_phase_id);

-- =============================================================================
-- COMENTARIOS DE TABLAS
-- =============================================================================
COMMENT ON TABLE production.production_cells           IS 'Celdas o líneas de producción por fábrica y fase';
COMMENT ON TABLE production.production_cell_employees  IS 'Empleados asignados a cada celda de producción';
COMMENT ON TABLE production.production_orders          IS 'Órdenes de producción — agrupa pedidos a producir en una semana';
COMMENT ON TABLE production.production_order_lines     IS 'Líneas de la OP — un producto con cantidades por talla';
COMMENT ON TABLE production.production_schedules       IS 'Programación semanal — semana ISO con capacidad y carga';
COMMENT ON TABLE production.production_schedule_lines  IS 'Asignación de líneas de OP a días y fases de la semana';
COMMENT ON TABLE production.production_phase_progress  IS 'Avance por fase para cada línea de la OP';
COMMENT ON TABLE production.production_vouchers        IS 'Vales/tarjetas de proceso — lote de pares por fase';
COMMENT ON TABLE production.production_voucher_details IS 'Detalle del vale por empleado y talla';
COMMENT ON TABLE production.piece_work_rates           IS 'Tarifas de destajo por fase y fecha vigencia';
COMMENT ON TABLE production.piece_work_records         IS 'Registros de producción a destajo por empleado';
COMMENT ON TABLE production.material_requirements      IS 'Resultado guardado de la explosión de materiales por OP';
COMMENT ON TABLE production.material_requirement_lines IS 'Línea de requerimiento por componente/material';
COMMENT ON TABLE production.production_in_process      IS 'Unidades en proceso por fase en tiempo real';
COMMENT ON TABLE production.surplus_records            IS 'Excedentes de producción vs. plan';
COMMENT ON TABLE production.phase_restrictions         IS 'Restricciones NoPasa: productos/estilos bloqueados por fase';
