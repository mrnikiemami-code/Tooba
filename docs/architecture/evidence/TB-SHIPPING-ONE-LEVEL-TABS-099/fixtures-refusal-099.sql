-- TB-SHIPPING-ONE-LEVEL-TABS-099 — additional SYNTHETIC refusal fixtures (scratch only).
-- An optionless method (legacy_option_code IS NULL) must be refused by the activation authority.
-- An inactive legacy parent must be refused too (never auto-activated).
begin;

insert into fulfillment.shipping_services
    (shipping_service_id, code, provider_kind, icon_key, color_key, sort_order, is_active, created_at, updated_at)
values
    ('09900000-0000-4000-8000-0000000000a3', 'scratchoff', 'post', 'post', 'blue', 30, false,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00');

insert into fulfillment.shipping_methods
    (shipping_method_id, method_code, provider_kind, legacy_service_code, legacy_option_code, sort_order, is_active, created_at, updated_at)
values
    ('09900000-0000-4000-8000-0000000000c3', 'scratchsvc', 'post', 'scratchsvc', null, 30, false,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00'),
    ('09900000-0000-4000-8000-0000000000c4', 'scratchoff:child', 'post', 'scratchoff', 'child', 40, false,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00');

commit;
