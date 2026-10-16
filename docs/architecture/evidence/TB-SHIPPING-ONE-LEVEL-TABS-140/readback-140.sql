-- TB-SHIPPING-ONE-LEVEL-TABS-140 readback (read-only). Scratch DB: tooba_140_scratch on 127.0.0.1:55461.
-- VERIFIED run = the isolated sandbox journey executed AFTER the Fulfillment shipping-group mapping repair.
-- Pre-fix attempts left 3 checkouts on this same scratch DB; the SCOPED section isolates the verified run.
\pset footer off
\set chk '01a143bb-c830-7000-913e-58cd322fddc4'
\set so '01a143bb-c83a-7000-b6d7-e5c6c9f63f17'
\set pay '01a143bb-c9d1-7000-847e-1204a1d13012'
\set evt '01a143bb-ca43-7000-ab7e-f3fcf41f41db'

\echo == A. SCOPED TO THE VERIFIED CHECKOUT ==
select 'A1_checkouts=' || count(*) from "order".checkouts where checkout_id = :'chk';
select 'A2_seller_orders=' || count(*) || ' paid=' || count(*) filter (where status = 'Paid') || '|' || coalesce(max(order_number), '-') || '|' || coalesce(max(grand_total_snapshot::text), '-') || '|' || coalesce(max(currency), '-') from "order".seller_orders where seller_order_id = :'so';
select 'A3_order_shipping_groups=' || count(*) from "order".order_shipping_groups where seller_order_id = :'so';
select 'A4_GROUP=' || g.shipping_group_id || '|' || g.method_code || '|' || g.service_code || '|' || g.option_code || '|' || g.shipping_charge_snapshot || '|' || g.state || '|' || g.requested_delivery_date || '|' || g.requested_delivery_time_window || '|' || g.lead_days from "order".order_shipping_groups g where g.seller_order_id = :'so' order by g.method_code;
select 'A5_order_lines=' || count(*) from "order".order_lines where seller_order_id = :'so';
select 'A6_LINE=' || l.line_id || '|' || l.offer_id || '|' || l.quantity || '|' || l.line_total_snapshot from "order".order_lines l where l.seller_order_id = :'so' order by l.offer_id;
select 'A7_group_line_links=' || count(*) from "order".order_shipping_group_lines gl join "order".order_shipping_groups g on g.shipping_group_id = gl.shipping_group_id where g.seller_order_id = :'so';
select 'A8_LINK=' || gl.shipping_group_id || '|' || gl.order_line_id from "order".order_shipping_group_lines gl join "order".order_shipping_groups g on g.shipping_group_id = gl.shipping_group_id where g.seller_order_id = :'so' order by gl.shipping_group_id, gl.order_line_id;
select 'A9_order_payment_inbox=' || count(*) || '|event=' || coalesce(string_agg(event_id::text || '|payment=' || payment_id::text, ','), '-') from "order".payment_inbox where payment_id = :'pay';
select 'A10_payments=' || count(*) || ' succeeded=' || count(*) filter (where status = 'Succeeded') from payment.payments where payment_id = :'pay';
select 'A11_allocations=' || count(*) from payment.allocations a join payment.payments p on p.payment_id = a.payment_id where p.payment_id = :'pay';
select 'A12_ALLOC=' || a.seller_order_id || '|' || a.target_kind || '|' || a.allocated_amount || '|' || a.currency from payment.allocations a where a.payment_id = :'pay' order by a.target_kind desc, a.allocated_amount;
select 'A13_fulfillment_inbox=' || count(*) || '|event=' || coalesce(string_agg(event_id::text || '|payment=' || payment_id::text || '|processed=' || (processed_at is not null)::text, ','), '-') from fulfillment.payment_inbox where event_id = :'evt';
select 'A14_fulfillment_units=' || count(*) from fulfillment.fulfillments where seller_order_id = :'so';
select 'A15_UNIT=' || f.fulfillment_id || '|' || f.seller_order_id || '|' || f.checkout_id || '|' || f.status || '|' || coalesce(nullif(f.shipping_method_code, ''), '<multi-group>') || '|' || coalesce(nullif(f.shipping_method_label, ''), '<multi-group>') from fulfillment.fulfillments f where f.seller_order_id = :'so';
select 'A16_fulfillment_groups=' || count(*) from fulfillment.shipping_groups g join fulfillment.fulfillments f on f.fulfillment_id = g.fulfillment_id where f.seller_order_id = :'so';
select 'A17_GROUP=' || g.shipping_group_id || '|' || g.fulfillment_id || '|' || g.seller_order_id || '|' || g.method_code || '|' || g.service_code || '|' || g.option_code || '|' || g.method_label || '|' || g.requested_delivery_date || '|' || g.requested_delivery_time_window || '|' || g.lead_days from fulfillment.shipping_groups g join fulfillment.fulfillments f on f.fulfillment_id = g.fulfillment_id where f.seller_order_id = :'so' order by g.method_code;
select 'A18_fulfillment_items=' || count(*) from fulfillment.items i join fulfillment.fulfillments f on f.fulfillment_id = i.fulfillment_id where f.seller_order_id = :'so';
select 'A19_ITEM=' || i.fulfillment_id || '|' || i.order_line_id || '|' || i.quantity_ordered || '|group=' || coalesce(i.shipping_group_id::text, '-') from fulfillment.items i join fulfillment.fulfillments f on f.fulfillment_id = i.fulfillment_id where f.seller_order_id = :'so' order by i.order_line_id;
select 'A20_item_group_matches_order_group=' || (count(*) filter (where og.seller_order_id is not null) = count(*))::text from fulfillment.items i join fulfillment.fulfillments f on f.fulfillment_id = i.fulfillment_id left join "order".order_shipping_groups og on og.shipping_group_id = i.shipping_group_id where f.seller_order_id = :'so';
select 'A21_fulfillment_shipments=' || count(*) from fulfillment.shipments s join fulfillment.fulfillments f on f.fulfillment_id = s.fulfillment_id where f.seller_order_id = :'so';

\echo == B. DB-WIDE FULFILLMENT INVARIANT (whole scratch database) ==
select 'B1_fulfillment_units=' || count(*) from fulfillment.fulfillments;
select 'B2_fulfillment_groups=' || count(*) from fulfillment.shipping_groups;
select 'B3_fulfillment_items=' || count(*) from fulfillment.items;
select 'B4_fulfillment_payment_inbox=' || count(*) from fulfillment.payment_inbox;
select 'B5_fulfillment_shipments=' || count(*) from fulfillment.shipments;
select 'B6_shadow_fk_column_present=' || count(*) from information_schema.columns where table_schema = 'fulfillment' and table_name = 'shipping_groups' and column_name = 'fulfillment_unit_fulfillment_id';
select 'B7_groups_per_unit=' || string_agg(x.unit || '=>' || x.groups, ',') from (select f.fulfillment_id::text as unit, count(g.shipping_group_id)::text as groups from fulfillment.fulfillments f left join fulfillment.shipping_groups g on g.fulfillment_id = f.fulfillment_id group by f.fulfillment_id) x;

\echo == C. PRE-EXISTING ATTEMPTS ON THE SAME SCRATCH DB (context, not part of the verified run) ==
select 'C1_checkouts_total=' || count(*) from "order".checkouts;
select 'C2_seller_orders_total=' || count(*) || ' paid=' || count(*) filter (where status = 'Paid') from "order".seller_orders;
select 'C3_row=' || seller_order_id || '|' || status || '|' || order_number from "order".seller_orders order by order_number;
select 'C4_payments_total=' || count(*) from payment.payments;

\echo == D. OUTBOX PROCESSING ==
select 'D1_order_outbox_pending=' || count(*) filter (where processed_at is null) || ' of ' || count(*) from "order".outbox_messages;
select 'D2_fulfillment_outbox_pending=' || count(*) filter (where processed_at is null) || ' of ' || count(*) from fulfillment.outbox_messages;
select 'D3_payment_outbox_pending=' || count(*) filter (where processed_at is null) || ' of ' || count(*) from payment.outbox_messages;
