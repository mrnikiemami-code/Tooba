-- TB-SHIPPING-NEW-METHOD-ACTIVATION-170 — SCRATCH-ONLY Geography reference fixture.
-- Only the destination reference chain is inserted (country/area/city + labels). No rate, no coverage,
-- no legacy service option and no method row is created here: those must be produced through the real
-- owner Admin APIs during acceptance so the run proves the actual pathway, not a seeded shortcut.
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

COMMIT;
