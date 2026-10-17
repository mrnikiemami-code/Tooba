-- TB-172 — SCRATCH-ONLY tax closes for the authenticated customer's seller categories.
-- The Development catalog bootstrap classifies offers but only one demo category carries an active 9% rule.
-- Without a rule the read-only payment draft legitimately returns 409 checkout.tax.unavailable, so the
-- authenticated journey cannot reach the arithmetic/identity assertions. These rules are scratch reference
-- data for the demo categories of the seeded seller, not shipping or payment fixtures.
BEGIN;

INSERT INTO tax.rules
    (rule_id, jurisdiction, market, category_id, kind, rate, effective_from, effective_to,
     status, specificity, override_policy, created_at, updated_at)
SELECT
    ('21a17200-0000-4000-8000-0000000000' || lpad(row_number() over (order by oc.category_id)::text, 2, '0'))::uuid,
    'IR-NAT', 'IR', oc.category_id, 'Percentage', 0.09000000, '2026-01-01 00:00:00+00', NULL,
    'Active', 10, 'Disabled', now(), now()
FROM (
    SELECT DISTINCT category_id
    FROM tax.offer_classifications
    WHERE offer_id IN (
        SELECT offer_id FROM offer.offers
        WHERE seller_party_id = (SELECT seller_party_id FROM offer.offers ORDER BY seller_party_id, offer_id LIMIT 1)
    )
) AS oc
WHERE NOT EXISTS (
    SELECT 1 FROM tax.rules r
    WHERE r.category_id = oc.category_id AND r.status = 'Active'
)
ON CONFLICT (rule_id) DO NOTHING;

COMMIT;
