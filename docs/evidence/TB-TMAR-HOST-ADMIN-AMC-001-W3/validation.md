# Validation — W3

| Request | Classification | Validator |
|---|---|---|
| `ListUnitOfMeasuresQuery` | NO_VALIDATOR_REQUIRED | none |
| `GetUnitOfMeasureQuery` | NO_VALIDATOR_REQUIRED | none (route Guid) |
| `CreateUnitOfMeasureCommand` | VALIDATOR_REQUIRED | Code/Dimension non-empty; Translations present; Name/ShortName non-empty |
| `UpdateUnitOfMeasureCommand` | VALIDATOR_REQUIRED | UnitId + same write shape |
| `DeactivateUnitOfMeasureCommand` | NO_VALIDATOR_REQUIRED | none |

Business uniqueness / dimension enum / language existence remain in Directory `Result` failures (not FluentValidation).
