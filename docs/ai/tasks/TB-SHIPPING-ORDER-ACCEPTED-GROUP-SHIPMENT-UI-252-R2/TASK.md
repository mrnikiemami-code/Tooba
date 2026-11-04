PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R2
Parent-Task: TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — make committed Post parcel creation work and remove technical UI clutter
Mode: FIX_REAL_POST_RECIPIENT_MAPPING_AND_FINISH_COMPACT_LOCALIZED_MODAL
Expected-Base-SHA: 2c73cd938d1b948390c9b0666636171032cc449c
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

USER REJECTION — NOT VISUALLY OR FUNCTIONALLY ACCEPTED
User tried canonical order 01a1a200-cbd2-7000-9901-4b9fd076f5d9. Modal displays recipient name محمد امامی, yet Create refuses: «ارسال پستی نیازمند نام گیرنده است.» Error appears BOTH inline and toast.
UI shows unnecessary «ایجاد مرسوله از گروه پذیرفته‌شده», «تعهد ارسال (فقط خواندنی)», «اقلام همین گروه», tenant store-alpha, raw Store on group cards, ISO date and Asia/Tehran. User rejects this as unprofessional.
Do not ask for screenshots. The supplied screenshots are evidence of rejection, not new screenshot work authorization.

REVIEW CORRECTION
R1 source CreateShipmentCoreAsync passes providerMetadataJson:null to the real Fulfillment operation. Preventing arbitrary CLIENT metadata is correct; throwing away required SERVER-derived recipient data is not.
Exact cause of the Post refusal still requires tracing registry validation and real adapter; do not assume the displayed Order recipient equals the consumed Fulfillment/metadata field.
R1 AcceptedGroupCreateShipmentTransportRoutingTests uses SimulateCreateShipmentCore and callback substitutes, plus a source assertion forcing null metadata. That is NOT real orchestrator -> Fulfillment directory -> method validation -> persisted parcel proof. Remove/replace misleading proof, retain meaningful negative tests.
252/R1 acceptance was insufficient for delivery; document this review correction.

A — REAL WORKING POST CREATE
Trace actual request/error code and the exact field read by the owning Post validator. Compare Order recipient snapshot, Fulfillment snapshot and method execution input WITHOUT exposing PII in evidence. Identify which mapping is missing.
Construct required recipient/address/contact/postal execution facts SERVER-SIDE from committed Order/Fulfillment snapshots, through owning Contracts/adapters. Reuse existing method-specific serialization/validation authority if present.
Client must NOT override accepted method/recipient/owner/service through ProviderMetadataJson. Do not re-enable arbitrary JSON, remove validation, substitute seller name for recipient, fabricate postal/mobile, or fill merchant data to hide the bug.
Preserve Manual/Free operation semantics and dedicated CarrierApi path from R1. If serialized metadata is required by the existing internal Post adapter, generate trusted server metadata; distinguish that from caller-supplied metadata. Update obsolete tests/comments that insist metadata is always null.
If the committed recipient truly is absent, expose a precise editable completion only through an authorized existing completion seam; do not silently rewrite historical order evidence. In the reported case a displayed name exists: fix the actual loss/mismatch first.
No broad transport redesign or new method-change workflow.

B — FINISH THE EXISTING FORM, SMALL AND PROFESSIONAL
Use existing design system/components and a relevant local UI workflow; preserve density/theme/RTL. No new screen, screenshots or full redesign.
Modal hierarchy:

One title «ایجاد مرسوله».
One compact read-only method + delivery day/hour summary; seller name only once where useful.
Recipient name/mobile/address as readable facts; no technical explanation cards.
Product rows and a clear quantity column.
One primary «ایجاد مرسوله» + «انصراف».
Remove repeated banners/headers and implementation language: «گروه پذیرفته‌شده», «تعهد ارسال (فقط خواندنی)», «همین گروه», unnecessary responsibility text.
Never show Store, raw enum values, store-alpha, internal IDs or Asia/Tehran on customer/operator-facing group cards/modal. Where ownership matters, use concise localized «ارسال توسط فروشگاه آرمان» / marketplace display name from real context. In SingleStore hide redundant ownership entirely; do not hide permission/business restrictions.
Render FA date with existing Jalali formatter, short weekday if helpful, localized digits; hour range 09:00–10:00 inside LTR bdi with correct order. Keep timezone in stored execution facts; omit technical timezone token from ordinary UI.
IRR fee no decimals, currency placement consistent with existing formatting. Do not use truthiness fallback that replaces lawful zero payable with a different amount.
Show ONE clear inline create error; no duplicate toast for the same failure. Preserve quantity edits and release pending correctly. Disable only according to actual eligibility; keep meaningful empty/blocked reasons short.
No hidden validation, clipping, global CSS, giant controls or redundant colored summary boxes.

C — EXECUTABLE PROOF BEFORE HANDOFF

RED -> GREEN test through REAL operation orchestration + owning Fulfillment method validator/directory and persistence: packed Manual Post group with valid committed recipient creates one parcel linked to that exact accepted group; correct recipient execution input; method/date/time/fees unchanged. If relational target needed, use existing owned disposable harness and clean it. A simulated orchestrator/callback stub or source-string assertion is NOT sufficient.
Same real path: malicious client metadata/method override/cross-group line still refused before allocation; missing actual recipient gives typed refusal; valid accepted name must not produce recipient-required error.
Mount actual modal: accepted fields + compact localized copy, no Store/tenant/technical prose/ISO date leakage, correct date/hour direction, same-tick one call, transport exception/typed refusal one inline message and retryable state with edits preserved. Same seller/two groups stay independent.
Authorized canonical read-only UI check on reported order: displayed data maps correctly and modal is clean. DO NOT create another parcel, cancel, pack, dispatch or pay this user order. Its screenshots indicate the user already acted; re-read current quantities/state and preserve them. Positive create proof belongs to isolated data.
Run only affected tests/typecheck/build and actual positive-path proof; no unrelated full sweep. No Tipax account/calls.

CANONICAL / RECOVERY / DELIVERY
D:\Users\User\source\repos\SarvNewVer FE3000 -> Host5088 SingleStore/store-alpha/tooba_alpha. No alternate final source/ports/database/buyer/order.
Read current main/base and preserve intervening/user edits; no force/reset/stash/rebase, seed/reset/business SQL writes. DataProtection and actual user order/cart state preserved.
Preserve effective local CapturedPreview Enabled/SelectionEnabled/CheckoutEnabled + date count on redeploy; checked-in defaults unchanged. No migration expected. Any unavoidable additive schema requires private backup+normal runner+readback.
Update ALL THREE: AGENTS.md; docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md; docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md.
Record 252/R1 user rejection, actual root cause, corrected real-path proof, professional UI scope and deferred external Tipax acceptance. Keep history append-only; no claim User/Visual ACCEPT.
Preserve TASK verbatim under docs/ai/tasks/TB-SHIPPING-ORDER-ACCEPTED-GROUP-SHIPMENT-UI-252-R2/TASK.md and evidence TASK/RESULT/RECEIPTS.
Return exact Post validation/mapping source, tests actually executed, canonical deployment and ready-to-review URL. Priority: real working existing form, no new feature scope.
STOP_FOR_ARCHITECT; automaticNextImplementationTask NONE.
END_TOOBA_TASK
