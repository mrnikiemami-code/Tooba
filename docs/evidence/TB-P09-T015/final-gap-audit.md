# Final gap audit — TB-P09-T015

## Already green (T013 / T014 / T014-R1)

- Cart / Order / Inventory / Fulfillment / Return / Offer / BulkInquiry quantities are `decimal` / `numeric(18,6)`
- `QuantityNormalizer` + `FinancialRounder` in `QuantityFoundation.cs`
- Product owns Unit / Places / optional Step; Offer owns Min/Max
- One GlobalRoundingMode; no Product/Offer rounding overrides
- Invoice header aggregates persisted (`TotalItemCount` int, `TotalQuantity` decimal)
- Admin list `LineCount` is integer from `SellerOrders.Sum(o => o.TotalItemCount)`
- Invoice print: `تعداد اقلام` = count; `جمع مقدار` only when units match
- UoM admin AppDataGrid + LanguageId; ru-RU fixture exists
- Cart / PDP qty inputs are text + `inputMode=decimal` (no HTML `type=number` step=1)

## Residual gaps found and fixed in T015

1. Customer list `ItemCount` was `decimal` aliased from `Lines.Sum(Quantity)`.
   Now `int` from `SellerOrders.Sum(o => o.TotalItemCount)`; list load dropped line JOIN.
2. Admin fulfillment qty input used `type=number` + `Math.max(1, …)` which blocked pack 0.50 of 0.75.
   Now `inputMode=decimal`, `step=any`, `parseQuantityInput`; `selectableQuantityMax` keeps 0.75.
3. Bulk inquiry qty used HTML `type=number` (browser step 1). Now `inputMode=decimal`.
4. `Step_null_accepts_precision_value` now asserts 1 / 1.2 / 1.25.
5. Inventory EF snapshot still described OnHand/Reserved/Quantity as `int` after T013 SQL migration.
   Snapshot aligned to `numeric(18,6)` (no new migration).

## Display-only / out of scope

- `AdminOrderCompletenessComposer.FormatMoneyFa` `decimal.Round` is display formatting
- Wallet / Settlement / Pricing `decimal.Round` are other financial modules
- Page / grid `parseInt` is pagination / calendar, not product quantity
