-- ============================================================
-- Convierte columnas PascalCase → snake_case para que EF Core funcione.
-- Ejecutar UNA SOLA VEZ. Cada paso está en su propia transacción.
-- ============================================================

-- 1. Función de conversión
CREATE OR REPLACE FUNCTION public.nanchesoft_to_snake_case(input_name text)
RETURNS text
LANGUAGE sql
IMMUTABLE
AS $$
    SELECT lower(
        regexp_replace(
            regexp_replace(
                regexp_replace(input_name, '[\s\-]+', '_', 'g'),
                '([a-z0-9])([A-Z])', '\1_\2', 'g'
            ),
            '([A-Z]+)([A-Z][a-z])', '\1_\2', 'g'
        )
    );
$$;

-- 2. Renombrar tablas (PascalCase → snake_case)
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT table_schema, table_name
        FROM information_schema.tables
        WHERE table_type = 'BASE TABLE'
          AND table_schema IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY table_schema, table_name
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.table_name);
        IF r.table_name <> new_name
           AND NOT EXISTS (
                SELECT 1 FROM information_schema.tables t
                WHERE t.table_schema = r.table_schema AND t.table_name = new_name
           ) THEN
            EXECUTE format('ALTER TABLE %I.%I RENAME TO %I;', r.table_schema, r.table_name, new_name);
            RAISE NOTICE 'Renamed table %.% → %', r.table_schema, r.table_name, new_name;
        END IF;
    END LOOP;
END $$;

-- 3. Renombrar columnas (PascalCase → snake_case)
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT table_schema, table_name, column_name
        FROM information_schema.columns
        WHERE table_schema IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY table_schema, table_name, ordinal_position
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.column_name);
        IF r.column_name <> new_name
           AND NOT EXISTS (
                SELECT 1 FROM information_schema.columns c
                WHERE c.table_schema = r.table_schema AND c.table_name = r.table_name AND c.column_name = new_name
           ) THEN
            EXECUTE format('ALTER TABLE %I.%I RENAME COLUMN %I TO %I;', r.table_schema, r.table_name, r.column_name, new_name);
        END IF;
    END LOOP;
END $$;

-- 4. Renombrar constraints (cada uno en bloque protegido para evitar rollback total)
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT tc.table_schema, tc.table_name, tc.constraint_name
        FROM information_schema.table_constraints tc
        WHERE tc.table_schema IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY tc.table_schema, tc.table_name, tc.constraint_name
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.constraint_name);
        IF r.constraint_name <> new_name
           AND NOT EXISTS (
                SELECT 1 FROM information_schema.table_constraints tc2
                WHERE tc2.table_schema = r.table_schema AND tc2.constraint_name = new_name
           ) THEN
            BEGIN
                EXECUTE format('ALTER TABLE %I.%I RENAME CONSTRAINT %I TO %I;', r.table_schema, r.table_name, r.constraint_name, new_name);
            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Skipped constraint rename % → %: %', r.constraint_name, new_name, SQLERRM;
            END;
        END IF;
    END LOOP;
END $$;

-- 5. Renombrar índices (con manejo de conflictos)
DO $$
DECLARE
    r record;
    new_name text;
BEGIN
    FOR r IN
        SELECT schemaname, indexname
        FROM pg_indexes
        WHERE schemaname IN ('accounting','auth','catalog','config','core','finance','hr','inventory','org','payroll','product','purchase','sales','subscription','public')
        ORDER BY schemaname, indexname
    LOOP
        new_name := public.nanchesoft_to_snake_case(r.indexname);
        IF r.indexname <> new_name
           AND NOT EXISTS (
                SELECT 1 FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = r.schemaname AND c.relname = new_name
           ) THEN
            BEGIN
                EXECUTE format('ALTER INDEX %I.%I RENAME TO %I;', r.schemaname, r.indexname, new_name);
            EXCEPTION WHEN OTHERS THEN
                RAISE NOTICE 'Skipped index rename % → %: %', r.indexname, new_name, SQLERRM;
            END;
        END IF;
    END LOOP;
END $$;

-- 6. Verificación
SELECT table_schema, table_name, column_name
FROM information_schema.columns
WHERE table_schema IN ('core','product','subscription')
  AND table_name IN ('tenants','companies','plans','finished_products','product_size_runs','finished_product_supplies')
  AND ordinal_position <= 4
ORDER BY table_schema, table_name, ordinal_position;
