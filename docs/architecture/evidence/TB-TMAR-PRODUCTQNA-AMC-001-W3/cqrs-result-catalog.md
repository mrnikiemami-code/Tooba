# TB-TMAR-PRODUCTQNA-AMC-001-W3 — CQRS / Result / catalog

## Outcome

Customer submit + Storefront published GET dispatch through MediatR `ISender` → Application handlers → `ProductQnAOperation` (`SemanticException` → `Result`) → `ApiResponseFactory.Created` / `From`. Zero `Results.Json`. Zero endpoint `catch (SemanticException)`.

## Inventory

| Request | Validator | Classification |
|---|---|---|
| `SubmitProductQuestionCommand` | `SubmitProductQuestionCommandValidator` | VALIDATOR_REQUIRED_PRESENT |
| `GetPublishedQuestionsQuery` | `GetPublishedQuestionsQueryValidator` | VALIDATOR_REQUIRED_PRESENT |

## Error ownership

- Catalog/resource set/resx: `Tooba.ProductQnA.Contracts`
- Registration: `ProductQnAModule` (Infrastructure)
- Endpoints Errors/Resources: ABSENT
- `customer.session.required` remains Foundation-owned (not re-registered)

## Coupling

Zero foreign Application/Infrastructure/Domain. Catalog lookup remains `Catalog.Contracts` only.
