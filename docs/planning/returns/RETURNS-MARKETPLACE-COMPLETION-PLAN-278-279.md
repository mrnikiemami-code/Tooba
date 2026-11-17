# CURRENT — final consolidated279 issued — 2026-11-17

278 result source57cccbbe7cffea23c30ee0f474291cb3dda7bc58: PARTIAL after independent source review; retain useful delivered features. Read its ARCHITECT-REVIEW.
Only TB-RETURNS-AUDITED-DECISION-CORRECTION-PARTIAL-RESOLUTION-AND-DELIVERY-ALLOCATION-279 next. FULL issued TASK and SHA256 receipt are in the repo, no reconstruction from Bridge summaries.
Detailed chosen domain/serialization/product decisions: RETURNS-279-EXECUTION-DECISIONS.md.
Complete278 durability/lifecycle blockers in279 alongside audited decision correction, partial accepted/rejected quantities, rejected-goods custody, delivery-slice capacity and Returns-mediated shared-payment budget.
One result, no extra per-helper repair tasks or279 follow-up automatically. No user/provider/full-Returns acceptance until evidence supports it. Preserve existing history and read-only trial cases. Migration boundary is bounded additive Returns schema, verified fresh backup then runner, no data fix.

# SUPERSEDED — original278–279 planning checkpoint (historical context)

State: USER_REQUESTED_FAST_CONSOLIDATED_COMPLETION. Two main delivery slices, not ten micro-tasks.
Only TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278 is issued next. 279 is planned, NOT ISSUED; no delegation/automatic loop.
Source audit base:5a6c5f82118c366005425fa717ef1e0ffd1a06b6. Prior277/R1/R2 acceptance is bounded technical acceptance of that repair chain, not full Returns/customer/Marketplace readiness.

## Recover this conversation
Read this file first, then docs/architecture/evidence/TB-RETURNS-CUSTOMER-MULTI-SELLER-PARTIAL-SELECTION-AND-TEMPLATE-FEEDBACK-278/SOURCE-AUDIT.md, its issued TASK, current AGENTS, and latest RESULT/ARCHITECT-REVIEW. Do not rebuild the plan from old worker PASS banners.
User requires: seller-isolated partial returns; retain correct goods; deliberate per-item reasons; actual template Toast; beautiful shared modals; independent approval/first refund; a reasonable way to correct a decision; fast consolidated work.
The prior multi-seller findings were only in chat until THIS checkpoint. They are now recorded, not already fixed.

## Observed user trial, not global acceptance
Seven user-provided images show case01a1e9c5-c729-7000-b614-79daf3d0e812:
receipt modal with usable/damaged and ceiling1 -> received damaged1 -> decision modal -> approval stored without money -> separate start-refund modal -> recorded successful attempt and DamagedNonSaleable/Settled.
This demonstrates the displayed sequence and the user's interaction, not proof of real PSP settlement, multi-seller correctness, mobile completeness or all rejection/correction paths. The lower-left dark toast is still not the template toast.
Previous case01a1e6b0-f33a-7000-80ee-beffb6344490 is also settled history. Both cases and their order/payment/stock facts remain read-only for implementation; no reset/reversal/clone/new demo to show earlier stages.

## Source-confirmed gaps and delivery assignment
| Gap | Source | Delivery |
|---|---|---|
| Customer cannot choose2of3 or exclude good items | return-ui.tsx ReturnFormModal initializes every shipped item at quantityShipped; renders text summary, no quantity/checkbox editor |278 |
| Customer uses shipped/full quantity, not delivered/eligible remainder | same modal; order page tests aggregate Delivered |278 authoritative customer eligibility + per-line entry |
| Reason belongs to entire case | Customer CreateReturnRequest/ReturnLineCommand only line+quantity; case has Reason |278 separate selected line/reason child cases, no false per-line reason field |
| Multiple sellers must not share one case | CreateInternalAsync binds one SellerOrderId/SellerPartyId and checks each line; this existing rule is correct |278 retain, prove2 sellers/same checkout |
| Missing per-line approval/refund in multi-line case | Approve/Reject commands address ReturnRequestId only, amount sums case items |278 new customer cases default to one line+reason;279 explicitly handles legacy multi-item constraints |
| Customer lost-response retry gets a new key | ReturnFormModal does not pass key; createCustomerReturn defaults crypto.randomUUID each call |278 durable submission/child identities and partial-success recovery |
| Fractional pcs can be requested then cannot be received | creation checks storage scale+capacity but not owning unit quantum; receipt now checks actual unit |278 same quantum at NEW creation |
| Partially delivered seller hides return button | customer-panel/orders/[checkoutId]/page.tsx requires fulfillment.status==Delivered |278 use eligible line facts, not aggregate-status permission |
| Multi-delivery same-line expiry subtracts unrelated old returns | evaluator sums still-in-window slices then subtracts ALL already-returned quantity |279 delivery-slice allocation fix, no guessed legacy reassignment |
| Template notification still bypassed | workspace uses DS ToastRegion; actual AppProviders installs react-toastify |278 correct actual integration |
| Decisions are permanently immutable | existing unique decision row and case transitions |279 audited pre-money reconsideration, not blind reset |
| Rejected received goods need physical resolution | recovery already reports physical_resolution_required with no executable resolution |279 define/implement lawful ownership, never label closed while goods owed |

## Product/ownership decisions
1 A customer may select the actual remaining quantity per item from any seller in the checkout. Correct/unselected items stay untouched.
2 Existing durable ReturnRequest stays seller-owned. New customer submission creates independently actionable CHILD CASES per (SellerOrderId, OrderLineId, selected reason/portion). Each child contains ONE order line; multiple selected units of that same line/reason belong together.
Example: sellerA socks3 -> choose2 wrong colour = caseA quantity2; sellerB select damaged product only = caseB; sellerB's correct product is not submitted.
Two different reasons on one line are separate portions/cases with a shared remaining ceiling. This is deliberate granularity, not a fabricated one-case-per-checkout authority.
3 No new master-case schema needed for278. The customer UI can group a frozen submission and display child receipts/results by seller/item. Existing single-seller API creates each child with stable keys. Partial successes are visible/recoverable; no cross-seller atomic transaction claim.
4 Old multi-item cases are not split/migrated/backfilled. Whole-case decision limitation stays explicit until279; do not silently reject/approve other lines.
5 Different stored unit precision is a real restriction; inability to select2of3, wrong-colour goods or a subset of one seller is not a justified business restriction.
6 Wrong-colour/defect reason is distinct from physical usable/damaged inspection. Never set condition from the customer's allegation.
7 Correcting approval/rejection is not intrinsically forbidden. Before financial/physical effects, an authorized deliberate audited reconsideration should be possible. Existing decision row is immutable for race/audit protection; that implementation constraint must be replaced by a lawful correction authority, not exposed as an arbitrary forever business prohibition.
After any financial attempt exists (including unknown), a decision cannot simply be reset: reconcile it first. Successful refund is history, not an erasable UI flag. No automatic reverse charge or claim that real bank settlement can be undone by changing case status.
Reopening a Rejected case must re-check remaining capacity because rejection may already have released it; received goods still need an honest physical obligation.279 owns this.
8 Shared template feedback is EXACTLY react-toastify from app/providers.tsx (top-right,RTL,colored,3000ms). Reference features/admin-shipping-authoring/tb261-notification.mount.test.ts and its real panel. DS ToastRegion import is NOT compliance with this user's template requirement. Use shared container once; no new provider/library/global style overhaul.

## Delivery278 — usable customer selection + actual template feedback
One implementation/result includes:
- authenticated owning customer eligibility per seller/line (delivered, returnable window, prior quantities, exact remaining, unit quantum/blocker), and no permission inferred from shipped/global Delivered;
- one proper shared modal over checkout seller groups, deliberate item/quantity/reason portions, no preselect/full-order return, exact decimal bounds;
- one frozen customer submission + stable child keys/bodies, deterministic grouping, exact replay, session interruption/partial-success recovery; no duplicate/reset of successful children;
- creation enforces accepted owning unit and capacity server-side; foreign seller/customer/line fails closed;
- own child cases listed clearly with independent tracking, correct items unchanged;
- Returns customer/admin/seller outcomes use existing react-toastify pattern, no persistent dark ToastRegion substitute;
- controlled two-seller/same-checkout owning + mounted proof, partial delivery visibility, failure/retry/retarget tests, focused checks and served-artifact proof;
- no business fixtures/provider/money/stock calls or schema expected. User trial cases stay unchanged.
Delivered outcome is actual socks2of3 + damaged-other-seller support. Do not distribute these into separate helper/Toast/test micro-tasks.

## Delivery279 — remaining decision/physical/temporal integrity (NOT ISSUED)
After278 source/result review, issue ONE consolidated execution envelope:
- reviewed audited reconsideration of approve/reject before any money attempt or irreversible source/stock effect; append history, positive guards and serialized version binding; no delete/overwrite of history;
- no misleading “cannot ever go back” copy; explain allowed correction vs recorded financial effect;
- explicit treatment of legacy multi-item cases: no unreviewed whole-case money when only a portion is accepted; design/store the necessary per-portion authority or safely separate reviewed successor cases without duplicating capacity;
- permanent per-delivery-slice consumption/allocation for same-line repeat deliveries, including return-window boundary; old returned units cannot consume a newer batch twice; legacy unbound rows stay explicit, not guessed/backfilled;
- rejected physically received goods remain active owed work until lawful resolution; no false settled/archive;
- prove chosen refundable amounts against accepted item money facts and shared checkout payment budget; don't infer shipping/tax/discount refund policy from a case label or today's product price.
This may need one bounded additive Returns migration. Require exact schema/design proof, verified fresh pre-apply backup, MigrationRunner and rollback-only relational tests. No DDL until its eventual envelope authorizes it; no279 implementation/worker now.

## Exit criteria / source of truth
278 must prove A2wrong-colour socks of3 + Bdamaged item, Bcorrect item untouched; seller scopes and partial submission remain independent; template toast actually appears through the existing provider.
279 must prove correction/expiry/partial physical-financial laws in controlled owning/relational tests, preserve real user history and distinguish real provider readiness.
A worker PASS is not user acceptance. No “all Returns complete” until the above gaps are resolved AND user has exercised the intended path. No provider/PSP readiness assertion from fake/controlled ports.
One worker/task, result review, next task only then. No screenshots requested, broad repeated suites, restart/build loops or automatic next implementation.
