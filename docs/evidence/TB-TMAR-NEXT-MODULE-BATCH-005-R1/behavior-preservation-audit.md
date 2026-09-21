# behavior-preservation-audit

- Seller balance/entries/statements/payout list/request: directory ports unchanged; HTTP via CQRS Result
- Admin balances + seller display names: same shape AdminSettlementBalanceListItem (moved to Settlement.Application.Models)
- Payout grid: same Pending|Failed scope, filters/sort/search/paging; Party names via IPartyLookup
- Process/retry payout: same directory ExecutePayoutAsync
- Domain/Infra exception messages → stable codes (behavior of throw type preserved; Host presentation now ApiResponseFactory)
- Cart: exception message codes only; no Cart redesign

Behavior-Preservation: VERIFIED (focused suite)
