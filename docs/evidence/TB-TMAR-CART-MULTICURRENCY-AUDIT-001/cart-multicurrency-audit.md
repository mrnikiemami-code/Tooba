# TB-TMAR-CART-MULTICURRENCY-AUDIT-001 — Cart single-currency assumptions audit

Parent: TB-TMAR-STORECONTEXT-GOLDEN-001 (ARCHITECT-ACCEPTED)
Track: CART_MULTICURRENCY_BOUNDED_AUDIT
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Nature: **AUDIT ONLY** — no production code, no schema, no migration, no behavior change.

## 0. Parent acceptance SoT bookkeeping evidence

Because the parent task deliberately left `lastAcceptedTask`/`lastAcceptedCommit` unchanged pending
Architect review, only recovery SoT bookkeeping was updated:

| Field | Before | After |
| --- | --- | --- |
| `lastAcceptedTask` | `TB-TMAR-STORECONTEXT-FOUNDATION-001` | `TB-TMAR-STORECONTEXT-GOLDEN-001` |
| `lastAcceptedCommit` | `48720fd3ae5bc69ccb5ae6532a2000f7a2b9d4f8` | `f2667a249d43fb542903a08b429cd1ea8e219704` |
| `lastAcceptedSoTStamp` | `95706f174cbfe63f32cc84641b77ace403ab190f` | `f2667a249d43fb542903a08b429cd1ea8e219704` |
| `nextTask` | `USER_REVIEW_STORECONTEXT_GOLDEN_001` | `USER_REVIEW_CART_MULTICURRENCY_AUDIT_001` |

Also updated: `TOOBA-TMAR-MASTER-RECOVERY.md` (`Next task`), `TOOBA-ARCHITECT-BOOTSTRAP.md`
(`Last accepted TMAR task`, `Current next task`) and the two durable guard assertions that read
those values. Certification meaning was not altered beyond the already-recorded accepted
StoreContext state: `structureLock.certifiedModules` stays `[Order, Cart, StoreContext]`,
`storeContext.state` stays `PLATFORM_CONTEXT_REFERENCE_PATTERN`, Cart/Order entries untouched.

## 1. Current single-currency assumptions table

Every occurrence of `ShoppingCart.Currency` (cart-level currency) and its classification.

| # | Location | Member / expression | Role | Classification |
| --- | --- | --- | --- | --- |
| 1 | `Tooba.Cart.Domain/Aggregates/ShoppingCart.cs` (CreateCore) | `if (string.IsNullOrWhiteSpace(currency) \|\| currency.Trim().Length != 3) throw "cart.currency.invalid"` | cart creation invariant | `REMOVE_CART_LEVEL_CURRENCY` (the *invariant* must go) |
| 2 | same, `CreateAuthenticated(... string currency ...)` | creation input → `CreateCore` | cart creation input | `KEEP_DEFAULT_SELECTION_ONLY` |
| 3 | same, `CreateGuest(... string currency ...)` | creation input → `CreateCore` | cart creation input | `KEEP_DEFAULT_SELECTION_ONLY` |
| 4 | same, `CreateCore` object initializer | `Currency = currency.Trim().ToUpperInvariant()` | persistence field + invariant | `REMOVE_CART_LEVEL_CURRENCY` |
| 5 | `Tooba.Cart.Infrastructure/Directories/CartDirectory.cs` `CreateAuthenticatedAsync` | `_ = CurrencyCode.Parse(currency);` | creation validation (Pricing `CurrencyCode`) | `KEEP_DEFAULT_SELECTION_ONLY` |
| 6 | same, `CreateGuestAsync` | `_ = CurrencyCode.Parse(currency);` | creation validation | `KEEP_DEFAULT_SELECTION_ONLY` |
| 7 | same, `CreateAuthenticatedCoreAsync` | `_ = CurrencyCode.Parse(currency);` | creation validation (login-adoption path) | `KEEP_DEFAULT_SELECTION_ONLY` |
| 8 | same, `ValidateOfferAndQuoteAsync` | `_campaignPrices.TryResolveEligibleCampaignPriceAsync(campaignId, offerId, cart.Market, cart.Channel, cart.Currency, now, ct)` | price-resolution input | `REQUIRES_LATER_CHECKOUT_DECISION` + feeder of the Pricing blocker |
| 9 | same, `ValidateOfferAndQuoteAsync` | `new PriceResolutionQuery(offerId, cart.Market, cart.Channel, cart.Currency, now, null, null, quantity)` | price-resolution input | feeder of the Pricing blocker |
| 10 | same, `MergeLineWithoutReservationAsync` | `var quotedCurrency = source.QuotedCurrency ?? target.Currency;` | merge fallback | `REMOVE_CART_LEVEL_CURRENCY` (fallback is wrong; line truth must win) |
| 11 | same, `ToSnapshotAsync` | `cart.Currency` → `CartSnapshot.Currency` | persistence field / public read model | `REQUIRES_LATER_CHECKOUT_DECISION` |
| 12 | `Tooba.Cart.Application/Presentation/CartPresentationComposer.cs` `PresentAsync` | `line.QuotedCurrency ?? snapshot.Currency` | presentation fallback for a line | `REMOVE_CART_LEVEL_CURRENCY` |
| 13 | same, `PresentAsync` (both `CartPage` constructions) | `snapshot.Currency` → `CartPage.Currency` | presentation page currency | `REQUIRES_LATER_CHECKOUT_DECISION` |
| 14 | same, `PresentAsync` | `snapshot.SubtotalExclusiveOfTax` → `CartPage.SubtotalExclusiveOfTax` | presentation aggregate | `REMOVE_CART_LEVEL_CURRENCY` (see §5, invalid across currencies) |
| 15 | `Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs` | `entity.Property(x => x.Currency).HasMaxLength(3);` | persistence mapping | see §7 |
| 16 | `Tooba.Cart.Infrastructure/Persistence/Migrations/20260823100150_InitialCart.cs` | `currency = table.Column<string>(maxLength: 3, nullable: false)` | DB column | see §7 |

`KEEP_LINE_LEVEL_CURRENCY` applies to `CartLine.QuotedCurrency` and
`CartLineSnapshot.QuotedCurrency` / `CartLineView.Currency` — see §2.

No occurrence was classified `KEEP_LINE_LEVEL_CURRENCY` under a *cart-level* member; the cart-level
members are all either default-selection-only, an invalid fallback/aggregate, or a
checkout-decision-dependent read model.

## 2. CartLine currency truth

`CartLine.QuotedCurrency` (`string?`, `Tooba.Cart.Domain/Entities/CartLine.cs`) is already the
authoritative currency of an individually quoted line. Evidence per path:

- **creation** — `CartLine.Open(..., string quotedCurrency, ...)` sets `QuotedCurrency = quotedCurrency`;
  `CartDirectory.AddOrIncreaseLineAsync` passes `quote.Currency` from the resolved `PriceQuote`.
- **replacement / requote** — `CartLine.ReplaceHold(..., string quotedCurrency, ...)` sets
  `QuotedCurrency = quotedCurrency`. Callers always pass a quote currency:
  - `ChangeLineCoreAsync` → `quote.Currency`
  - `MergeLineWithoutReservationAsync` → `quote.Currency` (only the pre-resolution fallback uses
    `target.Currency`, see #10 above)
  - `RevalidateCampaignQuotesAsync` → `quote.Currency`
  - no caller passes `cart.Currency` into `ReplaceHold`.
- **snapshot** — `CartDirectory.ToSnapshotAsync` maps `line.QuotedCurrency` into
  `CartLineSnapshot.QuotedCurrency`.
- **presentation** — `CartPresentationComposer` maps `line.QuotedCurrency ?? snapshot.Currency` into
  `CartLineView.Currency`.

Verdict: line-level currency truth is already sufficient. No change to `CartLine` is needed.

## 3. Pricing contract capability / verdict

Contracts inspected (`Tooba.Pricing.Contracts`):

- `PriceQuote(PriceId, OfferId, Market, SalesChannel Channel, decimal Amount, string Currency, bool TaxExclusive, bool IsAuthored)`
- `PriceResolutionQuery(OfferId, Market, Channel, Currency, At, CustomerPartyId, OrganizationPartyId, Quantity)`
- `IPriceLookupGateway.ResolvePriceAsync(PriceResolutionQuery, ct)`
- `IPriceLookupGateway.ResolvePricesBatchAsync(offerIds, market, channel, currency, at, ct)`
- `IPriceLookupGateway.ResolveCampaignPricesBatchAsync(offerIds, campaignId, market, channel, currency, at, ct)`
- `ICampaignCartPriceAuthority.TryResolveEligibleCampaignPriceAsync(campaignId, offerId, market, channel, currency, at, ct)`
- `CurrencyCode` (owned ISO value object; rejects TMN/IRT/TOMAN, 3 ASCII letters, `Scale` 0 for IRR/JPY/KRW else 2)

Verdict:

- The Pricing API is already **per-query**: it accepts a currency per call and returns the quote's
  own `Currency`. Nothing in the contract forces all quotes of a cart to share one currency.
- The only currency-uniform seams are the **batch** overloads, which take a single `currency`
  argument. Multi-currency Cart must therefore group offers by currency before batching.
- **Cart can become multi-currency WITHOUT changing Pricing Contracts.**
- Pricing must **not** be redesigned in the next slice.

## 4. Add / increase line behavior and the blocker

Exact current path that resolves a new line price:

```text
CartDirectory.AddOrIncreaseLineAsync
  → existing line? ChangeLineCoreAsync
  → new line?      ValidateOfferAndQuoteAsync
        → ICampaignCartPriceAuthority.TryResolveEligibleCampaignPriceAsync(campaignId, offerId, cart.Market, cart.Channel, cart.Currency, now, ct)   [campaign]
        → IPriceLookupGateway.ResolvePriceAsync(new PriceResolutionQuery(offerId, cart.Market, cart.Channel, cart.Currency, now, null, null, quantity), ct)  [base]
  → CartLine.Open(..., quote.Amount, quote.Currency, ...)
```

Deterministic target rule for the next implementation:

1. initial cart creation receives `StoreContext.DefaultCurrency` **only** as an initial/default
   selection input;
2. when adding/requoting a line, the line currency must come from the resolved
   `PriceQuote.Currency`;
3. Cart must **not** reject a second line merely because its quote currency differs from another
   line's currency — there is currently no such rejection in `AddOrIncreaseLineAsync`
   (the only line-level guards are `cart.offer.missing`, `cart.offer.inactive`,
   `cart.offer.channel_mismatch`, quantity policy, min/max quantity, `cart.inventory.*`,
   `cart.pricing.quote_missing`), and no new one may be added.

**PRICING_CURRENCY_SELECTION_BLOCKER** — explicitly identified:

`PriceResolutionQuery` **requires** `Currency`, and `IPriceLookupGateway` exposes **no** overload
that resolves a price without a currency. Therefore, to price the first line of a cart, or to price
a line for a cart whose cart-level currency was removed, Cart must supply *some* currency before
Pricing can answer. Supplying `StoreContext.DefaultCurrency` re-introduces a hidden default-selection
policy; supplying a line-independent cart currency preserves the very assumption being removed.
No fix is invented in this audit.

## 5. Presentation subtotal state

Invalid cross-currency summing sites (`MULTICURRENCY_INVALID_TOTAL`):

| Location | Expression | Problem |
| --- | --- | --- |
| `CartPresentationComposer.PresentAsync` | `var subtotal = lines.Sum(item => item.LineAmountExclusiveOfTax ?? 0);` | sums line amounts across all currencies into one scalar |
| `CartPresentationComposer.PresentAsync` | `new CartPage(..., snapshot.Lines.Sum(item => item.Quantity), subtotal, lines, ...)` | emits that single scalar as `SubtotalExclusiveOfTax` with a single `CartPage.Currency` |
| `CartPresentationComposer.PresentAsync` (converted branch) | hardcoded `0` for item count and subtotal | trivially single-currency but must stay consistent with the new shape |
| `Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs` | `CartPage.Currency` + `CartPage.SubtotalExclusiveOfTax` | contract shape itself cannot express per-currency totals |

The next design must not sum `10 USD + 500000 IRR` into one number.

**Recommended presentation shape: `TotalsByCurrency`** — group `CartLineView` amounts by
`CartLineView.Currency` and emit one total per currency, and **do not emit an aggregate subtotal
when more than one currency is present**. `CartPage.Currency` then becomes meaningless as a page
scalar; keep it only as a single-currency convenience or deprecate it. This is the only shape that
fits the current contracts without inventing FX and without a multi-currency arithmetic rule.
(Alternative "no aggregate subtotal when >1 currency" is the degenerate case of the same shape and
is acceptable if a collection member is undesirable.)

Not implemented in this audit.

## 6. Contract change map

| Contract member | File | Change class |
| --- | --- | --- |
| `CartSnapshot.Currency` (positional record parameter) | `Tooba.Cart.Contracts/Checkout/CartContracts.cs` | `BREAKING_PRE_RELEASE_SAFE` (rename to `DefaultCurrency` or remove) |
| `CartLineSnapshot.QuotedCurrency` | same | `KEEP_UNCHANGED` (line truth) |
| `CartPage.Currency` | `Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs` | `BREAKING_PRE_RELEASE_SAFE` (demote/deprecate) |
| `CartPage.SubtotalExclusiveOfTax` | same | `BREAKING_PRE_RELEASE_SAFE` (only valid for a single currency) |
| `CartPage.TotalsByCurrency` (new) | same | `CAN_ADD_COMPATIBLY` (additive member on a positional record → move to init/optional parameter) |
| `CartLineView.Currency` | same | `KEEP_UNCHANGED` (already per-line) |
| `ICartDirectory.CreateAuthenticatedAsync(..., string currency, ...)` | `Tooba.Cart.Application/Ports/ICartDirectory.cs` | `BREAKING_PRE_RELEASE_SAFE` (semantic rename to `defaultCurrency`) |
| `ICartDirectory.CreateGuestAsync(string market, string currency, ...)` | same | `BREAKING_PRE_RELEASE_SAFE` (same) |
| `CartCommerceContext(Market, DefaultCurrency, SalesChannel)` | `Tooba.Cart.Application/Ports/ICartCommerceContextResolver.cs` | `KEEP_UNCHANGED` (already `DefaultCurrency`) |
| `GuestCartCreated(CartSnapshot, string)` | same | `KEEP_UNCHANGED` (no currency member) |
| `CartMergeResult(CartSnapshot, bool, Lines)` | same | `KEEP_UNCHANGED` |
| `ShoppingCart.CreateAuthenticated/CreateGuest(..., string currency, ...)` | `Tooba.Cart.Domain/Aggregates/ShoppingCart.cs` | `BREAKING_PRE_RELEASE_SAFE` (semantic rename + invariant removal) |
| `CartLine.Open/ReplaceHold(..., string quotedCurrency, ...)` | `Tooba.Cart.Domain/Entities/CartLine.cs` | `KEEP_UNCHANGED` |

Adding `TotalsByCurrency` to a positional `CartPage` record is only additive if it is added as an
optional trailing parameter (the record already uses trailing defaults such as
`string Status = "Active"`), otherwise all construction sites change.

## 7. Persistence impact

Direct evidence:

- `Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs`:
  `entity.Property(x => x.Currency).HasMaxLength(3);` on the `ShoppingCart` entity.
- `Tooba.Cart.Infrastructure/Persistence/Migrations/20260823100150_InitialCart.cs`:
  `currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)`
  and `quoted_currency = table.Column<string>(..., nullable: true)`.
- `ShoppingCart.Currency` is a non-nullable `init` property; `CartLine.QuotedCurrency` is nullable.

Verdict — three possible postures for the first implementation slice:

1. **DB column removal** — requires an EF migration, a change to `ShoppingCart`, and a data
   backfill decision. Highest cost, not required to remove the *invariant*.
2. **Temporary nullable/deprecated column** — no schema drop; mark the member deprecated and stop
   using it as an invariant. Low cost, reversible.
3. **No DB change** — keep the column as the recorded default-selection currency and only remove the
   invariant/fallback/aggregate uses.

Recommended for the first slice: **(3) then (2)** — no migration in the first slice; keep the column
as the default-selection record, remove the invariant and the fallback, and revisit the column once
Checkout's currency decision is made. **No migration in this audit.**

## 8. Next implementation file list (one Cart slice only)

1. `src/backend/Modules/Cart/Tooba.Cart.Domain/Aggregates/ShoppingCart.cs`
2. `src/backend/Modules/Cart/Tooba.Cart.Domain/Entities/CartLine.cs` (expected: no change)
3. `src/backend/Modules/Cart/Tooba.Cart.Application/Ports/ICartDirectory.cs`
4. `src/backend/Modules/Cart/Tooba.Cart.Application/Commands/CreateGuestCart/CreateGuestCartCommand.cs`
5. `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Directories/CartDirectory.cs`
6. `src/backend/Modules/Cart/Tooba.Cart.Application/Presentation/CartPresentationComposer.cs`
7. `src/backend/Modules/Cart/Tooba.Cart.Contracts/Checkout/CartContracts.cs`
8. `src/backend/Modules/Cart/Tooba.Cart.Contracts/Presentation/CartPresentationContracts.cs`
9. `src/backend/Modules/Cart/Tooba.Cart.Infrastructure/Persistence/CartDbContext.cs`
   (only if the slice elects posture 1 or 2 — posture 3 needs no change)
10. Cart tests inside `Tooba.Cart.Tests` and the Cart guards in `Tooba.Host.Tests/Architecture`
    (exact names to be fixed by the implementation task)

Proposed next implementation scope: **ONE slice** — remove the cart-level currency *invariant* and
the cart-level *fallback/aggregate* uses, adopt explicit `DefaultCurrency` default-selection
semantics end to end, and introduce per-currency totals. The `PRICING_CURRENCY_SELECTION_BLOCKER`
must be resolved as an explicit, separately stated decision inside that slice (or deferred by an
explicit Architect decision) — it must not be silently worked around.

## 9. Deferred work (explicit)

Deferred to separate, separately-authorized tasks:

- Order: any multi-currency line/order invariant or settlement decision.
- Checkout: conversion from a multi-currency cart to an order, and the currency recorded on the
  order. Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`.
- Payment: payment groups, settlement currency, partial-currency settlement.
- Pricing: any redesign, `AllowedCurrencies`, FX, or batch API change.
- DB schema / EF migrations.
- Frontend: `frontendFrozen = true`, no frontend change.

## 10. Golden architecture requirements preserved by the recommendation

The recommendation keeps: Cart `ARCH-COMPLETE-002` certified; capability-first foldering;
path↔namespace alignment; no god-files; no Host business logic; cross-module access only through
Contracts; unchanged CQRS/MediatR 12.5 endpoint flow; no ceremonial handlers; FluentValidation
transport/input validation only; no cross-module DB access; no cross-bounded-context ACID; no
frontend change; Checkout paused.

## 11. Validation

- No production `.cs` / `.csproj` / config file changed (audit-only).
- `TmarDurableGuardTests` — PASS
- `TmarCompleteReferenceStructureGateTests` — PASS
- Cart / Order / StoreContext certifications unchanged; frontend remains frozen.
