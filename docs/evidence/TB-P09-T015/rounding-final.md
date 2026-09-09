# Rounding final — TB-P09-T015

One store-level `GlobalRoundingMode` (Floor / Ceiling / Nearest).

Quantity path: `IQuantityNormalizer` (places + optional Step).
Money path: `FinancialRounder` (same mode, money places). Step is not applied to money.

## Step = null, places = 2

- 1 → 1
- 1.2 → 1.2
- 1.25 → 1.25

`QuantityFoundationTests.Step_null_accepts_precision_value`

## Step = 0.25, input 1.37

- Floor → 1.25
- Ceiling → 1.50
- Nearest → 1.25

`QuantityFoundationTests.Step_quarter_normalizes`

Exact step values are unchanged. Arithmetic is `decimal`.

## Invoice Floor money 0

998 × 20% = 199.6 raw → Discount 199, LineNet 799.

`QuantityFoundationTests.Financial_floor_precision_zero_rounds_discount_once`
`InvoiceHeaderAggregateTests`

No Product/Offer rounding override fields exist.
