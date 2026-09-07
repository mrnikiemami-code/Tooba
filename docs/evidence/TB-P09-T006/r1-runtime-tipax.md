# R1 Runtime — Tipax Shipment

- Checkout `01a07bd0-fa34-7000-a91f-6f053183d80a`
- Shipment `e1a487d9-840e-48dc-b5ce-36eb755628f8`
- Method `tipax` / label `تیپاکس`, status `Created` (pre-dispatch)
- Tipax fields validated (recipient + fullAddress required); metadata persisted (packageCount/weight/sender/recipient/province/city/postal…)
- Next actions projected: `cancel_shipment,assign_tracking`
- No fake carrier booking / success
- Tipax appears in `/v1/admin/shipping-methods` only when enabled
