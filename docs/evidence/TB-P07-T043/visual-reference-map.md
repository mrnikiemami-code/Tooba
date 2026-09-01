# TB-P07-T043 — Visual reference map

Reference: `SarvNewVerRequirment/reference/Image/ChatGPT Image Sep 1, 2026, 10_41_18 PM.png`

| Reference region | Implementation target |
| --- | --- |
| Compact breadcrumb + dominant title + inline order ref | `header` block in `admin-order-detail-screen.tsx` |
| Five KPI cards, equal height, icon right, badge inline | `SummaryCard` (`min-h-[104px]`, compact padding) |
| Two balanced info cards (customer / payment) | Equal `min-h-[220px]` sections with divider rows |
| Financial tabs integrated in card chrome | `بخش مالی سفارش` header row with segmented tabs |
| Seller breakdown table + dual summary panels | `SellerFinancialTable` + `FinancialSummaryCards` |
| Bottom finance history grid | Compact section header + canonical `AppDataGrid` |

No new sections or data fields introduced.
