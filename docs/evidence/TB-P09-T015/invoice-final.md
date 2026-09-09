# Invoice final — TB-P09-T015

Canonical invoice is the Order projection (LOCK-INVOICE-001).

## Header fields

- `TotalItemCount` / LineCount → `int`
- `TotalQuantity` → `decimal`
- Gross / Discount / NetBeforeTax / Tax / Duty / TaxAndDuty / Payable
- `RoundingModeUsed`, `MoneyDecimalPlacesUsed`

Invariants:

- `TotalTaxAndDutyAmount = TotalTaxAmount + TotalDutyAmount`
- `TotalItemCount` = number of lines, never aliased to `TotalQuantity`
- Mixed-unit TotalQuantity is not shown with a shared unit (`InvoiceHeaderSemantics`)

## Floor example

Gross 998, 20%, Floor, money places 0:

- RawDiscount 199.6
- DiscountAmount 199
- LineNet 799
- Header sums finalized line values (no second sum-only rounding)

T014 Floor fixture header: `Floor|0|998|199|799|71|0|71|870|1|1`

Customer list `ItemCount` now uses header `TotalItemCount` (T015 residual).
