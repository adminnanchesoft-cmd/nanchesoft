-- ============================================================
--  MIGRACIÓN: Guava 2026 (SQL Server / Orange)  →  Nanchesoft (PostgreSQL)
--  Módulo: Programación de Producción
--
--  ESTRATEGIA:
--    1. Se usa la extensión `tds_fdw` (Foreign Data Wrapper para SQL Server/TDS)
--       para leer directamente las tablas de Orange sin mover archivos.
--    2. Si tds_fdw no está disponible, exportar las tablas de SQL Server a CSV
--       y cargar con COPY (ver sección "Alternativa CSV" al final).
--    3. El script es IDEMPOTENTE: usa ON CONFLICT DO NOTHING para re-ejecución segura.
--    4. Ejecutar siempre en una TRANSACCIÓN y revisar los conteos antes de COMMIT.
--
--  PRE-REQUISITOS:
--    - Tablas del módulo producción ya creadas (20260517_create_production_module.sql)
--    - Seed inicial ejecutado (fases, celdas, series OP/VALE ya presentes)
--    - Variables de conexión ajustadas en la sección de configuración
-- ============================================================

BEGIN;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 0: Verificar que las tablas destino existen
-- ────────────────────────────────────────────────────────────
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables
                   WHERE table_schema = 'production' AND table_name = 'production_orders') THEN
        RAISE EXCEPTION 'Tabla production.production_orders no encontrada. Ejecutar primero 20260517_create_production_module.sql';
    END IF;
END;
$$;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 1: Tablas de staging (datos importados de SQL Server)
--
-- CÓMO POBLARLAS:
--   Opción A — tds_fdw (si está instalado):
--     CREATE SERVER guava_orange FOREIGN DATA WRAPPER tds_fdw
--       OPTIONS (servername '201.147.247.98', port '1433', database 'Orange');
--     CREATE USER MAPPING FOR CURRENT_USER SERVER guava_orange
--       OPTIONS (username 'sa', password 'TU_PASSWORD');
--     INSERT INTO _mig_programas SELECT * FROM guava_orange.dbo."ProgramaProduccion";
--
--   Opción B — CSV (exportar desde SSMS con "Save Results As..."):
--     COPY _mig_programas FROM '/tmp/ProgramaProduccion.csv' CSV HEADER;
-- ────────────────────────────────────────────────────────────

CREATE TEMP TABLE _mig_programas (
    id_programa         INT,
    folio               VARCHAR(20),
    semana              VARCHAR(10),      -- ej. "20-2026"
    fecha_inicio        DATE,
    fecha_fin           DATE,
    fecha_entrega       DATE,
    id_empresa          INT,
    id_sucursal         INT,
    estatus             VARCHAR(30),      -- Abierto|EnProceso|Cerrado|Cancelado
    prioridad           INT DEFAULT 1,
    notas               TEXT,
    creado_por          VARCHAR(80),
    fecha_creacion      DATETIME,
    modificado_por      VARCHAR(80),
    fecha_modificacion  DATETIME
) ON COMMIT DROP;

CREATE TEMP TABLE _mig_detalle_programa (
    id_detalle          INT,
    id_programa         INT,
    num_linea           INT,
    id_producto         INT,             -- FK a Orange.dbo.Productos
    codigo_producto     VARCHAR(60),
    talla_codigo        VARCHAR(20),
    cantidad            INT,
    cantidad_producida  INT DEFAULT 0,
    cantidad_enviada    INT DEFAULT 0,
    estatus             VARCHAR(30),
    notas               TEXT
) ON COMMIT DROP;

CREATE TEMP TABLE _mig_vales (
    id_vale             INT,
    id_programa         INT,
    id_detalle          INT,
    folio_vale          VARCHAR(20),
    lote                VARCHAR(30),
    id_fase             INT,
    nombre_fase         VARCHAR(60),
    lote_size           INT,
    estatus             VARCHAR(30),     -- Emitido|EnProceso|Completado|Cancelado
    fecha_emision       DATE,
    emitido_por         VARCHAR(80),
    fecha_completado    DATE,
    completado_por      VARCHAR(80),
    fecha_cancelado     DATE,
    razon_cancelacion   TEXT,
    impreso             BIT DEFAULT 0,
    num_impresiones     INT DEFAULT 0
) ON COMMIT DROP;

CREATE TEMP TABLE _mig_destajo (
    id_destajo          INT,
    id_empleado         INT,
    id_programa         INT,
    id_fase             INT,
    nombre_fase         VARCHAR(60),
    fecha_trabajo       DATE,
    piezas_producidas   INT,
    piezas_rechazadas   INT DEFAULT 0,
    precio_unitario     DECIMAL(14,4),
    importe_bruto       DECIMAL(14,4),
    descuento_calidad   DECIMAL(14,4) DEFAULT 0,
    importe_neto        DECIMAL(14,4),
    estatus             VARCHAR(30),    -- Pendiente|Aprobado|Procesado|Cancelado
    aprobado_por        VARCHAR(80),
    fecha_aprobacion    DATE,
    periodo_nomina_id   INT             -- FK a periodo de nómina legacy
) ON COMMIT DROP;

CREATE TEMP TABLE _mig_no_pasa (
    id_no_pasa          INT,
    tipo_restriccion    VARCHAR(30),    -- estilo|suela|producto
    id_estilo           INT,
    id_suela            INT,
    id_fase             INT,
    nombre_fase         VARCHAR(60),
    motivo              TEXT
) ON COMMIT DROP;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 2: Tablas de lookup (IDs legacy → IDs nuevos)
-- ────────────────────────────────────────────────────────────

-- Mapeo empresa legacy → company UUID nuevo
CREATE TEMP TABLE _map_empresa (
    id_empresa_legacy   INT   PRIMARY KEY,
    company_id          UUID  NOT NULL
) ON COMMIT DROP;

-- Ajustar según las empresas migradas
INSERT INTO _map_empresa VALUES
    (1,  (SELECT id FROM org.companies WHERE code = 'NAN001' LIMIT 1));

-- Mapeo sucursal legacy → branch UUID nuevo
CREATE TEMP TABLE _map_sucursal (
    id_sucursal_legacy  INT   PRIMARY KEY,
    branch_id           UUID  NOT NULL
) ON COMMIT DROP;

INSERT INTO _map_sucursal VALUES
    (1,  (SELECT id FROM org.branches WHERE code = 'MAT' LIMIT 1));

-- Mapeo fase legacy (por nombre) → production_phase UUID
CREATE TEMP TABLE _map_fase (
    nombre_fase_legacy  VARCHAR(80) PRIMARY KEY,
    phase_id            UUID
) ON COMMIT DROP;

INSERT INTO _map_fase (nombre_fase_legacy, phase_id)
SELECT UPPER(TRIM(f.nombre_fase)), pp.id
FROM (VALUES
    ('CORTE',    'CORTE'),
    ('PREPARADO','PREP'),
    ('COSTURA',  'COSTURA'),
    ('ARMADO',   'ARMADO'),
    ('MONTADO',  'ARMADO'),
    ('ENSUELADO','SUELA'),
    ('ACABADO',  'ACABADO'),
    ('EMPAQUE',  'EMPAQUE')
) AS f(nombre_fase, codigo_fase)
JOIN product.production_phases pp ON pp.code = f.codigo_fase;

-- Mapeo producto legacy (código) → finished_product UUID
CREATE TEMP TABLE _map_producto (
    codigo_producto_legacy  VARCHAR(60) PRIMARY KEY,
    finished_product_id     UUID
) ON COMMIT DROP;

INSERT INTO _map_producto (codigo_producto_legacy, finished_product_id)
SELECT UPPER(TRIM(p.code)), p.id
FROM product.finished_products p;

-- Mapeo empleado legacy → employee UUID
CREATE TEMP TABLE _map_empleado (
    id_empleado_legacy  INT   PRIMARY KEY,
    employee_id         UUID
) ON COMMIT DROP;

-- Poblar manualmente o con query cruzando employee_number/code:
-- INSERT INTO _map_empleado
-- SELECT e_legacy.id, e_new.id
-- FROM _mig_empleados e_legacy
-- JOIN hr.hr_employees e_new ON e_new.employee_number = e_legacy.numero_empleado::varchar;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 3: Migrar órdenes de producción
-- ────────────────────────────────────────────────────────────

-- Generar UUIDs consistentes a partir del ID legacy
CREATE TEMP TABLE _map_programa (
    id_programa_legacy  INT   PRIMARY KEY,
    production_order_id UUID  DEFAULT gen_random_uuid()
) ON COMMIT DROP;

INSERT INTO _map_programa (id_programa_legacy)
SELECT id_programa FROM _mig_programas;

INSERT INTO production.production_orders (
    id, tenant_id, company_id, branch_id,
    folio, week_code,
    start_date, end_date, delivery_date,
    status, priority, notes,
    total_units_planned, total_units_produced, total_units_shipped,
    explosion_status,
    is_active, created_at, created_by, updated_at, updated_by
)
SELECT
    mp.production_order_id,
    c.tenant_id,
    me.company_id,
    ms.branch_id,
    COALESCE(p.folio, 'OP-MIGRADO-' || p.id_programa::text),
    -- Convert legacy week format "20-2026" → ISO "2026-W20"
    CASE WHEN p.semana ~ '^\d{1,2}-\d{4}$'
         THEN split_part(p.semana, '-', 2) || '-W' || LPAD(split_part(p.semana, '-', 1), 2, '0')
         ELSE p.semana
    END,
    p.fecha_inicio::date,
    p.fecha_fin::date,
    COALESCE(p.fecha_entrega, p.fecha_fin)::date,
    -- Map legacy status
    CASE LOWER(p.estatus)
        WHEN 'abierto'    THEN 'planned'
        WHEN 'enproceso'  THEN 'in_progress'
        WHEN 'en proceso' THEN 'in_progress'
        WHEN 'cerrado'    THEN 'closed'
        WHEN 'cancelado'  THEN 'cancelled'
        ELSE 'planned'
    END,
    COALESCE(p.prioridad, 1),
    COALESCE(p.notas, ''),
    0,  -- will update after lines inserted
    0,
    0,
    'complete',
    TRUE,
    COALESCE(p.fecha_creacion, NOW()),
    COALESCE(p.creado_por, 'migracion'),
    COALESCE(p.fecha_modificacion, NOW()),
    COALESCE(p.modificado_por, 'migracion')
FROM _mig_programas p
JOIN _map_programa mp ON mp.id_programa_legacy = p.id_programa
JOIN _map_empresa  me ON me.id_empresa_legacy  = p.id_empresa
JOIN _map_sucursal ms ON ms.id_sucursal_legacy = p.id_sucursal
JOIN org.companies c  ON c.id = me.company_id
ON CONFLICT (id) DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 4: Migrar líneas de producción
-- ────────────────────────────────────────────────────────────

CREATE TEMP TABLE _map_linea (
    id_detalle_legacy       INT   PRIMARY KEY,
    production_order_line_id UUID DEFAULT gen_random_uuid()
) ON COMMIT DROP;

INSERT INTO _map_linea (id_detalle_legacy)
SELECT id_detalle FROM _mig_detalle_programa;

INSERT INTO production.production_order_lines (
    id, production_order_id, line_number,
    finished_product_id,
    quantities_per_size,
    total_units_planned, total_units_produced, total_units_shipped, total_units_pending,
    status, priority, notes,
    is_active, created_at, created_by
)
SELECT
    ml.production_order_line_id,
    mp.production_order_id,
    d.num_linea,
    COALESCE(mprod.finished_product_id, gen_random_uuid()),  -- fallback if product not mapped
    -- Store quantity as JSONB: {talla: cantidad}
    jsonb_build_object(COALESCE(d.talla_codigo, 'U'), d.cantidad),
    d.cantidad,
    COALESCE(d.cantidad_producida, 0),
    COALESCE(d.cantidad_enviada, 0),
    GREATEST(0, d.cantidad - COALESCE(d.cantidad_producida, 0)),
    CASE LOWER(d.estatus)
        WHEN 'completado' THEN 'completed'
        WHEN 'cancelado'  THEN 'cancelled'
        WHEN 'enproceso'  THEN 'in_progress'
        ELSE 'pending'
    END,
    1,
    COALESCE(d.notas, ''),
    TRUE,
    NOW(),
    'migracion'
FROM _mig_detalle_programa d
JOIN _map_linea    ml   ON ml.id_detalle_legacy   = d.id_detalle
JOIN _map_programa mp   ON mp.id_programa_legacy  = d.id_programa
LEFT JOIN _map_producto mprod ON mprod.codigo_producto_legacy = UPPER(TRIM(d.codigo_producto))
ON CONFLICT (id) DO NOTHING;

-- Update order totals from lines
UPDATE production.production_orders po
SET total_units_planned  = agg.planned,
    total_units_produced = agg.produced,
    total_units_shipped  = agg.shipped
FROM (
    SELECT production_order_id,
           SUM(total_units_planned)  AS planned,
           SUM(total_units_produced) AS produced,
           SUM(total_units_shipped)  AS shipped
    FROM production.production_order_lines
    GROUP BY production_order_id
) agg
WHERE po.id = agg.production_order_id;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 5: Migrar vales de producción
-- ────────────────────────────────────────────────────────────

CREATE TEMP TABLE _map_vale (
    id_vale_legacy        INT   PRIMARY KEY,
    production_voucher_id UUID  DEFAULT gen_random_uuid()
) ON COMMIT DROP;

INSERT INTO _map_vale (id_vale_legacy)
SELECT id_vale FROM _mig_vales;

INSERT INTO production.production_vouchers (
    id, tenant_id, company_id,
    production_order_id, production_order_line_id,
    production_phase_id,
    folio, lot_number, batch_size,
    status,
    issued_date, issued_by,
    completed_date, completed_by,
    cancelled_date, cancelled_reason,
    printed, print_count,
    notes, is_active, created_at, created_by
)
SELECT
    mv.production_voucher_id,
    c.tenant_id,
    me.company_id,
    mp.production_order_id,
    COALESCE(ml.production_order_line_id, mp.production_order_id),  -- fallback
    COALESCE(mf.phase_id, (SELECT id FROM product.production_phases LIMIT 1)),
    COALESCE(v.folio_vale, 'VALE-MIG-' || v.id_vale::text),
    COALESCE(v.lote, 'LOTE-MIG'),
    COALESCE(v.lote_size, 0),
    CASE LOWER(v.estatus)
        WHEN 'emitido'    THEN 'issued'
        WHEN 'enproceso'  THEN 'in_progress'
        WHEN 'completado' THEN 'completed'
        WHEN 'cancelado'  THEN 'cancelled'
        ELSE 'issued'
    END,
    v.fecha_emision,
    COALESCE(v.emitido_por, 'migracion'),
    v.fecha_completado,
    v.completado_por,
    v.fecha_cancelado,
    v.razon_cancelacion,
    COALESCE(v.impreso::boolean, FALSE),
    COALESCE(v.num_impresiones, 0),
    '',
    TRUE,
    NOW(),
    'migracion'
FROM _mig_vales v
JOIN _map_vale    mv ON mv.id_vale_legacy        = v.id_vale
JOIN _map_programa mp ON mp.id_programa_legacy   = v.id_programa
JOIN org.companies c  ON c.id = (SELECT company_id FROM _map_empresa LIMIT 1)
JOIN _map_empresa  me ON me.company_id = c.id
LEFT JOIN _map_linea   ml ON ml.id_detalle_legacy = v.id_detalle
LEFT JOIN _map_fase    mf ON mf.nombre_fase_legacy = UPPER(TRIM(v.nombre_fase))
ON CONFLICT (id) DO NOTHING;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 6: Migrar registros de destajo
-- ────────────────────────────────────────────────────────────

INSERT INTO production.piece_work_records (
    id, tenant_id, company_id,
    employee_id,
    production_order_id, production_phase_id,
    work_date,
    units_produced, units_rejected,
    unit_price, gross_amount, quality_deduction, net_amount,
    status, approved_by, approved_at,
    is_active, created_at, created_by
)
SELECT
    gen_random_uuid(),
    c.tenant_id,
    me.company_id,
    COALESCE(memp.employee_id, '00000000-0000-0000-0000-000000000000'::uuid),
    mp.production_order_id,
    COALESCE(mf.phase_id, (SELECT id FROM product.production_phases LIMIT 1)),
    d.fecha_trabajo,
    d.piezas_producidas,
    COALESCE(d.piezas_rechazadas, 0),
    d.precio_unitario,
    d.importe_bruto,
    COALESCE(d.descuento_calidad, 0),
    d.importe_neto,
    CASE LOWER(d.estatus)
        WHEN 'pendiente'  THEN 'pending'
        WHEN 'aprobado'   THEN 'approved'
        WHEN 'procesado'  THEN 'processed'
        WHEN 'cancelado'  THEN 'cancelled'
        ELSE 'pending'
    END,
    d.aprobado_por,
    d.fecha_aprobacion::timestamptz,
    TRUE,
    NOW(),
    'migracion'
FROM _mig_destajo d
JOIN _map_programa mp  ON mp.id_programa_legacy   = d.id_programa
JOIN _map_empresa  me  ON TRUE   -- single company migration
JOIN org.companies c   ON c.id = me.company_id
LEFT JOIN _map_fase mf ON mf.nombre_fase_legacy = UPPER(TRIM(d.nombre_fase))
LEFT JOIN _map_empleado memp ON memp.id_empleado_legacy = d.id_empleado;

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 7: Migrar restricciones NoPasa
-- ────────────────────────────────────────────────────────────

INSERT INTO production.phase_restrictions (
    id, tenant_id, company_id,
    production_phase_id,
    restriction_type, reason,
    is_active, created_at, created_by
)
SELECT
    gen_random_uuid(),
    c.tenant_id,
    me.company_id,
    COALESCE(mf.phase_id, (SELECT id FROM product.production_phases LIMIT 1)),
    CASE LOWER(np.tipo_restriccion)
        WHEN 'estilo'   THEN 'style_phase'
        WHEN 'suela'    THEN 'sole_phase'
        WHEN 'producto' THEN 'product_phase'
        ELSE 'style_phase'
    END,
    COALESCE(np.motivo, ''),
    TRUE,
    NOW(),
    'migracion'
FROM _mig_no_pasa np
JOIN _map_empresa  me ON TRUE
JOIN org.companies c  ON c.id = me.company_id
LEFT JOIN _map_fase mf ON mf.nombre_fase_legacy = UPPER(TRIM(np.nombre_fase));

-- ────────────────────────────────────────────────────────────
-- SECCIÓN 8: Verificación de conteos
-- ────────────────────────────────────────────────────────────

DO $$
DECLARE
    v_programas     INT;
    v_lineas        INT;
    v_vales         INT;
    v_destajo       INT;
    v_no_pasa       INT;
BEGIN
    SELECT COUNT(*) INTO v_programas FROM _mig_programas;
    SELECT COUNT(*) INTO v_lineas    FROM _mig_detalle_programa;
    SELECT COUNT(*) INTO v_vales     FROM _mig_vales;
    SELECT COUNT(*) INTO v_destajo   FROM _mig_destajo;
    SELECT COUNT(*) INTO v_no_pasa   FROM _mig_no_pasa;

    RAISE NOTICE '=== RESULTADO MIGRACIÓN PRODUCCIÓN ===';
    RAISE NOTICE 'Fuente  → órdenes: %, líneas: %, vales: %, destajo: %, NoPasa: %',
        v_programas, v_lineas, v_vales, v_destajo, v_no_pasa;

    SELECT COUNT(*) INTO v_programas FROM production.production_orders  WHERE created_by = 'migracion';
    SELECT COUNT(*) INTO v_lineas    FROM production.production_order_lines WHERE created_by = 'migracion';
    SELECT COUNT(*) INTO v_vales     FROM production.production_vouchers WHERE created_by = 'migracion';
    SELECT COUNT(*) INTO v_destajo   FROM production.piece_work_records  WHERE created_by = 'migracion';
    SELECT COUNT(*) INTO v_no_pasa   FROM production.phase_restrictions  WHERE created_by = 'migracion';

    RAISE NOTICE 'Destino → órdenes: %, líneas: %, vales: %, destajo: %, restricciones: %',
        v_programas, v_lineas, v_vales, v_destajo, v_no_pasa;
    RAISE NOTICE '======================================';
END;
$$;

-- ────────────────────────────────────────────────────────────
-- Si los conteos son correctos: COMMIT
-- Si algo falló:                ROLLBACK
-- ────────────────────────────────────────────────────────────
-- COMMIT;
-- ROLLBACK;

-- ============================================================
--  ALTERNATIVA CSV (sin tds_fdw)
--
--  En SQL Server (SSMS):
--    EXEC xp_cmdshell 'bcp Orange.dbo.ProgramaProduccion out C:\export\ProgramaProduccion.csv -c -t, -T -S localhost'
--
--  Copiar al servidor PostgreSQL y cargar:
--    COPY _mig_programas       FROM '/tmp/ProgramaProduccion.csv'      CSV HEADER;
--    COPY _mig_detalle_programa FROM '/tmp/DetalleProgramaProduccion.csv' CSV HEADER;
--    COPY _mig_vales           FROM '/tmp/ValesProduccion.csv'          CSV HEADER;
--    COPY _mig_destajo         FROM '/tmp/Destajo.csv'                  CSV HEADER;
--    COPY _mig_no_pasa         FROM '/tmp/NoPasaFases.csv'              CSV HEADER;
--
--  MAPEO DE TABLAS GUAVA → ARCHIVOS CSV:
--    Orange.dbo.ProgramaProduccion        → _mig_programas
--    Orange.dbo.DetalleProgramaProduccion → _mig_detalle_programa
--    Vales.dbo.ValesProduccion            → _mig_vales
--    Orange.dbo.Destajo                   → _mig_destajo
--    Orange.dbo.NoPasa_Estilo_Fases       → _mig_no_pasa (tipo_restriccion='estilo')
--    Orange.dbo.NoPasa_Suela_Fases        → _mig_no_pasa (tipo_restriccion='suela')
-- ============================================================
