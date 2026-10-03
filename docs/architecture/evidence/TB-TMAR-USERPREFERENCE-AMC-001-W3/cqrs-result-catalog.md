# TB-TMAR-USERPREFERENCE-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Customer/Admin preference HTTP dispatches through MediatR `ISender` → Application handlers → `UserPreferenceOperation` → `ApiResponseFactory.From`. Zero `Results.Json` / endpoint `catch (SemanticException)`.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| UpsertUserPreferenceCommand | UpsertUserPreferenceCommandValidator | VALIDATOR_REQUIRED_PRESENT |
| GetUserPreferenceQuery | — | VALIDATOR_NOT_REQUIRED (Actor-only) |
| UpsertUiPreferenceCommand | UpsertUiPreferenceCommandValidator | VALIDATOR_REQUIRED_PRESENT |
| GetUiPreferenceQuery | GetUiPreferenceQueryValidator | VALIDATOR_REQUIRED_PRESENT |

## Error ownership

- Catalog/resource set/resx: `Tooba.UserPreference.Contracts`
- Registration: `UserPreferenceModule`
- Endpoints Errors/Resources: ABSENT
