# settlement-party-boundary

- Seller display names via Tooba.Party.Contracts.IPartyLookup.GetDisplayNamesAsync
- AdminPayoutGridQueryEngine and ListAdminSettlementBalancesQueryHandler consume IPartyLookup only
- PartyDbContext references on Settlement path: 0
- No cross-module SQL/JOIN

Settlement-Party-Boundary: CONTRACTS_ONLY
