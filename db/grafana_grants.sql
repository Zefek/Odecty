-- Grant read-only access on diagnostics tables to the grafana role.
-- Idempotent: re-running GRANT is a no-op if privileges already exist.
-- Run as a superuser or as the owner of the tables (typically the role used by EF migrations).

GRANT USAGE ON SCHEMA public TO grafana;

GRANT SELECT ON public.heater_diagnostics    TO grafana;
GRANT SELECT ON public.ls_sensor_diagnostics TO grafana;
GRANT SELECT ON public.garage_diagnostics    TO grafana;

-- Optional: auto-grant SELECT on any future table created in the public schema
-- by the role that owns the existing tables (replace <owner_role>). Useful if
-- you add more diagnostic tables later and want grafana to pick them up
-- without having to remember to grant explicitly. Default privileges are
-- per-creator: this applies only to objects created by <owner_role> after
-- this statement runs.
--
-- ALTER DEFAULT PRIVILEGES FOR ROLE <owner_role> IN SCHEMA public
--   GRANT SELECT ON TABLES TO grafana;
