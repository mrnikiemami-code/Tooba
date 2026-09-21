# settlement-architecture-guard-audit

Project: Tooba.Settlement.Tests / Architecture/SettlementArchitectureGuardTests.cs

Enforces: physical layout, Contracts-only foreign refs (reject Payment/Order/Returns/Party App/Domain/Infra), no foreign DbContext, Host SettlementDbContext allowlist, Host PartyDbContext Settlement-path=0, endpoints no manual Results.Json/ex.Message, AdminPayoutGridQueryEngine module-owned, clock/id/silent-catch/localized prose clean.

Settlement-Architecture-Guards: ENFORCED
