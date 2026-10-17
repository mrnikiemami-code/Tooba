-- TB-SHIPPING-METHOD-COVERAGE-BROWSER-174 — SCRATCH-ONLY geography + owned-rate fixture.
-- Run AFTER canonical migrations and the Host Development bootstrap. Reference data only:
-- the coverage mode and the grant are written exclusively through the real Admin UI / owner API.
BEGIN;

-- Destination 1 (allowed by the method-owned grant): Tehran.
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

-- Destination 2 (NOT granted to the method, but fully rated and name-resolvable): Karaj.
INSERT INTO geography.administrative_areas
    (administrative_area_id, country_id, parent_administrative_area_id, code, is_active, created_at, updated_at)
VALUES ('55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', NULL, 'ABZ', true, now(), now())
ON CONFLICT (administrative_area_id) DO NOTHING;

INSERT INTO geography.cities
    (city_id, country_id, administrative_area_id, code, is_active, created_at, updated_at)
VALUES ('66666666-6666-6666-6666-666666666666', '11111111-1111-1111-1111-111111111111',
        '55555555-5555-5555-5555-555555555555', 'ABZ-C', true, now(), now())
ON CONFLICT (city_id) DO NOTHING;

INSERT INTO geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)
VALUES ('11111111-aaaa-4aaa-8aaa-111111111174', '11111111-1111-1111-1111-111111111111',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'ایران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

INSERT INTO geography.administrative_area_name_translations (translation_id, administrative_area_id, language_id, name, created_at, updated_at)
VALUES ('22222222-aaaa-4aaa-8aaa-222222222274', '22222222-2222-2222-2222-222222222222',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'تهران', now(), now()),
       ('55555555-aaaa-4aaa-8aaa-555555555574', '55555555-5555-5555-5555-555555555555',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'البرز', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

INSERT INTO geography.city_name_translations (translation_id, city_id, language_id, name, created_at, updated_at)
VALUES ('33333333-aaaa-4aaa-8aaa-333333333374', '33333333-3333-3333-3333-333333333333',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'تهران', now(), now()),
       ('66666666-aaaa-4aaa-8aaa-666666666674', '66666666-6666-6666-6666-666666666666',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'کرج', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

-- Method-owned rates for BOTH real options at BOTH administrative areas. The second destination is
-- fully priced on purpose: excluding `standard` there while Restricted can only be the coverage gate,
-- never a missing rate.
INSERT INTO shipping.rate_policies
    (rate_policy_id, service_code, option_code, country_id, administrative_area_id, city_id, service_area_id,
     charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at)
VALUES
    ('21a17400-0000-4000-8000-000000000011', 'tipax', 'standard',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', NULL, NULL,
     205000.00, 'IRR', 2, 3, NULL, true, now(), now()),
    ('21a17400-0000-4000-8000-000000000012', 'tipax', 'express',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', NULL, NULL,
     480000.00, 'IRR', 1, 1, NULL, true, now(), now()),
    ('21a17400-0000-4000-8000-000000000013', 'tipax', 'standard',
     '11111111-1111-1111-1111-111111111111', '55555555-5555-5555-5555-555555555555', NULL, NULL,
     210000.00, 'IRR', 2, 3, NULL, true, now(), now()),
    ('21a17400-0000-4000-8000-000000000014', 'tipax', 'express',
     '11111111-1111-1111-1111-111111111111', '55555555-5555-5555-5555-555555555555', NULL, NULL,
     490000.00, 'IRR', 1, 1, NULL, true, now(), now())
ON CONFLICT (rate_policy_id) DO NOTHING;

COMMIT;
