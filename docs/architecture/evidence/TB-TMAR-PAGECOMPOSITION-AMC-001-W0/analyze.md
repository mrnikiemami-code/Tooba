# TB-TMAR-PAGECOMPOSITION-AMC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only.

## Ownership

| Surface | Owner |
|---|---|
| Storefront home composition GET | PageComposition.Endpoints.Storefront |
| Admin home composition CRUD/reorder/restore | PageComposition.Endpoints.Admin |
| CQRS queries/commands + presentation composer | PageComposition.Application |
| PageDefinition / PageSection / SectionCatalog | PageComposition.Domain |
| Directory / seed / DbContext | PageComposition.Infrastructure |
| Stable error codes | PageComposition.Contracts.Errors |
| Thin admin authorizer | PageComposition.Endpoints.Admin (+ Host adapter if any) |
| Host PageComposition HTTP | CLOSED_HOST_ZERO (already evacuated) |

## Foreign coupling

- Foreign Application/Infrastructure/Domain: **ZERO**
- No Catalog/Party/Offer project references — self-contained module
- Host development seed call seam allowed

## Blockers for COMPLETE_REFERENCE_PATTERN

1. **Solution Explorer:** projects under flat `/Modules/`; Contracts + Endpoints missing from slnx; need `/Modules/PageComposition/`.
2. **God / root dumps / structure:**
   - Domain root `PageCompositionEntities.cs` (~499 lines: keys, tenant ids, catalog, aggregates)
   - Application root `PageCompositionContracts.cs` + `PageCompositionFailureMapper.cs`
   - Technical-axis `Commands/` / `Queries/` / root `Validators/`
   - Infrastructure root Directory/Seed + root `Migrations/`
3. **Error ownership:** Catalog + resx in Endpoints; codes in Contracts — move catalog/resx to Contracts; register in Infrastructure.
4. **Failure semantics:** Domain throws `InvalidOperationException` with Persian message text; Application maps via `message.Contains` (`PageCompositionFailureMapper`) — must become typed `SemanticException` + `PageCompositionOperation`/`Result`.
5. **API result:** Endpoints use `Results.Json` + catch SemanticException helper — need `IRequest<Result>` + `ApiResponseFactory.From`.
6. **Validator inventory:** only Add + Reorder have validators; remaining requests need exhaustive NO_VALIDATOR / REQUIRED classification.
7. **Structure-Handoff-State:** `REQUIRED`

## Wave plan

| Wave | Focus |
|---|---|
| W1 | `/Modules/PageComposition/` slnx + Contracts/Endpoints |
| W2 | Split Domain/App/Infra physical tree (capability-first) |
| W3 | Contracts catalog/resx + SemanticException/Result + validators + thin endpoints |
| W4 | Structure + Certify + SoT/manifest |

## Microservice extractability

Blocked until Solution Explorer, structure, typed faults (no message parse), Result/API mapping, and Contracts-owned catalog close. Zero foreign module coupling already — strong extractability once quality gates pass.
