\echo === PRE-DOWN STATE (upgraded) ===
select count(*) as grants_total, count(*) filter (where option_code is null) as legacy_level, count(*) filter (where option_code is not null) as option_level from shipping.coverage_grants;
select service_code, option_code, mode from shipping.method_coverages order by service_code, option_code;
select count(*) as rate_rows from shipping.rate_policies;
