-- ============================================================
-- Nómina Operativa – Script consolidado (Fases 1, 4, 5, 7, 8)
-- Aplica todas las migraciones del módulo en orden.
-- Idempotente: seguro re-aplicarlo en producción.
-- ============================================================

-- ── Fase 1: extensiones de catálogos ─────────────────────────

\i 20260526_nomina_operativa_fase1.sql

-- ── Fase 4: AttendancePolicy ──────────────────────────────────

\i 20260526_nomina_operativa_fase4_attendance_policy.sql

-- ── Fase 5: ClockImport ───────────────────────────────────────

\i 20260526_nomina_operativa_fase5_clock_import.sql

-- ── Fase 7: employee_incidents — columnas origin y manually_edited

\i 20260526_nomina_operativa_fase7_manual_incidents.sql

-- ── Fase 8: nom_payroll_incident_types — columna payroll_concept_id

\i 20260526_nomina_operativa_fase8_operational_prepayroll.sql

-- ============================================================
-- Fin del script consolidado.
-- Las entidades creadas por EnsureCreated() (work_schedules,
-- attendance_daily_summaries, clock_records, clock_biometric_devices,
-- attendance_policies, attendance_policy_rules, clock_import_logs,
-- clock_import_mappings) se generan automáticamente en primer
-- arranque del API; este script sólo cubre ALTER TABLEs en
-- tablas existentes.
-- ============================================================
