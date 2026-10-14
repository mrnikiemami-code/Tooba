-- TB-SHIPPING-GEOGRAPHY-READ-FOUNDATION-043 — read-side seed for the disposable read-verification database.
-- Destination: disposable, task-exclusive container geo043-verify (postgres:17), db geo043, host port 55433.
-- The schema is created by the 042-validated migration source (Up). This file only inserts reference rows.
\set ON_ERROR_STOP on

-- countries: two active, one inactive
insert into geography.countries (country_id, country_code, is_active, created_at, updated_at) values
 ('10000000-0000-0000-0000-000000000001', 'IRN', true,  now(), now()),
 ('10000000-0000-0000-0000-000000000002', 'DEU', true,  now(), now()),
 ('10000000-0000-0000-0000-000000000003', 'FRA', false, now(), now());

insert into geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at) values
 (gen_random_uuid(), '10000000-0000-0000-0000-000000000001', 'fa000000-0000-0000-0000-000000000000', 'ایران', now(), now()),
 (gen_random_uuid(), '10000000-0000-0000-0000-000000000001', 'ee000000-0000-0000-0000-000000000000', 'Iran', now(), now()),
 (gen_random_uuid(), '10000000-0000-0000-0000-000000000002', 'ee000000-0000-0000-0000-000000000000', 'Germany', now(), now());
-- administrative areas of IRN; MZK1/MZK2 deliberately share one label (ambiguous area label)
insert into geography.administrative_areas (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at) values
 ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', null, 'THR',  true, now(), now()),
 ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', null, 'ALB',  true, now(), now()),
 ('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', null, 'MZK1', true, now(), now()),
 ('20000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', null, 'MZK2', true, now(), now());

insert into geography.administrative_area_name_translations (translation_id, administrative_area_id, language_id, name, created_at, updated_at) values
 (gen_random_uuid(), '20000000-0000-0000-0000-000000000001', 'fa000000-0000-0000-0000-000000000000', 'تهران', now(), now()),
 (gen_random_uuid(), '20000000-0000-0000-0000-000000000001', 'ee000000-0000-0000-0000-000000000000', 'Tehran', now(), now()),
 (gen_random_uuid(), '20000000-0000-0000-0000-000000000002', 'fa000000-0000-0000-0000-000000000000', 'البرز', now(), now()),
 (gen_random_uuid(), '20000000-0000-0000-0000-000000000002', 'ee000000-0000-0000-0000-000000000000', 'Alborz', now(), now()),
 (gen_random_uuid(), '20000000-0000-0000-0000-000000000003', 'fa000000-0000-0000-0000-000000000000', 'مرکزی', now(), now()),
 (gen_random_uuid(), '20000000-0000-0000-0000-000000000004', 'fa000000-0000-0000-0000-000000000000', 'مرکزی', now(), now());

-- cities of IRN; SV1/SV2 share one label inside two different areas (must not be collapsed)
-- EN1 has only an English label; NOTR has no label at all; INA is inactive
insert into geography.cities (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at) values
 ('30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'TEH1',  true,  now(), now()),
 ('30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002', 'KAR',   true,  now(), now()),
 ('30000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000003', 'SV1',   true,  now(), now()),
 ('30000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000004', 'SV2',   true,  now(), now()),
 ('30000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'EN1',   true,  now(), now()),
 ('30000000-0000-0000-0000-000000000006', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'NOTR',  true,  now(), now()),
 ('30000000-0000-0000-0000-000000000007', '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 'INA',   false, now(), now()),
 ('30000000-0000-0000-0000-000000000008', '10000000-0000-0000-0000-000000000002', null,                                        'BER',   true,  now(), now());

insert into geography.city_name_translations (translation_id, city_id, language_id, name, created_at, updated_at) values
 (gen_random_uuid(), '30000000-0000-0000-0000-000000000001', 'fa000000-0000-0000-0000-000000000000', 'تهران', now(), now()),
 (gen_random_uuid(), '30000000-0000-0000-0000-000000000001', 'ee000000-0000-0000-0000-000000000000', 'Tehran', now(), now()),
 (gen_random_uuid(), '30000000-0000-0000-0000-000000000002', 'fa000000-0000-0000-0000-000000000000', 'کرج', now(), now()),
 (gen_random_uuid(), '30000000-0000-0000-0000-000000000003', 'fa000000-0000-0000-0000-000000000000', 'ساوه', now(), now()),
 (gen_random_uuid(), '30000000-0000-0000-0000-000000000004', 'fa000000-0000-0000-0000-000000000000', 'ساوه', now(), now()),
 (gen_random_uuid(), '30000000-0000-0000-0000-000000000005', 'ee000000-0000-0000-0000-000000000000', 'EnOnlyCity', now(), now());

-- service areas; SA1/SA3 share a label in two different cities (city scoping must separate them)
insert into geography.service_areas (service_area_id, city_id, code, is_active, created_at, updated_at) values
 ('40000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001', 'SA1', true, now(), now()),
 ('40000000-0000-0000-0000-000000000002', '30000000-0000-0000-0000-000000000001', 'SA2', true, now(), now()),
 ('40000000-0000-0000-0000-000000000003', '30000000-0000-0000-0000-000000000002', 'SA3', true, now(), now());

insert into geography.service_area_name_translations (translation_id, service_area_id, language_id, name, created_at, updated_at) values
 (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', 'fa000000-0000-0000-0000-000000000000', 'منطقه ۱', now(), now()),
 (gen_random_uuid(), '40000000-0000-0000-0000-000000000001', 'ee000000-0000-0000-0000-000000000000', 'Zone 1', now(), now()),
 (gen_random_uuid(), '40000000-0000-0000-0000-000000000002', 'fa000000-0000-0000-0000-000000000000', 'منطقه ۲', now(), now()),
 (gen_random_uuid(), '40000000-0000-0000-0000-000000000003', 'fa000000-0000-0000-0000-000000000000', 'منطقه ۱', now(), now());

select 'countries' as t, count(*) from geography.countries
union all select 'areas', count(*) from geography.administrative_areas
union all select 'cities', count(*) from geography.cities
union all select 'service_areas', count(*) from geography.service_areas
union all select 'city_labels', count(*) from geography.city_name_translations;
