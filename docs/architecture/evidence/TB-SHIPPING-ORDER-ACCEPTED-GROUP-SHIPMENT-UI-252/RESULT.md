PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252
Parent-Task: TB-STOREFRONT-GROUP-DELIVERY-CALENDAR-CLOSURE-251
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: ACCEPTED_GROUP_PARCEL_EXECUTION_UI_AND_SERVER_BIND_DEPLOYED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 7786d4bc0898b03fcdef6a7bbcba2a1c6950eb82
Base-SHA: 7786d4bc0898b03fcdef6a7bbcba2a1c6950eb82
Code-SHA: 068fb09fc059e3ae1856a0e0829102499290e61c
Evidence-SHA: EVIDENCE_SHA_PENDING
automaticNextImplementationTask: NONE

## Model (agreed)
Seller section → accepted delivery GROUPS → physical parcels from one group.
Method/date/time/fee are read-only accepted facts. No post-order method change.

## Legacy override removed/guarded
- `AdminCreateShipmentModal` no longer loads method catalogue / defaults to `post`.
- Create opens only from an accepted-group card with `shippingGroupId`.
- Ops-menu `create_shipment` no longer offers carrier dropdown override; directs to group cards.
- Server `CreateShipmentCoreAsync` binds via `AcceptedGroupShipmentCreateBinder` using `IOrderCommittedCarrierShipmentFactsReader`; derives method; refuses missing/foreign/cross-line/excess/method-mismatch/legacy-absent before fulfillment allocate.
- Fulfillment domain defense: `EnsureShippingGroupMethod`.

## Software proof
- Binder unit tests 7/7.
- FulfillmentShippingGroupExecutionTests 10/10.
- FE source contracts 4/4 (`admin-order-accepted-group-shipment-252.test.ts`).
- FE typecheck 0; production build 0.
- Canonical Host republished; FE3000 production restarted.
- Local CapturedPreview env ON (Enabled/SelectionEnabled/CheckoutEnabled + AvailableDateCount=7); checked-in defaults unchanged OFF.
- Authorized read-only order `01a1a200-cbd2-7000-9901-4b9fd076f5d9`: 2 accepted groups, both method «پیشتاز», distinct dates 2026-11-23 / 2026-11-30 windows 09:00–10:00 Asia/Tehran, owner Store, fees 125000 IRR; create eligible on group with remaining packed qty; other group blocked with remaining-qty reason. No create/pack/dispatch mutation performed on this order.
- Review URL: `http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9`

## Unmet / deferred
- Live Tipax provider verification deferred (external).
- Visual/User ACCEPT not claimed.
- Same-seller dual-group fixture not present on the screenshot order (two sellers × one group each); UI/server still render groups independently without seller+method collapse.
- Disposable Postgres matrix not re-run (unit + existing fulfillment group tests + live order read used).

## Migration
None.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT
