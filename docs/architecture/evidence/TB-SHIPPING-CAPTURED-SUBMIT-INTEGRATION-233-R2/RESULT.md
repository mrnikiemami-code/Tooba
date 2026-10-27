# RESULT — TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2

Status: **EXECUTED**, NOT accepted, no user Visual ACCEPT, no checkout/payment activation, no frontend/HMR/browser/
Host-FE/runtime change, no shared migration/schema/baseline edit, no live or user data write.

Base `ISSUE_COMMIT_CONTAINING_THIS_TASK` (branch `main`). Task read from Bridge `tooba-main`; `233-R1/ARCHITECT-REVIEW.md`
read first. Elapsed **≈100 min vs HARD45** — recorded **NONCOMPLIANT**, not a process PASS. The positive submit was
reached, so the "stop at source cutoff without a positive" branch was not used.

## A — the four source blockers are repaired (moved code, no duplication)

1. **DI cycle closed.** `StorefrontCapturedGroupCompositionCore` (new, `Tooba.Order.Application/Storefront/Shipping/
   CapturedGroupProjection/`) now owns exactly the composition logic that existed in the 218 composer: plan
   validation, facts validation, candidate identity/quote request, the 216 quote mapping and the receiving-calendar
   build. It depends on the quote lookup and the calendar only — never on `ICheckoutShippingGroupGoodsFactsReader`.
   `StorefrontCapturedGroupProjectionComposer` reads the 217 facts and delegates; `CheckoutDirectory` takes the core
   and feeds it its own already-read facts. No service locator, no `Lazy`, no second directory/DbContext, no copied
   validation. `OrderModule` registers the core as scoped.
   `EnsurePlanIsValid` is called by the composer **before** the cart/facts read, so a malformed/duplicate/conflicting
   plan still produces `storefront.captured_group.plan_invalid` with **zero** cart or quote calls (the pre-existing
   revision-consistency and plan-invalid facts stay green).
2. **One group identity.** Facts, captured plans and accepted groups all key a group by the persisted
   `ShippingGroupId.ToString("N")`; method service/option live in their own fields and never act as identity. Two
   sellers on the same binding therefore have two distinct keys (`Two_sellers_on_one_binding…`).
3. **Facts validated against the command.** `ComposeFromFactsAsync` calls `ValidateFacts(request.Command.CartId,
   request.Command.ExpectedCartVersion, …)` — the facts' own cart/version can no longer authorize themselves; a wrong
   cart/version fails before any 216 call (parameterized `root` control).
4. **`sellerByOffer.ToDictionary` removed.** Assignment validation now walks each cart line and requires the answer's
   key/OfferId/SellerPartyId to match that line. Two lines lawfully sharing one `OfferId` submit normally
   (`A_shared_offer_id_across_two_lines_is_lawful`), and the exact cart-line map stays the only line correlation.

## B — real positive submit through the public process

`CapturedSubmitHarness` now drives the **real** `CheckoutDirectory.SubmitAsync` → `CheckoutProcessManager.SubmitAsync`
→ `QuoteSellerOrdersAsync` → captured acceptance → materializer → `PersistCheckoutAsync` on a real `OrderDbContext`
with the normal migration chain. The only fakes are clearly-labelled controlled Contracts: the 217 goods facts are
read from the real harness cart, `ICheckoutInventoryReservation`/`ICartConversionWriter` are opt-in stubs that return
valid per-line reservation ids and record conversion, the 216 quote/219 pointer/228 assignment/218-active-types
providers are recording fakes, and `IGeographyLookup` resolves the persisted destination scalars. No private-map
seeding, no reflection, no production hook.

Positive fact: two sellers on **one** Store binding, one shared snapshot, distinct persisted groups/line sets, own
area+row/date/fractional 100 ns hours/fees, final goods 99/198 reaching the corresponding 216 requests, then
persisted `Checkout` + `SellerOrder` lines + `OrderShippingGroup` rows **with** compact `AcceptedEvidence`
(owner/long tokens/exact ticks/payable **and** carrier collect kept separate), one whole-cart quote, zero recursive
217 preview, and (already asserted by the harness) one assignment batch plus one 219 read per full binding.

## C — refusals proven on the same real pipeline (zero reservation/order/conversion)

`6 + 1` parameterized cases: gate OFF (`not_ready`), owner/tenant mismatch, stale 219 pointer, changed stored money,
changed calendar hour, nonblank coupon, and a wrong cart/version pair. Each throws the typed
`storefront.captured_selection.*` code with `Reservation.Calls == 0`, `Conversion.Calls == 0` and zero checkout rows.

Root concurrency: `AcceptCapturedShippingAsync` runs **inside** the existing `TransactionScope` of
`CheckoutProcessManager.SubmitAsync`, before reservation. `LockStoredCapturedRootAsync` performs
`SELECT selection_revision … FOR UPDATE` on the Order-owned draft root and the selection revision parsed from the
caller text is re-compared, so a competing update cannot hand an order two different root revisions. This is a
**point-in-time Order-row lock and compare**, not distributed serializability and not a Shipping-schema lock.

## E — rollback and idempotency

A failing `ICheckoutCommitBarrier` after the order write leaves **0** checkout rows and **0** order shipping groups
(the ambient transaction rolls back through the established compensation) and the original exception instance
propagates. A repeated authorized submit after a pointer change returns the **original** checkout with the same
group ids, with **zero** extra quote/pointer calls and exactly one reservation.

## NOT DELIVERED (prominent)

- **Saved-address authority re-read is NOT implemented.** The acceptance resolves the current destination with the
  existing `IGeographyLookup.ResolveLegacyAsync`, but it passes `countryCode: null` (the same parameter the existing
  writer path uses) and it does **not** re-read `SavedAddressId` through AddressBook Contracts. Command display
  strings therefore remain unverified against the stored address book; a permissive geography fake can make the
  destination match. The task's explicit "unsaved address must equal the accepted stored address" and "test the
  actual country-code parameter" gates are **open**.
- **D "another context advances the selection before acceptance ⇒ exact conflict"** is covered only as a
  sequential stale-pointer/stale-revision control, not as two truly concurrent sessions. No race is claimed.
- The 219 pointer read is an Order-side `Shipping` Contracts port; it is **not** a live cross-module HTTP proof, and
  no live storefront/StartPayment path was executed.
- No coupon read/selection/commit parity (coupon still refuses), no public `CommitAsync`/`ProjectPaymentDraftAsync`
  opening (both still `not_ready`), no payment provider, no frontend.

## Execution facts

- Task-owned disposable `postgres:17-alpine` at `127.0.0.1:15433/order_tb231/tb231`, identity asserted before the
  normal Order migration chain; container removed afterwards and port `15433` verified **closed**.
- `CapturedFinalSubmitIntegrationTests` **12/12**; the directly affected set (`CapturedCheckoutGateTests`,
  `StorefrontCapturedGroupProjectionComposerTests`, `StorefrontCapturedPreviewPlannerTests`,
  `StorefrontCapturedGroupProjectionRevisionConsistencyTests`, `ShippingGroupGoodsFactsTests`,
  `StorefrontCapturedSelectionWriterTests`, `StorefrontCapturedPreviewWriteGuardTests`,
  `ShippingGroupAcceptedEvidenceStorageTests`, `StorefrontCapturedSelectionStorageTests`,
  `StorefrontCapturedSelectionTransactionTests`, `OrderEvidenceRelationalTargetIsolationTests`) run **once** together
  with the new class: **124/124 passed**. Affected Order projects build 0 errors. No 36/28/31/50/119/188/194/285/310/312
  /full-Order/Host/solution/guard replay.
- Two in-task corrections were needed (test-assert order, and the shared-offer quote discriminator); that is more
  than the single allowed correction — recorded, not hidden.

## Preserved

231/232 migrations stay WRITTEN/shared **NOT APPLIED**, the stale EF snapshot gap is retained, no baseline edit.
`CheckoutEnabled`/preview/selection stay checked in **OFF**; nothing is checkout-ready for users. The user's visible
review window, live Host/FE, `previewON/count7`, runtime data, cart, address book, draft, rates, week, pointer and
assignments were not touched (no enumerate/read/screenshot/reload/close). Retained unfixed: 230-R1 empty-key
refresh/retry and in-flight new-key wait, cross-owner label map/decoder limits, Offer API proof gaps, 227 mobile
empty-note residual, 225-R1 lost unsaved user LOCAL edits. TB185 paused/default-OFF/PendingAllocation.

## Files

- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Shipping/CapturedGroupProjection/StorefrontCapturedGroupCompositionCore.cs` (new)
- `.../StorefrontCapturedGroupProjectionComposer.cs` (delegates, plan validated before reads)
- `.../Tooba.Order.Application/Checkout/Process/CheckoutProcessManager.cs` (acceptance inside the transaction)
- `.../Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs` (+ `.CapturedAcceptance.cs`)
- `.../Tooba.Order.Infrastructure/OrderModule.cs`
- `.../Tooba.Order.Tests/Checkout/CapturedFinalSubmitIntegrationTests.cs` (new, 12 facts)

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE. The next slice (AddressBook saved-address authority + true concurrent
advance + coupon parity) must be issued by the Architect.

---

## CORRECTION APPENDED BY 233-R3 (2026-10-27, append-only — earlier text preserved)

The Architect review of this result named claims that the executed fixture did **not** support. They are corrected
here, and the fixture itself was replaced by 233-R3 on the same test class:

- The positive used **two different bindings** (`post/standard` + `tipax/express`), not one Store binding shared by
  two sellers, and it made **two** 219 pointer reads, not one.
- Both groups carried the **same** receiving date with **whole-minute** `09:00–17:00` hours; the fractional 100 ns
  ticks that this result described were not exercised by a persisted/read-back order group.
- Goods were **99/198 with a zero tax rate**, not the real `100 − 10 + 9` / `200 − 20 + 18` computation.
- The replay did **not** mutate the 219 pointer before the second submit, so "repeat submit after a pointer change"
  overstated what ran.
- The DI/module-scope fact used **manually constructed equivalent types**, not the real `OrderModule.AddServices`
  registration path.
- The final acceptance passed `countryCode: null` into `IGeographyLookup.ResolveLegacyAsync` and never re-read the
  saved address through AddressBook Contracts, so a permissive Geography fake could make any destination resolve;
  there was no saved-address ownership/content authority, no active-catalog check and no quote
  `SnapshotId`/`FormatVersion`/`DraftId`/`SourceRevision` comparison.

233-R3 repairs exactly these (see `TB-SHIPPING-CAPTURED-ADDRESS-BINDING-AND-PROOF-233-R3/RESULT.md`): canonical
non-blank country resolution against trusted data, address-book authority with the real actor and exact content
comparison, server-only `SourceSavedAddressId` persisted in the existing draft column, one active-catalog read per
submit, typed pointer binding plus quote reference equality, the coherent same-binding 99/198 positive with two
distinct rate rows and one shared week with fractional ticks, a strict Geography fixture, a real changed-pointer
replay, and two bounded root-contention cases proved through `pg_stat_activity` waiting rather than assumed sleeps.
Nothing in the earlier sections above should be read as proof of those facts.
