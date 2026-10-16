-- TB-SHIPPING-ADMIN-ISOLATED-ACCEPTANCE-HARNESS-157 — minimum SCRATCH-ONLY destination fixture.
-- The rate write seam validates its destination through the read-only Geography contract
-- (IGeographyLookup.GetByIdsAsync), so an empty scratch geography cannot express a real scope.
-- Exactly one consistent chain (country -> area -> city -> service area) and its names are inserted
-- into the DISPOSABLE scratch database only. No shared DB, no schema change, no production data.
BEGIN;

INSERT INTO geography.countries (country_id, country_code, is_active, created_at, updated_at)
VALUES ('11111111-1111-1111-1111-111111111111', 'IR', true, now(), now())
ON CONFLICT (country_id) DO NOTHING;

INSERT INTO geography.administrative_areas
    (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
VALUES ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', NULL, 'THR', true, now(), now())
ON CONFLICT (administrative_area_id) DO NOTHING;

INSERT INTO geography.cities
    (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at)
VALUES ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111',
        '22222222-2222-2222-2222-222222222222', 'THR-C', true, now(), now())
ON CONFLICT (city_id) DO NOTHING;

INSERT INTO geography.service_areas (service_area_id, city_id, code, is_active, created_at, updated_at)
VALUES ('44444444-4444-4444-4444-444444444444', '33333333-3333-3333-3333-333333333333', 'THR-C-01', true, now(), now())
ON CONFLICT (service_area_id) DO NOTHING;

INSERT INTO geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)
VALUES ('11111111-aaaa-4aaa-8aaa-111111111111', '11111111-1111-1111-1111-111111111111',
        '00000000-0000-4000-8000-000000000001', 'ایران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

INSERT INTO geography.administrative_area_name_translations (translation_id, administrative_area_id, language_id, name, created_at, updated_at)
VALUES ('22222222-aaaa-4aaa-8aaa-222222222222', '22222222-2222-2222-2222-222222222222',
        '00000000-0000-4000-8000-000000000001', 'تهران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

INSERT INTO geography.city_name_translations (translation_id, city_id, language_id, name, created_at, updated_at)
VALUES ('33333333-aaaa-4aaa-8aaa-333333333333', '33333333-3333-3333-3333-333333333333',
        '00000000-0000-4000-8000-000000000001', 'تهران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

INSERT INTO geography.service_area_name_translations (translation_id, service_area_id, language_id, name, created_at, updated_at)
VALUES ('44444444-aaaa-4aaa-8aaa-444444444444', '44444444-4444-4444-4444-444444444444',
        '00000000-0000-4000-8000-000000000001', 'منطقه ۱', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

COMMIT;
