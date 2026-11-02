# RESULT — TB-SHIPPING-ORDER-GROUP-CARRIER-EXECUTION-249

Status: **PASS** within issued software/deploy scope  
Decision: **GROUP_SCOPED_CARRIER_EXECUTION_DEPLOYED_PROVIDER_UNVERIFIED**  
NOT Architect ACCEPT, NOT live Tipax ACCEPT, NOT PSP/paid certification

Parent: `TB-SHIPPING-CARRIER-CONFIGURATION-AND-CHECKOUT-248-R1`  
Base-SHA: `8e73cef67d48fc8acd4ed8dc599040d2d7310130`  
Stop-State: `STOP_FOR_ARCHITECT`  
automaticNextImplementationTask: `NONE`

## Summary

Detached admin/seller carrier forms are removed. CarrierApi create/track/cancel is bound to accepted `order_shipping_groups` with server-authoritative account/service/recipient/destination/package provenance. Manual/Free stay on existing fulfillment shipment workflows. Live Tipax communication remains deferred by user (no credentials requested).

## 248-R1 gaps closed here

- Unconditional detached panel with raw group ID / first active account / default service `"7"` / hardcoded package and swapped origin/destination labels.
- Order facts existence-only (no eligibility/owner/provenance).
- Seller empty status scaffold vs admin operational form.
- Client financial/routing/owner authority on create.

## Landed

- Capture quote path persists execution provenance (origin/destination codes, weight, declared value, L×W×H) into `ShippingCapturedCarrierQuoteEvidence` → `ShippingGroupAcceptedCarrierQuote`.
- Order migration `20261102190000_AddAcceptedCarrierExecutionProvenance` on cart + order groups; CHECK allows legacy quote-without-provenance (ineligible) or full provenance.
- `OrderCommittedCarrierShipmentFacts` + `ListByCheckoutAsync`: owner, transport mode, payment lock, package readiness, `CanExecuteCarrierShipment` / reason codes.
- `ShippingCarrierShipmentDirectory.CreateAsync`: requires eligibility + owner scope match + config version; ignores client account/service/recipient/package overrides; sender origin must match accepted origin.
- `GET …/shipments/capabilities?checkoutId=` admin+seller; classification **55 requests / 95 routes**.
- Admin `AdminOrderGroupCarrierExecution` inside items/shipping panel; seller equivalent; detached `AdminCarrierShipmentPanel` unmounted from order detail.

## Verification

- Shipping focused (shipment directory + classification + authoring context/owner harness + error catalog filter): **51/51**.
- Order focused (evidence Create + carrier freshness): **13/13**.
- FE `admin-order-items-shipping.test.ts`: **18/18**; `npm run typecheck` 0; `npm run build` 0.
- Host Release publish 0.

## Live / runtime

Canonical FE `http://127.0.0.1:3000` PID **31248** → Host `http://127.0.0.1:5088` PID **46140** `publish-primary` SingleStore / store-alpha / `tooba_alpha`. `/health` 200. Unauth capabilities GET **401**. No real user Order fulfilled; no live Tipax call.

## Migrations

Private backup: `D:\temp\tb249-backup\tooba_alpha-pre-tb249.dump` 2,709,644 bytes SHA256 `9655FE63953153EEFE5D15AEAD7C4E6C24FB6FD64C528F5E2FDC179246F7C3A9`.

Runner `apply --tenant store-alpha`: Order current `20261102190000_AddAcceptedCarrierExecutionProvenance`. Readback: provenance columns present on `order.order_shipping_groups`.

## Remaining

- LIVE TIPAX deferred by user.
- Orders accepted before provenance columns remain `package_incomplete` until a new lawful capture.
- Worker PASS ≠ Architect/User ACCEPT.

## Stop

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
