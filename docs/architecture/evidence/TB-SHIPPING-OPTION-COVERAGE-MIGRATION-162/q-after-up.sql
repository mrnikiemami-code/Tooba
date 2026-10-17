\echo === MIGRATION HISTORY ===
select migration_id from shipping.__ef_migrations_history order by 1;
\echo === coverage_grants.option_code COLUMN ===
select column_name, data_type, character_maximum_length, is_nullable
  from information_schema.columns
 where table_schema='shipping' and table_name='coverage_grants' and column_name='option_code';
\echo === CHECK CONSTRAINTS (coverage_grants, method_coverages) ===
select conname, pg_get_constraintdef(oid) as def
  from pg_constraint
 where conrelid in ('shipping.coverage_grants'::regclass, 'shipping.method_coverages'::regclass) and contype='c'
 order by conname;
\echo === INDEXES ===
select indexname, indexdef from pg_indexes
 where schemaname='shipping' and tablename in ('coverage_grants','method_coverages') order by indexname;
\echo === method_coverages TABLE ===
select column_name, data_type, is_nullable from information_schema.columns
 where table_schema='shipping' and table_name='method_coverages' order by ordinal_position;
\echo === LEGACY GRANT PRESERVATION ===
select coverage_grant_id, service_code, city_id, service_area_id, is_active, option_code
  from shipping.coverage_grants order by coverage_grant_id;
\echo === LEGACY RATE PRESERVATION (charges unchanged) ===
select rate_policy_id, service_code, option_code, charge_amount, currency_code from shipping.rate_policies order by rate_policy_id;
