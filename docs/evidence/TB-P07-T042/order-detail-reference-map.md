# TB-P07-T042 — Order detail reference map

Reference: `SarvNewVerRequirment/reference/Image/ChatGPT Image Sep 1, 2026, 10_41_18 PM.png`

| Reference region | Implementation |
|------------------|----------------|
| 5 KPI cards (items, sellers, total, payment, status) | Top summary cards in `admin-order-detail-screen.tsx` |
| Customer + shipping card | Left/right info cards from order snapshots |
| Payment info card | `AdminPaymentOpsView` block |
| Seller share table (gross/commission/payable/settlement status) | `SellerFinancialTable` from `AdminSellerFinancialView` |
| Financial summary + customer receipt cards | `FinancialSummaryCards` |
| Tabs: summary / seller shares / payments | Local tab state |
| Bottom finance history grid | `AppDataGrid` over `FinancialEvents` |

Data source: extended `AdminOrderDetailPage` from `AdminPanelComposer.GetOrderAsync`.
