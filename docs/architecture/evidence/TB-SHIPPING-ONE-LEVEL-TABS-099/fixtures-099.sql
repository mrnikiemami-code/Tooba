-- TB-SHIPPING-ONE-LEVEL-TABS-099 — minimal deterministic SYNTHETIC fixtures for the isolated scratch DB.
-- Scratch-only. Contains no real customer/merchant/order data and no copied tooba_alpha rows.
-- One ACTIVE legacy parent + two ACTIVE option siblings + their aligned one-level projection rows.
-- Deterministic UUIDs are task-owned constants so identity can be re-verified before/after.
begin;

insert into fulfillment.shipping_services
    (shipping_service_id, code, provider_kind, icon_key, color_key, sort_order, is_active, created_at, updated_at)
values
    ('09900000-0000-4000-8000-0000000000a1', 'scratchsvc', 'post', 'post', 'blue', 10, true,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00');

insert into fulfillment.shipping_service_options
    (shipping_service_option_id, shipping_service_id, code, sort_order, is_active, created_at, updated_at)
values
    ('09900000-0000-4000-8000-0000000000b1', '09900000-0000-4000-8000-0000000000a1', 'alpha', 10, true,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00'),
    ('09900000-0000-4000-8000-0000000000b2', '09900000-0000-4000-8000-0000000000a1', 'beta', 20, true,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00');

insert into fulfillment.shipping_methods
    (shipping_method_id, method_code, provider_kind, legacy_service_code, legacy_option_code, sort_order, is_active, created_at, updated_at)
values
    ('09900000-0000-4000-8000-0000000000c1', 'scratchsvc:alpha', 'post', 'scratchsvc', 'alpha', 10, true,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00'),
    ('09900000-0000-4000-8000-0000000000c2', 'scratchsvc:beta', 'post', 'scratchsvc', 'beta', 20, true,
     timestamptz '2026-10-16 00:00:00+00', timestamptz '2026-10-16 00:00:00+00');

commit;
