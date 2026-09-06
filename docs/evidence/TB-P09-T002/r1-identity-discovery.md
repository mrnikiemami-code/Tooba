# TB-P09-T002-R1 — Identity discovery

## Existing contracts reused

- `IOperatorProfileDirectory` (Admin Catalog history / Access Control enrichment already use `GetAsync` + `DisplayName`).
- `IIdentityContactLookup` (Access Control Host enrichment for email/mobile display).
- Host composition pattern in `AccessControlEndpoints.EnrichUserHitsAsync` and `CatalogActorHttpBinding` — resolve display in Host, never Order→Identity SQL JOIN.

## Smallest extension

- `IOperatorProfileDirectory.GetManyAsync` — one EF query for distinct owner ids.
- `IIdentityContactLookup.GetContactsAsync` — one EF query for email/phone identifiers of distinct user ids.

## Boundaries respected

- Order / Checkout modules do not reference Identity DbContext.
- Actor resolution happens only in `AdminOrderCompletenessComposer` (Host).
- No parallel user-directory abstraction.
