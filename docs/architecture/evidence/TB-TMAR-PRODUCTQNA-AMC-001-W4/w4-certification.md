# TB-TMAR-PRODUCTQNA-AMC-001-W4 — Certification

## Verdict

`COMPLETE_REFERENCE_PATTERN` + `ARCH-COMPLETE-002 STRUCTURE_CERTIFIED`

## HTTP / CQRS

- Applicability: `HTTP_OWNING`
- Ownership: `MODULE_ENDPOINTS` via `MapProductQnAModuleEndpoints`
- CQRS: MediatR via `AddToobaCqrsFoundation(SubmitProductQuestionCommand assembly)`
- Endpoints: thin `ISender` + `ApiResponseFactory.Created` / `From` — zero `Results.Json`
- Fault mapping: `ProductQnAOperation` / `SemanticException` → `Result`

## Validator inventory

| Request | Classification |
|---|---|
| SubmitProductQuestionCommand | VALIDATOR_REQUIRED_PRESENT |
| GetPublishedQuestionsQuery | VALIDATOR_REQUIRED_PRESENT |

## Boundaries

- Foreign Application/Infrastructure/Domain coupling: **ZERO**
- Cross-module: `Catalog.Contracts.ICatalogReviewProductLookup` only
- Error catalog/resources: `Tooba.ProductQnA.Contracts` registered by `ProductQnAModule`
- Host ProductQnA: CLOSED_HOST_ZERO
- Microservice-extractable: **true**

## Structure handoff

Structure-State `READY_FOR_CERTIFY` in `w4-structure-gate.md` for the same surface.
