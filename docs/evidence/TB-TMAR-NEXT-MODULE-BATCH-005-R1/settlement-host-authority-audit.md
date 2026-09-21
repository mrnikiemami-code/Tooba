# settlement-host-authority-audit

## Before (parent BATCH-005 residuals)

- Host SettlementPanelComposer injected SettlementDbContext + PartyDbContext
- Host AdminPayoutGridQueryEngine owned DB-native payout grid query
- Host constructed AdminPayoutGridQueryEngine inside composer

## After R1

- SettlementPanelComposer deleted
- AdminPayoutGridQueryEngine moved to Tooba.Settlement.Infrastructure/Queries
- Host SettlementEndpoints uses ISender + ApiResponseFactory only
- Host SettlementDbContext hits: MarketplaceDevelopmentBootstrap.cs (bootstrap migrate), Program.cs if any resolve via DI for bootstrap only
- Host PartyDbContext on Settlement path: 0

## Verdict

Settlement-Host-DbAuthority: NONE for production business/query
