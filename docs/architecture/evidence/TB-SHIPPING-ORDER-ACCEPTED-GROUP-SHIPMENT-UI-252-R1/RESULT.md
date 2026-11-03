PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R1
Parent-Task: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: TRANSPORT_ROUTING_EXACT_METHOD_AND_MODAL_RELIABILITY_DEPLOYED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 063e100c8f3f60b8d08b08f55b25714d3e551ef6
Base-SHA: 063e100c8f3f60b8d08b08f55b25714d3e551ef6
Code-SHA: 688124f5f045e01eff324f17a328968d7086cf40
Evidence-SHA: 6b05177a18062166e8c49d8cf166a49a6c1e6664
automaticNextImplementationTask: NONE

## Architect context
252 worker PASS was Architect PARTIAL: CarrierApi could still hit generic create_shipment; method prefix equality; modal same-tick race; binder-only tests.

## Transport routing (A)
- Shared `AcceptedGroupParcelCreateAuthority` derives Manual vs CarrierApi from committed Order facts only.
- Generic create_shipment / binder refuses CarrierApi with `order.shipment.carrier_api_use_dedicated_path` before Fulfillment allocate/provider.
- Exact accepted method/service/option identity (no prefix `post:other`≈`post:standard`); registry map is a separate post-bind adapter.
- `ProviderMetadataJson` refused on Manual path; orchestrator always passes `providerMetadataJson: null`.
- Admin detail `createParcelEligible` false for CarrierApi with dedicated-path FA reason; `transportMode` exposed.
- Seller CreateShipment requires `shippingGroupId` and uses the same authority; null metadata.

## Modal reliability (B)
- `AdminCreateShipmentModal` sync `inFlightRef` before first await + try/finally; thrown failure shows localized error and releases pending; qty edits preserved.
- Duplicate line selections aggregated before remaining-qty validation.

## Proof (C)
- Binder + transport routing tests 16/16 (CarrierApi refuse before fulfillment; prefix mismatch; aggregate excess; null metadata source).
- Seller validator ShippingGroupId required + FulfillmentValidatorBehaviorTests focused green.
- Mounted modal 3/3: same-tick one request; thrown error; dual Manual/CarrierApi action split.
- Carrier mount suite still 10/10 after jsdom dialog/CSS stubs.
- FE typecheck 0; production build 0.
- Canonical FE3000→Host5088 republished; CapturedPreview local env ON (Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7); checked-in defaults unchanged OFF.
- Authorized read-only order `01a1a200-cbd2-7000-9901-4b9fd076f5d9`: 2 Manual groups پیشتاز; dates 2026-11-23 / 2026-11-30 windows 09:00–10:00; Store; 125000 IRR; one createParcelEligible; other remaining-qty block. No mutation.

## Review URL
http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9

## Unmet / deferred
- Live Tipax provider verification deferred (external).
- Visual/User ACCEPT not claimed.
- Isolated dual-group CarrierApi live shape not present on this user order (fixture covered in mount); Manual dual groups proven live.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT
