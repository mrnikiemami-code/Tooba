-- TB-SHIPPING-ONE-LEVEL-TABS-083 — READ-ONLY compatibility audit of persisted legacy method codes.
-- SELECT only. No DDL, no DML, no backfill. Target: tooba_alpha (Development), read-only.
\pset pager off
select 'order.checkouts' as source, shipping_method_code as code, length(shipping_method_code) as len, count(*) as rows
from "order".checkouts where shipping_method_code is not null group by 2,3
union all
select 'order.cart_shipping_drafts', shipping_method_code, length(shipping_method_code), count(*)
from "order".cart_shipping_drafts where shipping_method_code is not null group by 2,3
union all
select 'fulfillment.shipments', shipping_method_code, length(shipping_method_code), count(*)
from fulfillment.shipments where shipping_method_code is not null group by 2,3
union all
select 'fulfillment.fulfillments', shipping_method_code, length(shipping_method_code), count(*)
from fulfillment.fulfillments where shipping_method_code is not null group by 2,3
union all
select 'fulfillment.consolidated_packages', shipping_method_code, length(shipping_method_code), count(*)
from fulfillment.consolidated_packages where shipping_method_code is not null group by 2,3
order by 1, 2;
