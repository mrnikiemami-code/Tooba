\echo === AFTER DOWN: migration history ===
select migration_id from shipping.__ef_migrations_history order by 1;
\echo === AFTER DOWN: option_code column must be gone ===
select count(*) as option_code_columns from information_schema.columns where table_schema='shipping' and table_name='coverage_grants' and column_name='option_code';
\echo === AFTER DOWN: method_coverages must be gone ===
select to_regclass('shipping.method_coverages') as method_coverages;
\echo === AFTER DOWN: indexes on coverage_grants ===
select indexname, indexdef from pg_indexes where schemaname='shipping' and tablename='coverage_grants' order by indexname;
\echo === AFTER DOWN: legacy rows preserved (identity + activation + scope) ===
select coverage_grant_id, service_code, city_id, service_area_id, is_active from shipping.coverage_grants order by coverage_grant_id;
\echo === AFTER DOWN: rates untouched ===
select rate_policy_id, service_code, option_code, charge_amount from shipping.rate_policies order by rate_policy_id;
