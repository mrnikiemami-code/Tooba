# TB-TMAR-CART-MULTICURRENCY-LINES-001 — Cart line-level currency authority + totals by currency

Parent: TB-TMAR-CART-MULTICURRENCY-AUDIT-001 (ARCHITECT-ACCEPTED at `28208446347c1829f281b7008a7183beef044448`)
Track: CART_MULTICURRENCY_LINES
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE (`frontendFrozen = true`)

One Cart slice only. No Order/Checkout/Payment multi-currency, no Pricing redesign, no DB migration.

## 1. Architect blocker resolution — `PRICING_CURRENCY_SELECTION_BLOCKER`

The audit found that `PriceResolutionQuery` requires a `Currency` and `IPriceLookupGateway` exposes no
currency-free overload, so a cart that removed its cart-level currency had nothing to hand Pricing.
Resolved exactly as the Architect decided, with **no Pricing change**:

| Situation | Currency passed into Pricing |
| --- | --- |
| NEW line, caller supplies a currency | that requested currency (validated by `Pricing.Contracts.CurrencyCode`) |
| NEW line, caller supplies nothing | `cart.DefaultCurrency` |
| EXISTING line (increase / quantity change / campaign revalidation) | the line's own `CartLine.QuotedCurrency` |
| Merge into a same-offer target line | target line currency, else source line currency |

`PriceQuote.Currency` returned by Pricing is authoritative and is the value persisted into
`CartLine.QuotedCurrency`. The requested/default value is only a **selector input**; it never overrides
what Pricing answers. Campaign price resolution is fed the same selected currency instead of
`cart.Currency`.

## 2. Default vs line currency semantics

- `ShoppingCart.Currency` → `ShoppingCart.DefaultCurrency` (documented as default selection only, not
  the currency invariant of all lines).
- `CartSnapshot.Currency` → `CartSnapshot.DefaultCurrency` (default-selection metadata, not line/order
  currency).
- `CartLine.QuotedCurrency` / `CartLineSnapshot.QuotedCurrency` / `CartLineView.Currency` are unchanged
  and remain the single line-level currency truth.
- The `CreateCore` 3-character validation is retained, but its meaning is now a default-selection shape
  check rather than a cart-wide transaction invariant. No cart-level equality invariant exists between
  lines: two lines of different currencies coexist.
- `cart.DefaultCurrency` is never compared against a resolved quote currency, and never rejects a line.

## 3. Persistence — no schema change proof

| Aspect | State |
| --- | --- |
| Domain property | `ShoppingCart.DefaultCurrency` |
| EF mapping | `entity.Property(x => x.DefaultCurrency).HasColumnName("currency").HasMaxLength(3)` |
| Physical column | `currency` (unchanged) |
| Migration added | none |
| Snapshot | `CartDbContextModelSnapshot` updated so a future EF diff does not rename/drop `currency` |

No `Migrations/*.cs` file was added or edited; only the model snapshot's CLR member name was aligned
with the new domain name while keeping `HasColumnName("currency")`.

## 4. Merge / requote rules

- `ChangeLineCoreAsync`, `MergeLineWithoutReservationAsync`, `RevalidateCampaignQuotesAsync` all
  requote with the line's own currency. The previous `source.QuotedCurrency ?? target.Currency`
  fallback is gone.
- Merge keeps the surviving target line's currency; for a source-only line the source line's currency
  is preserved. There is no fallback to `cart.DefaultCurrency`.
- Missing line currency fails closed with the stable code `cart.line.currency_missing`
  (`CartErrorCodes.LineCurrencyMissing`), mapped in `CartExceptionMapper` and surfaced through
  `Tooba.Cart.Endpoints` resources (`CartErrors.resx`, `CartErrors.fa.resx`).
- Unique `(CartId, OfferId)` behaviour is preserved; no duplicate same-offer lines are created.

The selection rules live in a small cohesive Cart-owned helper,
`Tooba.Cart.Infrastructure/Directories/CartLineCurrency.cs`, so `CartDirectory` does not grow into a
god-file for this concern.

## 5. `TotalsByCurrency` contract

```csharp
public sealed record CartCurrencyTotal(string Currency, decimal SubtotalExclusiveOfTax);

public sealed record CartPage(
    Guid CartId,
    int Version,
    string Market,
    string DefaultCurrency,
    string Channel,
    decimal ItemCount,
    IReadOnlyList<CartCurrencyTotal> TotalsByCurrency,
    IReadOnlyList<CartLineView> Lines,
    string? GuestSecret,
    string Status = "Active");
```

- `CartPage.Currency` and `CartPage.SubtotalExclusiveOfTax` are removed; there is no scalar
  cross-currency subtotal anywhere in the Cart presentation contract.
- Totals are grouped by line currency in deterministic ordinal currency order
  (`Tooba.Cart.Application/Presentation/CartCurrencyTotals.cs`). `10 USD + 500000 IRR` is never summed.
- A Converted cart keeps `DefaultCurrency` metadata and returns empty `TotalsByCurrency` and empty
  `Lines`, as before.
- Presentation requires `line.QuotedCurrency` and fails closed with the same stable code; there is no
  `QuotedCurrency ?? DefaultCurrency` fallback.

## 6. Add-line request / CQRS / validation

- `CartAddLineRequest` gained an optional `string? Currency = null`; the endpoint passes it into
  `AddCartLineCommand.Currency`, which forwards it to `ICartDirectory.AddOrIncreaseLineAsync(..., requestedCurrency)`.
- FluentValidation only shapes the value: absent/blank is allowed, a present value must trim to exactly
  3 characters (`cart.validation.currency_shape`). Price existence is intentionally **not** validated
  in FluentValidation.
- Endpoints → `ISender` → Application and MediatR 12.5 are unchanged. No StoreContext type is imported
  into Cart.Application.

## 7. Compile-only external edits

Mechanical, behavior-preserving edits required to compile after the intentional pre-release contract
change:

| File | Edit |
| --- | --- |
| `Tooba.Order.Application/Storefront/Services/StorefrontShippingService.cs` | reads `cart.DefaultCurrency`; adds a local `CartSubtotalExclusiveOfTax(cart)` that sums the per-currency totals, preserving the previous arithmetic |
| `Tooba.Order.Application/Storefront/Services/StorefrontCheckoutService.cs` | `StubCartPage` passes `Array.Empty<CartCurrencyTotal>()` |
| `Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs` | `cart.Currency` → `cart.DefaultCurrency` at order/money-places/quote sites |
| `Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutSubmitHost.cs` | `cart.Currency` → `cart.DefaultCurrency` |
| `Host/Tooba.Host.Tests/CheckoutOrderFoundationTests.cs` | test double implements the widened `AddOrIncreaseLineAsync` signature |

No Order/Checkout/Payment business logic was changed. The Order-side "which currency does an order
record when lines differ" decision was deliberately **not** made here (see §8).

## 8. Explicitly deferred

- Order: line/order currency invariant or settlement currency decision.
- Checkout: conversion from a multi-currency cart to an order (remains `PAUSED_AT_SAFE_W5_CHECKPOINT`).
- Payment: payment groups / settlement currency.
- Pricing: any redesign, `AllowedCurrencies`, FX, or batch API change.
- DB schema / EF migrations.
- Frontend (frozen).

The Order-side scalar shipping/checkout subtotal is left as a compile-compatibility sum; making it
currency-aware is an Order task, not this Cart task.

## 9. Validation

- `dotnet build src/backend/Tooba.slnx` — 0 errors.
- `Tooba.Cart.Tests` (full) — 22 passed, including the new
  `CartMulticurrencyTests` (mixed-currency totals, no scalar subtotal, fail-closed missing currency,
  converted-cart metadata, optional-currency validator shape).
- `HostCartResidualGuardTests` — PASS.
- `TmarCompleteReferenceStructureGateTests` — PASS.
- `TmarDurableGuardTests` — PASS (state file + guard expectation updated to
  `USER_REVIEW_CART_MULTICURRENCY_LINES_001`).
- `Hand_written_source_size_does_not_expand_beyond_baseline` — pre-existing failure; identical set of
  violations exists on a pristine `main` checkout (verified by stashing the task changes), and no Cart
  file is the cause (the confined-selection helper keeps `CartDirectory` below its previous LOC).

## 10. Certification state

- Cart remains `COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002` STRUCTURE_CERTIFIED.
- StoreContext remains `PLATFORM_CONTEXT_REFERENCE_PATTERN` + STRUCTURE_CERTIFIED.
- Order certification unchanged.
- Checkout remains `PAUSED_AT_SAFE_W5_CHECKPOINT`.
- `frontendFrozen = true`; no frontend production change.
- Recorded state: `LINE_LEVEL_CURRENCY_AUTHORITY_WITH_DEFAULT_SELECTION_AND_TOTALS_BY_CURRENCY`.
- `nextTask = USER_REVIEW_CART_MULTICURRENCY_LINES_001`.
