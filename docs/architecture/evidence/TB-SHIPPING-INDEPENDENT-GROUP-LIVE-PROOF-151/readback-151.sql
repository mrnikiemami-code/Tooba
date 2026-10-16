-- TB-SHIPPING-INDEPENDENT-GROUP-LIVE-PROOF-151 — read-only persisted-state read-back.
\pset pager off
\echo == A. draft shipping groups (per-group schedule authority) ==
select shipping_group_id, seller_party_id, method_code, service_code, option_code,
       shipping_amount, currency, minimum_delivery_date, selected_delivery_date,
       selected_delivery_time_window, lead_days, state, updated_at
from "order".cart_shipping_groups
order by seller_party_id;

\echo == B. draft group membership (each cart line exactly once) ==
select l.shipping_group_id, l.cart_line_id, g.seller_party_id, g.method_code
from "order".cart_shipping_group_lines l
join "order".cart_shipping_groups g on g.shipping_group_id = l.shipping_group_id
order by l.cart_line_id;

\echo == C. group count / membership integrity ==
select (select count(*) from "order".cart_shipping_groups) as draft_groups,
       (select count(distinct shipping_group_id) from "order".cart_shipping_group_lines) as groups_with_lines,
       (select count(*) from "order".cart_shipping_group_lines) as membership_rows,
       (select count(distinct cart_line_id) from "order".cart_shipping_group_lines) as distinct_lines;

\echo == D. draft header scalars (multi-group must keep no global schedule) ==
select cart_id, cart_version, shipping_method_code, shipping_amount,
       minimum_delivery_date, selected_delivery_date, selected_delivery_time_window
from "order".cart_shipping_drafts;

\echo == E. no checkout / order / reservation was created by the shipping flow ==
select (select count(*) from "order".checkouts) as checkouts,
       (select count(*) from "order".order_shipping_groups) as order_shipping_groups,
       (select count(*) from "order".seller_orders) as seller_orders;

\echo == F. tax fixture coverage (why the read-only payment draft answered 409) ==
select (select count(*) from tax.offer_classifications) as tax_offer_classifications,
       (select count(*) from tax.rules) as tax_rules,
       (select string_agg(distinct category_id::text, ',') from tax.rules) as rule_categories,
       (select string_agg(category_id::text, ',') from tax.offer_classifications where offer_id in
            ('01a145fd-fe25-7000-8667-69e0b6b8530d', '01a145fe-00b4-7000-8c42-4f0307b65d72')) as cart_offer_categories;
