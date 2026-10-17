-- TB-SHIPPING-STOREFRONT-OWNED-RATE-LIVE-164 — SCRATCH-ONLY destination + Shipping-owned rate fixture.
-- One consistent Geography chain and exact method-owned IRR policies. A provider/service-level row
-- (option_code NULL, 99,000 IRR) is deliberately present to prove it never prices a strict option.
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

-- Exact method-owned policies: tipax:standard = 205,000 IRR, tipax:express = 480,000 IRR (city scope).
INSERT INTO shipping.rate_policies
    (rate_policy_id, service_code, option_code, country_id, administrative_area_id, city_id, service_area_id,
     charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at)
VALUES
    ('21a00000-0000-4000-8000-000000000001', 'tipax', 'standard',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222',
     '33333333-3333-3333-3333-333333333333', NULL, 205000.00, 'IRR', 2, 3, NULL, true, now(), now()),
    ('21a00000-0000-4000-8000-000000000002', 'tipax', 'express',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222',
     '33333333-3333-3333-3333-333333333333', NULL, 480000.00, 'IRR', 1, 1, NULL, true, now(), now())
ON CONFLICT (rate_policy_id) DO NOTHING;

-- Provider/service-level legacy row (option_code NULL) that must NEVER price a strict option.
INSERT INTO shipping.rate_policies
    (rate_policy_id, service_code, option_code, country_id, administrative_area_id, city_id, service_area_id,
     charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at)
VALUES ('21a00000-0000-4000-8000-000000000003', 'tipax', NULL,
        '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222',
        '33333333-3333-3333-3333-333333333333', NULL, 99000.00, 'IRR', 1, 5, NULL, true, now(), now())
ON CONFLICT (rate_policy_id) DO NOTHING;

-- Explicit Unrestricted declarations (TB-161 default expressed explicitly) for both strict methods.
INSERT INTO shipping.method_coverages
    (method_coverage_id, service_code, option_code, mode, created_at, updated_at)
VALUES
    ('31a00000-0000-4000-8000-000000000001', 'tipax', 'standard', 0, now(), now()),
    ('31a00000-0000-4000-8000-000000000002', 'tipax', 'express', 0, now(), now())
ON CONFLICT (method_coverage_id) DO NOTHING;

COMMIT;
