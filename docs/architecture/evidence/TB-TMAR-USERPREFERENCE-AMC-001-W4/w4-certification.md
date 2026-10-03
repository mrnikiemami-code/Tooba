# TB-TMAR-USERPREFERENCE-AMC-001-W4 — Certification

## Verdict

`COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

## HTTP / CQRS

- Applicability: `HTTP_OWNING`
- Ownership: `MODULE_ENDPOINTS` via `MapUserPreferenceModuleEndpoints`
- CQRS: MediatR via `AddToobaCqrsFoundation` on UserPreference.Application assembly
- Endpoints: thin `ISender` + `ApiResponseFactory.From` — zero `Results.Json` in Endpoints
- Fault mapping: `UserPreferenceOperation` / Domain `SemanticException` → `Result`

## Validator inventory

| Request | Classification |
|---|---|
| UpsertUserPreferenceCommand | VALIDATOR_REQUIRED_PRESENT |
| GetUiPreferenceQuery | VALIDATOR_REQUIRED_PRESENT |
| UpsertUiPreferenceCommand | VALIDATOR_REQUIRED_PRESENT |
| GetUserPreferenceQuery | VALIDATOR_NOT_REQUIRED (Actor-only trust boundary) |

## Boundaries

- Foreign Application/Infrastructure/Domain coupling: **ZERO**
- Cross-module: `Order.Contracts.Fulfillment` session seam in Customer actor resolver only
- Error catalog/resources: `Tooba.UserPreference.Contracts` registered by `UserPreferenceModule`
- Host Preferences: CLOSED_HOST_ZERO (`hostPreferencesAmc`)
- Microservice-extractable: **true**

## Structure handoff

Structure-State `READY_FOR_CERTIFY` in `w4-structure-gate.md` for the same surface.
