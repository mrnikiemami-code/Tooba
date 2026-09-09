# Reuse vs change — TB-P09-T014

Reuse:
- UoM / Product policy / Offer min-max / GlobalRoundingMode from T013
- Language Registry + AppDataGrid + Dialog
- Seller offer form and Product workspace
- SellerOrder as invoice header; Admin invoice HTML composer
- Promotion/Tax pipelines (pass GlobalRoundingMode)

Change:
- New UoM admin API/UI
- Product unit options include current inactive unit; FE step-precision message
- Offer shows Product unit; FE Min≤Max
- Rounding helper mentions invoices (FA+EN)
- Additive header/duty/snapshot columns + FinancialRounder
- Promo/tax use FinancialRounder; header sums finalized lines
- List/report quantity from Header
