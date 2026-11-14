PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266
Parent-Task: TB-SHIPPING-COURIER-ORIGIN-RETURNS-LINKAGE-AND-OBLIGATION-CLOSURE-265-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 334e8693f7ee92f8fde3f8610b915a40b78f7dd3
Mode: REPAIR_USER_REJECTED_QUEUE_AND_PAYMENT_AUTHORITY_WITH_AUTHORIZED_BACKED_UP_ORDER_CLEANUP_AND_SMALL_COHERENT_TRIAL_SET
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

## Goal
User cannot test the delivered screen: unreadable clipped queue, PendingPayment checkout with dispatched/returned fixture, unresolved group locator. User now instructs: «خوب تسک بده ارسال درست بشه من الان نمی تونم تست کنم به نظر من همه سفارشات را حذف کند و فقط چند تا تستی بمونه».
ONE worker, ONE task, no delegation/resuming old agents. Repair these defects and deliver a small coherent trial set on SAME canonical environment, then STOP. Read AGENTS and docs/planning/shipping/SHIPPING-USER-TRIAL-REJECTION-2026-11-14.md. Earlier ready-for-user-trial banner is superseded. No broad redesign/extra stages/test loops.

## 1. Diagnose and close actual payment/supply authorization
Source confirmed: old TB265 test directly SQL-inserts seller status Paid, inserts checkout without payment, constructs FulfillmentUnit.CreateFromPaidOrder, directly dispatches; controlled test readers answer paid while runtime does not. This is a fixture construction defect, not proof that ordinary unpaid checkout passed payment. Append correction to prior265 evidence: no-raw-SQL/fully-production-path claims were false for these helpers.
Trace current owning payment/supply policy from checkout to fulfillment creation AND existing-unit mutations. Production creation checks IsPaid, but existing DispatchShipmentCoreAsync and Order.ProjectActions do not visibly recheck payment. Prove actual public paths before concluding.
For OnlinePurchase prepayment, current authoritative payment/supply permission must gate NEW physical preparation/dispatch through individual, consolidated-package and bulk routes, including direct publicly exposed owning seams. No browser-only gate, no trusting seller status Paid or existence of Fulfillment as substitute payment truth. Reuse one existing narrow owning Contracts policy; do not introduce cross-module DbContext access or duplicate payment authority.
Preserve legitimate explicitly supported deferred/COD flows only if current business policy defines them; freight payer پیش/پس کرایه is NOT goods payment permission. Historical sent/returned facts remain readable; unpaid history inconsistency must not be relabelled as paid/delivered. Reverse-logistics audit is distinct from authorizing a NEW shipment.
Refusal must occur BEFORE inventory consume/physical state/event/parcel effects. Authoritative operations projection and command agree; exact replay of an already recorded operation returns history rather than creating another shipment. Preserve actor/tenant/source/quantity/package/reservation/occurrence gates.

Focused public proof: actual unpaid checkout refuses new supply/dispatch with localized reason and ZERO consumes/new dispatches; lawful paid/supply-authorized case works via existing owning event/command path, not SQL-paid fixture; package/bulk cannot bypass. Existing inconsistent fixture history stays honest. No live provider/wallet/payment/refund money.

## 2. Fix queue readability using the existing grid
User screenshot: operational badge clipped vertically, multi-line scope cut, tiny dense text, order identity reduced to meaningless UUID tail. Inspect actual AppDataGrid row-height/density/cell overflow and page cell content; don't infer from JSX utility class names.
Same shared grid/RTL design, readable status and scope, human order number, quantity and dispatch timing aligned without overlap/clipping. Set correct supported row sizing/cell layout instead of hiding labels, shrinking fonts, overlaying badges or building a second grid. Keep sort/filter/paging/full-set counts and exact work-unit identities.
One clear operations entry per physical row. Verify real browser390 and1440: text fully legible, badge not clipped, controls usable, no document overflow. Scoped table horizontal scrolling is acceptable when necessary; do not crush desktop columns into illegible mobile text. Existing Toast for confirmed actions, MessageBox for blocking confirmation, local validation; no new notifications.

## 3. Exact operations locator must resolve
Old test's Fulfillment group does not have corresponding Order captured group. Clicking queue yields "target not found" and seller summary instead of promised target. Repair projection/mapping for lawful current rows and coherent fixtures; do not guess another group or present summary as exact target. A genuinely stale/unresolvable locator shows a concise explicit state with safe navigation and zero unintended action.
Prove row -> matching group/parcel/package in real route, after reload and back-to-queue filter preserved. Use SAME shared operational component, no finance panel or new operational page.

## 4. Authorized order cleanup — strict scope, backup and inventory conservation
This user instruction SUPERSEDES the earlier no-order-deletion restriction ONLY for the canonical DEVELOPMENT order dataset and directly dependent operational records. It does NOT authorize wiping database/catalog/users/shipping settings or payment-provider history.
Before deletion:
- Verify same canonical SingleStore/store-alpha/tooba_alpha5432, Development, not another deployment; no new database.
- Produce private full custom-format PostgreSQL backup, SHA256, pg_restore --list success and documented exact restoration command to SAME target (no restore now, no test DB). Record baseline per affected owner and protected catalog/accounts/settings counts.
- Build precise deletion manifest of order/cart/fulfillment/shipment/package/return/source-claim/decision/operation histories and dependencies. Use repository's supported developer cleanup/reset tooling scoped to order data if available. Never TRUNCATE CASCADE across schemas, disable FK checks, drop/recreate database, blanket reseed, or hand-delete unknown tables.
- Resolve/reconcile order-bound Held stock through owning Inventory cancellation/cleanup semantics ONCE before deletion; never directly zero Reserved or release consumed sale reservation; consumed stock/ledger is historical physical truth. Preserve audit of cleanup effects in private manifest. Active outbox/jobs must not recreate deleted work. No global reclaimer.
- Do NOT erase genuine external payment/refund/wallet/settlement accounting or call providers. If genuine financial dependencies exist, or no safe owner-aware cleanup path can preserve integrity, do NOT force deletion: finish repairs, report exact cleanup blocker and affected counts. Don't claim all orders removed or hide them behind a filter.
After preflight, authorized deletion may proceed without another routine confirmation, using the scoped existing workflow. If it needs implementation, build one explicit Development-only owner-aware cleanup command with a dry-run manifest and expected-target/backup checks, no production endpoint or generic purge framework. Raw SQL may be used ONLY inside the reviewed scoped cleanup implementation where owning persistence requires it, with explicit target IDs and transactions; it may NOT fabricate statuses or seed paid business facts.
Protected: products/offers/stock identities/catalog/media, customers/accounts/permissions, shipping methods/drafts/publications/languages/geography, DataProtection keys, configuration and CapturedPreview. Do not overwrite user method01a17d0d-e033-7000-af43-49a1daa28822. No edition/connection/port switch.
Report deleted/retained counts and protected counts after, no orphan queue/case rows, no negative Reserved, no silently resurrected jobs. Backup is mandatory before destructive action.

## 5. Leave ONLY a few coherent trial scenarios
Target FOUR clearly named TB266 review orders, at most SIX only if a distinct acceptance scenario cannot share an order:
1 unpaid awaiting payment: NO new shipping/preparation permission.
2 lawfully supply-authorized ready order: accepted group/timing + real parcel prep/tracking/dispatch route.
3 returned usable item: reviewed same-goods resend path.
4 damaged/mixed returned item: new healthy-stock replacement vs existing Returns case; partial1of3 can leave2 owed if supported in same scenario.
Create through owning checkout/payment-test confirmation/supply/shipping commands/events, real accepted capture and state transitions. No INSERT/UPDATE status='Paid', no faked payment reader attached to canonical runtime, no direct factory pretending it proves the paid path. Use EXISTING safe developer/test payment confirmation mechanism if available; production provider calls remain forbidden. Controlled financial ports in a relational test are useful focused proof but NOT a coherent live payment state.
If no safe existing way to authoritatively confirm task payment without external money exists, leave unpaid demo plus coherent read-only examples ONLY where genuinely lawful, disclose blocker; do not invent provider transaction or toggle edition. Do not manufacture hundreds of fixture rows by running old integration classes. Reuse minimal task-owned scenarios.
Provide Persian short order labels, exact review links and expected next action. A user should not need to create data or interpret UUIDs. Mark any irreversible business action clearly; don't tell user "test refund" as if live UI were wired to controlled financial ports.

## 6. Verification budget / delivery
One canonical DB, FE3000->Host5088. No disposable/Docker/15433 DB, no provider/real money/fake waybill. No screenshots deliverable, no broad module/base parity/stress tests, no repeated green checks. Minimal targeted negative unpaid+positive authorized/public package/bulk proof; real browser queue readability and exact navigation on the small trial set. Affected typecheck/build/publish once after final changes. Preserve263 feedback,264 readiness/mobile and265 quantity/claim/closure invariants; no scheduling/Returns redesign.
Schema only if actually required, additive repository migrations+backup, no unrelated model repair.
Persist TASK verbatim docs/ai/tasks/TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266/TASK.md and evidence TASK/RESULT/RECEIPTS/result-contract, cleanup dry-run/backup receipt/before-after counts/audit correction, public-payment proof, browser geometry and user trial checklist under docs/architecture/evidence/TB-SHIPPING-PAYMENT-GATED-DISPATCH-READABLE-QUEUE-AND-CLEAN-TRIAL-DATA-266/.
Update AGENTS/START-HERE/architect recovery/current plan with real progress/limits. Commit AND push, full base/code/evidence/final HEAD and remote parity, runtime/build/migrations, exact safe URLs. No force/amend/reset/stash/rebase/unrelated overwrite.
PASS only if required work actually complete; PARTIAL if cleanup/positive authority/browser blocked. No worker/user/architect acceptance conflation. STOP_FOR_ARCHITECT; no next task proposed. User trial follows review.
END_TOOBA_TASK
