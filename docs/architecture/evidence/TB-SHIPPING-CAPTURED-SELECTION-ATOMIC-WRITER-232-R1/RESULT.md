# TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232-R1 — RESULT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232-R1
Parent-Task: TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232
Channel: tooba-main
Worker-Id: tooba-worker-01
Mode: BOUNDED_GROUP_ISOLATION_AND_FAILURE_ATOMICITY_REPAIR
Base-SHA: 71b0e71d418149776b432db9705dd72c36390e14 (expected `ISSUE_COMMIT_CONTAINING_THIS_TASK`; local HEAD was
`520c508d7` at receipt, fast-forwarded to the issue commit; the three pre-existing Shipping line-ending drifts were
never staged)
Status: **EXECUTED — PASS within scope**, NOT accepted, no user Visual ACCEPT, no live/HTTP writer, no shared
migration apply, no frontend/Host/runtime change.

## 1. Group-scoped evidence instead of method-scoped (blocker 1)

`StorefrontCapturedSelectionWriter` no longer keeps `evidenceByMethod[candidate.MethodCode]`. Each server group
now produces one typed `CapturedCommitment` — server `GroupKey`, the authoritative `SellerPartyId` of that group's
own cart lines (`CartSellerOf`, taken from the cart, never from an optional composition field), `MethodCode`,
`ServiceCode`, `OptionCode`, the exact `LineIds` set and the evidence built from that group's own quote/slot.
`AttachEvidenceToOneGroupEach` matches each planner-built group by the FULL obligation
(seller + canonical method + service + option + identical line membership, order-insensitive) and requires
exactly one match per group and one group per commitment; zero matches, a second match (ambiguous obligation) or
a leftover commitment refuses the WHOLE write with `storefront.captured_selection.intent_invalid`, so no group can
inherit another group's evidence and no group can be written without its own. `CartShippingGroupPlanner` legacy
uniqueness rules, list order, labels, indices and string splitting are not used and were not changed.

## 2. Clean caller + fresh persisted graph (blocker 2)

New store member `IStorefrontShippingDraftStore.GetForCapturedSelectionAsync(cartId)`:

- `EnsureNoForeignPendingWork()` refuses with the new `storefront.captured_selection.pending_work` when ANY
  `Added`/`Modified`/`Deleted` entry exists, BEFORE any transaction, query or mutation, and changes nothing (the
  caller's pending work is left exactly as it was);
- every already-tracked `CartShippingDraft` of that cart is detached with its groups/lines, so the returned
  graph is the current persisted authoritative root plus complete children (`AsNoTracking`), never identity-map
  truth. The legacy `GetByCartIdAsync`/`SaveAsync` path is unchanged for non-captured callers.

`ReplaceCapturedSelectionAsync` re-reads the authoritative tracked root itself and derives both the CAS check and
the reusable groups from it. CAS uses the persisted `SelectionRevision`, not the caller object. Cheap intent
validation (group keys, duplicate/blank keys, service code, legacy shorthand, captured+`ShippingGroups` mixture,
explicit `ExpectedSelectionRevision` text, nonblank coupon) now runs BEFORE any foreign quote/pointer read.

`ExpectedSelectionRevision` is parsed with `NumberStyles.None`: omitted/null/blank/malformed/negative ⇒
`storefront.captured_selection.intent_invalid`; the explicit text `"0"` remains the lawful absence/legacy-0 token.

## 3. Rollback restores only this attempt, including cancellation (blocker 3)

The previous rollback restored the root from `OriginalValues` AFTER the first `SaveChanges` had already accepted
the attempt's scalars, and swept EVERY tracked group line outside `keep` (unscoped, other drafts included). Both
are gone.

`RestoreRejectedAttemptAsync` now:

- rolls back the attempt's own transaction with a NON-cancelled token and swallows only "transaction already
  closed", so cleanup can never mask or replace the original exception/cancellation;
- detaches ONLY the attempt's own groups/lines and the previous groups/lines of the SAME draft root, then detaches
  that root — no context clear, no global change-tracker sweep, no unrelated caller touched;
- re-reads the persisted root inside the same context, so the tracked graph's `CurrentValues` AND
  `OriginalValues` are exactly the pre-attempt persisted row from the rolled-back database. This is deliberately a
  detach-and-reload rather than a hand-written nav/state restore: the hand restore was PROVEN to corrupt the
  tracker (see "correction" below).

Consequently nothing from the rejected attempt can be published by a later `SaveChanges`, including after a BEG
failure, a before-first-save failure, a between-stages failure and caller cancellation.

## 4. Coupon fail-closed for this slice (blocker 4)

`SaveAsync` refuses a nonblank `CouponCode` with the new `storefront.captured_selection.coupon_unavailable`
BEFORE destination resolution, planner resolve, any quote or any write. Blank canonicalises to absent and takes
the ordinary path. The planner's own `couponCode` parameter and its 217 promotion passthrough were removed, so no
code path can quietly send a coupon the read never displayed. Couponed captured checkout stays unsupported until
read + selection + final checkout share one coupon context; that is recorded here, not implemented.

## Proof — focused only, task-owned disposable PostgreSQL

`postgres:17-alpine`, container `tb232r1-order-pg`, `127.0.0.1:15433/order_tb231` user `tb231`, identity asserted
by the existing helper before DDL, normal Order migration chain, container removed and port 15433 verified closed.

`StorefrontCapturedSelection` classes: **26/26 passed** on the final source.

Writer facts (`StorefrontCapturedSelectionWriterTests`, fake ports):

- `Two_sellers_sharing_one_store_binding_keep_their_own_evidence` (RED on the reviewed method-keyed version, GREEN
  now): two different sellers, same Store service/option, distinct lines, equal freight+packaging but different
  matched row ids and different chosen dates/hours — each group persists its OWN area/row/date/ticks.
- `Two_sellers_with_different_prices_each_keep_their_own_money` (control): different prices succeed with their own
  money.
- single-group and independent-binding controls remain green.
- `Nonblank_coupon_is_refused_before_any_quote_or_write` (zero planner/quote/write calls) and
  `Blank_coupon_takes_the_ordinary_path`.
- explicit-revision facts: blank/omitted/malformed revision refuses, `"0"` remains lawful.

Relational facts (`StorefrontCapturedSelectionStorageTests`, real PostgreSQL through the real store):

- `Two_sellers_with_one_binding_persist_two_independent_evidences`: read back from a fresh context, TWO groups with
  TWO independent evidences (AreaGroupA/RateRowA/09:00 vs AreaGroupB/RateRowB/10:00) and the legacy scalar is the
  sum (34,000).
- `Exact_current_token_succeeds_through_an_actually_stale_context`: the exact current token succeeds even though
  the caller's detached snapshot was stale.
- `A_stale_observed_token_conflicts_after_another_writer_advanced_it`: stale token ⇒ `...stale`, old selection
  intact.
- `A_failure_between_the_delete_and_insert_stages_restores_the_whole_attempt`: the attempt changes address, cart
  version (7→99), note, money (17,000→90,000), recipient names, destination (other country/province/city) and
  `UpdatedAt`, then fails on the real unique `cart_shipping_group_lines.cart_line_id` index BETWEEN the delete and
  insert stages. After it: revision 1, note NULL, cart version 7, money 17,000, old address/postal/province, empty
  names, old `UpdatedAt`, old destination ids (and service area NULL), `OriginalValues` equal to those same values,
  `HasChanges() == false`, `SaveChanges() == 0`, all group/line entries `Unchanged`, and a following lawful root
  edit publishes only its own change (note) while the rejected money/destination/version never appear. The other
  draft's clean graph keeps its own revision/note/group.
- `Unrelated_pending_work_is_refused_before_mutation_and_preserved_exactly`: unrelated pending work is refused with
  `...pending_work`, nothing written, and the pending entity kept exactly.
- `Caller_cancellation_leaves_no_attempt_and_the_old_selection`: caller cancellation keeps the ORIGINAL
  `CancellationToken` on the surfaced exception, leaves no attempt tracked, `SaveChanges() == 0`, DB old.

Affected broader classes were run ONCE, not as a confidence loop: Storefront + Checkout = **285/286**, with the
single remaining failure being the PRE-EXISTING `StorefrontShippingPackagingChargeBoundaryTests` allow-list guard
(verified red at base `520c508d7`: it expects exactly the three `OperationalShadow` files while base already had
8 Packaging-referencing production files from 229/231/232). The `DateTimeOffset.UtcNow` architecture guard from
229/231 is likewise pre-existing and untouched here. Order projects (Domain/Application/Infrastructure/Tests) build
with 0 errors.

## Correction inside the task (disclosed)

The first version of this repair kept the "snapshot then hand-restore" rollback. The new between-stages fact
exposed that it corrupts the tracker: restoring the previous group to `Unchanged` and re-adding it to the root
navigation made EF re-attach it as `Added`, and the next query then created a second instance with the same
`ShippingGroupId` ⇒ `InvalidOperationException: another instance with the key value ... is already being tracked`.
That is a real defect of the hand restore, not of the test. The rollback was replaced by the detach-and-reload
described above, and both that fact and the cancellation/stale-context facts are green on real PostgreSQL.

One test-hygiene change is included: the two relational classes that share this one disposable database
(`StorefrontCapturedSelectionStorageTests`, `ShippingGroupAcceptedEvidenceStorageTests`) are now in one
non-parallel xUnit collection. Without it the two classes wipe each other's fixtures through their whole-table
resets (observed: "Sequence contains no elements" / spurious `...stale`), which is a test-environment artefact, not
a product behaviour.

## Honest limits

- The writer is still model-bound over the existing `PUT /v1/storefront/shipping/selection`; no HTTP/live call, no
  Host/FE refresh, no runtime enablement. `SelectionEnabled` checked-in default stays false.
- `CommitAsync` and `ProjectPaymentDraftAsync` keep refusing `storefront.captured_preview.not_ready` for a stored
  captured draft even with all flags OFF; no order/reservation/payment/cart conversion, no historical read.
- Migrations 231 (`20261027150000`) and 232 (`20261027180000`) remain WRITTEN and shared-NOT-APPLIED; the EF
  snapshot baseline gap stays disclosed, no regeneration.
- The sequential winner/loser fixtures are optimistic-concurrency/rollback proof, not a simultaneous race.
- Commit-time destination/cart/pointer/quote revalidation and coupon read/write/final-checkout parity remain later
  work; `DestinationServiceAreaId` is still always NULL.
- Retained unfixed and unchanged: 230-R1 empty-key retry/in-flight wait, cross-owner label map/decoder gaps, Offer
  API proof gaps, 227 mobile empty-note residual, the unrecovered 225-R1 lost unsaved user LOCAL tab edits.
- The dedicated visible Chrome review window, live Host/FE, previewON/count7, runtime data, buyer/cart, admin
  draft/rates/week/pointer/assignments were NOT touched (no enumerate/read/automate/reload/close).

## Process

Elapsed ≈29 min from claim to proof completion, inside HARD35, with one in-task correction round (rollback
tracker corruption + strengthened mid-stage fact). No commit amendment, no force-push, no history rewrite.
STOP_FOR_ARCHITECT; Automatic-Next-Task NONE; TB185 paused/default-OFF/PendingAllocation preserved.

## Correction appended by 232-R2 (2026-10-27) — cancellation fact is pre-cancelled, not a write-boundary proof

The R1 fact `Caller_cancellation_leaves_no_attempt_and_the_old_selection` cancels its
`CancellationTokenSource` **before** calling `ReplaceCapturedSelectionAsync`. It therefore proves pre-cancelled
token safety only; it does **not** prove cancellation after the first deletion/root save. The original test is
left as-is and is not relabelled. The real boundary proof (caller CTS cancelled inside the second
`SaveChangesAsync`, caller token retained on the thrown `OperationCanceledException`, rollback with a
non-cancelled token, owned transaction disposed, no pending attempt, old graph intact) now lives in
`Order.Tests/Checkout/StorefrontCapturedSelectionTransactionTests`
(`Cancellation_at_the_write_boundary_keeps_the_caller_token_and_disposes_the_transaction`).

Also recorded by 232-R2: R1's adapter allocated `IDbContextTransaction` without disposing it on any path
(measured RED: zero `TransactionDisposed` events for create/replace) and its rollback/detachment ordering could
let a secondary rollback failure mask the primary exception. Both are repaired and proven in
`TB-SHIPPING-CAPTURED-SELECTION-TRANSACTION-CLEANUP-232-R2`. Everything else in this RESULT stands unchanged;
the writer remains NOT accepted pending Architect review.
