# RESULT — TB-SHIPPING-CAPTURED-PUBLIC-COMMIT-EDGE-234

Pipeline: BRIDGE-WAKE-V1 · Worker: tooba-worker-01 · Base: `3f4ee29e6` (the issue commit; working tree was
already on it, no fast-forward needed) · Parent: TB-SHIPPING-CAPTURED-ADDRESS-BINDING-AND-PROOF-233-R3.
Status: **EXECUTED**, NOT accepted by the Architect, **NO user Visual ACCEPT**, no checkout activation, no
frontend/HMR/browser/Host-FE/runtime change, no live storefront/API/business write, no shared migration apply,
no schema/baseline edit.

Elapsed ≈28 min wall-clock from the Bridge claim (≈`18:03Z`) to this report vs HARD35 — inside the limit, but
the requested source22/proof13 split was **not** independently timed, so no split claim is made. The
one-correction budget was **exceeded** (see §G) — this is not a process PASS.

Core 231/232/233-R3 was treated as accepted-within-recorded-scope and was **not** reopened: no new quote
engine, no second calendar, no new price/window authority, no write inside the read path.

---

## A — ONE coherent final gate (default OFF, unchanged in configuration)

`StorefrontCapturedPreviewPlanner.IsCapturedCommitEnabled` is the single final-commit gate and requires all
four conditions at once: `Enabled` && `SelectionEnabled` && `CheckoutEnabled` && `HasValidAvailableDateCount`.
`SelectionEnabled` alone never authorizes a commit, and every checked-in default stays OFF
(`StorefrontCapturedPreviewOptions` is untouched, `AppSettings`/`appsettings` not edited).

`StorefrontShippingService.CommitAsync` resolves that gate **before any read**:

* incoherent/OFF gate ⇒ `EnsureCapturedPreviewIsNotCheckoutReady()` ⇒ `storefront.captured_preview.not_ready`
  (same stable code as before, and a stored capture can never fall through to the legacy branch when the
  flags are OFF);
* coherent gate + a stored draft whose groups carry `AcceptedEvidence` ⇒ the new captured branch;
* coherent preview but a draft **without** stored evidence ⇒ the same `not_ready` refusal (never legacy);
* nowhere OFF, no stored evidence ⇒ the legacy path exactly as before (see §F proof 5).

## B — Captured public commit: no legacy replay, no second preview

New `StorefrontCheckoutService.SubmitCapturedSelectionAsync(cartId, guestSecret, expectedCartVersion,
idempotencyKey, draft, couponCode, expectedSelectionRevision, ct)` builds the command from the **stored**
root/groups only (`StorefrontShippingService.BuildOrderGroupPlans(draft)`, now `internal static`, still the
single mapping used by the legacy preview/draft paths):

* it does **not** call `PrepareShippingAsync`, does **not** read the live cart through the presentation
  gateway, does **not** call the legacy `SaveSelectionAsync`, and produces **no** 217/218 preview;
* placed-by actor/access come from the existing `IOrderStorefrontActor` boundary
  (`ResolvePlacementActor(usingSaved)`), guest access from `BuildCartAccess`;
* a nonblank coupon is refused with `storefront.captured_selection.coupon_unavailable` **before**
  delegation (no silent discount removal/replacement);
* the result is mapped from the **persisted** Order snapshot (`MapCapturedPage`), keeping the original
  per-group ids, accepted payable/currency/dates and 100 ns ticks — no post-commit catalog/Shipping/address
  read can turn a durable success into an external failure.

Delegation is to the **existing** `ICheckoutDirectory.SubmitAsync`, i.e. the accepted
`CheckoutProcessManager.SubmitAsync`: idempotency/access lookup first (`FindExistingCheckoutAsync` +
`EnsureCheckoutAccess` + `ReconcileCartConversionAsync`), then the real cart/version check, the ONE 216 quote
per group, and `AcceptCapturedShippingCoreAsync` **inside** the established submit transaction before the
first reservation. Nothing about that order was changed.

**Replay is public-facade replay, not direct-directory replay:** a repeated key returns the existing order
before `LoadActiveCartAsync` is ever reached, so a converted cart, a removed/moved address or a moved pointer
cannot block the return. `ReconcileCartConversionAsync` already returns immediately for a converted/absent
cart and does not reserve again.

## C — Payment draft: one real validation pass, zero writes

New port member `ICheckoutDirectory.ProjectCapturedPaymentDraftAsync(SubmitCheckoutCommand, ct)` returning
`CapturedPaymentDraftSnapshot(Quote, Groups)`, implemented in
`CheckoutDirectory.CapturedPaymentDraft.cs`:

1. `LoadPreviewCartAsync` (the existing guard/access/status/empty boundary) + explicit
   `cart.Version == command.ExpectedCartVersion`;
2. exactly ONE whole-cart `QuoteSellerOrdersAsync(..., requireReservation: false)`;
3. the **same** acceptance core used by the final submit, called with
   `requireExplicitSelectionRevision: false` — no explicit-revision requirement and **no**
   `LockStoredCapturedRootAsync`, so the read path takes no lock and writes nothing;
4. `ComposePreviewSnapshot` (extracted from the previous inline preview body, one implementation, both
   callers) builds the snapshot.

Zero `SaveChanges`, zero reservation, zero cart conversion, zero order/checkout rows, zero abuse quota.
`StorefrontShippingService.ProjectPaymentDraftAsync` routes a captured draft here behind
`IsCapturedCommitEnabled` and keeps the old `not_ready` refusal for an incoherent/OFF gate and for a
non-captured draft while preview is ON. The 217 goods reader (`ICheckoutShippingGroupGoodsFactsReader`) was
**not** modified, so the captured READ is not taught final-acceptance logic and cannot recurse.

Response shape reuses the existing `StorefrontShippingGroupView` through the additive optional
`groupViews` parameter of `MapPage`; the client contract is unchanged.

## D — Real StartPayment token forwarding (string, lossless)

`StorefrontStartPaymentBody.ExpectedSelectionRevision` (string) → `StorefrontStartPaymentRequest` →
`StorefrontStartPaymentComposer` → `StorefrontShippingService.CommitAsync` → captured branch →
`SubmitCheckoutCommand.ExpectedSelectionRevision` → parsed only by the accepted Directory
(`TryParseSelectionRevision`, strict digits, `"0"` lawful). `4294967296` survives byte-for-byte; a JSON
**number** for that field fails deserialization; `null`, whitespace-prefixed, `abc`, `-1`, `1.5` are refused
with `storefront.captured_selection.intent_invalid` before any reservation. Legacy requests (no captured
draft, no evidence) are untouched.

## E — Focused proof (one disposable target, real Order persistence)

One task-owned disposable `postgres:17-alpine` named `tb234` at `127.0.0.1:15433/order_tb231/tb231`
(identity asserted by `TOOBA_TB231_ORDER_POSTGRES` before DDL, normal Order migration chain, container
removed and `15433` verified closed afterwards).

New class `StorefrontPublicCommitEdgeTests` — **5/5 passed**:

1. `Public_commit_of_two_captured_groups_persists_the_order_without_legacy_rewrite` — two sellers on ONE
   Store binding through the real facade: persisted checkout + two shipping groups carrying their own
   payable (125/75), own receiving date, own fractional 100 ns open/close ticks; exactly 1 reservation,
   1 conversion, 2 sixteen-quotes; stored root `SelectionRevision == 1` and both stored groups intact
   (no legacy rewrite).
2. `Public_replay_after_pointer_change_and_converted_cart_returns_the_original_order` — same key after the
   pointer advanced, the saved address was dropped and the cart became Converted returns the SAME
   `CheckoutId` with **zero** additional quote/pointer/address/reservation/conversion calls and unchanged
   order/group row counts; a foreign actor is denied (plain `InvalidOperationException`, access boundary)
   with no new rows.
3. `Captured_payment_draft_validates_read_only_and_refuses_stale_or_closed_states` — a real saved address is
   read with the real actor; the snapshot carries both validated groups (125/75) with 2 quotes and **zero**
   checkout/order-group rows, zero reservation/conversion and an unchanged root revision; a moved pointer ⇒
   `storefront.captured_selection.conflict`; a dropped address ⇒
   `storefront.captured_selection.destination_unverified`; the incoherent gate ⇒ `not_ready` for both the
   draft read and the commit.
4. `Start_payment_body_token_reaches_the_accepted_commit_without_starting_payment` — real
   `StorefrontStartPaymentBody` JSON → request → composer: five malformed/missing tokens refused with
   `intent_invalid` and zero orders/reservations; then the root revision is set to `4294967296` in SQL and
   the same path commits the real order (2 groups, reservation 1) with the commit key prefixed by
   `start-payment:commit:`, `PaymentInitiationPort` never called, outcome honestly `HoldPreparationFailed`
   (controlled hold preparation), root revision still `4294967296`; a numeric JSON token throws.
5. `Legacy_off_route_is_unchanged_and_captured_gate_never_reopens_it` — with all capture flags OFF a legacy
   draft (no evidence) runs the **legacy** validation and stops on its own rule
   (`shipping.first_name_required`) with zero pointer/address/reservation calls; with preview ON but no
   stored evidence the same draft is refused `not_ready` and no order is written.

Directly affected existing classes re-ran ONCE with it on the same target: **200/200 passed**
(`CapturedFinalSubmitIntegrationTests`, `StorefrontCaptured*` writer/storage/transaction/selection classes,
`ShippingGroupGoodsFacts*`, `ShippingGroupIdentity*`, `ShippingGroupAcceptedEvidence*`,
`StorefrontPaymentDraft`, `StorefrontStartPayment{Endpoint,Composer}`). Builds: `Tooba.Order.Application`,
`Tooba.Order.Infrastructure`, `Tooba.Order.Endpoints`, `Tooba.Order.Tests` — **0 errors** each. No broad
suite / 25 / 68 / 124 / 36 / 28 / 31 / 50 / 119 / 188 / 194 / 285 / 310 / 312 / full-Order / Host /
solution / guard replay was run.

## F — Honest limits

* **No HTTP/live proof.** The whole edge was exercised in-process with the real services and the real
  Order schema; no listener, no browser, no live cart, no request was replayed over the wire. Transport
  reachability of the two existing endpoints is unchanged and model-bound only.
* **Controlled Contracts.** Inventory reservation/conversion, Pricing/Promotion/Tax, 216 quote, 219
  pointer, 228 assignment, Fulfillment catalog, Geography, AddressBook, hold preparation and Payment are
  the same labelled fakes the accepted 233-R3 class uses. Only the Order schema, the real quote composition,
  the accepted acceptance and Order persistence are real. No fake successful `CheckoutDirectory`.
* **No payment.** The controlled hold preparation stops after the real Order commit
  (`HoldPreparationFailed`); the payment initiation port was never reached and no PSP/network call exists in
  the fixtures. `Initiated` is not claimed.
* **Scope.** SingleStore + Store owner + Manual + no coupon only. Marketplace/Seller, carrier/weight,
  coupon parity, history/repricing endpoint and every activation/readiness statement stay out of scope.
* **Legacy control** proves branch selection and the legacy rule's own refusal with zero writes, not a
  successful legacy order end-to-end (that path was deliberately not re-proved here).
* **Two PRE-EXISTING red guards remain** and are disclosed, not repaired:
  `OrderStorefrontArchitectureGuardTests.Application_storefront_uses_contracts_only_no_foreign_application`
  (`DateTimeOffset.UtcNow` inside `StorefrontCapturedPreviewPlanner`) and
  `StorefrontShippingPackagingChargeBoundaryTests.Packaging_is_confined_to_the_default_off_operational_shadow`
  (the expected allow-list predates the 231/232 capture files). Both were confirmed red **at the base
  commit** `3f4ee29e6` (`git show` of the base planner still contains six `DateTimeOffset.UtcNow`
  occurrences; `git grep Packaging HEAD` still lists the same seven non-shadow production files), so this
  diff neither introduced nor widened them.
* **No live/activation/acceptance claim:** no checkout activation, no StartPayment certification, no
  payment-provider work, no Visual ACCEPT.

## G — Corrections beyond the single allowed round (recorded)

1. Six existing test doubles had to implement the new `ICheckoutDirectory` member (test-only, throw
   `NotSupportedException`).
2. `FakeCartGateway` now models an absent cart by returning `null` instead of throwing.
3. Shared 233-R3 seed members (`CartId`, seller/offer/line ids, draft group ids, `SavedAddressId`,
   `ReadyAsync`, and a new evidence-free `SeedLegacyGroupDraftAsync` helper) were made `internal` for reuse.
4. The edge test needed web JSON options for the body and a saved-address seed for the address-book negative
   (the earlier attempt used an address-less draft, so the address book was legitimately not consulted).
5. The legacy-OFF control (§F proof 5) was added after the first proof run to satisfy TASK §5.

## H — Preserved

No migration, schema, baseline or shared-history edit; 231/232 migrations stay WRITTEN/shared-NOT-APPLIED and
the stale EF snapshot gap is untouched. No frontend/HMR/browser/live cart/address/assignment/draft/pointer/
payment write; the Host/FE runtime was not rebuilt or restarted. The user's visible review window
(`previewON`/`count7`, `USER` unsaved work) was **never** enumerated, read, automated, reloaded or closed.
Known 230 empty-key/in-flight, cross-owner label/decoder, Offer API proof, 227 mobile residual and the
unrecovered 225-R1 local edits stay retained. TB185 paused/default-OFF/PendingAllocation preserved.

## NEXT (Architect)

Review this edge proof. Remaining parity work, explicitly **not** delivered here: coupon read/selection/
commit parity, Marketplace/Seller responsibility paths, an Order-only historical read, and the two
pre-existing red guards above. Captured checkout stays non-activated until that review.

STOP_FOR_ARCHITECT; Automatic-Next-Task NONE.
