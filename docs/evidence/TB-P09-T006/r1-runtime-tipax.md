# R1 Runtime — Tipax Shipment

Multi-method order checkout `01a07bd2-53a4-7000-bbe6-d6cbf8f9994b` (qty=3, line `01a07bd2-53b9-7000-8ff7-08b66919c0da`):

| Method | ShipmentId | Status | Label |
|--------|------------|--------|-------|
| post | `e2639a2c-6776-46c8-88dd-575351a2cd78` | Created | پست |
| tipax | `7140c150-1921-4987-86b4-b26a58a34917` | Created | تیپاکس |
| snapp_courier | `6494430e-1315-41cb-a986-0c6d025fd2ed` | Created | اسنپ / پیک آنلاین |

## Tipax

- Tipax fields validated (recipient + fullAddress required)
- DB metadata (no secrets): packageCount, senderContact, recipient, province/city/postal, fullAddress, serviceType
- Pre-dispatch `Created`; next actions include `cancel_shipment,assign_tracking`
- No fake carrier booking

## Post

- Required Post fields accepted; metadata persisted (`ServiceType=پیشتاز`, recipient/postal/address, notes=`t006-r1-post`)
- Provider metadata version=1
