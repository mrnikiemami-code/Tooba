# Host DbContext scan — BATCH-005

Production Host references to SettlementDbContext:

| File | Role |
|------|------|
| Settlement/SettlementPanelComposer.cs | ctor injects SettlementDbContext for AdminPayoutGridQueryEngine |
| Grid/AdminPayoutGridQueryEngine.cs | query authority over SettlementDbContext |
| Development/MarketplaceDevelopmentBootstrap.cs | migrate bootstrap (allowlisted pattern) |

CartDbContext production business/query authority in Host: none found beyond bootstrap/migrate patterns for this batch.

Required for COMPLETE: SettlementPanelComposer/AdminPayoutGridQueryEngine must leave Host (or stop taking SettlementDbContext).
