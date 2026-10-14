-- TB-SHIPPING-GEOGRAPHY-RUNNER-VERIFY-046 — behavioural invariant checks executed against the
-- task-exclusive disposable database (geo046_scratch on 127.0.0.1:55433) AFTER `Tooba.MigrationRunner
-- apply` had created the geography schema there. Output is recorded in log-scenarios.txt.
-- Reuses the focused 042 ancestry/FK validations instead of an exhaustive new suite.
-- Every negative case runs inside a PL/pgSQL subtransaction so the SQLSTATE is captured without
-- aborting the session.

\set ON_ERROR_STOP on
\pset pager off
\set QUIET on

-- Fixture (disposable database only): two countries, a root area, a child area, a city, a service area.
insert into geography.countries (country_id, country_code, is_active, created_at, updated_at)
values ('11111111-1111-4111-8111-111111111111', 'IR', true, now(), now()),
       ('22222222-2222-4222-8222-222222222222', 'DE', true, now(), now());

insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('a0000000-0000-4000-8000-000000000001', '11111111-1111-4111-8111-111111111111', null, 'ROOT-IR', true, now(), now());

insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('a0000000-0000-4000-8000-000000000002', '11111111-1111-4111-8111-111111111111', 'a0000000-0000-4000-8000-000000000001', 'CHILD-IR', true, now(), now());

insert into geography.cities (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at)
values ('c0000000-0000-4000-8000-000000000001', '11111111-1111-4111-8111-111111111111', 'a0000000-0000-4000-8000-000000000002', 'CITY-IR', true, now(), now());

insert into geography.service_areas (service_area_id, city_id, code, is_active, created_at, updated_at)
values ('d0000000-0000-4000-8000-000000000001', 'c0000000-0000-4000-8000-000000000001', 'SA-IR', true, now(), now());

select 'P1 fixture accepted: countries=' || (select count(*) from geography.countries)
    || ' areas=' || (select count(*) from geography.administrative_areas)
    || ' cities=' || (select count(*) from geography.cities)
    || ' service_areas=' || (select count(*) from geography.service_areas);

-- N1: cross-country parent must be rejected by the composite FK.
do $$
begin
    begin
        insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
        values ('a0000000-0000-4000-8000-0000000000ff', '22222222-2222-4222-8222-222222222222', 'a0000000-0000-4000-8000-000000000001', 'BAD-XCOUNTRY', true, now(), now());
        raise notice 'N1 cross_country_parent -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N1 cross_country_parent -> sqlstate=%', sqlstate;
    end;
end $$;

-- N2: self-parent must be rejected by the ancestry trigger.
do $$
begin
    begin
        insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
        values ('a0000000-0000-4000-8000-0000000000f1', '11111111-1111-4111-8111-111111111111', 'a0000000-0000-4000-8000-0000000000f1', 'SELF-PARENT', true, now(), now());
        raise notice 'N2 self_parent -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N2 self_parent -> sqlstate=%', sqlstate;
    end;
end $$;

-- N3: two-row INSERT that closes a cycle (A -> B, B -> A). This is the 042 amendment case: the
-- composite FK is an end-of-statement check and accepts the pair, so only an INSERT-firing immediate
-- trigger can catch it.
do $$
begin
    begin
        insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
        values ('a0000000-0000-4000-8000-0000000000f2', '11111111-1111-4111-8111-111111111111', 'a0000000-0000-4000-8000-0000000000f3', 'CYCLE-A', true, now(), now()),
               ('a0000000-0000-4000-8000-0000000000f3', '11111111-1111-4111-8111-111111111111', 'a0000000-0000-4000-8000-0000000000f2', 'CYCLE-B', true, now(), now());
        raise notice 'N3 multirow_insert_cycle -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N3 multirow_insert_cycle -> sqlstate=%', sqlstate;
    end;
end $$;

-- N4: re-parenting the root under its own descendant must be rejected by the UPDATE trigger.
do $$
begin
    begin
        update geography.administrative_areas
           set parent_administrative_area_id = 'a0000000-0000-4000-8000-000000000002'
         where administrative_area_id = 'a0000000-0000-4000-8000-000000000001';
        raise notice 'N4 update_cycle -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N4 update_cycle -> sqlstate=%', sqlstate;
    end;
end $$;

select 'N5 stored graph intact after rejected writes: root_parent=' ||
       coalesce((select parent_administrative_area_id::text from geography.administrative_areas where administrative_area_id='a0000000-0000-4000-8000-000000000001'), 'null')
    || ' area_count=' || (select count(*) from geography.administrative_areas);

-- N6: a city may not point at an area of another country.
do $$
begin
    begin
        insert into geography.cities (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at)
        values ('c0000000-0000-4000-8000-0000000000ff', '22222222-2222-4222-8222-222222222222', 'a0000000-0000-4000-8000-000000000001', 'BAD-CITY', true, now(), now());
        raise notice 'N6 city_cross_country_area -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N6 city_cross_country_area -> sqlstate=%', sqlstate;
    end;
end $$;

-- N7: a service area with a nonexistent city is rejected.
do $$
begin
    begin
        insert into geography.service_areas (service_area_id, city_id, code, is_active, created_at, updated_at)
        values ('d0000000-0000-4000-8000-0000000000ff', 'c0000000-0000-4000-8000-00000000dead', 'BAD-SA', true, now(), now());
        raise notice 'N7 service_area_missing_city -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N7 service_area_missing_city -> sqlstate=%', sqlstate;
    end;
end $$;

-- Positive controls: a legitimate new branch and a localized name are accepted.
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
values ('a0000000-0000-4000-8000-000000000010', '11111111-1111-4111-8111-111111111111', 'a0000000-0000-4000-8000-000000000001', 'VALID-CHILD', true, now(), now())
returning 'P2 valid child area accepted: ' || code;

insert into geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)
values ('e0000000-0000-4000-8000-000000000001', '11111111-1111-4111-8111-111111111111', 'f0000000-0000-4000-8000-000000000001', 'Iran', now(), now())
returning 'P3 localized country name accepted: ' || name;

-- N8: duplicate (place, language) name is rejected by the unique index.
do $$
begin
    begin
        insert into geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)
        values ('e0000000-0000-4000-8000-0000000000ff', '11111111-1111-4111-8111-111111111111', 'f0000000-0000-4000-8000-000000000001', 'Duplicate', now(), now());
        raise notice 'N8 duplicate_place_language -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N8 duplicate_place_language -> sqlstate=%', sqlstate;
    end;
end $$;

-- N9: countries are Restrict-protected while they still own areas.
do $$
begin
    begin
        delete from geography.countries where country_id = '11111111-1111-4111-8111-111111111111';
        raise notice 'N9 delete_country_with_areas -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N9 delete_country_with_areas -> sqlstate=%', sqlstate;
    end;
end $$;

-- N10: the ancestry guard fails closed outside READ COMMITTED (recorded in log-isolation.txt).
begin isolation level repeatable read;
do $$
begin
    begin
        insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
        values ('a0000000-0000-4000-8000-0000000000f9', '11111111-1111-4111-8111-111111111111', 'a0000000-0000-4000-8000-000000000001', 'RR-WRITE', true, now(), now());
        raise notice 'N10 repeatable_read_ancestry_write -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'N10 repeatable_read_ancestry_write -> sqlstate=%', sqlstate;
    end;
end $$;
commit;
