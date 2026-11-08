PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253
Parent-Task: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R3
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PARTIAL
Decision: OPERATIONAL_DELIVERY_GROUP_AUTHORITY_AND_UI_DEPLOYED_RELATIONAL_MATRIX_UNMET
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: e33517eb45b319ee77f53efb3256b91ad4451b4e
Base-SHA: e33517eb45b319ee77f53efb3256b91ad4451b4e
Code-SHA: e899c72ba3992b2538163fc1dcbf587d21d96ab6
Evidence-SHA: 27983b59d67227ff7059d29cb50a2c07946e2839
automaticNextImplementationTask: NONE

## Architect context
Server-owned Marketplace operational delivery grouping + professional «ارسال سفارش» UI deployed on canonical FE3000→Host5088 SingleStore/store-alpha/tooba_alpha. Cross-seller merge is Marketplace-only; Store is never promoted; Seller stays separate; empty origin never equals.

## Product rule recorded
- Cross-seller operational grouping requires Marketplace owner identity + proven non-empty origin + exact method/service/option + exact day/open/close/timezone + compatible transport binding.
- Store and Seller remain separate even with identical method/date/hour.
- Consolidated-package create/offer uses the same authority (CarrierApi cross-group consolidate refused).

## LANDED
- `MarketplaceOperationalDeliveryGroupAuthority` (Contracts) + admin detail `operationalDeliveryGroups` projection.
- CreateConsolidatedPackage mutation + ops projection guarded by the same authority.
- FE `AdminOrderOperationalDeliverySection` primary cards; seller packing sections retained without duplicate create cards.
- Adjacent deposit ConfirmEligible + method-identity normalize + dialog wrap preserved in tree.

## Proof
- A Authority matrix: **8/8** Order.Tests MarketplaceOperationalDeliveryGroupAuthorityTests.
- B Disposable relational command→Fulfillment matrix: **UNMET** this box (not substituted with InMemory).
- C Mounted FE: operational delivery mount + panel/adjacent **25/25**; typecheck 0; production build 0.
- D Canonical read-only order `01a1b92d-e97e-7000-999a-143350d0edaf`: ownerKinds Store,Store,Store → **3 separate** operational groups (no cross-merge). FE page 200. No pack/create/cancel/payment mutation.

## Environment
- FE `http://127.0.0.1:3000` → Host `http://127.0.0.1:5088` publish-primary PID6472 DeploymentId `store-alpha-canonical-tb253` Edition SingleStore tenant store-alpha / tooba_alpha.
- CapturedPreview local ON (Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7); checked-in OFF.
- DataProtection keys preserved (KEYS_AFTER=True).

## Review URL
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf

## Unresolved / deferred
- Disposable PostgreSQL relational consolidate matrix (required for unqualified PASS).
- Live Tipax; Visual/User ACCEPT; Marketplace fixture live UI (canonical order is Store-owned).

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT
