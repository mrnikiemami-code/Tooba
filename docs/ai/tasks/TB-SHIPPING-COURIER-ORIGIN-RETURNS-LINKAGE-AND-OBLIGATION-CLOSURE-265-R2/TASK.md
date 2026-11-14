PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R2
Parent-Task: TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: a0e1f24e55f14f8bc366c9018da9f117e718aca8
Mode: CLOSE_THREE_CONFIRMED_REMAINING_GAPS_AND_HAND_OFF_FOR_USER_TRIAL
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## Goal and execution boundary
User: «تسک بده تمام شد من تست کنم». ONE bounded repair, then STOP for architect review and user trial. No new shipping stage, no delegation, no historical agent resumed, no automatic continuation. Read current AGENTS.md, 265-R1/ARCHITECT-REVIEW.md, original265/R1 task and relevant owner rules. Legitimate subsequent issue/docs commits are allowed descendants. Do not reopen accepted 263 notifications,264 scheduling or264-R3 mobile design. Keep their regressions only where directly affected.

The prior worker report overclaimed exact replay/concurrent approval/partial support. Fix the concrete source defects below; do not satisfy them by new prose, disabled buttons, generic refusals or mocks replacing the missing authority. If a genuinely unavailable external dependency prevents proof, report PARTIAL with exact evidence instead of another PASS.

## 1 — Exact Inventory release identity and truthful result
InventoryReturnObligationRelease currently looks up by key in THREE places and never compares reservation/requested quantity. Inbox Quantity holds ACTUALLY RELEASED quantity, which cannot identify a request that was partly fulfilled.
Persist the exact immutable requested identity (purpose, key, reservation, requested quantity) alongside actual effect through the existing owning Inventory seam. Minimal additive schema allowed if necessary; do not reinterpret old rows or backfill guesses. All replay branches, including concurrency winner, compare identical intent BEFORE answering; changed reservation or requested quantity gets a typed localized conflict and zero change. Keep atomic transaction spanning reservation + Reserved decrement + replay fact/event. Consumed no-op must remain Consumed, never report Released; original consumed sale reservation remains untouched.

Focused real-storage cases: same key/same request after partial fulfillment replays exact actual amount; changed reservation and changed requested amount refuse with zero writes; injected failure after position update/before commit leaves reservation/Reserved/identity unchanged from independent context, exact retry performs effect once. Existing green concurrency case need not be repeatedly run.

## 2 — Serialize approval/rejection and replay completion
ReturnDirectory.ApproveAsync / RejectAsync currently read Requested independently with no common lock/CAS; checking in-memory Status does not protect concurrent decisions. A courier case already Completed+RefundReleased also falls through to ordinary Approve instead of answering completion.
Use the existing owning Returns persistence protocol to make the approval-vs-rejection decision durable and serialized, re-read current state under the protocol. Both paths must observe SAME authority; no transaction held across external payment calls that creates cross-owner deadlocks. Durable approval wins -> reject cannot release its claim; durable rejection wins -> approval cannot refund. Two approvals share the SAME financial attempt/key. Completed exact approval replay returns existing result before ordinary transition guard, with zero new money/disposition/closure effects. Pending/unknown resumes the SAME durable attempt, known success + disposition failure retries only disposition; do not rewrite the refund engine.
Prove via production public/application boundary, independent contexts and controlled financial ports: approve-vs-reject under deterministic barrier, approve-vs-approve, completed approval replay. Verify durable case/claim/holds and money recorder together; don't prove only sequential late rejection.

## 3 — Partial source replacement return must conserve remaining obligation
ClaimAsync currently accepts selected partial quantity, EnsureCourierOriginClosureAsync closes Order first, then Plan throws replacement_partial_claim_unsupported when resolution.Quantity > selected quantity. This is the user's valid partial-return path, not a reason to close obligation then stop.
Implement exact selected allocation through owning Fulfillment/Inventory/Order seams. Preserve unselected healthy stock, source capacity and remaining lawful delivery obligation. If the existing replacement parcel/resolution is larger than the selected portion, use explicit owning cancellation/re-preparation/allocation handling to retain/rebind unselected stock rather than cancel/refund the whole quantity. No invented stock via ordered/packed inflation, no damaged-as-usable, no release of sibling holds, no second reservation consume.
Validate/record a recoverable executable allocation BEFORE irreversible closure. No accepted claim may become an unsupported partial branch after closing Order. Retain exact identity and safe recovery if a later owner is temporarily unavailable. Preserve independent same-line sourceB. Active line keys must not falsely advertise lawful remaining partial quantity then reject it merely because another disjoint portion is claimed; use the existing common serialized capacity authority.
One focused real-storage scenario: sourceA damaged3 with a Created replacement/hold3, select1 for Returns, preserve2 for order fulfillment; sourceB same order line unchanged. Requested claim blocks only claimed capacity through real owning rules. Approval closes1, releases only correct unconsumed1, remainder2 remains held and can complete lawful preparation/dispatch. Rejected Requested case releases only claim, not stock. Exact retry/crash resumes recorded allocation without duplicate release/refund. Historical physical shipped/packed/ordered facts remain unchanged.

## 4 — Concrete operator handoff, not bundle-string proof
After fixes, validate ONE positive task-owned case via real existing operations UI/public command -> create case -> reload -> persisted «مشاهدهٔ پروندهٔ مرجوعی» -> existing Returns detail. Verify the actual positive detail/approval path with controlled financial execution, never real payment/provider/wallet movement. If UI approval cannot safely use controlled financial ports in canonical runtime, exercise approval through production owning application with controlled ports and disclose the exact boundary; do not click real refund. Browser reads/navigation + safe task-owned create are authorized, user/screenshot rows remain read-only.
Verify touched screens at390 and desktop without screenshot artifacts: existing controls usable, no document overflow, Persian condition/phase/errors, no duplicate action. Toast once on confirmed outcome; existing MessageBox for irreversible decision/blocker; field validation local, no persistent success banner. Pending recovery remains recoverable, no auto-POST.
Provide exact URLs and a SHORT Persian user checklist covering same healthy goods resend, healthy-stock replacement, linked Returns approval/partial release, and no duplicate action on reload. User should not have to manufacture fixtures or decode IDs. Identify safe review case(s) and clearly distinguish read-only vs business mutation.

## Constraints and verification budget
ONE canonical tooba_alpha@127.0.0.1:5432; SingleStore/store-alpha, FE3000 -> Host5088. No extra/disposable/Docker/15433 DB; no edition/connection/port/appsettings switch, raw business SQL/manualDDL/reset/reseed/delete/backfill/global reclaimer. Task-labelled minimal production-path fixtures on SAME database permitted. User/screenshot/cart/payment/service data, including shipping draft01a17d0d-e033-7000-af43-49a1daa28822, read-only. No provider call, real refund/wallet credit, fake waybill. Preserve DataProtection and CapturedPreview localON7dates/checked-inOFF.255 parked, Tipax deferred, parent common-package workflow remains explicitly unsupported; no child-lock bypass.
Minimal additive migrations only via repository MigrationRunner after private backup. Read old data honestly, no unknown identity inferred from current settings.
Focused failing cases above, then affected suites/typecheck/build/publish ONCE after final relevant changes; repeat only for new failure/change. No broad whole-module/base parity/stress/screenshot matrix, no cosmetic sweep, no tool/network retry loops. No silent removal of existing owner guards.

## Delivery and recovery
Persist TASK verbatim in docs/ai/tasks/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R2/TASK.md and evidence TASK/RESULT/RECEIPTS/result-contract plus focused real storage proofs under docs/architecture/evidence/TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R2/.
Update AGENTS/START-HERE/architect recovery/current263-265 plan with actual remaining state and handoff. One top CURRENT marker; historical PASS never means Architect/user ACCEPT.
Commit AND push main normally, no force/amend/reset/stash/rebase/unrelated overwrite. Supply full base/code/evidence/final HEAD and origin parity, real migrations/runtime/build, fixture IDs and review URLs, checks actually executed, explicit financial seam/limits.
If all required bounded proofs pass, report READY_FOR_ARCHITECT_REVIEW_AND_USER_TRIAL, not all-shipping-fixed. If incomplete, report PARTIAL once and stop; do not invent another implementation task.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
