# TB-TMAR-PRODUCTQNA-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only.

## Ownership

| Surface | Owner |
|---|---|
| POST `/v1/customer/product-questions` | ProductQnA.Endpoints.Customer |
| GET `/v1/storefront/products/{slug}/questions` | ProductQnA.Endpoints.Storefront |
| Submit / GetPublished CQRS | ProductQnA.Application |
| ProductQuestion / ProductAnswer aggregates | ProductQnA.Domain |
| Directory / seed / DbContext | ProductQnA.Infrastructure |
| Stable error codes | ProductQnA.Contracts.Errors |
| Thin customer actor resolver | ProductQnA.Endpoints.Customer |
| Host ProductQnA HTTP | CLOSED_HOST_ZERO (already evacuated) |

## Foreign coupling

- Foreign Application/Infrastructure/Domain: **ZERO**
- Legal Contracts-only: `Catalog.Contracts.ICatalogReviewProductLookup` in Infrastructure directory
- Host composition/seed seams allowed

## Blockers for COMPLETE_REFERENCE_PATTERN

1. **Solution Explorer:** ProductQnA projects sit under flat `/Modules/` — missing `/Modules/ProductQnA/` folder; Contracts + Endpoints not listed in slnx.
2. **God / root dumps / structure:**
   - Domain root `ProductQuestion.cs` (enums + two aggregates)
   - Application root `ProductQaContracts.cs` (ports + models)
   - Application technical-axis `Commands/` / `Queries/` (not capability-first)
   - Validator co-located inside Commands file
   - Infrastructure root: `ProductQaDirectory.cs`, `ProductQnADevelopmentSeed.cs`
   - Infrastructure `Migrations/` at project root (must be `Persistence/Migrations/`)
3. **Error ownership:** Catalog + resource set live in Endpoints; codes already in Contracts — move catalog/resx to Contracts; register in Infrastructure.
4. **API result:** Endpoints use `Results.Json` + `catch (SemanticException)` instead of `IRequest<Result>` + `ApiResponseFactory.From`.
5. **Validator inventory:** Submit has validator (codes not in Contracts.Errors); GetPublished = NO_VALIDATOR but carries page/pageSize transport input → VALIDATOR_REQUIRED.
6. **Structure-Handoff-State:** `REQUIRED`

## Wave plan

| Wave | Focus |
|---|---|
| W1 | `/Modules/ProductQnA/` slnx grouping + Contracts/Endpoints entries |
| W2 | Split Domain/App/Infra physical tree (capability-first) |
| W3 | Contracts error catalog/resx + Result/`ProductQnAOperation` + validator inventory + thin endpoints |
| W4 | Structure + Certify + SoT/manifest |

## Microservice extractability

Blocked until Solution Explorer, structure, Result/API mapping, and Contracts-owned catalog close. Catalog Contracts lookup remains the only cross-module seam (legal).
