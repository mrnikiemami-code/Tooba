# R1 Runtime — Method Enablement

GET `/v1/admin/shipping-methods` after removing `tipax` from `Tooba:ShippingMethods:EnabledCodes`:

- tipaxListed=false
- codes=post, snapp_courier, store_courier, in_person

`create_shipment` with `shippingMethodCode=tipax` while disabled:

- checkout=`01a07bcf-75a6-7000-a785-32db368dd09f`
- HTTP=400
- title=`روش ارسال برای این فروشگاه فعال نیست.`

Defect fix: `AdminOrderOperationsComposer` now uses injected `ShippingMethodsOptions` (previously `Enabled(null)` always allowed all registry codes).

PASS=true
