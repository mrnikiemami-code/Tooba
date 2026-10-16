\echo == draft groups: identity + schedule + membership (TB-153) ==
select g.shipping_group_id, g.seller_party_id, g.method_code, g.service_code, g.option_code,
       g.shipping_amount, g.currency, g.minimum_delivery_date, g.selected_delivery_date,
       g.selected_delivery_time_window, g.lead_days, g.state
from "order".cart_shipping_groups g
order by g.seller_party_id;
\echo == one-to-one line membership ==
select l.shipping_group_id, l.cart_line_id from "order".cart_shipping_group_lines l order by l.shipping_group_id, l.cart_line_id;
\echo == duplicate id / duplicate line probes (both must be 0 rows) ==
select 'duplicate_group_ids' k, count(*) c from (select shipping_group_id from "order".cart_shipping_groups group by shipping_group_id having count(*) > 1) d
union all
select 'lines_in_two_groups', count(*) from (select cart_line_id from "order".cart_shipping_group_lines group by cart_line_id having count(*) > 1) x
union all
select 'orphan_join_rows', count(*) from "order".cart_shipping_group_lines l
    left join "order".cart_shipping_groups g on g.shipping_group_id = l.shipping_group_id where g.shipping_group_id is null;
\echo == zero checkout / order / payment probe ==
select 'checkouts' k, count(*) c from "order".checkouts
union all select 'seller_orders', count(*) from "order".seller_orders
union all select 'order_lines', count(*) from "order".order_lines
union all select 'order_shipping_groups', count(*) from "order".order_shipping_groups
union all select 'payments', count(*) from payment.payments
union all select 'payment_attempts', count(*) from payment.attempts
union all select 'cart_groups', count(*) from "order".cart_shipping_groups
union all select 'cart_group_lines', count(*) from "order".cart_shipping_group_lines
union all select 'tax_rules', count(*) from tax.rules;
