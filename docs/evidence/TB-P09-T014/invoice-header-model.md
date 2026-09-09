# Invoice header model — TB-P09-T014

Canonical invoice remains Order-backed. No `invoices` table.

`SellerOrder` now snapshots: TotalItemCount, TotalQuantity, NetAmountBeforeTax, TotalDutyAmount, TotalTaxAndDutyAmount, RoundingModeUsed, MoneyDecimalPlacesUsed.

Reused: SubtotalSnapshot (gross), DiscountSnapshot, TaxSnapshot, GrandTotalSnapshot (payable).
