-- TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-172 — SCRATCH-ONLY destination + legacy owned-rate fixture.
-- Run AFTER the Host Development bootstrap created the demo catalog/offers and after canonical migrations.
-- Only reference data and *legacy existing methods'* owned rates are inserted here. The NEW canonical-only
-- method (tipax:scratch172), its activation, its owned rate and its coverage declaration are produced
-- exclusively through the real owner Admin APIs during acceptance — never seeded.
BEGIN;

-- 1) Consistent Geography chain used by the authenticated storefront address.
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

-- 1b) Persian labels for the same chain. The storefront address form submits province/city NAMES, and
--     Geography name resolution matches a code OR any translation label; without labels the name-based
--     resolution is legitimately "unresolved" and the owned-rate gate would fail closed for every method.
INSERT INTO geography.country_name_translations (translation_id, country_id, language_id, name, created_at, updated_at)
VALUES ('11111111-aaaa-4aaa-8aaa-111111111172', '11111111-1111-1111-1111-111111111111',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'ایران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

INSERT INTO geography.administrative_area_name_translations (translation_id, administrative_area_id, language_id, name, created_at, updated_at)
VALUES ('22222222-aaaa-4aaa-8aaa-222222222271', '22222222-2222-2222-2222-222222222222',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'تهران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

INSERT INTO geography.city_name_translations (translation_id, city_id, language_id, name, created_at, updated_at)
VALUES ('33333333-aaaa-4aaa-8aaa-333333333371', '33333333-3333-3333-3333-333333333333',
        (SELECT language_id FROM localization.languages WHERE code = 'fa-IR' LIMIT 1), 'تهران', now(), now())
ON CONFLICT (translation_id) DO NOTHING;

-- 2) Owned rates for the PRE-EXISTING legacy methods only (tipax:standard / tipax:express).
--    These are not new-method fixtures: they let the gate-ON leg keep the legacy tree priced so that
--    OFF parity can be compared like-for-like. Declared at country+administrative-area scope because the
--    storefront address form resolution stops at administrative area in this chain.
INSERT INTO shipping.rate_policies
    (rate_policy_id, service_code, option_code, country_id, administrative_area_id, city_id, service_area_id,
     charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at)
VALUES
    ('21a17200-0000-4000-8000-000000000011', 'tipax', 'standard',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', NULL, NULL,
     205000.00, 'IRR', 2, 3, NULL, true, now(), now()),
    ('21a17200-0000-4000-8000-000000000012', 'tipax', 'express',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', NULL, NULL,
     480000.00, 'IRR', 1, 1, NULL, true, now(), now())
ON CONFLICT (rate_policy_id) DO NOTHING;

COMMIT;
