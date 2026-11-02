# RECEIPTS — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249

## Identity

- Task: `TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249`
- Bridge row: `e783c76e-0269-42fe-9282-93b2f7bac042`
- Base-SHA: `8e73cef67d48fc8acd4ed8dc599040d2d7310130`
- ClaimedAtUtc: `2026-11-02T15:23:44Z` (approx)

## Tests

| Suite | Result |
|-------|--------|
| ShippingCarrierShipment* + ShippingRequestClassification + AuthoringContext/Owner + ErrorCatalog filter | 51 passed |
| ShippingGroupAcceptedEvidenceTests + CheckoutDirectoryCarrierQuoteFreshness | 13 passed |
| admin-order-items-shipping.test.ts | 18 passed |
| npm run typecheck | 0 |
| npm run build | 0 (Next 15.5.23) |
| Host Release publish | 0 |

## Backup / migrate

- `D:\temp\tb249-backup\tooba_alpha-pre-tb249.dump` 2709644 bytes SHA256 `9655FE63953153EEFE5D15AEAD7C4E6C24FB6FD64C528F5E2FDC179246F7C3A9`
- `Tooba.MigrationRunner apply --tenant store-alpha` → Order `20261102190000_AddAcceptedCarrierExecutionProvenance` pendingCount=0 result=ok
- Columns verified: `accepted_carrier_origin_location_code`, `accepted_carrier_destination_location_code`, `accepted_carrier_weight_grams`, `accepted_carrier_declared_value`, `accepted_carrier_length_cm`, `accepted_carrier_width_cm`, `accepted_carrier_height_cm`

## Runtime

- Host PID 46140 `artifacts/publish-primary` http://127.0.0.1:5088 /health 200
- FE PID 31248 `next start -p 3000` http://127.0.0.1:3000/fa 200
- GET `/v1/admin/shipping-services/shipments/capabilities?checkoutId=…` unauth → 401

## Controlled vs live

- Controlled provider unit tests prove create replay / UNKNOWN / label unsupported / eligibility refuse.
- Zero live Tipax network in this task (user deferred credentials).
