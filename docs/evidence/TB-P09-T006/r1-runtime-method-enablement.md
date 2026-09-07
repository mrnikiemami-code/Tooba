# R1 Runtime — Method Enablement

## Enabled list

GET `/v1/admin/shipping-methods` with all codes enabled → `post,tipax,snapp_courier,store_courier,in_person`.

## Disabled backend reject

Temporarily removed `in_person` from `Tooba:ShippingMethods:EnabledCodes` and restarted Host:

- tipaxListed remains true; `in_person` absent from GET list (`post,tipax,snapp_courier,store_courier`)
- `create_shipment` with `shippingMethodCode=in_person` on checkout `01a07bd3-ee11-7000-b51f-c5e245220244`:
  - HTTP 400
  - title=`روش ارسال برای این فروشگاه فعال نیست.`

Also previously proven with tipax disabled (same FA reject title). Backend authoritative via injected `ShippingMethodsOptions` (not frontend-only).

Config restored after smoke.

PASS=true
