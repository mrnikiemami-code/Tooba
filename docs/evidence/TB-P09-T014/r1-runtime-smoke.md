# Runtime smoke — TB-P09-T014-R1

Host `:5088` from `src/backend/Host/Tooba.Host`. `Host: alpha.localhost`.

Checkout `01a08175-306a-7000-a19c-5c2b294a9004` has 3 lines.

Temporary header quantity 5.25 (one line 1.25) then restored to original 2+3+1 / TotalQuantity 6.

Verified:

- Header `TotalItemCount` = 3 (integer), `TotalQuantity` = 5.25 (decimal)
- `GET /v1/admin/orders/{id}` `lineCount` = 3; seller financial `lineCount` = 3
- `POST /v1/admin/orders/query` `lineCount` = 3 (not 5.25)
- Invoice HTML `تعداد اقلام` = 3; `جمع مقدار` = 5.25; no `.000000`
- Duty remains 0; `TotalTaxAndDutyAmount` = `TaxSnapshot` + 0
- Shared empty unit snapshot allowed `جمع مقدار`; mixed units omit it (unit test)

DB restored after smoke.
