-- TB-SHIPPING-GEOGRAPHY-DEV-BOOTSTRAP-047 — post-bootstrap verification on the proven LOCAL DEV tenant
-- database (tooba_alpha). Read-only except for NEGATIVE probes. Each probe runs inside a PL/pgSQL
-- subtransaction; probes that raise are rolled back by that subtransaction, and the script additionally
-- deletes its own probe rows at the end so a probe that unexpectedly succeeds cannot survive either.
-- Output is recorded in log-verify.txt.

\pset pager off
\set QUIET on

-- V1 shape vs the reviewed legacy catalogue (12 provinces / 47 cities).
select 'V1 shape: countries=' || (select count(*) from geography.countries)
    || ' areas=' || (select count(*) from geography.administrative_areas)
    || ' cities=' || (select count(*) from geography.cities)
    || ' service_areas=' || (select count(*) from geography.service_areas)
    || ' country_names=' || (select count(*) from geography.country_name_translations)
    || ' area_names=' || (select count(*) from geography.administrative_area_name_translations)
    || ' city_names=' || (select count(*) from geography.city_name_translations);

-- V2 country identity.
select 'V2 country: code=' || country_code || ' active=' || is_active from geography.countries;

-- V3 parent integrity: every province is a root here, and no area points at itself.
select 'V3 area_roots=' || (select count(*) from geography.administrative_areas where parent_administrative_area_id is null)
    || ' areas_with_parent=' || (select count(*) from geography.administrative_areas where parent_administrative_area_id is not null)
    || ' self_parents=' || (select count(*) from geography.administrative_areas where parent_administrative_area_id = administrative_area_id);

-- V4 every city sits under an area of its own country (the composite FK's whole point) and none is orphaned.
select 'V4 cities_without_area=' || (select count(*) from geography.cities where administrative_area_id is null)
    || ' cross_country_city_area=' || (select count(*) from geography.cities c join geography.administrative_areas a on a.administrative_area_id = c.administrative_area_id where a.country_id <> c.country_id)
    || ' cities_per_area_min=' || (select min(n) from (select count(*) as n from geography.cities group by administrative_area_id) s)
    || ' cities_per_area_max=' || (select max(n) from (select count(*) as n from geography.cities group by administrative_area_id) s);

-- V5 codes are unique inside the country and independent of the Persian labels. The underscore appears
-- only because two legacy province codes are multi-word (razavi_khorasan, east_azerbaijan); no code
-- contains Persian text, so identity never depends on a localized label.
select 'V5 distinct_area_codes=' || (select count(distinct code) from geography.administrative_areas)
    || ' distinct_city_codes=' || (select count(distinct code) from geography.cities)
    || ' ascii_codes=' || (select count(*) from geography.cities where code ~ '^[A-Za-z0-9_-]+$')
    || ' non_ascii_codes=' || ((select count(*) from geography.cities where code !~ '^[A-Za-z0-9_-]+$') + (select count(*) from geography.administrative_areas where code !~ '^[A-Za-z0-9_-]+$'));

-- V6 translations are keyed by the Localization-owned language, one row per place (the unique
-- (place, language) index makes "one" structural, not incidental).
select 'V6 language_code=' || l.code || ' is_default=' || l.is_default
    || ' area_labels=' || (select count(*) from geography.administrative_area_name_translations t where t.language_id = l.language_id)
    || ' city_labels=' || (select count(*) from geography.city_name_translations t where t.language_id = l.language_id)
    || ' country_labels=' || (select count(*) from geography.country_name_translations t where t.language_id = l.language_id)
    from localization.languages l where l.code = 'fa-IR';
select 'V6b non_persian_area_labels=' || (select count(*) from geography.administrative_area_name_translations where name ~ '^[A-Za-z0-9_-]+$');

-- V7 the ancestry guard objects are present in the migrated schema.
select 'V7 ancestry_function=' || (select count(*) from pg_proc p join pg_namespace n on n.oid = p.pronamespace where n.nspname = 'geography' and p.proname = 'administrative_areas_ancestry_guard')
    || ' guards=' || (select count(*) from pg_trigger t join pg_class c on c.oid = t.tgrelid join pg_namespace n on n.oid = c.relnamespace where n.nspname = 'geography' and t.tgname in ('trg_administrative_areas_ancestry_guard_insert', 'trg_administrative_areas_ancestry_guard_update'));

-- V8 schema fact, reported not assumed: cities.administrative_area_id is nullable BY DESIGN
-- (CitySnapshot.AdministrativeAreaId is Guid? and the migration declares the column nullable), so
-- "a city with no area" is legal and is NOT a constraint the bootstrap should tighten.
select 'V8 cities.administrative_area_id nullable=' || is_nullable from information_schema.columns
 where table_schema = 'geography' and table_name = 'cities' and column_name = 'administrative_area_id';

-- V9..V12 NEGATIVE probes: the migrated constraints really are live in this database.
do $$
declare
    probe_country uuid;
begin
    begin
        insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
        select '00000000-0000-4000-8000-000000042001'::uuid, c.country_id, null, 'geo047-probe-self', true, now(), now() from geography.countries c limit 1;
        update geography.administrative_areas set parent_administrative_area_id = administrative_area_id where code = 'geo047-probe-self';
        raise notice 'V9 self_parent -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'V9 self_parent -> sqlstate=%', sqlstate;
    end;
    begin
        insert into geography.countries (country_id, country_code, is_active, created_at, updated_at)
        values ('00000000-0000-4000-8000-000000042010'::uuid, 'ZZ', true, now(), now());
        insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
        values ('00000000-0000-4000-8000-000000042011'::uuid, '00000000-0000-4000-8000-000000042010'::uuid, null, 'geo047-probe-zz-area', true, now(), now());
        insert into geography.cities (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at)
        select '00000000-0000-4000-8000-000000042012'::uuid, c.country_id, '00000000-0000-4000-8000-000000042011'::uuid, 'geo047-probe-crosscountry', true, now(), now()
        from geography.countries c where c.country_code = 'IR';
        raise notice 'V10 city_cross_country_area -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'V10 city_cross_country_area -> sqlstate=%', sqlstate;
    end;
    begin
        insert into geography.city_name_translations (translation_id, city_id, language_id, name, created_at, updated_at)
        select '00000000-0000-4000-8000-000000042003'::uuid, t.city_id, t.language_id, 'probe', now(), now()
        from geography.city_name_translations t limit 1;
        raise notice 'V11 duplicate_city_label -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'V11 duplicate_city_label -> sqlstate=%', sqlstate;
    end;
    begin
        delete from geography.countries where country_id = (select country_id from geography.countries where country_code = 'IR');
        raise notice 'V12 delete_country_with_places -> NOT REJECTED (unexpected)';
    exception when others then
        raise notice 'V12 delete_country_with_places -> sqlstate=%', sqlstate;
    end;
end $$;

-- Cleanup of this script's own probe rows: a probe that unexpectedly succeeded would otherwise persist.
delete from geography.city_name_translations where name = 'probe';
delete from geography.cities where code like 'geo047-probe%';
delete from geography.administrative_areas where code like 'geo047-probe%';
delete from geography.countries where country_code = 'ZZ';

-- V13 nothing of the verification itself remains, and the bootstrapped shape is intact.
select 'V13 probe_residue=' || ((select count(*) from geography.countries where country_code = 'ZZ')
    + (select count(*) from geography.administrative_areas where code like 'geo047-probe%')
    + (select count(*) from geography.cities where code like 'geo047-probe%')
    + (select count(*) from geography.city_name_translations where name = 'probe'))
    || ' countries=' || (select count(*) from geography.countries)
    || ' areas=' || (select count(*) from geography.administrative_areas)
    || ' cities=' || (select count(*) from geography.cities)
    || ' service_areas=' || (select count(*) from geography.service_areas)
    || ' translations=' || ((select count(*) from geography.country_name_translations) + (select count(*) from geography.administrative_area_name_translations) + (select count(*) from geography.city_name_translations));
