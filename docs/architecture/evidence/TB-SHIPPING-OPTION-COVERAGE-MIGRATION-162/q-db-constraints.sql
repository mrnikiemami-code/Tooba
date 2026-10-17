\echo === NEGATIVE: duplicate service-level grant identity (NULLS NOT DISTINCT) ===
INSERT INTO shipping.coverage_grants (coverage_grant_id, service_code, country_id, is_active, created_at, updated_at)
VALUES ('99999999-9999-4999-8999-999999999999','tipax','e3451206-e503-51a8-91ad-afa9ab70e330',true,now(),now());
\echo === NEGATIVE: mode=2 is not a known restriction mode ===
INSERT INTO shipping.method_coverages (method_coverage_id, service_code, option_code, mode, created_at, updated_at)
VALUES ('99999999-aaaa-4aaa-8aaa-999999999999','tipax','standard',2,now(),now());
\echo === NEGATIVE: padded option code is not canonical ===
INSERT INTO shipping.method_coverages (method_coverage_id, service_code, option_code, mode, created_at, updated_at)
VALUES ('99999999-bbbb-4bbb-8bbb-999999999999','tipax',' standard ',0,now(),now());
\echo === POSITIVE: two declarations for the same service, different methods ===
INSERT INTO shipping.method_coverages (method_coverage_id, service_code, option_code, mode, created_at, updated_at) VALUES
 ('aaaaaaaa-9999-4999-8999-aaaaaaaaaaaa','tipax','standard',1,now(),now()),
 ('bbbbbbbb-9999-4999-8999-bbbbbbbbbbbb','tipax','express',0,now(),now());
\echo === NEGATIVE: duplicate declaration identity ===
INSERT INTO shipping.method_coverages (method_coverage_id, service_code, option_code, mode, created_at, updated_at)
VALUES ('cccccccc-9999-4999-8999-cccccccccccc','TIPAX','express',1,now(),now());
\echo === POSITIVE: option-scoped grant distinct from the legacy service-level row at the same scope ===
INSERT INTO shipping.coverage_grants (coverage_grant_id, service_code, option_code, country_id, administrative_area_id, city_id, is_active, created_at, updated_at)
VALUES ('66666666-6666-4666-8666-666666666666','tipax','standard','e3451206-e503-51a8-91ad-afa9ab70e330','1f8050a8-c325-59fa-959f-82d76b5b2bc5','c841be1e-0f47-54aa-8be8-a7b5c33f6f64',true,now(),now());
\echo === FINAL STATE ===
select service_code, option_code, mode from shipping.method_coverages order by option_code;
select count(*) as grants_total, count(*) filter (where option_code is null) as legacy_level, count(*) filter (where option_code='standard') as standard_level from shipping.coverage_grants;
