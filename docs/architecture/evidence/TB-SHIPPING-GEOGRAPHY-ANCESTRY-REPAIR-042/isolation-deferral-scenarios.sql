-- TB-SHIPPING-GEOGRAPHY-ANCESTRY-REPAIR-042 — isolation, deferral and ON CONFLICT scenarios.
-- Destination: disposable task-exclusive container geo042-verify (postgres:17), database geo042, port 55432.
\set VERBOSITY verbose
\set ON_ERROR_STOP off
\pset pager off
\echo '=== T5a: plain UPDATE in READ COMMITTED (expect success) ==='
begin isolation level read committed;
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000013', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
commit;
\echo '=== T5b: ancestry UPDATE in REPEATABLE READ (expect 0A000 isolation_unsupported) ==='
begin isolation level repeatable read;
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000016', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
commit;
\echo '=== T5c: ancestry UPDATE in SERIALIZABLE (expect 0A000 isolation_unsupported) ==='
begin isolation level serializable;
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000016', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
commit;
\echo '=== T5d: code-only UPDATE in REPEATABLE READ (expect success: WHEN clause keeps trigger off) ==='
begin isolation level repeatable read;
update geography.administrative_areas set code = 'A2B', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
commit;
\echo '=== T6: SET CONSTRAINTS ALL DEFERRED must not defer the guard (expect immediate 23514 on 2nd statement) ==='
begin;
set constraints all deferred;
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000013', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000012';
update geography.administrative_areas set parent_administrative_area_id = '00000000-0000-0000-0000-000000000012', updated_at = now()
 where administrative_area_id = '00000000-0000-0000-0000-000000000013';
commit;
\echo '=== T7: INSERT ... ON CONFLICT DO UPDATE that would close a cycle (expect 23514) ==='
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000013', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000012', 'B3', true, now(), now())
on conflict (administrative_area_id) do update set parent_administrative_area_id = excluded.parent_administrative_area_id, updated_at = now();
\echo '=== T8: INSERT ... ON CONFLICT DO UPDATE without cycle (expect success) ==='
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('00000000-0000-0000-0000-000000000012', '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000011', 'A2C', true, now(), now())
on conflict (administrative_area_id) do update set parent_administrative_area_id = excluded.parent_administrative_area_id, updated_at = now();
\echo '=== T9: graph state + acyclic assertion ==='
with recursive walk(root) as (
    select administrative_area_id from geography.administrative_areas
    union all
    select a.parent_administrative_area_id from geography.administrative_areas a join walk w on a.administrative_area_id = w.root
    where a.parent_administrative_area_id is not null
)
select count(*) as reachable_rows from walk;
select administrative_area_id, parent_administrative_area_id, code from geography.administrative_areas order by code;
