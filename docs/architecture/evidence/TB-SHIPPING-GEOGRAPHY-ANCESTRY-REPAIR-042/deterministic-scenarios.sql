-- TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042 — deterministic single-session scenarios.
-- Destination: disposable task-exclusive container geo042-verify (postgres:17), database geo042, port 55432.
\set VERBOSITY verbose
\set ON_ERROR_STOP off
\pset pager off
\echo '=== T0a: installed triggers ==='
select tgname, tgenabled, tgtype, tgisinternal
from pg_trigger
where tgrelid = 'geography.administrative_areas'::regclass and not tgisinternal
order by tgname;
\echo '=== T0b: guard function exists ==='
select proname, provolatile, pg_get_function_identity_arguments(oid) as args
from pg_proc where proname = 'administrative_areas_ancestry_guard';
\echo '=== T0c: baseline data (country + root) ==='
insert into geography.countries (country_id, country_code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000001', 'IRN', true, now(), now());
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000011', '00000000-0000-0000-0000-000000000001', null, 'ROOT', true, now(), now());
\echo '=== T1: legitimate root insert + child insert + re-root + re-parent (expect 4x success) ==='
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000012', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'A', true, now(), now());
update geography.administrative_areas set parent_administrative_area_id = null, updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000011', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
update geography.administrative_areas set code = 'A2', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
\echo '=== T2a: INSERT with self-parent (expect ancestry_cycle / 23514) ==='
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000021', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000021', 'SELF', true, now(), now());
\echo '=== T2b: same-statement two-row INSERT cycle X->Y,Y->X (expect ancestry_cycle / 23514) ==='
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000014', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000015', 'X', true, now(), now()),
       ('00000000-0000-0000-0000-000000000015', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000014', 'Y', true, now(), now());
\echo '=== T2c: 2-node UPDATE cycle (first ok, second expect ancestry_cycle) ==='
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000013', '00000000-0000-0000-0000-000000000001', null, 'B', true, now(), now());
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000013', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000012', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000013';
\echo '=== T2d: 3-node UPDATE cycle X->Y->Z then Z->X (expect ancestry_cycle) ==='
update geography.administrative_areas set parent_administrative_area_id = null, updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000013';
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000016', '00000000-0000-0000-0000-000000000001', null, 'Z', true, now(), now());
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000013', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000016';
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000012', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000013';
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000016', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
\echo '=== T3: deep legitimate chain (5 levels) then deep-node re-root (expect success) ==='
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000031', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'D1', true, now(), now()),
       ('00000000-0000-0000-0000-000000000032', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000031', 'D2', true, now(), now()),
       ('00000000-0000-0000-0000-000000000033', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000032', 'D3', true, now(), now()),
       ('00000000-0000-0000-0000-000000000034', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000033', 'D4', true, now(), now());
update geography.administrative_areas set parent_administrative_area_id = null, updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000034';
\echo '=== T4: final graph state (must be acyclic) ==='
select administrative_area_id, parent_administrative_area_id, code from geography.administrative_areas order by code;
