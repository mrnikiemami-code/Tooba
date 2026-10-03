# TB-TMAR-OPERATORPROFILE-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Admin operator profile GET/PUT dispatch through MediatR `ISender` → Application handlers → `OperatorProfileOperation` (`SemanticException` → `Result`) → `ApiResponseFactory.From`. Zero `Results.Json`.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| `GetOperatorProfileQuery` | `GetOperatorProfileQueryValidator` | VALIDATOR_REQUIRED_PRESENT |
| `UpsertOperatorProfileCommand` | `UpsertOperatorProfileCommandValidator` | VALIDATOR_REQUIRED_PRESENT |

## Error ownership

- Catalog/resource set/resx: `Tooba.OperatorProfile.Contracts`
- Registration: `OperatorProfileModule`

## Coupling

Zero foreign Application/Infrastructure/Domain. Cross-module display remains `Contracts.Ports.IActorDisplayLookup`.
