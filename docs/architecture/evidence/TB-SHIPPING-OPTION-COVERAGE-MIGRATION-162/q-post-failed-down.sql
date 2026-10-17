\echo === STATE AFTER FAILED DOWN (transaction should have rolled back) ===
select migration_id from shipping.__ef_migrations_history order by 1;
select count(*) as grants, count(*) filter (where option_code is null) as legacy_level from shipping.coverage_grants;
select to_regclass('shipping.method_coverages') as method_coverages;
