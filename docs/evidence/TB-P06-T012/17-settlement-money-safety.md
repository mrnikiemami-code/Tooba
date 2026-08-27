# 17 — Settlement Money Safety

Task: `TB-P06-T012`

All amounts use `decimal` in .NET settlement entries; currency preserved on each entry (`IRR`). Commission snapshot stored on accrual row. No floating-point money in domain. No cross-currency settlement without explicit FX (not implemented).

Frontend displays via `formatSettlementMoney` / Persian digits.
