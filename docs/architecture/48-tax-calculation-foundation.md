# Tooba — Tax Calculation Foundation

Status:

```text
IN_PROGRESS — TB-P03-T007 REPAIR awaiting Architect ACCEPT
```

Task:

```text
TB-P03-T007
```

## Purpose

Tax is a separate commercial calculation from Pricing. Authored catalog/offer/price amounts stay tax-exclusive. Checkout must calculate tax after price revalidation and persist an immutable snapshot on Order lines. Payment later charges the tax-inclusive payable total; it does not calculate tax.

## Ownership

Module `Tax` owns `TaxDbContext`, schema `tax`, tax categories, effective-dated rules, offer classification references, and `ITaxCalculator`.

Order snapshots tax results. Pricing does not store tax amounts. Catalog/Offer do not store rates.

No foreign DbContext, no cross-module FK.

## Locked decisions

- Base price = tax exclusive
- Tax is calculated separately
- Rules are configurable and effective-dated
- No hard-coded jurisdiction rate, date, or law
- `TAX_EXEMPT` != `ZERO_RATE` != `NO_APPLICABLE_RULE` != `CALCULATION_ERROR`
- Checkout fail-closed on missing rule or calculation error
- Historical Order tax snapshots do not change when rules later change
- Locale != Market != Currency != Tax Jurisdiction
- Client/request cannot inject a tax percentage
- Trusted override only when a rule explicitly allows an internal trusted path
- Rounding is deterministic: IRR/JPY/KRW scale 0, otherwise 2, `AwayFromZero`
- Request-to-reserve and online-purchase both calculate tax into the commercial snapshot
- B2B VAT invoice / tax profile / government filing remain out of scope

## Checkout integration

Before final Order snapshots:

1. Re-resolve Pricing
2. Calculate Tax with explicit jurisdiction on the checkout command
3. Persist line/order tax snapshots

`NoApplicableRule` and `CalculationError` abort checkout. They do not become silent zero tax.

## Tenant isolation

Tax data lives in the tenant/edition database resolved by existing commerce connection seams. Tenant A rules cannot classify or tax Tenant B offers.
