# RESULT — TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1
Parent-Task: TB-SHIPPING-FULL-OWNER-FLOW-AND-PREVIEW-CONTRACT-235
Bridge-Task-Row: 5daf73f2-b83d-45e5-82ba-e2ac37383c62
Channel: tooba-main
Worker-Id: tooba-worker-01
Status: PARTIAL (all three requested slices implemented and exercised; the measured-RED control and the task's
test-seam wording are the two shortfalls recorded below)
Stop-State: STOP_FOR_ARCHITECT
Base-SHA: 1662e6f92a3b0e20f44f4b24b0b39d8856b85d60 (HEAD == origin/main at claim; the issuance SHA of the
corrected artifact resolved to this same commit — the artifact itself was transported by Bridge, so no separate
repository commit contains only the task text)

## A — per-line assignment validation (two confirmed defects, closed at the choke point)

`CheckoutDirectory.CapturedAcceptance.ResolveGroupOwner` previously did two things wrong:

1. it only asked `assignments.TryGetValue(offerId, …)` — a row whose **dictionary key** was the line's offer but
   whose **payload** `OfferId` belonged to another offer was accepted as "this line's assignment";
2. it carried `OfferFulfillmentResponsibility? responsibility = null` and filled it with `??=`, so a *null*
   responsibility on the first line was silently repaired by a later line's value (and a group could resolve an
   owner from a partially known assignment).

Now, per cart line, before any reservation/order/conversion:

- `assignment.OfferId == offerId` (exact offer of that cart line) **and** `assignment.SellerPartyId == group.SellerPartyId`;
  otherwise `storefront.captured_selection.unavailable`;
- `assignment.Responsibility` must be present **and** `Enum.IsDefined` on it (unknown/undefined never resolves an owner);
- the assigned type must be non-empty (`Guid.Empty` is not a type) and exactly the stored evidence type, otherwise
  `storefront.captured_selection.conflict` (an empty/absent type can never "match" a stored type);
- initialization is explicit (`initialized` flag): the first valid line *sets* responsibility+type, every later line
  must equal them; a null-first / valid-second and valid-first / null-second ordering both refuse;
- the resolved full owner still comes from the shared `StorefrontCapturedOwnerPolicy` and is compared with the stored
  owner exactly (`IsSameAs`); a mismatched stored owner stays `conflict`.

No new fault code, no new policy, no owner rewrite: the existing `StorefrontCapturedSelectionReasons` codes are reused.

## B — one coherent payment preview root, one presentation-cart load

`StorefrontShippingService.ProjectPaymentDraftAsync` already loaded and authorized `CartPage`; it then called the
captured facade with only a `cartId`, so the facade performed a **second** `RequireCartAsync`. It now passes the same
authorized `CartPage` into `StorefrontCheckoutService.ProjectCapturedPaymentDraftAsync`, which no longer loads the cart
(it still validates `draft.CartId == cart.CartId`, and `CheckoutDirectory` keeps its own independent authoritative
cart/version read — the public caller cannot supply a cart object).

`ComposePreviewSnapshot` used to override **only** `ShippingAmount` and left `MinimumDeliveryDate`,
`RequestedDeliveryDate`, `ShippingMethodCode/Label` and the time window from the *stale command*. It now takes a typed
`CheckoutPreviewShippingOverride` (`Application/Checkout/Contracts/CheckoutSnapshots.cs`) built from the **accepted**
groups of this same acceptance:

- `ShippingAmount` = Σ accepted group `ShippingCharge`;
- `MinimumDeliveryDate` = the earliest accepted group minimum (a real value, not a fabricated cross-group promise);
- requested date / method code / method label / time window are propagated **only** when every accepted group says the
  same thing; otherwise they stay explicitly empty — no invented aggregate date or window across independent groups;
- no accepted group ⇒ `null` override ⇒ the legacy command value is preserved.

`CapturedPaymentDraft` derives that override from `acceptance.Groups`, so money, dates and method on the page all come
from the one root the same call accepted, and `SelectionRevision`/`CartVersion` keep coming from that acceptance.

Proven by the vertical fact below: the root is advanced (revision 1 → 2, freight +40 per owner) between the caller's
outer read and the internal acceptance, and the response is one coherent set at revision `2` (money **and** dates),
with zero reservation/order/conversion/writes.

## C — real multi-owner vertical (new `StorefrontFullOwnerVerticalTests`, 6/6)

Target: ONE task-owned disposable `postgres:17-alpine`, container `tb235r1-order-pg`,
`127.0.0.1:15433 / order_tb231 / tb231`. Identity is asserted by
`OrderEvidenceRelationalTestTarget.FromEnvironment()` (exact host/port/database/user, no multi-host, no 5432) **before**
any DDL; the normal Order migration chain then runs. Container removed at the end of this task and port 15433 verified
closed.

Owner fixtures (three real partitions of ONE cart, one catalog binding `post:standard`, one shared real type):

| partition | owner (Domain) | capture snapshot | draft | revision / selection version | rate row | freight / packaging / payable | receiving date | open / close 100ns ticks |
|---|---|---|---|---|---|---|---|---|
| Marketplace Platform | `Marketplace` (no tenant, no seller key) | `01a17d20-…a1` | `01a17d21-…a1` | 5 / 2 | `01a17d22-…a1` | 120 / 5 / 125 | 2026-10-27 | 09:00+.1111111 / 17:00+.2222222 |
| Seller B | `Seller` = real `Offer.SellerPartyId` | `01a17d20-…b1` | `01a17d21-…b1` | 6 / 3 | `01a17d22-…b1` | 70 / 3 / 73 | 2026-10-28 | 10:00+.3333333 / 18:00+.4444444 |
| Seller C | `Seller` = real `Offer.SellerPartyId` | `01a17d20-…c1` | `01a17d21-…c1` | 7 / 4 | `01a17d22-…c1` | 90 / 4 / 94 | 2026-10-29 | 11:00+.5555555 / 19:00+.6666666 |

Cart: three lines, three distinct sellers (Marketplace/Platform responsibility on the first, `Seller` responsibility on
the other two), amounts 100/200/300, version 7, real buyer. The pointer/quote fixtures are keyed by the **full**
requested owner + binding (`OwnerKind`, `OwnerSellerPartyId`, service, option) and throw on an unknown owner — they never
answer the same capture for every owner; the quote fixture is keyed by the stored draft id.

Facts asserted:

1. **read → write**: real `StorefrontShippingService.SaveSelectionAsync` (real planner → real 232 writer/store) persists a
   revision-1 root with **3** groups; exactly **3** pointer reads (one per distinct full owner+binding) and **3** quotes
   (one per selected group); each stored group's evidence carries **its own** owner/row/money/date/ticks/currency.
2. **public payment preview**: 3 further pointer/quote reads, `CheckoutId` null, 3 groups, `SelectionRevision == "1"`,
   `ShippingAmount == 292`, each label its own group's label, own payable amount, own service/option, own
   `SelectedDeliveryDate`, `OrderLineIds` null and one real `CartLineIds` entry; three distinct labels (no cross-owner
   collapse); zero checkouts, zero reservations.
3. **public commit**: 3 pointer/quote reads, 1 reservation, 1 conversion; persisted `order.order_shipping_groups` = 3
   with matching owner kind/seller/payable/row/date/ticks; a `Seller` owner's group seller equals the real selling party
   (the Marketplace group is never rewritten as its seller, and no seller is rounded into `Store`).
4. **swapped owner responses**: Seller B ↔ Seller C pointer answers swapped ⇒ `storefront.captured_selection.conflict`
   with zero reservation/conversion/checkout/order-group and the stored root still revision 1 (the stored evidence is not
   its own validator).
5. **foreign assignment payload** (right dictionary key, payload `OfferId` of another offer) ⇒
   `storefront.captured_selection.unavailable` before any reservation/order.
6. **null responsibility** on the first line repaired by a valid second line, and the reversed order ⇒ the same
   `unavailable` with zero writes.
7. **coherent accepted root**: after the real writer advances the root to revision 2 (+40 per owner), the public preview
   reports revision `"2"`, `ShippingAmount == 292 + 120` and each group's own new money **and** its own date, with zero
   checkouts/order groups/reservations/conversions and the stored root still 2.

Controlled-vs-real boundary: Pricing/Promotion/Tax, 216 quote, 219 pointer, 228 assignment, Fulfillment catalog,
Geography, AddressBook and reservation/conversion are **labelled controlled Contracts**; the Order schema, migrations,
quote composition, captured acceptance, selection writer/store, public service wiring and persistence are **real**. No
accepted evidence was manually seeded for the vertical: the positive path runs the real writer and the real acceptance.
No HTTP, no live shopper, no PSP/payment, no carrier/Marketplace network call; no test-only seam was added to production
code (the root advance in fact 7 is performed by the real selection writer between the caller's read and the inner
acceptance, not by an injected hook).

## Executed proof / commands (same isolated target, run once)

- `dotnet test … --filter "FullyQualifiedName~StorefrontFullOwnerVerticalTests"` → **6/6 passed**
  (4 facts + one 2-case theory), on `15433/order_tb231/tb231`.
- `dotnet test … --filter "FullyQualifiedName~Checkout|FullyQualifiedName~Storefront"` → **412 total, 410 passed, 2
  failed**, both failures PRE-EXISTING red guards that are red at the base commit and were not repaired or widened:
  `OrderStorefrontArchitectureGuardTests.Application_storefront_uses_contracts_only_no_foreign_application`
  (`DateTimeOffset.UtcNow` inside `StorefrontCapturedPreviewPlanner.cs`) and
  `StorefrontShippingPackagingChargeBoundaryTests.Packaging_is_confined_to_the_default_off_operational_shadow`
  (packaging allow-list).
- Affected Order projects compile with **0 errors** (`Tooba.Order.Application`, `.Infrastructure`, `.Endpoints`,
  `.Tests` built by the test run).
- Container `tb235r1-order-pg` removed afterwards; `127.0.0.1:15433` verified closed.

## RED control — honest status

`Assignment_payload_for_another_offer_is_refused_per_line` and the two `Missing_responsibility_…` cases were written as
RED→GREEN regressions, but the RED step was **not measured** against the pre-fix source in this time box (that would
have required reverting the acceptance partial or a base worktree build). Source-derived expectation, disclosed as such:
on the pre-fix code the payload case reaches the composition and fails as `conflict` (owner mismatch) instead of
`unavailable`, and the null-first case resolves a `Seller` owner from the later line and then fails as `conflict`
instead of `unavailable`; the GREEN assertions on the fixed code are measured.

## Not delivered / limits

- No HTTP listener, live browser, Host/FE refresh, PSP/payment, carrier or Marketplace-network exercise. Owner
  correctness is proven through the real Order-owned code paths with controlled Contracts, not live traffic.
- Coupon read/selection/final parity is still **not** implemented; nonblank coupon still refuses
  (`storefront.captured_selection.coupon_unavailable`) and remains a required next delivery slice, not a final exclusion.
- No test-only seam was added for "replace the selection between outer facade read and fresh inner acceptance"; the
  equivalent fact uses the real writer advance (disclosed above).
- No physical per-tenant DB isolation claim; SingleStore exact-tenant control is unchanged and still passing.
- Frontend/existing-UI connection and the complete desktop/mobile practical trial remain required delivery work; nothing
  is activated, no checkout/payment gate was enabled, no Visual ACCEPT.
- No schema/migration/baseline edit; 231/232 migrations stay `shared NOT APPLIED` with the stale EF snapshot gap.
  `previewON/count7` and the user's unsaved review window were untouched (never enumerated/read/automated/reloaded).

## Timing / process

Bridge claim `2026-10-27T19:23:40Z`, final proof `19:46Z` → ≈23 minutes wall clock, inside HARD35, but the
one-correction budget was **exceeded** (several in-module corrections in the new test harness: single-seller grouping,
marketplace group-key line seller, real Offer registration for the third line, a missing `using`). Recorded as
NONCOMPLIANT on corrections, not a process PASS.

## Changed paths

- `src/backend/Modules/Order/Tooba.Order.Application/Checkout/Contracts/CheckoutSnapshots.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontCheckoutService.cs`
- `src/backend/Modules/Order/Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.CapturedAcceptance.cs`
- `src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.CapturedPaymentDraft.cs`
- `src/backend/Modules/Order/Tooba.Order.Tests/Checkout/CapturedFinalSubmitIntegrationTests.cs` (fixture seams only)
- `src/backend/Modules/Order/Tooba.Order.Tests/Checkout/StorefrontCapturedPreviewContractTests.cs` (authorized-cart call shape)
- `src/backend/Modules/Order/Tooba.Order.Tests/Checkout/StorefrontFullOwnerVerticalTests.cs` (new)
- evidence + recovery docs (this folder, `235/RESULT.md` corrections, three recovery TOPs, `docs/ai/tasks/` artifact)

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE. Coupons, existing frontend connection and the complete
practical desktop/mobile trial remain required before any sellable/ready claim.

## G — issuance/merge reconciliation (recorded, no source change)

The archived issuance reached main as 5baee32b (corrected 235-R1 envelope: routing header, BEGIN_TOOBA_TASK
block and RESULT CONTRACT). It arrived AFTER this task's implementation commit cb3d054bb, so the base received the
correction through the merge 2c452586 rather than being present when the work started. The three recovery TOPs
(AGENTS.md, docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md,
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md) were reconciled by keeping the executed 235-R1
header CURRENT, demoting the corrected-envelope header and every older header, and preserving the full delivery
scope + earlier history untouched. No source, test, gate, schema or runtime file was changed by the reconciliation;
235-R1/TASK.md was taken verbatim from the corrected issuance.

Expected-Base-SHA for the Architect review = 5baee32b (correction) with implementation commit cb3d054bb and
merge 2c452586 on top; Code-SHA / Evidence-SHA = the pushed main tip containing all three.
