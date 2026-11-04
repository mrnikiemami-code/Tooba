PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R2
Parent-Task: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Status: PASS
Decision: TRUSTED_POST_RECIPIENT_METADATA_AND_COMPACT_LOCALIZED_MODAL_DEPLOYED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 2c73cd938d1b948390c9b0666636171032cc449c
Base-SHA: 2c73cd938d1b948390c9b0666636171032cc449c
Code-SHA: 96e34482ff2b76abc3e2e41f14688af5463a7d8e
Evidence-SHA: PENDING_EVIDENCE_STAMP
automaticNextImplementationTask: NONE

## Architect context / review correction
252-R1 user-rejected: Create refused `fulfillment.shipping.post.recipient_required` («ارسال پستی نیازمند نام گیرنده است.») while modal showed a recipient; duplicate toast+inline; technical UI clutter (گروه پذیرفته‌شده / تعهد ارسال / Store / store-alpha / ISO / Asia/Tehran).
Root cause: R1 correctly blocked client `ProviderMetadataJson`, but `CreateShipmentCoreAsync` forwarded `providerMetadataJson: null`, so Post validator saw empty `{}` and refused `recipientName`. Displayed Order recipient was never mapped into Fulfillment method metadata.

## A — Trusted server Post metadata
- Owning validator: `ShippingProviderMetadataValidator` Post branch requires `recipientName` + `destinationAddress` (`fulfillment.shipping.post.recipient_required` / `address_required`).
- New `AcceptedGroupTrustedProviderMetadata` (Order.Contracts) builds method JSON from committed `OrderCommittedCarrierShipmentFacts` (+ Fulfillment snapshot fallback via Application builder).
- Binder emits `TrustedProviderMetadataJson`; orchestrator/seller create forward that only — never client ProviderMetadataJson.
- CarrierApi dedicated-path refusal preserved; Manual/Free unchanged.

## B — Compact localized modal/cards
- Modal title «ایجاد مرسوله»; one method+seller+Jalali date+LTR hour+fee summary; recipient facts; qty column; primary+cancel.
- Removed technical banners/prose; hide Store/store-alpha/Asia/Tehran on operator UI; marketplace/seller ownership only when useful.
- Fee uses nullish (`??`) not truthiness (`||`) so lawful zero is preserved.
- One inline create error; success/error toasts removed for create path; `admin-error-map` maps post recipient/address codes.

## C — Proof
- REAL Fulfillment directory: packed Manual Post + trusted metadata → persisted parcel linked to accepted group with recipient in ProviderMetadataJson; null metadata refuses before persist; missing name builder refuses.
- Transport routing: Manual binds trusted metadata accepted by validator; client JSON still refused; source forwards `bound.TrustedProviderMetadataJson`.
- Mount modal 3/3: compact copy (no Store/tenant/technical prose/ISO leakage), same-tick one call, typed refusal one inline error + qty preserved, dual Manual/CarrierApi independence.
- FE `tsc --noEmit` 0; `npm run build` 0.
- Canonical FE3000→Host5088 republished; CapturedPreview local env ON (Enabled/SelectionEnabled/CheckoutEnabled/AvailableDateCount=7); checked-in defaults unchanged OFF; DataProtection keys preserved.
- Authorized read-only order `01a1a200-cbd2-7000-9901-4b9fd076f5d9`: 2 Manual پیشتاز groups; dates 2026-11-23 / 2026-11-30 windows 09:00–10:00; fees 125000; one createParcelEligible; other remaining-qty block. No create/pack/dispatch/payment mutation.

## Post validation/mapping source
- `Tooba.Fulfillment.Application/Shipping/ShippingProviderMetadata.cs` → Post `ValidateAndNormalize`
- Trusted build: `Tooba.Order.Contracts/Shipping/AcceptedGroupTrustedProviderMetadata.cs`
- Forward: `AdminOrderOperationsOrchestrator.Fulfillment.CreateShipmentCoreAsync` + seller `SellerMutateFulfillmentCommand`

## Review URL
http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9

## Unmet / deferred
- Live Tipax provider verification deferred (external).
- Visual/User ACCEPT not claimed.
- 252/R1 acceptance demoted by this user rejection + review correction.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE
END_TOOBA_WORKER_RESULT
