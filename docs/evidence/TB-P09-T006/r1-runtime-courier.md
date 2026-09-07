# R1 Runtime — Courier / Local Shipment

- Method used: `snapp_courier` (اسنپ / پیک آنلاین) — manual/external record only
- Checkout `01a07bd2-d8ff-7000-9642-3e06064575bb`
- Shipment `f113e9c6-ce3d-493c-bada-c59ac08a1710`
- Status `Created`; method label FA; next `cancel_shipment,assign_tracking`
- Metadata: pickup/destination/contacts/window/package/driverNote/externalReference — no live Snapp API success
- Postal-only fields not forced
- Also exercised `store_courier` on split-delivery shipments
