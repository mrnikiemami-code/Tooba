PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R3
Parent-Task: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — remove invented pickup facts from trusted metadata
Mode: REMOVE_RECIPIENT_AS_ORIGIN_FALLBACK_WITHOUT_REDESIGNING_POST_FORM
Expected-Base-SHA: 36845985ba2d9a931b65bcec8e936ee23154f674
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

BOUNDED REVIEW CORRECTION
R2 fixes the reported Post recipient loss with a real Fulfillment directory+validator test and improves the modal. Preserve that work and compact UI.
However its NEW Order.Contracts/Shipping/AcceptedGroupTrustedProviderMetadata.cs introduces fabricated facts outside Post:

BuildCourier(address,...) writes pickupAddress=address AND destinationAddress=address where address is facts.RecipientAddress.
BuildInPerson(address,city) writes pickupLocation=First(recipient address,recipient city).
The customer's destination is NOT the seller's origin or collection location. "Trusted server" serialization does not make a guessed value authoritative.
This is the remaining concrete correction; no additional carrier feature or broad UI rewrite.

REQUIRED FIX

Remove both destination-as-origin/pickup fallbacks.
Only use explicitly authoritative committed sender/origin or collection-location facts from existing owning Contracts/read seams. Validate owner+group context. If that authority does not exist, fail closed with a typed localized reason requiring the missing origin/collection setup. Do not create new merchant/reference data or silently use recipient fields, first location, defaults or arbitrary client JSON.
Do not infer "store_courier" needs no input merely because "{}" was convenient: inspect the existing owning validator's actual requirements and honor them. Unknown registry code must follow the established typed unsupported/not-eligible behavior; never claim complete guessed metadata.
Keep Post's server-derived recipientName/destinationAddress mapping and client-metadata rejection working. CarrierApi stays on the dedicated 249 path; accepted method/date/time/money immutable.
Preserve R2 compact modal, Jalali date, readable LTR hour, no Store/tenant/timezone/technical-prose leakage, one inline failure. Surface missing-origin/collection reason through existing error presentation; no new generic form.

FOCUSED PROOF ONLY

Fixture with DISTINCT recipient destination and sender origin: courier payload uses proper respective fields.
Origin absent with recipient present -> typed refusal before any parcel allocation; never copies recipient address into origin.
Collection requires actual collection location; missing collection location with recipient address/city present still refuses.
Post positive real Fulfillment directory+validator test from R2 stays green, preserving exact group/recipient. No source-only assertion as a substitute.
One actual orchestrator wiring test into the owning directory (reuse existing fixture) verifies trusted Post metadata reaches it; R2 directory-only test plus simulated callback does not by itself prove this full connection. No new relational matrix unless actually needed.
Relevant tests/typecheck/build only; do not rerun unrelated suites. No screenshot/image tasks or live provider calls.

PRESERVATION / CANONICAL HANDOFF
Same user source D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088 SingleStore/store-alpha/tooba_alpha. Preserve local CapturedPreview Enabled/SelectionEnabled/CheckoutEnabled and date count on restart; checked-in defaults unchanged. DataProtection preserved.
No user order/cart/price/merchant/stock/payment/parcel mutation. User already tried order 01a1a200-cbd2-7000-9901-4b9fd076f5d9: read current state, do not recreate/pack/cancel/dispatch. Positive writes in isolated test data only.
No seed/reset/business SQL/new final runtime/DB/ports. No migration expected. No Tipax credentials/network. Preserve actual main changes and unsaved work; no force/reset/stash/rebase.
Authorized read-only handoff URL remains:
http://127.0.0.1:3000/admin/orders/01a1a200-cbd2-7000-9901-4b9fd076f5d9

RECOVERY / RESULT
Update AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md AND docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md. Record R2 Post closure and the reviewed origin/collection defects separately; no unqualified acceptance of unproven services.
Preserve TASK verbatim in docs/ai/tasks/TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R3/TASK.md and evidence TASK/RESULT/RECEIPTS.
Report exact source of origin/collection authority or typed unavailable behavior, tests actually run, canonical deploy/gates and review URL. Keep final receipt-vs-envelope SHA distinguished.
Priority: remove these specific invented facts and leave the existing Post form ready for user review. Do not expand scope.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.
END_TOOBA_TASK
