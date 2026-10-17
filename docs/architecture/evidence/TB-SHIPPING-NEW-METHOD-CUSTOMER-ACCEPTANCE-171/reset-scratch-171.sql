-- TB-SHIPPING-NEW-METHOD-CUSTOMER-ACCEPTANCE-171 — scratch-only reset so the acceptance run is repeatable.
-- Removes ONLY the rows the acceptance itself created for the new canonical method and the shipping draft
-- of the scratch customer. No seeded/demo data, no other method and no shared database is touched.
BEGIN;
DELETE FROM shipping.rate_policies WHERE service_code = 'tipax' AND option_code = 'scratch171';
DELETE FROM shipping.coverage_grants WHERE service_code = 'tipax' AND option_code = 'scratch171';
DELETE FROM shipping.method_coverages WHERE service_code = 'tipax' AND option_code = 'scratch171';
DELETE FROM "order".cart_shipping_group_lines;
DELETE FROM "order".cart_shipping_groups;
DELETE FROM "order".cart_shipping_drafts;
DELETE FROM fulfillment.shipping_method_translations t
    USING fulfillment.shipping_methods m
    WHERE t.shipping_method_id = m.shipping_method_id AND m.method_code = 'tipax:scratch171';
DELETE FROM fulfillment.shipping_methods WHERE method_code = 'tipax:scratch171';
COMMIT;
