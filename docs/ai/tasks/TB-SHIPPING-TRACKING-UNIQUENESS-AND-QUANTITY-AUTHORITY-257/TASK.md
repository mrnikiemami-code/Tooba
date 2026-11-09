PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257
Parent-Task: TB-SHIPPING-CARRIER-ACCEPTANCE-CALENDAR-AND-DISPATCH-PLAN-256-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — unique automatic tracking and truthful remaining parcel quantities
Mode: REPAIR_AUTOMATIC_TRACKING_COLLISIONS_AND_UNIFY_EXECUTABLE_QUANTITY_AUTHORITY
Expected-Base-SHA: 662254a1efe2a1eb42534b7f9ccb9f2a28e6c55f
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## 1. Scope and current position
Read CURRENT TOPs, closure plan256–260 and 256-R2/ARCHITECT-REVIEW.md. Safe tree, actual HEAD/origin parity; issue/review documentation descendants are legitimate. Preserve concurrent/unrelated work.
256-R2 save prerequisite accepted within bounded scope; underlined timing-tab appearance user-approved. Do not reopen scheduling/save/UI composition.
ONLY implement the first TWO of five remaining defects:
(1) automatic tracking generates previously used code and fails duplicate;
(2) shipped items remain falsely available for a new parcel.
258 non-delivery/return/resend,259 physical packing choice,260 due queue are not issued.
User explicitly authorizes Architect to continue sequentially after each actual reviewed result; final handoff after five defects. Worker executes only this task then STOP; no self-issuance.

Read mandatory backend/frontend feature skills and relevant contracts/architecture rules before coding. No unrelated guard cleanup or new cross-module dependency.

## 2. A — automatic tracking must not give an already-used code
Reproduce exact current Generate/automatic button path in actual source. Root cause is NOT yet independently established; locate generator, normalization, uniqueness scope, assignment command and database unique constraint, including shared-package/member tracking.
Do not assume GUID prefix/timestamp collisions without evidence.
Report whether the button populates a local suggestion or persists through a command; preserve the intended user flow.

Requirements:
- Generate a fresh valid internal tracking reference with sufficient collision resistance via existing server-owned identity/numbering conventions, never a truncated time-ordered GUID prefix reused across parcels.
- Uniqueness must match existing real business/DB normalized scope, including concurrent requests; checking an array of loaded rows is not sufficient.
- Keep/enforce DB uniqueness. Collision during automatic assignment is retried internally with a bounded fresh candidate in a valid context/transaction; do not repeatedly return the same candidate or swallow unrelated persistence errors.
- If existing architecture separates candidate generation from assignment, make the allocation/reservation guarantee explicit; no claim of globally reserved uniqueness from a stateless suggestion alone. Use minimum suitable owning command seam, not a new generic numbering framework.
- Same target already assigned: deliberate retry/double-click/lost response must reconcile existing assignment, not silently replace a real tracking code. Different target must get different code.
- User-entered duplicates still receive concise inline validation; do not weaken validation to fix automatic mode.
- Manual/internal tracking is NOT a real post/Tipax waybill. Preserve provider-generated identifiers in CarrierApi; never overwrite/pretend provider issuance or make live provider calls.
- Shared consolidated package has one externally actionable tracking owner; child/internal references retain correct unique identity, no duplicated lifecycle buttons.
- Preserve correction restrictions/history after dispatch.
- UI pending is clear, same-tick one request, exact parcel target, retained input on known errors and safe reconciliation on uncertainty. No raw codes or duplicate toast+inline.

## 3. B — correct remaining quantity through ONE owning calculation
Source-confirmed bug:
Tooba.Order.Infrastructure/Integrations/Shipping/OrderCommittedCarrierShipmentFactsReader.cs
ResolveFulfillmentEligibility subtracts only Created open allocations from item.QuantityPacked:
remaining = packed - openAllocated; QuantityShipped is omitted.
Thus all shipped packed1/shipped1 groups can expose ExecutableLines1/createParcelEligible=true while command projection correctly offers no create action and modal is empty.

Correct rule for FIRST new allocation:
remaining packed available = packed - shipped - active Created allocation,
with quantities from authoritative Fulfillment facts; avoid double-subtracting quantityAllocated when it already includes shipped. Zero/negative means no executable line; inconsistent facts are handled explicitly, never manufacture stock.
Unify owning reusable calculation for facts/readiness/projection and command checks rather than three divergent formulas.
Inspect existing ports, group scope and item-membership rules. Do not bypass Order.Contracts/Fulfillment.Contracts or derive seller/group from labels.

Requirements:
- ExecutableLines, remainingPackedQuantity, createParcelEligible and owning blocker agree with valid command eligibility.
- Action button follows exact group/parcel server authority and transport mode. A seller-level create action alone cannot authorize another sibling group.
- UI modal shows the SAME authoritative maximum and cannot be empty while an enabled Create entry claims readiness. Missing data -> honest unavailable, not guessed fallback.
- Whole packed/shipped -> no create; partial shipped + open allocation -> only real remainder; cancelled pre-dispatch allocation releases its reservation; delivery never makes previously shipped quantity available again.
- CarrierApi/genericManual routing unchanged.
- Preserve exact-occurrence replay/reconcile BEFORE remaining gates (249/253). A retry of recorded work must still resolve original parcel despite consumed quantity; a new occurrence must obey remaining. Do not conflate these paths to hide over-allocation.
- Preserve immutable accepted money and original quantities/group evidence; no changed seller financial order.
- Synchronize carrier capabilities/readiness and generic accepted-group create where they consume these facts, including seller equivalent and operational view. No new UI surface.

## 4. Bounded proof — SAME DATABASE ONLY
NO extra/disposable/parallel DB, Docker PostgreSQL, port15433 or second runtime.
Canonical tooba_alpha only. No destructive SQL/reset/reseed. No automatic writes to user's orders, parcels, tracking or published method.
Inspect known read-only review order:
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
It has shipped parcels: after fix existing wholly shipped groups must no longer offer Create and quantities must agree. Do not change these parcels to prove it.

For write/concurrency proof prefer existing authorized TASK-OWNED shipment fixture in tooba_alpha; verify ownership. If none exists, a minimal explicitly task-labelled local integration fixture through owning production domain/application paths is authorized in this SAME DB only, with exact created IDs logged. No customer order copying, fabricated payment/history, real provider or user business-row overwrite. Do not use ad-hoc business SQL to set up facts.
Use transaction isolation/rollback only where it preserves independent-context concurrency meaning; do not fake concurrent uniqueness with a shared InMemory store. Preserve task fixture evidence, no sweeping cleanup.
If storage invariants need a new migration, use additive same-DB migration with private backup + exact pending inventory + repository runner; historical duplicates must be reported, not silently rewritten/deleted or uniqueness weakened.

Focused tests:
A duplicate candidate, automatic concurrent independent contexts -> distinct valid assignments, exact-target retry stable, manual duplicate rejected, package/member identity.
B packed3/shipped1/open1 -> remaining1; all shipped ->0; Created cancel releases only reserved remainder; sibling-group isolation; stale/concurrent new allocation refused; recorded-occurrence replay still succeeds.
C mounted operator automatic action pending/single-flight/error; Create entry/modal max agree and wholly shipped no dead-end modal.
Typecheck/affected production build; relevant boundary checks once. No full unrelated module suites or repetitive test loops. No baseline/allowlist edits.
Record root cause and specific proof distinguishing source/pure/mounted/real storage. Do not label InMemory as relational proof or pretend unexecuted DB suite passed.

## 5. Deploy and review
Original source D:\Users\User\source\repos\SarvNewVer.
Same FE3000 -> Host5088 SingleStore/store-alpha/tooba_alpha. No edition/connection/port switch or second final DB.
Preserve DataProtection, local CapturedPreview ON/7 dates, checked-in OFF; 255 parked, live Tipax deferred.
Deploy changed components to canonical source/runtime; verify actual build/API/authorized read-only rendered order.
Professional current UI preserved; no extra operational text, duplicate actions, checkbox leaks or redesign.
Provide canonical review URL and concise actual before/after quantities/actions. New tracking proof on controlled task fixture only, user order read-only.
Do not wait for another user question to finish this issued task, but no task258 without Architect result review.

## 6. Recovery and result
Persist exact TASK + RESULT/RECEIPTS in task/evidence dirs.
Update closure plan and all three TOPs:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Ledger must separately state issue1 tracking and issue2 quantity status, evidence limits; remaining3–5 NOT ISSUED. Do not claim five defects fixed.
Preserve save acceptance and scheduling live-published-order limitations.
Commit/push normally, verify safe local==origin/main; no force/amend/reset/stash/rebase. Include actual Base/Code/Evidence SHA, elapsed and runtime/DB/migration identity.
Return complete BEGIN_TOOBA_WORKER_RESULT with PASS/PARTIAL/BLOCKED and exact root cause/proof. Incomplete critical concurrency or production command wiring -> PARTIAL, not full PASS.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
