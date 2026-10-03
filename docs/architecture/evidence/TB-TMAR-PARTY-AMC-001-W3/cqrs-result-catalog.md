# TB-TMAR-PARTY-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Seller settings GET/PUT and Admin sellers list/query dispatch through MediatR `ISender` → Application handlers → `PartyOperation` (`SemanticException` → `Result`) where applicable → `ApiResponseFactory.From`. Zero `Results.Json`. Zero `catch (InvalidOperationException)`.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| `GetSellerSettingsQuery` | — | NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT |
| `UpdateSellerSettingsCommand` | `UpdateSellerSettingsCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `ListAdminSellersQuery` | — | NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT |
| `QueryAdminSellersGridQuery` | `QueryAdminSellersGridQueryValidator` | VALIDATOR_REQUIRED_PRESENT |

## Error ownership

- Catalog/resource set/resx: `Tooba.Party.Contracts`
- Registration: `PartyModule` (Infrastructure)
- Endpoints Errors/Resources: ABSENT
- Wire parity retained: `seller.settings.missing` / `seller.settings.rejected`
- Typed foundation faults: `party.operation.rejected`

## Coupling

Zero foreign Application/Infrastructure/Domain. Cross-module Admin sellers composition remains Offer/Order/Party Contracts ports only.
