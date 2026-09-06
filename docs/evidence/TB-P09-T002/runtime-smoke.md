# Runtime smoke

Host `:5088` (SingleStore / alpha.localhost), DevActor admin.

Checkout `01a0451c-fabf-7000-99f0-30d410a58638`:

1. Applied migration `checkout_operational_notes`.
2. POST note → 200 persisted (`01a078c8-…`).
3. GET notes → note present.
4. GET operational-history page=1 → 200, includes operational_note, return/refund, settlement_adjustment (totalCount=15).
5. GET invoice.html → 200 snapshot totals (381500 / line 350000); no settlement commission leakage.
6. GET receipt.html exercised.
7. Customer/storefront paths do not expose internal note body.

FE completeness + ops tests 7/7; AdminOrderCompletenessTests 6/6; recovery guard 3/3.
