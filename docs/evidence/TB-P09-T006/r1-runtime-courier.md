# R1 Runtime — Courier / Local Shipment

- Method used: `snapp_courier` (اسنپ / پیک آنلاین) — manual/external record only
- Checkout `01a07bd2-53a4-7000-bbe6-d6cbf8f9994b`
- Shipment `6494430e-1315-41cb-a986-0c6d025fd2ed`
- Status `Created`; method label FA; next `cancel_shipment,assign_tracking`
- Metadata persisted: pickup/destination/contacts/packageDescription/`externalReference=SNAP-MANUAL-R1` — no live Snapp API success
- Postal-only fields not forced
- Also exercised `store_courier` on split-delivery shipment B (`c5de09d0-fe21-4bfe-b235-6eb412e07b28`)
- Admin Order detail DTO does not expose `providerMetadataJson` (operational metadata only in fulfillment store)
