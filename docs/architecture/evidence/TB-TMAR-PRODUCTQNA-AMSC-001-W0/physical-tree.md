# TB-TMAR-PRODUCTQNA-AMSC-001-W0 — Physical tree (before)

Baseline: `HEAD == origin/main == 1fd2ab50`. No production file was moved in W0.

## Module tree (production `.cs` + resources only; `bin`/`obj` excluded)

```text
Modules/ProductQnA/
  Tooba.ProductQnA.Contracts/
    Errors/            ProductQnAErrorCodes.cs
                       ProductQnAErrorCatalogContributor.cs
                       ProductQnAErrorResourceSet.cs
    Resources/         ProductQnAErrors.resx
                       ProductQnAErrors.fa.resx
    Tooba.ProductQnA.Contracts.csproj
  Tooba.ProductQnA.Domain/
    Aggregates/        ProductQuestion.cs
                       ProductAnswer.cs
    Enums/             ProductQuestionStatus.cs
                       ProductAnswerStatus.cs
    Tooba.ProductQnA.Domain.csproj
  Tooba.ProductQnA.Application/
    Composition/       ProductQnAOperation.cs
    Models/            ProductQaModels.cs
    Ports/             IProductQaDirectory.cs
    Customer/Commands/    SubmitProductQuestionCommand.cs
    Customer/Validators/  SubmitProductQuestionCommandValidator.cs
    Storefront/Queries/   GetPublishedQuestionsQuery.cs
    Storefront/Validators/ GetPublishedQuestionsQueryValidator.cs
    Tooba.ProductQnA.Application.csproj
  Tooba.ProductQnA.Infrastructure/
    ProductQnAModule.cs
    Directories/       ProductQaDirectory.cs
    Development/       ProductQnADevelopmentSeed.cs
    Persistence/       ProductQnADbContext.cs
                       ProductQnAOutboxRegistration.cs
      Migrations/      20260826120000_InitialProductQnA.cs
                       20260826120000_InitialProductQnA.Designer.cs
                       ProductQnADbContextModelSnapshot.cs
    Tooba.ProductQnA.Infrastructure.csproj
  Tooba.ProductQnA.Endpoints/
    ProductQnAEndpointModule.cs
    Customer/          ProductQnACustomerEndpoints.cs
                       ProductQnACustomerActorResolver.cs
    Storefront/        ProductQnAStorefrontEndpoints.cs
    Tooba.ProductQnA.Endpoints.csproj
```

## Root allowlist state (as observed)

| Project | root `.cs` present | allowlist verdict |
|---|---|---|
| Contracts | none | compliant (empty allowlist) |
| Domain | none | compliant (empty allowlist) |
| Application | none | compliant (empty allowlist) |
| Infrastructure | `ProductQnAModule.cs` | compliant (composition entry only) |
| Endpoints | `ProductQnAEndpointModule.cs` | compliant (composition entry only) |

## File cohesion (LOC, non-generated)

| File | LOC |
|---|---|
| `Infrastructure/Directories/ProductQaDirectory.cs` | 107 |
| `Domain/Aggregates/ProductQuestion.cs` | 102 |
| `Infrastructure/Persistence/ProductQnADbContext.cs` | 65 |
| `Domain/Aggregates/ProductAnswer.cs` | 63 |
| `Infrastructure/Development/ProductQnADevelopmentSeed.cs` | 49 |

Largest hand-written production file = 107 LOC (ceiling 800, `ARCH-SIZE-001`); no
`tmar-source-size-baseline.json` entry. No god-file, no over-split.

## Solution Explorer grouping

`src/backend/Tooba.slnx` → `<Folder Name="/Modules/ProductQnA/">` containing all five projects
(`Domain`, `Contracts`, `Application`, `Infrastructure`, `Endpoints`). Canonical.

## Structure-Handoff-State

`REQUIRED` — physical placement is verified by W2, not by this wave.
