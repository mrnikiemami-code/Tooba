# 08 — Settlement Statement

Task: `TB-P06-T012`

Minimum `SettlementStatement` model with period start/end, opening/closing balance, credits/debits totals. Seller API: `GET /v1/seller/settlement/statements`. Statements close on operational cadence; no monthly-only lock-in.

Implementation: `Tooba.Settlement.Domain` + `SettlementDirectory.ListStatementsAsync`.
