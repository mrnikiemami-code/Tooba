# 18 — Settlement Authorization

Task: `TB-P06-T012`

SpiceDB/InMemory guard via `OpenSettlementUseCaseGuard`. Seller endpoints require authorized actor + seller party header; admin endpoints require admin panel access. Foreign seller data denied at Host boundary (`SellerPanelAccess`, `AdminPanelAccess`).

Single-Store edition: settlement handlers not registered; no marketplace leakage.
