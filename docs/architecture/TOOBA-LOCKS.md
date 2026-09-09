# Tooba — Architecture / Product Lock Registry

Canonical lock file. Future tasks must read applicable locks, list IDs, discover, state reuse vs change, and regress touched locked behavior.

```text
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
```

## Quantity

### LOCK-QTY-001 — Product quantity is decimal
True product amounts are `decimal` / `numeric(18,6)`. Record counts stay `int`.

### LOCK-QTY-002 — Product owns quantity policy
Product owns `UnitOfMeasureId`, `QuantityDecimalPlaces`, optional `QuantityStep`. Variant does not duplicate in this phase. `EffectiveQuantityPolicy` is the resolver.

### LOCK-QTY-003 — Offer owns purchase limits only
Offer owns optional `MinimumOrderQuantity` / `MaximumOrderQuantity`. No Unit / DecimalPlaces / Step on Offer.

### LOCK-QTY-004 — One quantity subsystem
No duplicate UoM/quantity platform. Inventory / Fulfillment / Returns reuse existing modules. Multiple selling units are out of scope; architecture must not block a later extension.

### LOCK-QTY-005 — Historical quantity snapshots
OrderLine quantity + unit/places/step snapshots are immutable. Later Product/rounding changes do not rewrite history.

## Multilingual

### LOCK-I18N-001 — Language Registry is dynamic
Multilingual data uses the DB-backed Language Registry (`LanguageId`). Do not model translations as fixed fa/en columns. UoM Name/ShortName exist for any active language.

## Rounding

### LOCK-ROUND-001 — One GlobalRoundingMode
Store-level Floor / Ceiling / Nearest only. No Product- or Offer-specific rounding mode.

### LOCK-ROUND-002 — Quantity vs money
Quantity normalization uses `IQuantityNormalizer` (places + optional Step). Financial rounding uses the same GlobalRoundingMode at money precision. Do not mix Step into money.

### LOCK-ROUND-003 — Historical documents
Changing GlobalRoundingMode affects new calculations only. Historical orders/invoices are not recalculated.

## Invoice / Finance

### LOCK-INVOICE-001 — Order-backed invoice
Canonical invoice is the Order projection (`SellerOrder` header + `OrderLine` lines). Do not create a second Invoice module.

### LOCK-INVOICE-002 — Header aggregates
Header stores LineCount (int), TotalQuantity (decimal), Gross, Discount, NetBeforeTax, Tax, Duty, TaxAndDuty, Payable. List/report primary totals read Header, not Line JOIN+SUM.

### LOCK-INVOICE-003 — Finalize then sum
Each Line financial result is rounded once (GlobalRoundingMode + money places). Header sums finalized Line values with no extra sum-only rounding. A new Invoice-level calculation is rounded once, then stored.

### LOCK-INVOICE-004 — Tax and Duty
`TaxAmount` = مالیات. `DutyAmount` = عوارض. `TotalTaxAndDutyAmount` = Tax + Duty. Do not use VatAmount to mean Duty.

### LOCK-INVOICE-005 — Invoice rounding snapshot
Header snapshots `RoundingModeUsed` and `MoneyDecimalPlacesUsed`. Historical invoices stay stable if settings change.

## P09 operations (reference)

### LOCK-OPS-001
Exact line+quantity targeting, lifecycle sequence, cancelled-order precedence, capability-driven actions. No Orders UI regression on touched surfaces.
