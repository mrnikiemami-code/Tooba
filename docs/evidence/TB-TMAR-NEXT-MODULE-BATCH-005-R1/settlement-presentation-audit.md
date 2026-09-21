# settlement-presentation-audit

- Expected failures: Result/SemanticError + SettlementErrorCatalogContributor
- Codes: settlement.account.missing, settlement.payout.rejected/invalid_amount/missing/invalid_state, idempotency, accrual/refund codes
- ApiResponseFactory maps Result failures to ProblemDetails
- PlatformHttpException only for auth/access (mapped via SemanticError platform.http)

Settlement-Error-Presentation: CENTRALIZED
