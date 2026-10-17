-- TB-SHIPPING-STOREFRONT-PRIVATE-BROWSER-166 — scratch-only closes for the reusable seed recipe.
-- Run AFTER the Host Development bootstrap has created the demo catalog/offers/tax classifications,
-- and AFTER applying migrations for Geography and Shipping with TOOBA_DESIGN_TIME_CONNECTION.
-- Identifiers are the deterministic development bootstrap values observed in the disposable scratch DB.
-- Replace them with the values you read back from your own scratch run (see seed-recipe-166.md §3).

BEGIN;

-- 1) Active 9% tax rules for the categories of seller A's offers.
--    The development bootstrap only classifies the seller-B category (…3fd3…) with an active rule;
--    without a rule the payment draft returns 409 and the authenticated journey cannot finish.
INSERT INTO tax.rules
    (rule_id, jurisdiction, market, category_id, kind, rate, effective_from, effective_to,
     status, specificity, override_policy, created_at, updated_at)
VALUES
    ('01a16600-0000-4000-8000-000000000a01', 'IR-NAT', 'IR', '01a14803-3dc5-7000-8c12-e11b78b232a5',
     'Percentage', 0.09000000, '2026-01-01 00:00:00+00', NULL, 'Active', 10, 'Disabled', now(), now()),
    ('01a16600-0000-4000-8000-000000000a02', 'IR-NAT', 'IR', '01a14803-3e21-7000-8fd5-028bf3d90edb',
     'Percentage', 0.09000000, '2026-01-01 00:00:00+00', NULL, 'Active', 10, 'Disabled', now(), now())
ON CONFLICT (rule_id) DO NOTHING;

-- 2) Administrative-area scoped owner rates.
--    The storefront address form submits provinceName/cityName only. A policy scoped down to city_id
--    does not match that name-based resolution in this scratch chain, so the same amounts are also
--    declared at country+administrative-area scope. Method-owned exact-option amounts are unchanged:
--    tipax:standard = 205000 IRR, tipax:express = 480000 IRR.
INSERT INTO shipping.rate_policies
    (rate_policy_id, service_code, option_code, country_id, administrative_area_id, city_id, service_area_id,
     charge_amount, currency_code, preparation_days, lead_days, free_above_threshold, is_active, created_at, updated_at)
VALUES
    ('21a16600-0000-4000-8000-000000000011', 'tipax', 'standard',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', NULL, NULL,
     205000.00, 'IRR', 2, 3, NULL, true, now(), now()),
    ('21a16600-0000-4000-8000-000000000012', 'tipax', 'express',
     '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', NULL, NULL,
     480000.00, 'IRR', 1, 1, NULL, true, now(), now())
ON CONFLICT (rate_policy_id) DO NOTHING;

COMMIT;
