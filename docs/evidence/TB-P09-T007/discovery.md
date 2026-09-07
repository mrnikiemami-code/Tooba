# Discovery

Order Detail already had `FinancialEvents` + `تاریخچه عملیات` on the same page.

Gaps vs T007:

- financial projection missed customer refund movements and used settlement accrual labels that did not match locked FA wording
- operational summaries lacked seller / shipment method / line / quantity scope text
- FE type badges said «دریافت مشتری» / «تسویه فروشنده» instead of locked labels

Approach: bounded Host projection over Payment ops + Returns refund attempts + SettlementEntry by SellerOrderId (no cross-module JOIN, no new ledger, no PayoutRequest batch total on Order Detail).
