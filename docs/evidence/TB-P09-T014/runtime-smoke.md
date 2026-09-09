# Runtime smoke — TB-P09-T014

Host `:5088` (`Host: alpha.localhost`). Postgres `tooba_alpha`. Actor admin `01a036c2-970e-7000-8eb7-94bf5cc2d8db`. Raw: `runtime-raw.json`.

## UoM

Created Language Registry locale `ru-RU` (`01a0844c-829e-7000-ab25-64b381896053`).
PUT kg translations include `килограмм` / `кг`.
GET `/v1/admin/catalog/units/?language=ru-RU` resolves kg dynamically (no fa/en columns).

## Product

`01a05387-fbd0-7000-acd3-4382ce92c773` unit kg, DecimalPlaces=2, Step=null. Saved 200.

## Offer

Seller Arman offer `01a030d1-40f1-7000-95f6-b8efc58e2619`: Min=0.50 Max=20.00. ProductUnit* fields on contract; this demo offer has no ProductId so unit label is empty. No unit selector.

## Rounding

PUT Floor → `رو به پایین`. Helper mentions historical invoices. After smoke restored Nearest.

## Invoice math (Floor, money places 0)

Checkout `01a08450-f2b0-7000-816d-552bb5fd54cd` (also prior `01a08450-62a7-7000-b69a-397f01bf019b`):

Gross 998, 20% → Discount 199, Net 799, Tax 71 (Floor of 71.91), Duty 0, TaxAndDuty 71, Payable 870.
Header: `Floor|0|998|199|799|71|0|71|870|1|1`.

Invoice HTML includes تعداد اقلام / عوارض / جمع مالیات و عوارض.

## Historical

Changed GlobalRoundingMode to Ceiling. Same checkout header stayed Floor / 199 / 799.

## Migration

`MigrationRunner apply --tenant store-alpha`: Order current `20260909140000_AddInvoiceHeaderAggregates`, pending 0. No wipe.
