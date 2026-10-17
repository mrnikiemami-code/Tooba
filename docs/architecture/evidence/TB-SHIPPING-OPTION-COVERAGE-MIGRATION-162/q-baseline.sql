select "MigrationId" from shipping.__ef_migrations_history order by 1;
select to_regclass('shipping.method_coverages') as method_coverages_table;
select count(*) as grants, count(*) filter (where is_active) as active_grants from shipping.coverage_grants;
