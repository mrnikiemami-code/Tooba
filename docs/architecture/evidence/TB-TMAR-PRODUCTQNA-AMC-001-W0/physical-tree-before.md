# ProductQnA physical tree (before AMC-001)

```
Modules/ProductQnA/
  Tooba.ProductQnA.Domain/
    ProductQuestion.cs                    # enums + ProductQuestion + ProductAnswer
  Tooba.ProductQnA.Application/
    ProductQaContracts.cs                 # models + IProductQaDirectory
    Commands/SubmitProductQuestionCommand.cs  # command + handler + validator
    Queries/GetPublishedQuestionsQuery.cs
  Tooba.ProductQnA.Contracts/
    Errors/ProductQnAErrorCodes.cs
  Tooba.ProductQnA.Infrastructure/
    ProductQnAModule.cs
    ProductQaDirectory.cs
    ProductQnADevelopmentSeed.cs
    Persistence/ProductQnADbContext.cs
    Migrations/…                          # root Migrations (non-canonical)
  Tooba.ProductQnA.Endpoints/
    ProductQnAEndpointModule.cs
    Customer/…
    Storefront/…
    Errors/ProductQnAErrorCatalogContributor.cs
    Resources/…
```

slnx: Domain/Application/Infrastructure under flat `/Modules/`; Contracts + Endpoints absent from solution folders.
