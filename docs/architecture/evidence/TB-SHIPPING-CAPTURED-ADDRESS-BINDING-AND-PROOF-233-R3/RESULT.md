# RESULT — TB-SHIPPING-CAPTURED-ADDRESS-BINDING-AND-PROOF-233-R3

Pipeline: BRIDGE-WAKE-V1 · Worker: tooba-worker-01 · Base: `2218873b9` (the issue commit, no drift, no
fast-forward needed) · Parent: TB-SHIPPING-CAPTURED-SUBMIT-INTEGRATION-233-R2.
Status: **EXECUTED**, NOT accepted by the Architect, **NO user Visual ACCEPT**, no checkout activation, no
frontend/HMR/browser/Host-FE/runtime change, no live storefront/API/business write, no shared migration apply,
no schema/baseline edit.

Elapsed ≈48 min measured from the first BRIDGE-WAKE that delivered this task (≈20:06) to the report (≈20:54),
plus the resumed run from ≈20:33, vs HARD40/source25/final15 — recorded **NONCOMPLIANT**, not a process PASS.
One correction budget was already consumed by compile/fixture fixes (see *Corrections*, honestly listed as more
than one in-task fix round).

---

## A — Country and saved-address authority (one actual fix)

**Country is no longer `null`.** The captured final acceptance resolves the canonical non-blank ISO code from
*current trusted country data* for the priced stored country id: `IGeographyLookup.ListCountriesAsync`, filtered
by `CountryId == draft.DestinationCountryId`, non-blank `CountryCode`, `Distinct(OrdinalIgnoreCase)`; the result
must be **exactly one** code, else `storefront.captured_selection.destination_unverified`. Only that code reaches
`ResolveLegacyAsync(code, province, city, serviceAreaName: null, ct)`; the resolution must be `Resolved` **and**
`identifiers.CountryId` must equal the priced country id, and the resolved area/city ids must equal the stored
priced destination ids (`SameDestination`), with service area still explicitly unsupported (`null`). No hardcoded
`"IR"`, no first-country fallback, no cross-country search, no label guessing.

**Saved address is read through the address book with the real actor.** When the priced root carries
`SavedAddressId`, the acceptance calls `IAddressBookCheckoutLookup.GetAsync(actor, savedId, ct)` where `actor`
comes only from server data (`command.PlacedByUserId` non-empty, `cart.OwnerUserId == PlacedByUserId`,
`command.CartAccess.UserId == owner`) — never from a browser owner field. Missing/deleted record, a foreign record
(the fake returns `null` for a different actor), or a record whose country/province/city/postal address/postal
code/mobile no longer equals the priced snapshot refuses with `...destination_unverified` **before** reservation,
order persistence and cart conversion. The address that is persisted cannot differ from the one that was priced:
the accepted command's province/city/postal address/postal code/mobile are compared *exactly* against the priced
root (`StorefrontAddressCanonical.Same`, ordinal-trimmed, country case-insensitive). The command itself carries
**no** country field, so country identity is asserted on the priced root against trusted country data plus the
resolved destination ids — that is the honest limit of the wire shape, not a fabricated equality.

**Server-only provenance, existing column.** `StorefrontCheckoutPlacement` gained one trailing optional
`SourceSavedAddressId`; `PrepareShippingAsync` returns `new StorefrontCheckoutPlacement(actor, snapshot,
shipping.SavedAddressId)` while `snapshot.SavedAddressId` keeps its legacy `null` meaning. The 232 captured writer
persists `prepared.SourceSavedAddressId ?? prepared.Shipping.SavedAddressId` into the **existing**
`cart_shipping_drafts.saved_address_id` column, so an unsaved/legacy address remains unknown and is never guessed.
No new column, no backfill, no second AddressBook read in preparation.

## B — Binding and response checks finished on existing authorities

- `IShippingCatalogReader.ListAsync` is called **once** per final submit; every stored service must match exactly
  one **active** service by trimmed case-insensitive code, and (when the stored option is non-null) exactly one
  **active** option with the same code. A null option is the absent identity — no fuzzy fallback. Failure is
  `storefront.captured_selection.unavailable`.
- The 219 pointer is read once per distinct typed `StorefrontCapturedPublicationBinding` (new public `For(...)`
  factory reusing the accepted identity type; no delimiter-string tenant identity) and the answer must be
  `Selected` **and** equal the stored `DraftId`/`SourceRevision`/`SelectionVersion`/`SnapshotId`/`FormatVersion`/
  `ShippingTypeId`, **and** echo back the requested service/option; a contradictory answer is a `conflict`.
- The accepted quote must additionally carry the stored `SnapshotId`, `FormatVersion`, `DraftId` and
  `SourceRevision` (on top of the already-accepted currency/area/row/type/money/days/timezone/date+100 ns ticks).
- The final gate now requires the three coherent flags **and** `HasValidAvailableDateCount`; a missing optional
  authority (`IAddressBookCheckoutLookup`/`IShippingCatalogReader`) inside a captured passage is an explicit
  `unavailable`, and the true legacy passage still returns the caller plans with zero foreign calls.

## C — Corrected real submit fixture (same test, no new framework)

`CapturedFinalSubmitIntegrationTests` was rewritten to the coherent shape the review demanded, on the real public
`CheckoutDirectory.SubmitAsync` (real `CheckoutProcessManager` transaction) with the real pricing/tax registration
and only controlled *ports* (price lookup, promotion, tax rate, cart, offer, 216 lookup, 219 pointer, catalog,
address book, reservation/conversion recorders, strict Geography):

- **Two different sellers, ONE Store binding** (`post`/`standard`), one shared immutable snapshot, one shared
  draft (`DraftId = cart id`), one source revision `2`, one selection version `1`, one tenant `store-alpha`, and
  **the same area group** for the same destination while each group owns its **own rate row**.
- Real existing pricing path: 100 − 10 + 10% = **99** and 200 − 20 + 10% = **198**; the observed values are the
  amounts the real composition passes into the 216 lookup (`harness.Quotes.Requests`), asserted sorted
  `[99, 198]`. Not "99/198 with zero tax".
- **One shared stored week**; the two chosen lawful dates carry different fractional 100 ns tick hours
  (`OpenTicksA/CloseTicksA` vs `OpenTicksB/CloseTicksB`), and the persisted evidence keeps each group's own
  date/ticks/timezone.
- Both real Order groups/lines are persisted and read back: own `ShippingGroupId`, own rate row, own freight
  (120/70), packaging 5, payable (125/75), own date and ticks, `Owner.Kind = Store`, `Owner.TenantKey =
  store-alpha`, `SellerPartyId = null`, lines mapped to real `SellerOrder` lines; exactly **one** 219 read,
  **two** 216 quotes, one assignment read, one catalog read, one reservation and one conversion.
- **Strict Geography fixture**: `ListCountriesAsync` answers only the priced country (and can be flipped to a
  different id for the negative case); `ResolveLegacyAsync` returns `Unresolved` for a null/blank/nonexistent
  code or a name other than the stored province/city — it never fabricates a resolution for arbitrary arguments;
  the actual non-blank code `"IR"` is asserted, and the other Geography members throw to prove they are unused.
- **Replay really mutates the pointer**: the test moves the 219 pointer to another revision/version **before** the
  second authorised submit and asserts the original checkout + historical evidence are returned with no extra
  pointer/quote call and one reservation.

## D — Bounded concurrency proof on the same Order target

1. `A_selection_advanced_before_the_root_lock_is_an_exact_conflict_with_zero_reservation`: an independent
   context B opens a transaction that updates the same `order.cart_shipping_drafts` row
   (`selection_revision = 2`) **uncommitted**; submit A blocks on the real row lock (proved by a
   `pg_stat_activity` waiter probe, ≤10 s, no assumed sleep), B commits, A fails with the exact
   `storefront.captured_selection.conflict` and zero reservation/conversion/checkout/group rows.
2. `A_held_root_lock_blocks_the_real_selection_writer_and_the_order_evidence_survives`: A takes the accepted
   Order root lock inside the submit transaction and pauses through the *existing* test commit barrier; the
   **actual 232 selection adapter** (`StorefrontShippingDraftStore.ReplaceCapturedSelectionAsync`) in an
   independent context at the same expected revision is proven blocked on that row by the same probe; the lock is
   released once, A persists, B advances the revision to 2, and the persisted order groups/evidence (revision 2,
   both dates) are unchanged, with one reservation.

Both cases run once, bounded by 10–30 s waits, independent `DbContext`/transactions (no accidental enlistment into
A's `TransactionScope`); the Shipping pointer read remains a point-in-time Contracts observation only — no
serializability claim.

## Executed proof

One task-owned disposable `postgres:17-alpine` container `/tb231` (`40c0f00e…`) at `127.0.0.1:15433`,
database `order_tb231`, role `tb231`, PostgreSQL 17.11, identity asserted before the normal Order migration chain
(`saved_address_id` + `selection_revision` present), removed afterwards with `15433` verified closed.

- `CapturedFinalSubmitIntegrationTests` — **25/25 passed** (positive two-seller same-binding 99/198, shared-offer
  line control, owned unchanged saved address with exactly one address-book read, **16** parameterised early
  refusals with their exact owning codes — gate, owner, pointer, amount, date, root revision, format version,
  coupon, address missing/foreign/edited, country, command address, inactive service, inactive option, missing
  authority — shared-offer positivity, changed-pointer replay, barrier rollback with the same exception instance,
  server provenance through preparation → captured draft, module-scope resolution fact, and the two concurrency
  cases).
- All `StorefrontCaptured*` classes (writer/storage/transaction/preview/guard/composition) ran **once** with it:
  **68/68 passed**; the selection writer/storage/transaction subset alone: **32/32 passed**
  (includes the new `The_server_source_saved_address_id_is_persisted_on_the_captured_draft`).
- `Tooba.Order.Tests` build: **0 errors** (7 pre-existing warnings). No 124/36/28/31/50/119/188/194/285/310/312 /
  full-Order / Host / solution / guard sweep was replayed.

## Corrections (honest)

More than the single allowed correction round was spent: (1) the first fixture string for
`RequestedDeliveryTimeWindow` exceeded `varchar(32)` (PostgreSQL 22001) and was reduced to `09:00-17:00`;
(2) two test files were missing `using` directives for the storefront models/contracts and did not compile;
(3) the replay assertion had to match the idempotent return (no re-quote/re-pointer). No production behaviour was
changed to satisfy a test, and no test was weakened to fit behaviour.

## NOT delivered / still open (prominent)

- Public `CommitAsync` and `ProjectPaymentDraftAsync` stay `not_ready`; `CheckoutEnabled`/preview/selection stay
  checked in **OFF**. Nothing here is checkout-ready or activatable for users, and there is no Marketplace proof
  (SingleStore only).
- Coupon read/selection/commit parity is **not** implemented (a non-blank coupon still refuses
  `...coupon_unavailable`).
- The A/B concurrency facts are bounded, one-run proofs on a sequential/short lock window — not a load or
  race-iteration campaign, and not a distributed-serializability claim; service area remains unsupported.
- Both acceptance and the Shipping pointer read are model-bound: no HTTP/live storefront exercise, no
  StartPayment path run, no payment provider.
- 231/232 migrations stay WRITTEN/shared **NOT APPLIED**; the stale EF snapshot gap is retained; no baseline edit.

## Preserved

The user's visible review window, live Host/FE, `previewON/count7`, runtime data, cart, address book, draft, rates,
week, pointer and assignments were not touched (never enumerated/read/automated/reloaded/closed). Retained
unfixed: 230-R1 empty-key refresh/retry + in-flight new-key wait, cross-owner label map/decoder limits, Offer API
proof gaps, 227 mobile empty-note residual, 225-R1 lost unsaved user LOCAL edits. TB185 paused/default-OFF/
PendingAllocation.

## Files

- `.../Tooba.Order.Application/Storefront/Services/StorefrontAddressCanonical.cs` (new, pure)
- `.../Tooba.Order.Application/Storefront/Services/StorefrontCheckoutService.cs` (server-only provenance)
- `.../Tooba.Order.Application/Storefront/Shipping/CapturedPreview/StorefrontCapturedSelectionWriter.cs`
- `.../Tooba.Order.Application/Storefront/Shipping/CapturedGroupProjection/StorefrontCapturedPublicationBinding.cs`
- `.../Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs` (+ `.CapturedAcceptance.cs`)
- `.../Tooba.Order.Tests/Checkout/CapturedFinalSubmitIntegrationTests.cs`
- `.../Tooba.Order.Tests/Storefront/StorefrontCapturedSelectionWriterTests.cs`
- `.../Tooba.Order.Tests/Checkout/StorefrontCapturedGroupProjectionSupport.cs`

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
