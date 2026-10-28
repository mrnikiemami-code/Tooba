PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236
Parent-Task: TB-SHIPPING-FULL-OWNER-VALIDATION-AND-PREVIEW-COHERENCE-235-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Program: Tooba Shipping — shared coupon context on the existing owner vertical
Mode: IMPLEMENT_COMMON_COUPON_READ_SELECTION_PREVIEW_FINAL_CONTEXT
Bridge-Task-Row: 9a393fb5-c1b4-4f3d-b82c-d145a433ac8d
Status: PARTIAL
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: 607e12c17df7440d2fb9e29418d026a6491e6d05
Base-SHA: 607e12c17df7440d2fb9e29418d026a6491e6d05
Code-SHA: 36c44a35079c931d3a26cd780a8d20ec29ff5486
Evidence-SHA: the pushed evidence commit containing this file (its exact full SHA is carried in the Bridge result envelope)
automaticNextImplementationTask: NONE

# 236 — ONE coupon intent from read to selection, payment preview and final commit

Status: **EXECUTED**, NOT accepted, no user Visual ACCEPT, no checkout/gate activation. Honest self-assessment
is PARTIAL because the provider-error / caller-cancellation half of §5.3 is disclosed as NOT re-proven in this
slice and the one-correction budget was exceeded.

## A — Root cause (actual, not survey)

Five concrete asymmetries existed at `607e12c17`:

1. The captured **read** had no coupon input at all: `StorefrontShippingProjectionRequest`/`ProjectAsync` and
   `StorefrontCapturedPreviewPlanner.{BuildAsync,ResolveAsync}` carried no intent, and the planner hardcoded
   `CouponCode: null` into the `SubmitCheckoutCommand` used for the 217/218 goods quote. A coupon could therefore
   never be seen by the read, so no confirmed goods context could exist.
2. The **selection writer** refused any nonblank coupon (`storefront.captured_selection.coupon_unavailable`)
   before any quote — a blanket exclusion, not a validation.
3. The **public payment preview** (`StorefrontShippingService.ProjectPaymentDraftAsync`) and the **public commit**
   (`SubmitCapturedSelectionAsync`) each threw the same blanket `coupon_unavailable` before delegating.
4. The **final acceptance** (`CheckoutDirectory.AcceptCapturedShippingCoreAsync`) threw the same code, i.e. the
   captured order path could never be committed with a coupon even if read and selection had agreed.
5. Two 235-R1 residuals were still open in these same paths: `ComposePreviewSnapshot` used
   `shippingOverride?.RequestedDeliveryDate ?? command.RequestedDeliveryDate`, so an *intentional* null
   ("groups disagree") silently resurrected the stale command date; and `ResolveGroupOwner` accepted an
   assignment whose `ShippingTypeId` was `Guid.Empty` **and** an evidence type that was also null, so a line with
   no assigned type could skip the activation check.

## B — Implementation (existing files, existing authorities)

**Promotion remains the only discount authority.** No coupon engine, no matching policy, no eligibility rule, no
arithmetic was added in Order. The coupon travels as a *confirmation token*: `StorefrontCouponIntent` exposes only
`Normalize` (whitespace/empty ⇒ `null`, otherwise the text `Trim()`-ed, no case-fold, no length cut, no inner-space
rewrite) and `Same` (`Ordinal`). Code matching/length policy and rejection/applied decisions stay inside the
existing 217 Promotion path (`ICheckoutPromotionPort` → `CheckoutPromotionAdapter` → `PromotionDecisionEngine`),
and pricing/tax stay in Pricing/Tax. `Order` owns only: "the same intent reached this quote", "the accepted
post-discount+tax goods of each group is what this intent produced", and "the stored confirmation still matches".

Reachability was implemented through the **actual** paths (not compat overloads only):

- `StorefrontShippingProjectionRequest` gained a trailing optional `CouponCode`;
  `StorefrontOrderEndpoints.ProjectShippingAsync` forwards `body.CouponCode` into
  `ProjectStorefrontShippingQuery` (new trailing optional field), whose handler passes it into
  `StorefrontShippingService.ProjectAsync(..., couponCode)` → `StorefrontCapturedPreviewPlanner.BuildAsync/ResolveAsync`
  → `SubmitCheckoutCommand.CouponCode = StorefrontCouponIntent.Normalize(couponCode)`.
  The GET payment-draft route already forwarded `?couponCode=`; it now reaches a path that accepts it.
- `StorefrontCapturedSelectionWriter` no longer refuses a nonblank coupon: it normalizes the intent, passes it into
  the SAME single `ResolveAsync` used by the read (so selection re-quotes with the identical intent), asserts
  `AssertEcho` **and** a new `AssertGoodsEcho` (client `ExpectedGoodsAmount` is only an echo; unparsable or
  mismatched ⇒ existing `storefront.captured_selection.conflict`), persists the intent on the root inside 232's
  SAME atomic replacement/CAS/rollback boundary, and confirms each group's goods amount on that group.
- `StorefrontShippingService.ProjectPaymentDraftAsync` and `SubmitCapturedSelectionAsync` lost the blanket refusal
  and now forward the lossless intent to `ICheckoutDirectory.ProjectCapturedPaymentDraftAsync` /
  `SubmitCapturedSelectionAsync` (no new quote coordinator).
- `CheckoutDirectory.AcceptCapturedShippingCoreAsync` replaced the blanket refusal with an exact comparison:
  `StorefrontCouponIntent.Same(currentIntent, stored.SelectionCouponCode)` else `storefront.captured_selection.conflict`
  (A→B, cleared, or a different code ⇒ stale selection, new confirmation required, never a silent rewrite), and
  every stored group's `ConfirmedGoodsAmount` must equal the goods this submit's own quote just produced for the
  same `GroupKey` (missing confirmation, unrepresentable value or any divergence ⇒ the same conflict, **before**
  any reservation/order/conversion). Final goods still come from THIS submit's already-quoted `SellerOrder` lines
  through the existing facts builder — no recursive 217/218, no second Pricing/Promotion/Tax pass.
- Residual (a): `ComposePreviewSnapshot` now branches on the **override object's presence**
  (`shippingOverride is null ? command.X : shippingOverride.X`) for method/label/minimum date/requested
  date/window, so an explicit null stays absent and old command dates never return; `ShippingAmount` keeps its
  existing `??` because a null amount there is not a meaningful "disagreement".
- Residual (b): `ResolveGroupOwner` requires `assignment.ShippingTypeId is Guid assignedTyped && assignedTyped != Guid.Empty
  && evidence.ShippingTypeId == assignedTyped` on **every** line — no null/empty type can skip activation.

**Schema (minimal, own Order, additive only):** `20261027230000_AddCapturedSelectionCouponContext` adds exactly
two nullable scalars: `order.cart_shipping_drafts.selection_coupon_code text` and
`order.cart_shipping_groups.confirmed_goods_amount numeric(18,2)`, each with one shape CHECK
(`ck_cart_shipping_drafts_selection_coupon_shape`: null, or already-trimmed and non-empty;
`ck_cart_shipping_groups_confirmed_goods_shape`: null, or `>= 0` and representable in `numeric(18,2)`), and the
matching `Down`. No new table, no JSON/JSONB, no per-line copy, no cross-module FK, no index, no backfill (legacy
rows stay NULL = "not confirmed"). Existing Order line discount/tax snapshots remain the financial history.
`ConfirmedGoodsAmount` is set through `CartShippingDraftGroup.ConfirmGoodsAmount`, which refuses an unrepresentable
amount with `shipping.group.goods.unrepresentable` before mutation. Coupon absence is explicitly `null` — never a
code, never a zero discount.

## C — Per-group quote-context ledger (worker execution)

All numbers are worker execution on ONE task-owned disposable `postgres:17-alpine` (`tb236`,
`127.0.0.1:15433/order_tb231`, user `tb231`, identity asserted before DDL by the existing helper, normal Order
migration chain including the new additive migration, container removed afterwards and `15433` verified closed).
Foreign Contracts are controlled fakes; Order DB/services are real. Three lawful owners on ONE binding
(Marketplace Platform + two real Seller parties) with distinct captures/labels/rows/dates.

- `One_coupon_reaches_read_selection_preview_and_commit_with_own_group_goods`: read with `" SAVE10 "` → Promotion
  sees exactly `SAVE10` on every request (no fold) and only OfferA is discounted ⇒ goods `99.00 / 220.00 / 330.00`
  (100−10+9% and untouched 200+10%, 300+10%, i.e. partial applicability is lawful and not rejected). Selection with
  the same intent + `ExpectedGoodsAmount` echoes ⇒ exactly 3 new quotes, persisted `selection_coupon_code = SAVE10`,
  `selection_revision = 1`, confirmed goods `99/220/330`. Public payment preview with the same intent ⇒ 3 pointer
  reads, shipping `649`, no reservation, no checkout. Clearing the intent (`null`) and switching to `SAVE20` both
  refuse `storefront.captured_selection.conflict` with **zero** reservation/order rows. Final public commit with
  `expectedSelectionRevision "1"` ⇒ exactly 3 new quotes, 1 reservation, 1 conversion, one checkout with 3 groups
  and 3 seller-order lines.
- `Changed_coupon_with_stale_goods_echo_is_refused_without_any_write`: `SAVE10` stored, then `SAVE20` with the old
  `99/220/330` echo ⇒ conflict with no mutation (`revision 1`, coupon still `SAVE10`, all groups still confirmed);
  a later selection with the new context (`88/220/330`) is lawful and advances to revision 2 with coupon `SAVE20`.
- `Same_code_with_changed_goods_refuses_preview_and_commit_before_any_reserve`: same code and same rate row, only
  the Promotion result changes (10→20 off) ⇒ preview and commit both `conflict` with 0 reservations, 0
  conversions, 0 checkouts, 0 order groups, revision still 1; restoring the Promotion result makes the preview
  valid again (healing, no rewrite).
- `Idempotent_replay_after_coupon_change_returns_the_original_order`: committed order replayed with a different
  coupon and any pointer state ⇒ the SAME checkout (and the same Order line ids), **0** extra quotes, **0** extra
  pointer reads, **0** extra reservations, still exactly one checkout.
- `Explicit_absence_in_the_accepted_root_never_resurrects_the_command_dates`: with an accepted override whose dates
  are deliberately null, the preview carries `null`/empty dates and the override amount; the legacy control (no
  override) still returns the command dates/window.
- `Payment_preview_reads_the_root_advanced_between_the_facade_and_the_core`: an **existing test port** wrapper
  (`IStorefrontShippingDraftStore` passed through `StorefrontFullOwnerVerticalTests.Vertical`) advances the real
  root to revision 2 (+40 per group, genuinely different dates) *after* the facade's draft read and *before* the
  core's fresh read — no production hook, no private-map seeding. The response describes the fresh root
  (`"2"`, 3 distinct dates, `Σ(payable+40)`), the derived override has null date/window because the groups
  disagree, and zero reservations/conversions/checkouts occur.
- `Storage_columns_are_additive_and_the_shape_checks_refuse_raw_values`: `information_schema` shows exactly the two
  new nullable columns and `0` json/jsonb columns in both tables; both CHECK names exist; a raw
  `UPDATE … selection_coupon_code = ' SAVE10 '` fails with SQLSTATE `23514` +
  `ck_cart_shipping_drafts_selection_coupon_shape`, and a raw `confirmed_goods_amount = -1` fails with `23514` +
  `ck_cart_shipping_groups_confirmed_goods_shape`, leaving the accepted selection untouched.
- `Blank_intent_is_absence_and_quotes_without_any_coupon`: whitespace intent reaches Promotion as `null` (never a
  code), the non-coupon base path applies (`99.00 / 198.00 / 330.00`, i.e. group 2 differs from the coupon read's
  `220.00`), proving absence is a real distinct path, not an empty string.
- `StorefrontShippingCouponEndpointTests` (5): the **actual endpoint delegate** `ProjectShippingAsync` forwards the
  body coupon **verbatim** — `" SAVE10 "`, `save10`, `4294967296`, `کد-تخفیف-۲۳۶` all arrive unchanged in
  `ProjectStorefrontShippingQuery` (no trim/fold at the HTTP boundary, decision belongs to Promotion), the identity
  gate runs exactly once, and the GET payment-draft delegate forwards `?couponCode=%20SAVE10%20` unchanged
  (`" SAVE10 "`).

Executed suites (worker execution, not root review): `StorefrontCapturedCouponParityTests` **8/8**,
`StorefrontShippingCouponEndpointTests` **5/5**, and the final directly-affected set
(`StorefrontCaptured*|CapturedFinalSubmitIntegrationTests|StorefrontFullOwnerVerticalTests|StorefrontPublicCommitEdgeTests|StorefrontShippingCouponEndpointTests|CapturedCheckoutGateTests|ShippingGroupAcceptedEvidence*`)
ONCE: **173/173**. `Tooba.Order.Tests` builds with 0 errors (the only build output is pre-existing analyzer
warnings). No 412/solution/Host/guard sweep was run.

## D — Preservation

No frontend source/HMR/browser/user-window access; no Host/FE rebuild, restart or runtime gate change; no shared
migration applied; no live buyer/cart/address/assignment/draft/publication/payment/business write. The USER VISIBLE
review window (USER unsaved work) was never enumerated, read, automated, reloaded or closed; local
previewON/count7 unchanged. All checked-in `Enabled`/`SelectionEnabled`/`CheckoutEnabled` stay `false`. 231/232
migrations remain WRITTEN/sharedNOTAPPLIED and the stale EF snapshot gap is retained (the new mapping was proven
against the real disposable DB, not by regenerating the baseline). TB185 stays paused/default-OFF/PendingAllocation.

## E — Changed paths

Source (Order only):
`Tooba.Order.Domain/Checkout/CartShippingDraft.cs`, `CartShippingDraftGroup.cs`;
`Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs`, **new** `StorefrontCouponIntent.cs`;
`Storefront/Services/StorefrontShippingService.cs`;
`Storefront/Shipping/CapturedPreview/StorefrontCapturedPreviewPlanner.cs`, `StorefrontCapturedSelectionWriter.cs`;
`Storefront/Shipping/Queries/ProjectStorefrontShipping/ProjectStorefrontShippingQuery.cs`;
`Tooba.Order.Endpoints/Storefront/StorefrontOrderEndpoints.cs`;
`Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs`, `CheckoutDirectory.CapturedAcceptance.cs`;
`Tooba.Order.Infrastructure/Storefront/StorefrontOrderStores.cs`;
`Tooba.Order.Infrastructure/Persistence/OrderDbContext.cs`;
**new** `Tooba.Order.Infrastructure/Persistence/Migrations/20261027230000_AddCapturedSelectionCouponContext.cs`;
`Tooba.Order.Application.csproj` / `Tooba.Order.Infrastructure.csproj` (`InternalsVisibleTo Tooba.Order.Tests` only,
so the focused proof drives the SAME internal composer/jurisdiction constant instead of duplicating values — no
production behavior is added by this visibility).

Tests: `Tooba.Order.Tests/Checkout/StorefrontCapturedCouponParityTests.cs` (new),
`Endpoints/StorefrontShippingCouponEndpointTests.cs` (new), `Checkout/StorefrontFullOwnerVerticalTests.cs`
(`Owners` exposed + `wrapDrafts` test-only port wrapper), `Checkout/CapturedFinalSubmitIntegrationTests.cs` and
`Checkout/ShippingGroupGoodsFactsHarness.cs` (fixture seeding of the confirmed goods amount),
`Storefront/StorefrontCapturedSelectionWriterTests.cs` (coupon forwarding + confirmed goods).

Docs: this RESULT, a correction appended to 235-R1/RESULT.md, and the three recovery TOPs.

## F — NOT DELIVERED / disclosed limits

- **§5.3 not fully closed:** Promotion/provider **failure propagation** and **caller cancellation** on the shared
  coupon path were not re-proven in this slice (the planner's dependency-failure propagation is unchanged code
  already covered by 229-R1; a cancelled-token fact for the coupon path was not added). **CORRECTED by TB-237
  (2026-10-28):** the claim that an invalid/rejected coupon "is surfaced by the existing Promotion/checkout
  policy" was **false for this quote path**. `CheckoutDirectory` never inspected `promotion.RejectionReasons`,
  so a supplied coupon that Promotion never applied (e.g. `COUPON_NOT_APPLICABLE` with zero discount, or an
  unrelated automatic promotion) could complete as if accepted. TB-237 added the whole-cart
  `checkout.coupon.not_applicable` refusal on that same path (`Applied[].CouponCode` evidence, partial
  applicability still lawful).
- The whole proof is **model-bound**: no HTTP listener, no live storefront/browser, no PSP/payment, no physical
  per-tenant DB claim. Endpoint forwarding is proven by invoking the real delegate with a capturing sender, not by
  a network round trip.
- Coupon identity is compared as an **exact trimmed token** (`Ordinal`); a case-insensitive Promotion policy would
  therefore require a new confirmation for `SAVE10` vs `save10`. This is deliberate (Order must not invent a
  matching policy) and is a known behavioural consequence, not a claim of parity with any future Promotion rule.
- Global per-order discount totals are **not** persisted; Order relies on each group's confirmed goods plus its own
  line snapshots. `QuotedDiscountAmount`-based guards were intentionally not extended.
- 235-R1's replacement test advanced the writer **before** the public preview, so it never exercised the real
  inter-read boundary; this task adds that boundary test through a test-only port wrapper (no production seam).
  The earlier claim is corrected in 235-R1/RESULT.md §H.
- No frontend wiring, no Order-only historical read endpoint, no real desktop 1440/390/mobile practical trial:
  those remain mandatory before any sellable claim. Coupon/owner backend proof is **not** release completion.
- Migration stays **WRITTEN / shared NOT APPLIED**; EF snapshot baseline gap retained.
- Known and retained unfixed: 230 empty-key refresh/in-flight wait, cross-owner label map/decoder limits, Offer
  API proof gaps, 227 mobile empty-note residual, 225-R1 lost unsaved user LOCAL edits.

## G — Elapsed / corrections

Claim `2026-10-27T23:18:13Z` → result `2026-10-27T23:5xZ`: **≈34 min wall clock**, inside HARD45, but the
source28/proof17 split was not timed (recorded NONCOMPLIANT on split, not on the total). The **one-correction
budget was EXCEEDED**: recorded corrections were (1) writer-test exception type
(`ContractOperationException` → `StorefrontOrderException`), (2) the explicit-absence preview fixture needed a real
`CartLineSnapshot` + `OrderTestData.SellerOrder` for currency resolution, (3) existing relational fixtures needed
`ConfirmGoodsAmount` seeding after the new mandatory confirmation, (4) the endpoint fake's `ISender` shape
(generic `Send<TRequest>` returning `Task` plus `Send(object)`) and two namespace/usings fixes, (5) the blank-control
expectation had to be corrected to the fixture's real non-coupon base path (`99/198/330`, not `109/220/330`).
Each was a test/fixture correction; no production policy was relaxed to make a fact pass. No amend/force-push/reset.

STOP_FOR_ARCHITECT; automaticNextImplementationTask: NONE.
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
