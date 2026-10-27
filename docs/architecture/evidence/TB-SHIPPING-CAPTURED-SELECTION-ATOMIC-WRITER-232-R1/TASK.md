PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232-R1
Parent-Task: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: BOUNDED_GROUP_ISOLATION_AND_FAILURE_ATOMICITY_REPAIR
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD35 / STOP source22 / final13 focused proof+recovery. One correction round; if the box expires, stop and report exact remaining work, do not silently extend it.
232 is NOT ACCEPTED. Read 232/ARCHITECT-REVIEW.md and this task. Applicable repo backend instructions. NO Bridge invocation/enqueue/wake/post-result scripts. No autonomous233.

GOAL
Repair the existing actual captured draft writer, not add another unused authority. Keep shared ResolveAsync, 231 compact evidence, existing HTTP selection entry and final-payment refusal. No frontend/HMR, Host/FE/runtime refresh, live business write or shared migration apply. The dedicated VISIBLE Chrome review window is the USER's unsaved work: never enumerate/select/automate/reload/close it.

1. GROUP-SCOPED EVIDENCE, NOT METHOD-SCOPED
StorefrontCapturedSelectionWriter currently assigns evidenceByMethod[candidate.MethodCode] and attaches by group.MethodCode. Two sellers selecting the SAME Store service/option share that code; the later group's row/date/money overwrites the earlier group's evidence. Different money may refuse a valid cart, equal money can silently adopt another group's rate row/date evidence. Fix correlation by full trusted server group obligation (server GroupKey and exact membership/seller + binding), preserving stable group identity, not list index, label, first match or string splitting.
Reuse the existing CartShippingGroupPlanner without changing its LEGACY uniqueness rules. Captured construction must keep the exact server partition; a narrowly additive typed captured path/overload is allowed only if necessary. No invented public method labels/codes to conceal collisions.
RED then GREEN: two DIFFERENT sellers, SAME Store binding/snapshot, distinct cart members, same freight+packaging but DIFFERENT matched row IDs and chosen dates/hours; both save their OWN evidence. Control with differing prices also succeeds. Validate persisted full graph, not just fake arguments. Keep single-group and independent binding controls.

2. CLEAN CALLER + FRESH PERSISTED GRAPH BEFORE MUTATION
GetByCartIdAsync is a tracking query; it returns stale tracked scalars/children. The writer calls Replace/ApplyRecipientNames/SetDestinationIdentity on that object BEFORE the atomic adapter. The adapter then calls context-wide SaveChanges without a clean-tracker guard.
Provide a captured-specific preflight/load on the EXISTING store as needed: refuse unrelated Added/Modified/Deleted work BEFORE any transaction or mutation and preserve it exactly; derive CAS and reusable groups from current persisted authoritative root+complete children, never EF identity-map truth. Legacy read/save semantics stay unchanged. Do not clear/refresh/detach unrelated callers or flush their work. Exact current expected token must work through an actually stale context; stale expected token must conflict with no mutation/leak.
Prepare an immutable/detached validated replacement intent before tracked mutation or snapshot owned originals before mutation, so transaction-start failure is also safe. Require expected root revision text explicitly on captured requests (omitted/null/blank is NOT an implicit version0); explicit "0" remains valid absence/legacy0. Reject captured+ShippingGroups mixture as well as scalar legacy shorthand. Validate cheap intent shape before foreign quote reads.

3. ROLLBACK RESTORES THE ENTIRE ATTEMPT ONLY, INCLUDING CANCELLATION
Current code captures only previousRevision and reads root.OriginalValues AFTER first SaveChanges accepted the NEW address/cart/time/money/destination into originals. On insertion failure it can mark rejected scalars Unchanged; later unrelated root edit can flush them. Capture pre-attempt persisted originals (ALL root scalar fields, groups, lines, owned evidence), restore both CurrentValues and OriginalValues correctly plus navigation identities after rejection. The current last line loop detaches EVERY tracked CartShippingDraftGroupLine not in keep (no cart/attempt scope): remove that global sweep; unrelated draft group/line identity and state must survive.
Cleanup rollback uses a noncancelled cleanup token; propagate ORIGINAL exception instance or caller cancellation token; a cancelled rollback must not mask failure or leave attempt tracking. Dispose own transaction before recovery/return, no nested transaction or global Clear. BEGIN failure, before-first-save failure, between-stages failure, actual second-save failure/cancellation all leave no rejected work for later SaveChanges.
Focused real PostgreSQL tests: mutate address, cart version, note, money, destination, names and UpdatedAt; inject failure AFTER first SaveChanges and prove all old scalars AND originals/navs preserved, SaveChanges==0, then a separate lawful root edit cannot publish rejected values. Unrelated clean and dirty graphs/lines of another draft stay exact (dirty caller preflight refused). Actual caller token cancellation retains token, no pending attempt, later SaveChanges0, DB old. Tests use current shared instance through actual service/writer where needed, not only an already-unmodified direct adapter fixture.

4. COUPON ASYMMETRY — FAIL CLOSED FOR THIS SLICE
Current read has no coupon intent, but writer accepts CouponCode and can store a different goods total/price under identical snapshot/row echo tokens without reconfirmation. For THIS bounded repair explicitly refuse nonblank captured CouponCode before any quote/write with a stable localized unavailable/intent code. Blank may canonicalize absent. Do not silently drop a supplied coupon, invent a coupon authority, enable couponed captured checkout or broaden into frontend changes. Record that coupon support requires matching read+selection+final checkout context in the future. Test nonblank => zero quote/persistence; blank => ordinary path.

PROOF / FINISH
Use existing task-owned isolated231 PostgreSQL target 127.0.0.1:15433/order_tb231/tb231, identity before DDL, existing local image, one disposable container, normal Order migrations there only; remove and verify port closed. No shared5432 changes, no migration replay, no schema redesign, no baseline snapshot regeneration.
Build changed Order projects; run only new repair class(es) plus directly affected 232 writer/storage/guard classes once. No 310/312/full Storefront/Shipping/Host/solution/guard confidence loop. Disclose pre-existing red guards; never call a sequential stale-writer fixture a simultaneous race.
No new order/history/payment writer. CommitAsync and ProjectPaymentDraftAsync stay not_ready for stored captured evidence even all flagsOFF; SelectionEnabled checked-in false. No activation/customer-ready claim.
Task verbatim + RESULT.md in this task evidence folder; append correction to232RESULT without deleting history; update ALL3TOPs (AGENTS.md, START-HERE-IF-CHATGPT-IS-LOST.md, SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md) with precise defects/proof/limits. 231/232 migrations remain sharedNOTAPPLIED; EF snapshot gap retained.
Exact-path commit/push/fetch parity ONLY. NO amend/force-with-lease/force-push/history rewrite (232 violated this; do not repeat). Leave unrelated CRLF drift alone.
Report actual elapsed and corrections. STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. No user trial invitation before review. TB185 paused/default-OFF/PendingAllocation; existing known UI/Offer/cross-owner gaps and lost unsaved225-R1LOCALwork remain recorded.
