# TB-SHIPPING-ONE-LEVEL-TABS-124 — Checkout per-item shipping grouping contract + Tax 409 trace

- Task: `TB-SHIPPING-ONE-LEVEL-TABS-124` (Parent `TB-SHIPPING-ONE-LEVEL-TABS-123`)
- Channel: `tooba-main` / Worker `tooba-worker-01`
- Mode: `BOUNDED_CHECKOUT_PER_ITEM_SHIPPING_GROUPING_AND_TAX_BLOCKER_TRACE`
- Base SHA: `b6e5639b1c8d29241c75d16edaf6b3668cbcaaae` (== origin/main at claim)
- Nature: observation-only source/contract audit. Zero source/test/frontend/config/migration changes. No Host/frontend/DB mutation.
- Status: **PASS** (both missions classified from authoritative source/contract evidence)

## 1. Verdict (Mission A — per-item shipping grouping)

**The user requirement "group items by the shipping method selected FOR EACH ITEM" is NOT implemented.
The current shipping selection is cart-wide (one method for the entire cart), and this is a real
contract limitation, not a UI shortcut.**

Answers with evidence:

| # | Question | Answer | Evidence |
|---|----------|--------|----------|
| a | Is `methodCode` stored once per cart/draft or per cart line / seller group? | **Once per cart.** One `CartShippingDraft` row keyed by `CartId` holds a single `ShippingMethodCode`. On submit it is copied once into `CheckoutGroup`. `OrderLine` has **no** shipping field at all. | `Tooba.Order.Domain/Checkout/CartShippingDraft.cs` (`public Guid CartId { get; init; }`, `public string ShippingMethodCode`), `Tooba.Order.Domain/Aggregates/CheckoutGroup.cs:113-125`, `Tooba.Order.Domain/Aggregates/OrderLine.cs` (no shipping member) |
| b | Can different cart items carry different `methodCode`s concurrently? | **No.** The selection request, draft, projection, payment-draft and checkout snapshot all carry exactly one scalar code. | `StorefrontOrderModels.cs` `StorefrontShippingSelectionRequest.ShippingMethodCode` (:187), `StorefrontShippingProjection.SelectedMethodCode` (:148), `StorefrontShippingDraftView.ShippingMethodCode` (:128) |
| c | Where are items grouped by shipping method (UI / order persistence / Fulfillment)? | **Nowhere for customer selection.** UI renders a flat cart-wide card list from one `methodCode` state; order persistence has one checkout-level code; Fulfillment creates one `FulfillmentUnit` per `SellerOrder` carrying that same single code. A `Shipment` may carry its own optional code, but only because an operator/seller supplies it at creation — it is not derived from a per-item customer choice. | `src/frontend/app/storefront/storefront-shipping.tsx:116,619,343`, `OrderFulfillmentBridge.cs:82-83`, `FulfillmentUnit.cs:56-59,90-91`, `FulfillmentUnit.CreateShipment(... shippingMethodCode, shippingMethodLabel ...)`:331-360 |
| d | Must grouping also partition by seller/origin/fulfillment constraints? | **Seller YES (already an authoritative boundary). Origin/warehouse NO (not in this contract).** One `SellerOrder` per seller; one `FulfillmentUnit` per `SellerOrder`; a line can never cross its seller's unit. Delivery promise and price are computed cart-wide over *all* sellers (`MaxSellerPreparationDays` = slowest seller), which is exactly the "one method fits the whole cart" behaviour the user rejects. Warehouse/origin (`WH-*`) exists only in Inventory and never enters shipping selection. | `FulfillmentUnit.cs:26-30` (`SellerOrderId`), `StorefrontShippingService.cs:72-73,222-223`, `StorefrontShippingCalculator.MaxSellerPreparationDays`, `TaxFoundationTests` cross-seller/`Cross_seller_line_is_rejected_by_domain` |
| e | Narrowest capability/contract gap | See §3. | — |

### 4-line worked example

Cart: A, C → `post:express`; B → `tipax:standard`; D → `store_courier`.

Expected groups under the user rule (seller partition applied first; assuming A,B ∈ seller S1 and C,D ∈ seller S2):

```
S1 → { A } post:express   + { B } tipax:standard      (2 groups, 2 prices, 2 promises)
S2 → { C } post:express   + { D } store_courier       (2 groups, 2 prices, 2 promises)
Shipping payable = sum of the 4 group prices
```

If all four belong to one seller: `{A,C} post:express`, `{B} tipax:standard`, `{D} store_courier`.

**Current implementation cannot represent either case.** It can only store `post:express` *or* `tipax:standard`
*or* `store_courier` for the whole cart, with one amount, one date and one time window. Two methods can
coexist only as *admin/seller-created* shipments after payment (operator decision), which is not the
customer-facing per-item rule and creates no grouping UI or price.

### Where intended ≠ existing

- Intended (product): per-item/per-group method choice, grouped, priced and promised per group.
- Existing (code): single cart-wide method code end-to-end (draft → checkout group → fulfillment unit).
- The `Shipment`-level `ShippingMethodCode` (:32) and the Admin-only `ConsolidatedPackage`
  (`CheckoutId` + one `ShippingMethodCode`, requires **≥2 distinct sellers**, created by Admin) are the only
  method-aware groupings that exist; neither is a cart-line grouping and neither is customer-selected.

## 2. Tax 409 first-blocker trace (Mission B)

`GET /v1/storefront/cart/{cartId}/payment-draft` → `StorefrontShippingService.ProjectPaymentDraftAsync`
requires an existing `CartShippingDraft`, then calls `_checkouts.PreviewAsync(...)` with the draft's
single `shippingMethodCode`. The preview composes tax per cart line; on failure it surfaces 409
`checkout.tax.unavailable`.

**Authoritative cause: the offer in the cart has no applicable active Tax rule — it is a Development
fixture gap, not a defect in checkout/shipping/payment code.**

`TaxDirectory.CalculateAsync` (`Tooba.Tax.Infrastructure/Adapters/TaxDirectory.cs:120-160`) fails closed when:
1. no `tax.offer_classifications` row exists for the offer → `NoApplicableRule`, or
2. no **Active** rule matches `jurisdiction + market + classification.CategoryId` effective at the time → `NoApplicableRule`.

The storefront always asks with a hard-coded jurisdiction and the commerce market:
`StorefrontCheckoutService.cs:29` `TaxJurisdiction = "IR-NAT"`, market `IR` (currency `IRR`).

The only Development rule the sanctioned seeds ever create is:

```
jurisdiction IR-NAT | market IR | category "standard" | Percentage | rate 0.09 | specificity 10 | Active
```

created by `WorkspaceDemoMarketplaceSeed.EnsureMarketplaceAsync` (via `ITaxDevelopmentSeedGateway`) and by
`StorefrontDemoCatalogBootstrap` (rule + `DEMO-` offer classification). **There is no Tax HTTP endpoint in
the product at all** (no `MapGroup/MapPost` under `Modules/Tax`), so the *only* sanctioned way to create a
rule is the module-owned Development seed gateway — never manual SQL.

The TB-123 cart line used the attribute-schema demo product, and that enricher classifies its offers into a
**per-offer category that never gets a rule**:

```csharp
// CatalogAttributeSchemaSellableEnricher.cs:125-128
var taxCode = $"sch-{offerId:N}"[..20];
var classified = await tax.EnsureDevelopmentOfferCategoryAsync(
    new EnsureDevelopmentOfferCategory(offerId, taxCode, "schema phone"), cancellationToken);
```

Read-only verification against the shared read-only dev store (`tooba_alpha`, SELECT only) confirms the
shape of the gap:

```
cat:standard|استاندارد|3218
cat:sch-01a04569de3f7000|schema phone|1
cat:sch-01a04569df137000|schema phone|1
rule:IR-NAT|IR|Active|Percentage|0.09|10
rule_category:standard
schema_offer:SCHEMA-PHONE-PHONE-BLK-128|cat=sch-01a04569de3f7000
schema_offer:SCHEMA-PHONE-PHONE-BLU-256|cat=sch-01a04569df137000
```

⇒ Only category `standard` has the Active rule. Every offer classified into a `sch-…` category (the
schema-phone demo offers used by TB-123) fails tax in **any** Development store, including the shared one.

### Sanctioned bounded recipe for the next isolated acceptance (no SQL, no code change)

A fresh scratch store bootstrapped with `Tooba:CatalogDemo:RunLegacyBootstraps=true` runs
`DevelopmentSchemaMigrator.ApplyAsync(seedCatalog: true)` (`Host/Tooba.Host/Program.cs:366-380`), which calls
`IWorkspaceDemoSeed.SeedNewProductAsync` → `WorkspaceDemoProductSeed.SeedProductAsync` (publishes
`workspace-live-shirt`) **plus** `WorkspaceDemoMarketplaceSeed.EnsureMarketplaceAsync`, which
creates/activates the `standard` rule and classifies its two offers (`ARM-LN-01`, `DGS-LN-01`, sellers
«فروشگاه آرمان» / «دیجی‌استایل نمونه»).

**Recipe:** use the workspace demo product (`/fa/products/workspace-live-shirt`) as the cart line instead of
the schema-phone product. Its offers are classified `standard` and the Active `IR-NAT`/`IR` 9% rule applies,
so `payment-draft` returns 200 and the payment page can render the shipping method label/price. The product
also spans **two sellers**, which makes it the right fixture for the per-item grouping proof.

**Smallest module-owned fixture gap (fail-closed, do NOT fabricate Tax authority):**
`CatalogAttributeSchemaSellableEnricher` (Catalog.Infrastructure/Development) creates a dedicated
category per offer but never pairs it with a rule, while `ITaxDevelopmentSeedGateway` exposes
`EnsureDevelopmentRuleAsync` and takes a `CategoryCode`. Owner of the gap: Catalog Development enricher
(consumer) + Tax Development seed seam (capability). Classifying the schema offers into `standard`, or
creating a matching rule for the generated code, is a **bounded Development-fixture task** and must not be
done silently inside a storefront/shipping task.

**Supported vs unproven**
- Supported (source): rule creation, classification, applicability conditions, jurisdiction/market constants, absence of Tax HTTP API, EER-of-ownership.
- Supported (read-only shared DB): only `standard` has an Active rule; `sch-*` categories have none.
- **Unproven at runtime in this task:** that a fresh scratch store actually ends up with `ARM-LN-01`/`DGS-LN-01`
  classified `standard` (asserted from the seed code path only; no Host/frontend was started, per task rule).

## 3. Recommended next slice (ONE, bounded)

**Slice:** introduce an explicit Order-owned *shipping group* for the storefront cart, keeping Order as the
single authority and Fulfillment as a consumer of the handoff snapshot.

1. Order domain: keep the cart→draft header (recipient/address/note) but move method/amount/date-window into
   `CartShippingGroup(CartId, SellerPartyId, MethodCode)` rows; every cart line belongs to exactly one group
   and the union of groups must cover all lines of its seller.
2. Order storefront Contracts: `StorefrontShippingSelectionRequest` gains `Groups[]`
   (`SellerPartyId`, `ShippingMethodCode`, `SelectedDeliveryDate`, `SelectedDeliveryTimeWindow`);
   `StorefrontShippingProjection` returns per-group eligible methods + per-group selected amount and a
   total. Recipient/address stay on the header.
3. Pricing stays authoritative in `ShippingOptions` via the existing `ResolveRate`; no second price source.
4. `OrderFulfillmentBridge` handoff: pass `(OrderLineId → MethodCode)` (or one shipment spec per group)
   instead of one checkout-level code; `FulfillmentUnit` still owns one unit per `SellerOrder`.

**Why a slice and not a fix:** it changes a locked storefront Contract shape and a module handoff boundary,
so it needs Architect review before implementation. **Do NOT** claim per-item grouping as working today.

**Minimal acceptance scenario:** guest cart with `workspace-live-shirt` (two sellers) + one second-seller
item; select `post:express` for seller A's line and `tipax:standard` for seller B's line (or two groups for
one seller); continue to payment; assert (i) each group's method/amount/date-window is persisted, (ii) the
payment page shows the grouped breakdown and the summed shipping amount, (iii) `CheckoutGroup`/order
persistence carries one method per group (not one per checkout), (iv) Fulfillment creates one unit per seller
with the correct per-group methods, (v) no shipping commit/order creation happens before the final payment
click. Run `npm run test:critical-storefront` if any shared storefront component is touched.

## 4. Concrete ownership / source map

| Concern | Owner | Path |
|---|---|---|
| Cart-wide shipping draft (single method/amount/date) | Order.Domain | `Modules/Order/Tooba.Order.Domain/Checkout/CartShippingDraft.cs` |
| Selection request/response/draft view contracts | Order.Application | `Modules/Order/Tooba.Order.Application/Storefront/Models/StorefrontOrderModels.cs:118-200` |
| Selection validation + pricing (client cannot forge price) | Order.Application | `.../Storefront/Services/StorefrontShippingService.cs:141-370`, `.../Storefront/Services/StorefrontShippingCalculator.cs` |
| Save-selection CQRS command + validator | Order.Application | `.../Storefront/Shipping/Commands/SaveStorefrontShippingSelection/**` |
| Eligible-method list (cart-wide; subtotal+province only) | Order.Application | `StorefrontShippingService.LoadEligibleMethodsAsync:373-392` |
| Checkout snapshot (single checkout-level method) | Order.Domain | `.../Aggregates/CheckoutGroup.cs:113-125` |
| Order line (no shipping field) | Order.Domain | `.../Aggregates/OrderLine.cs` |
| Handoff of one method per seller order | Order.Infrastructure | `.../Integrations/Fulfillment/OrderFulfillmentBridge.cs:67-84` |
| Fulfillment unit (per SellerOrder, one method) | Fulfillment.Domain | `.../Aggregates/FulfillmentUnit.cs:26-59, 85-120` |
| Shipment-level optional method (operator-supplied) | Fulfillment.Domain | `.../Aggregates/Shipment.cs:29-38`, `FulfillmentUnit.CreateShipment:331-360` |
| Admin multi-seller consolidation (one method, ≥2 sellers) | Fulfillment.Domain | `.../Aggregates/ConsolidatedPackage.cs:72-141` |
| Storefront shipping UI (single `methodCode` state) | Frontend | `src/frontend/app/storefront/storefront-shipping.tsx:116,280-295,327-347,613-633` |
| Tax applicability + fail-closed outcomes | Tax.Infrastructure | `Modules/Tax/Tooba.Tax.Infrastructure/Adapters/TaxDirectory.cs:120-160` |
| Sanctioned Development tax seed capability | Tax + Catalog.Infrastructure | `TaxDevelopmentSeedGateway.cs`, `WorkspaceDemoMarketplaceSeed.cs:151-181`, `StorefrontDemoCatalogBootstrap.cs:429-489` |
| Tax 409 mapping to checkout error | Order.Application | `StorefrontCheckoutService.cs:29`, `StorefrontOrderErrors` / `checkout.tax.unavailable` |

## 5. Non-interference / deviations / rollback

- No source, test, frontend, config or migration file touched. No Host/frontend started, no container or DB
  mutated (only read-only `SELECT` against the existing read-only dev store).
- Pre-existing dirty paths `src/frontend/app/admin/admin-fulfillment-work-queue-screen.tsx` and
  `src/frontend/app/admin/admin-order-detail-screen.tsx` were left untouched and unstaged.
- The Bridge task receipt stays UNSTAGED.
- Deviation: none of substance. Rollback: docs-only commit; revert the docs commit.
- No focused test was executed; no test was needed, because both verdicts are settled by contract/source and
  the single runtime uncertainty is explicitly marked unproven.

## 6. First remaining blocker & single next task boundary

- Blocker: cart-wide shipping contract cannot model per-item/per-group methods (needs Architect approval),
  and the schema-phone Development fixture creates categories without rules (fixture gap).
- Next task boundary: **design-review only** of the shipping-group contract slice in §3 plus a bounded
  Development-fixture decision for the schema-phone tax category. No implementation without approval.
