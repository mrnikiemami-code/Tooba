\echo === REAPPLIED: history ===
select migration_id from shipping.__ef_migrations_history order by 1;
\echo === REAPPLIED: legacy rows preserved back through Up ===
select coverage_grant_id, service_code, city_id, service_area_id, is_active, option_code from shipping.coverage_grants order by coverage_grant_id;
\echo === REAPPLIED: new-format data discarded by rollback (documented) ===
select count(*) as declarations_after_reapply from shipping.method_coverages;
select count(*) as option_level_grants_after_reapply from shipping.coverage_grants where option_code is not null;
\echo === REAPPLIED: indexes rebuilt ===
select indexname, indexdef from pg_indexes where schemaname='shipping' and tablename in ('coverage_grants','method_coverages') and indexname like 'ux_%' order by indexname;
