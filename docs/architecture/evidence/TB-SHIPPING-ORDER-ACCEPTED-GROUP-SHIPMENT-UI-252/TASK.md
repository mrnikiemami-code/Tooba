PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252
Parent-Task: TB-STOREFRONT-GROUP-DELIVERY-CALENDAR-CLOSURE-251
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — execute accepted delivery groups in the existing order UI
Mode: REPLACE_SELLER_SCOPED_METHOD_RESELECTION_WITH_COMMITTED_GROUP_PARCEL_EXECUTION
Expected-Base-SHA: 7786d4bc0898b03fcdef6a7bbcba2a1c6950eb82
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER-APPROVED MODEL — IMPLEMENT THIS, NO NEW DESIGN DEBATE
Seller is a financial/display section.
Inside it, show the customer's persisted shipping commitment GROUPS.
Each group owns its accepted line membership/quantities, delivery responsibility/owner, method/service/option, fee, receiving date/time and timezone.
A physical parcel is created FROM ONE such group; one group may produce multiple parcels subject to remaining lawful quantities.
Method/date/time/accepted fee are READ-ONLY in execution. Do not silently replace Post with Tipax, courier or collection. A future post-order method change would require a separate authorized business operation — OUT OF SCOPE, do not implement it now.
User requests professional existing UI, quick usable handoff on canonical environment. No screenshots.

SOURCE-CONFIRMED WRONG PATH AT BASE
admin-order-items-shipping-panel.tsx opens AdminCreateShipmentModal by sellerOrder, and lists parcels by seller.
admin-create-shipment-modal.tsx initializes methodCode="post", loads a method catalogue, lets the operator choose, then posts sellerOrderId + arbitrary shippingMethodCode/carrierDisplayName through executeAdminOrderOperation.
The user's screenshot is THIS legacy Fulfillment modal, not merely the separate carrier API component improved in 249. Fix the actual reachable path. Do not deliver another parallel panel while the old override still works.

A. TRACE AND INVARIANT
Trace the clicked admin Create parcel action end-to-end through existing Order/Fulfillment command/handler/port/persistence. Identify accepted Order group facts and exact line membership authoritative source.
Reuse Order.Contracts / Fulfillment.Contracts; no foreign DbContext, Host business logic or Shipping runtime repricing.
All actual parcel creation paths reachable from this order UI (Manual/Free/CarrierApi) must bind to the same accepted group identity; carrier execution retains 249 claim/occurrence/no-resend protocol.
The server must validate owner+checkout/seller scope+group membership+remaining quantities+payment/processing state and derive/verify the accepted method. Hiding a dropdown is not enough.
Reject a tampered cross-group line, foreign group, arbitrary different method, excess qty or missing required group; typed localized refusal, no allocation/dispatch side effect.
No method label heuristics, first-group/first-method fallbacks or mapping two canonical options to one generic method that changes execution semantics.
Handle historical orders without accepted group evidence honestly: keep history readable, show a specific read-only/unavailable reason for this new action; never fabricate a group/calendar/method or backfill user orders.

B. PROFESSIONAL EXISTING ORDER UI
Keep seller sections for context. Within each, present accepted delivery groups:

clear method label + delivery responsibility when meaningful;
chosen day/hour in local user format, fee with proper currency precision;
group items and remaining quantities;
parcels belonging to that exact group, not just any parcel of this seller;
one Create parcel action for that group when authorized/eligible.
Same seller can have TWO accepted groups with the SAME catalogue method but different responsibility/date/evidence. Render separately; no collapse by seller+method label.
Open compact existing modal FROM group. Show method, recipient/destination and accepted delivery date/hour as read-only summary. Select only allowed line quantities of this group; collect actual package grams/cm and sender completion only when needed.
Remove the global method/service picker from this order execution path. No duplicated sender address fields, technical catalogue explanations, raw IDs or detached generic carrier forms.
Retain native sizing/theme/RTL/accessibility, compact density, one primary submit+cancel. Do not repeat prior giant-radio/fixed-overlay redesign.
A parcel may contain part/all remaining units; never sum product dimensions to invent package dimensions. Reuse correct physical-spec and per-parcel completion authority.
Manual tracking, carrier API actions and any existing consolidation must not become alternate ways to change the accepted delivery commitment. Preserve existing legitimate consolidation rules; if a conflicting path is uncovered, restrict it with a precise reason rather than create a new policy in this task.
Shared admin/seller behavior must use the same server invariant. Fix seller equivalent reachable override if present; do not duplicate business rules.
No stored order money/calendar rewrite and no re-query of latest Shipping quote to render accepted history.

C. FOCUSED EXECUTABLE ACCEPTANCE

Mount the actual order group UI/modal. Same seller, two groups on one method but distinct owners/dates: separate cards, exact own lines, read-only accepted method/time, no method catalogue reselection. Parcel lists stay with exact group.
Real handler/adapter proof with isolated data: lawful partial allocation and second parcel; replay safe; method override/cross-group/foreign/excess quantity all refused before side effects. Reuse existing disposable harness if relational proof necessary; no giant replay of prior carrier matrices.
CarrierApi follows existing group-scoped configured execution; Manual/Free does not require a carrier account. Unpaid/unknown/legacy blocks are clear and have zero create calls.
Cancel/reopen preserves correct group identity. Same-tick double submit sends at most one action; server quantity/occurrence protection remains authoritative.
Canonical authorized read-only order UI handoff for screenshot order locator:
01a1a200-cbd2-7000-9901-4b9fd076f5d9
Use only normal authorized task-owned session. Read actual accepted groups and confirm read-only method/date/time; open modal only. NO create/pack/cancel/dispatch/payment mutation on this user order. If this order lacks evidence, report it and demonstrate history-safe blocked state; isolated committed fixture proves normal working path.
Run relevant focused tests/typecheck/production build. No screenshots, source-token-only proof or unrelated full-suite sweep.

CANONICAL DELIVERY AND PRESERVATION
D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088, SingleStore/store-alpha/tooba_alpha. No alternate final runtime/DB/port or replacement user order. Validate actual base/main; preserve intervening commits/unsaved edits; no reset/stash/rebase/force.
Maintain existing local CapturedPreview Enabled/SelectionEnabled/CheckoutEnabled and their configured values across backend restart; 251 identified accidental gate loss. Read back effective capabilities after deploy; checked-in defaults remain unchanged. No blind env replacement.
DataProtection preserved. No real Tipax calls/credentials. No user cart/order/payment/stock mutations, no reseed/reset/drop/business SQL write. If additive schema truly needed, private backup + normal module runner + precise target/readback; never modify applied history.
Prioritize fixing the actual reachable execution path and a usable handoff. No speculative carrier capabilities, post-order service-change workflow or general UI rewrite. Report a blocker immediately if needed; no fake PASS or renewed multi-hour promise.

RECOVERY / RESULT
Update ALL:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Record agreed seller -> accepted group -> physical parcel model, actual legacy override path removed/guarded, software proof and remaining external-only Tipax verification. Keep history append-only.
Preserve full TASK under docs/ai/tasks/TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252/TASK.md and docs/architecture/evidence/TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252/{TASK,RESULT,RECEIPTS}.md.
Return exact commands/contracts touched, per-group proof, tests run, canonical source/binary/runtime/gate parity, migration ledger and final authorized review URL. No secrets/screenshots. Worker PASS != User ACCEPT.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.
END_TOOBA_TASK