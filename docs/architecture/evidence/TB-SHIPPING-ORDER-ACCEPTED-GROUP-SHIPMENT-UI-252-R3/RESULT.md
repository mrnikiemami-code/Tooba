PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R3
Parent-Task: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: RECIPIENT_AS_ORIGIN_FALLBACK_REMOVED_FAIL_CLOSED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 36845985ba2d9a931b65bcec8e936ee23154f674
Base-SHA: 36845985ba2d9a931b65bcec8e936ee23154f674
Code-SHA: 590466c885891205bbc1b794d0e05237d60a9c6c
Evidence-SHA: PENDING_COMMIT
automaticNextImplementationTask: NONE

## Architect context
R2 Post trusted recipient metadata + compact modal preserved. R3 removes invented pickup/origin serialization outside Post.

## Origin / collection authority
- Committed facts expose optional `OriginPostalAddress` and `CollectionLocation` (null from current reader — no authoritative address seam yet beyond recipient).
- `OriginLocationCode` is NOT used as pickupAddress (code ≠ postal address).
- snapp_courier: pickup from `OriginPostalAddress`, destination from recipient; missing either → `fulfillment.shipping.courier.address_required` before allocate.
- in_person: pickupLocation only from `CollectionLocation`; recipient address/city never substituted → `fulfillment.shipping.pickup.location_required`.
- store_courier: validator has no required fields → emit null (directory normalizes `{}`); not guessed completeness.
- Unknown registry code → `fulfillment.shipping_method.unsupported` (not guessed `{}`).
- Post tipax recipient destination mapping unchanged; client ProviderMetadataJson still refused; CarrierApi dedicated path unchanged.

## Proof
- Origin metadata tests: distinct courier origin/dest; origin-absent refuse; collection-absent refuse; collection-only in_person; unsupported; store_courier null.
- Binder→directory Post wiring: trusted metadata persists parcel on accepted group (1/1).
- Transport routing 4/4 + Fulfillment Post directory 3/3 remain green.
- FE error-map localized origin/collection/unsupported; tsc 0; production build 0.
- Canonical FE3000→Host5088 republished; CapturedPreview local ON; keys preserved.
- Read-only order `01a1a200-…` unchanged (no mutation).

## Review URL
http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9

## Deferred
- Tipax live; Visual/User ACCEPT; populating OriginPostalAddress/CollectionLocation from a future authoritative seam.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT
