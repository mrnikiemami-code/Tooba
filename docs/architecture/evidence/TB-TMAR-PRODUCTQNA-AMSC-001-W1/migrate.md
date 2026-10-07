# TB-TMAR-PRODUCTQNA-AMSC-001-W1 — Migrate

## Verdict

`READY_FOR_CERTIFICATION` (behavior-preserving canonicalization; physical/structure gate owned by W2)

Baseline: `branch = main`, `HEAD == origin/main`, parent wave `TB-TMAR-PRODUCTQNA-AMSC-001-W0`
(Analyze verdict `READY_TO_MIGRATE`). No route, status code, response shape, stable error code,
localization key, schema, migration or domain-rule change.

## Scope

Canonicalize the module's cross-cutting seams so the module is extractable as a microservice with zero
foreign coupling. Four bounded repairs, all from the W0 blocker list.

## 1. Validator-code ownership + placement

The 7 transport/input-shape codes lived in `Contracts.Errors.ProductQnAErrorCodes` **and** were
registered as `ErrorDescriptor`s in the module catalog, while validators sat under the capability
technical axes (`Customer/Validators`, `Storefront/Validators`). Repository AMSC precedent
(AccessControl, Cart, BulkInquiry, Offer, Order, Payment) keeps transport validator codes in
`Application/Validation/<Module>ValidationCodes.cs` and does **not** register them in the error
catalog; the canonical `ValidationBehavior` maps them through the foundation `validation.failed`
descriptor.

| Before | After |
|---|---|
| `Contracts/Errors/ProductQnAErrorCodes.cs` (9 codes) | `Contracts/Errors/ProductQnAErrorCodes.cs` (2 semantic codes: `product_qna.rejected`, `product_qna.not_found` + `SessionRequired` foundation reference) |
| `Contracts/Errors/ProductQnAErrorCatalogContributor.cs` (9 descriptors) | 2 descriptors (`Rejected` 400/Validation, `NotFound` 404/NotFound) |
| `Application/Customer/Validators/SubmitProductQuestionCommandValidator.cs` | `Application/Validation/SubmitProductQuestionCommandValidator.cs` |
| `Application/Storefront/Validators/GetPublishedQuestionsQueryValidator.cs` | `Application/Validation/GetPublishedQuestionsQueryValidator.cs` |
| — | `Application/Validation/ProductQnAValidationCodes.cs` (7 codes, unchanged string values) |

The 7 stable code string values are byte-identical
(`product_qna.validation.{actor_required,body_required,product_required,question_body_required,slug_required,page_invalid,page_size_invalid}`),
and the `.resx` / `.fa.resx` entries for them are **unchanged** (localization data preserved; the
catalog contributor only stopped *registering descriptors* for them, the resource keys remain).

`ProductQnAErrorCodes` also gained the declared-code guard used by the composition seam:

```csharp
private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal) { Rejected, NotFound };

public static bool IsKnown(string? code) =>
    !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);
```

## 2. Typed Domain/Infrastructure faults

Domain threw `SemanticException(new SemanticError(code))`. The dominant ARCH-COMPLETE-002 Domain
convention (Order, Payment, Pricing, Content, Fulfillment, Notification, Media, Party, PageComposition,
Localization, CustomerProfile, BulkInquiry) is the typed, code-carrying `ContractOperationException`
mapped by an `IsKnown`-filtered seam. W1 switched the Domain/Directory fault type to that mechanism.

| Before | After |
|---|---|
| `Domain/Aggregates/ProductQuestion.cs` throws `SemanticException(new SemanticError(ProductQnAErrorCodes.Rejected))` ×6 | throws `ContractOperationException(ProductQnAErrorCodes.Rejected)` via one private `Rejected()` helper |
| `Domain/Aggregates/ProductAnswer.cs` throws `SemanticException(new SemanticError(...))` ×3 | throws `ContractOperationException(ProductQnAErrorCodes.Rejected)` via one private `Rejected()` helper |
| `Infrastructure/Directories/ProductQaDirectory.cs` throws `SemanticException(new SemanticError(...))` | throws `ContractOperationException(ProductQnAErrorCodes.Rejected)` via one private `Rejected()` helper |
| `Application/Composition/ProductQnAOperation.cs` maps only `SemanticException` | maps `ContractOperationException` **when the code is a declared ProductQnA code**, plus `SemanticException` for Application/Infrastructure guard sites; unknown codes/exceptions propagate untouched to the canonical global exception boundary |

```csharp
catch (ContractOperationException ex) when (ProductQnAErrorCodes.IsKnown(ex.Code))
{
    return Result.Failure<T>(new SemanticError(ex.Code));
}
catch (SemanticException ex)
{
    return Result.Failure<T>(ex.Error);
}
```

Classification remains typed-code-only — never message/prose heuristics. `ProductQnA.Domain` keeps its
legitimate **self-module** project reference to `Tooba.ProductQnA.Contracts` for the single stable
constant `ProductQnAErrorCodes.Rejected` (self-module layering, not cross-module coupling; foreign
App/Infra/Domain coupling remains ZERO).

## 3. Guard + behavior-test alignment

| File | Change |
|---|---|
| `ProductQnAModuleAmcW2StructureGuardTests` | validator path → `Application/Validation/` |
| `ProductQnAModuleAmcW3CqrsGuardTests` | validator namespaces → `Application.Validation` |
| `ProductQnAModuleAmcW4CertGuardTests` | capability-folder assertion → `Application/Validation/`; `Customer/Validators` + `Storefront/Validators` must not return |
| `ProductQnAModuleAmsc001W1MigrateGuardTests` (new) | W1 durable locks: declared-code guard + no transport codes in Contracts; Application-owned validation codes + no catalog registration; single `Validation/` folder + DI discoverability; typed code-carrying Domain/Directory faults; known-code + `SemanticException`-only composition seam; Contracts-only project references |

The old AMC guards were **repointed, never weakened**: every remaining assertion is kept verbatim and no
test count decreased.

## Behavior preservation

| Surface | State |
|---|---|
| Routes | `GET /v1/storefront/products/{slug}/questions`, `POST /v1/customer/product-questions` — unchanged |
| Status codes | `201 Created` + `Location` on submit; `400` rejected; `404` not-found; `401 customer.session.required` — unchanged |
| Stable error codes | 2 semantic + 7 transport string values byte-identical |
| Localization keys + both-culture text | unchanged (EN + FA) |
| DTO / response shape | unchanged |
| Domain rules | unchanged (same guards, same rejection points, same messages none) |
| DI lifetimes / outbox registration | unchanged |
| Schema / migrations | `UNCHANGED`, `migrationFilesChanged = 0` |
| Host composition seams | unchanged (`ALLOWED_COMPOSITION_ROOT` only) |
| Frontend | `FROZEN_UNTOUCHED` |

## Zero-coupling / microservice extractability

- Foreign `*.Application` / `*.Infrastructure` / `*.Domain` coupling: **ZERO** (unchanged).
- The only cross-module seam remains `Tooba.Catalog.Contracts.Ports.ICatalogReviewProductLookup`
  (Contracts-only, narrow `CatalogReviewableProductDto`).
- Cross-module EF join / foreign DbSet / foreign schema read: **NONE**.
- Own `product_qna` schema, own `ProductQnADbContext`, own migrations, own outbox registration.
- Module-owned endpoints; Host HTTP ownership ZERO.

W1 changes only in-module seams, so extractability is preserved and strictly improved (validator-code
ownership and fault typing are now module-internal concerns with no Contracts/Foundation coupling
beyond `ContractOperationException`).

## Focused validation

```text
dotnet test Tooba.Host.Tests --filter FullyQualifiedName~ProductQnAModuleAmsc001W1MigrateGuardTests
  Passed: 6, Failed: 0, Skipped: 0

dotnet test Tooba.Host.Tests --filter FullyQualifiedName~ProductQnA
  Passed: 27, Failed: 0, Skipped: 2 (Postgres Testcontainers)
```

## Residual debt

- Physical/structure normalization is **not** claimed here; `Structure-Handoff-State = REQUIRED`.
- W2 owns `Application/Validation/` placement verification, solution grouping verification, structure
  evidence, manifest allowlist reconciliation and the durable structure guard.
- W3 owns ARCH-COMPLETE-002 certification, SoT/manifest promotion, Master Recovery checkpoint and the
  durable cert guard.

## Completion state

`READY_FOR_CERTIFICATION` with `Structure-Handoff-State = REQUIRED` (W2 must return
`Structure-State = READY_FOR_CERTIFY` before W3).
