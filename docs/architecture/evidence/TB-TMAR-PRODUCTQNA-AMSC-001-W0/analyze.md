# TB-TMAR-PRODUCTQNA-AMSC-001-W0 — Analyze

## Mode

`ARCHITECT_DIRECT_AMSC` — Analyze only. No production code moved in this wave.

Baseline: `branch = main`, `HEAD == origin/main == 1fd2ab50`.

## Target analyzed

`src/backend/Modules/ProductQnA` (5 projects) + its Host composition seams + its AMC-001 evidence/guards.

## Structured state fields

| Field | Value |
|---|---|
| Foundation-State | `FOUNDATION_READY` |
| Ownership-State | `correct` |
| File-Cohesion-State | `COHESIVE` |
| Oversized/God-File-State | none (largest production file `Infrastructure/Directories/ProductQaDirectory.cs` = 107 LOC) |
| Localization-State | `CANONICAL` |
| API-Result-Pattern-State | `CANONICAL` |
| Stable-Error-Code-State | `CATALOGUED` (9 descriptors, single owner) |
| Logging-State | `CANONICAL` (no ad-hoc logging; no telemetry calls in module) |
| Sensitive-Logging-State | `NONE` |
| OpenTelemetry-State | `CANONICAL` |
| Correlation-Trace-State | `CANONICAL` |
| CQRS-State | `COMPLIANT` |
| Validator-Coverage-State | `EXHAUSTIVE` (2/2 endpoint-reachable requests, both validators present) |
| Contracts-Boundary-State | `CLEAN` |
| Cross-Module-Coupling-State | `LEGAL_CONTRACTS_ONLY` |
| Cross-Module-Join-State | `NONE` |
| Persistence-Ownership-State | `CORRECT` (own schema `product_qna`) |
| Endpoint-Ownership-State | `MODULE_OWNED` |
| Host-Residue-State | `ALLOWED_COMPOSITION_ROOT` only (Program.cs seam lines + module list entry) |
| Schema-Migration-State | `UNCHANGED` |
| Behavior-Preservation-Risk | `LOW` |
| Canonical-Reference-Used | Offer / BulkInquiry / Payment / Pricing / Content (`ContractOperationException` + `IsKnown` typed-fault seam); AccessControl / Cart / BulkInquiry (`Validation/` + `*ValidationCodes.cs`); BuildingBlocks (`Result`, `ApiResponseFactory`, `SemanticException`, `ContractOperationException`, `IErrorCatalogContributor`, `IErrorResourceSet`) |
| Final-Disposition | `READY_TO_MIGRATE` |

## Responsibility map

| Responsibility | Owner |
|---|---|
| Storefront `GET /v1/storefront/products/{slug}/questions` route | `Tooba.ProductQnA.Endpoints.Storefront` |
| Customer `POST /v1/customer/product-questions` route | `Tooba.ProductQnA.Endpoints.Customer` |
| Session/actor seam for the customer boundary | `Tooba.ProductQnA.Endpoints.Customer` |
| Submit CQRS command + handler | `Tooba.ProductQnA.Application.Customer.Commands` |
| Published-questions CQRS query + handler | `Tooba.ProductQnA.Application.Storefront.Queries` |
| Transport validators | `Tooba.ProductQnA.Application.{Customer,Storefront}.Validators` |
| Typed-fault → `Result<T>` seam | `Tooba.ProductQnA.Application.Composition` |
| Request/result/page models, directory port | `Tooba.ProductQnA.Application.{Models,Ports}` |
| `ProductQuestion` / `ProductAnswer` aggregates + status enums | `Tooba.ProductQnA.Domain` |
| Directory / DbContext / Outbox / development seed / migrations | `Tooba.ProductQnA.Infrastructure` |
| Stable error codes + catalog + resx | `Tooba.ProductQnA.Contracts` |
| Host composition (DI + route map + module list) | `Tooba.Host` — allowed |

## Ownership map

All responsibilities are in their true owning module. No `MUST_SPLIT` file. No Host business authority.

Host references (verified complete set):

- `Program.cs` — `using Tooba.ProductQnA.Endpoints;`, `AddProductQnAEndpointPresentation()`,
  handler-assembly registration (`SubmitProductQuestionCommand`), `MapProductQnAModuleEndpoints()`
  → `ALLOWED_COMPOSITION_ROOT`.
- `Composition/ToobaModuleComposition.cs` — `new ProductQnAModule()` → `ALLOWED_COMPOSITION_ROOT`.
- `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` — module schema descriptor → `ALLOWED_COMPOSITION_ROOT`.
- No Host `ProductQnA` folder, no Host endpoint, no Host persistence, no Host business policy.

## Current illegal dependencies

**ZERO.** No foreign `*.Application` / `*.Infrastructure` / `*.Domain` reference from any ProductQnA
production file. The only cross-module seam is `Tooba.Catalog.Contracts.Ports.ICatalogReviewProductLookup`
(Contracts-only, narrow `CatalogReviewableProductDto`).

## Cross-module join inventory

**NONE.** `ProductQaDirectory` reads only its own `ProductQnADbContext` and the Catalog Contracts port;
no EF navigation across modules, no cross-schema SQL.

## CQRS / MediatR gaps

**NONE.** Endpoint → `ISender` → `IRequest<Result<…>>` → `IRequestHandler<,>`; MediatR registered through
`AddToobaCqrsFoundation`. No endpoint persistence/directory access.

## Validation classification matrix

| Request | Classification | Validator |
|---|---|---|
| `SubmitProductQuestionCommand` | `VALIDATOR_REQUIRED` | `SubmitProductQuestionCommandValidator` (present, auto-discovered) |
| `GetPublishedQuestionsQuery` | `VALIDATOR_REQUIRED` | `GetPublishedQuestionsQueryValidator` (present, auto-discovered) |

No other endpoint-reachable request exists.

## Localization findings

- Canonical mechanism present: `ProductQnAErrorCodes` (Contracts), `ProductQnAErrorCatalogContributor`
  (Contracts), `ProductQnAErrorResourceSet` (Contracts), `ProductQnAErrors.resx` + `.fa.resx` (Contracts).
- All 9 stable codes have descriptors **and** both-culture resources. No hard-coded Persian/English
  user-facing text. No `ex.Message` classification. No endpoint `Accept-Language` parsing.
- `product_qna.not_found` is a semantic `NotFound`/404 code that is **deliberately not catalogued** as
  module-owned: the shared cross-cutting `not_found` descriptor is Foundation-owned. W1 preserves the
  descriptor registration set exactly (no duplicate ownership introduced).

## API result / error mapping findings

- Endpoints return `api.Created(location, result)` / `api.From(result)` / `api.FromFailure(...)`;
  no `Results.Json` / `Results.BadRequest` / local ProblemDetails builder / endpoint catch-map.
- Expected business failure → typed fault → `ProductQnAOperation.ExecuteAsync` → `Result<T>` →
  `ApiResponseFactory`.

## Logging / sensitive-data findings

- No logging, no `Console.WriteLine`, no second telemetry pipeline, no secrets in the module.
- `AuthorUserId` / `ModeratedByUserId` / `ModerationReason` are internal and never emitted in public DTOs.

## OpenTelemetry / correlation findings

- No custom correlation, no direct `StartActivity`, no manual `traceparent`. Presentation supplies
  `traceId`/`correlationId` through the canonical `IProblemDetailsContextProvider`.
- No cross-module call requiring `IModuleCallTracer` decoration (single Contracts read).

## File cohesion / splitting plan

`COHESIVE`. No file requires splitting. No god-file, no over-split, no duplicate CQRS request shape.

## Exact target paths / namespaces

```text
Modules/ProductQnA/
  Tooba.ProductQnA.Contracts/       Errors/{ProductQnAErrorCodes, ProductQnAErrorCatalogContributor, ProductQnAErrorResourceSet}.cs
                                    Resources/{ProductQnAErrors.resx, ProductQnAErrors.fa.resx}
  Tooba.ProductQnA.Domain/          Aggregates/{ProductQuestion, ProductAnswer}.cs, Enums/{ProductQuestionStatus, ProductAnswerStatus}.cs
  Tooba.ProductQnA.Application/     Composition/ProductQnAOperation.cs
                                    Models/ProductQaModels.cs, Ports/IProductQaDirectory.cs
                                    Validation/ProductQnAValidationCodes.cs
                                    Validation/SubmitProductQuestionCommandValidator.cs
                                    Validation/GetPublishedQuestionsQueryValidator.cs
  Tooba.ProductQnA.Infrastructure/  ProductQnAModule.cs
                                    Directories/ProductQaDirectory.cs
                                    Development/ProductQnADevelopmentSeed.cs
                                    Persistence/{ProductQnADbContext, ProductQnAOutboxRegistration}.cs
                                    Persistence/Migrations/… (InitialProductQnA + Designer + snapshot)
  Tooba.ProductQnA.Endpoints/       ProductQnAEndpointModule.cs
                                    Customer/{ProductQnACustomerEndpoints, ProductQnACustomerActorResolver}.cs
                                    Storefront/ProductQnAStorefrontEndpoints.cs
```

Solution grouping `/Modules/ProductQnA/` already canonical in `Tooba.slnx` (5 projects incl. Contracts + Endpoints).

## Behavior-preservation checklist

Routes `GET /v1/storefront/products/{slug}/questions` and `POST /v1/customer/product-questions`;
`201 Created` + `Location` for submit; request/response DTO semantics; the session seam and
`customer.session.required` 401; domain invariants (product/author/body/display-name limits, Pending→Published/Rejected);
stable codes `product_qna.rejected`, `product_qna.not_found`, `customer.session.required` + the 7
`product_qna.validation.*` codes; localization keys + both-culture text; `product_qna` schema, tables,
columns, indexes, migration IDs; DI lifetimes; outbox registration; Host composition seams.

## Certification blockers (ARCH-COMPLETE-002, current state)

1. **Legacy certification state.** `productQnAAmc001` (AMC-001) records `structureState: READY_FOR_CERTIFY`
   and manifest `structureCertified: true`, but the module has never been certified as AMSC-001 under
   ARCH-COMPLETE-002 (no AMSC SoT record, no Master Recovery checkpoint, no AMSC evidence tree).
2. **Domain fault typing.** Domain throws `SemanticException(new SemanticError(code))`. The dominant
   ARCH-COMPLETE-002 Domain convention (Order, Payment, Pricing, Content, Fulfillment, Notification,
   Media, Party, PageComposition, Localization, CustomerProfile, BulkInquiry) is the code-carrying
   `ContractOperationException` mapped by an `IsKnown`-filtered seam.
3. **Validator-code ownership + placement.** The 7 transport/input-shape codes live in
   `Contracts.Errors.ProductQnAErrorCodes` and are registered as `ErrorDescriptor`s in the module
   catalog, while the validators sit under the capability technical axes
   (`Customer/Validators`, `Storefront/Validators`). The repository's current AMSC precedent keeps
   transport validator codes in `Application/Validation/<Module>ValidationCodes.cs` and does **not**
   register them in the error catalog (AccessControl, Cart, BulkInquiry, Offer, Order, Payment) — the
   canonical `ValidationBehavior` maps them through the Foundation `validation.failed` descriptor.
4. **SoT/manifest honesty.** No AMSC-001 SoT records, no Master Recovery checkpoint, no
   ARCH-COMPLETE-002 durable guard for the AMSC lineage, no AMSC evidence tree.

## Wave plan

| Wave | Focus |
|---|---|
| W1 Migrate | Domain/Directory typed faults → `ContractOperationException(ProductQnAErrorCodes.Rejected)`; `ProductQnAErrorCodes` gains `KnownCodes` + `IsKnown(string?)`; `ProductQnAOperation` maps `ContractOperationException` under `IsKnown` + `SemanticException`; 7 validation codes → `Application/Validation/ProductQnAValidationCodes.cs`; validators → `Application/Validation/`; catalog contributor keeps exactly 2 semantic descriptors; guard updates |
| W2 Structure | Canonical `Application/Validation/` placement verification; solution grouping verification; structure guard + structure evidence; manifest allowlist reconciliation |
| W3 Certify | ARCH-COMPLETE-002 certification; AMSC SoT records + manifest promotion note; Master Recovery checkpoint; durable cert guard; certification evidence |

## Structure-Handoff-State

`REQUIRED` — W2 owns the physical/visual gate; this analysis does not claim final structural readiness.

## Microservice extractability

Already strong: own `product_qna` schema, own migrations, own outbox registration, zero foreign
Application/Infrastructure/Domain coupling, Contracts-only Catalog seam, module-owned endpoints, Host
composition-only residue. Remaining extraction work is zero-coupling-preserving quality closure
(typed fault mechanism, validator-code ownership, AMSC certification state).
