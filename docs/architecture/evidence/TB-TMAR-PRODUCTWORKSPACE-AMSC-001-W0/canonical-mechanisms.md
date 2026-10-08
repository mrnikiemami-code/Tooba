# TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W0 — Canonical Mechanism Discovery

Discovered from current repository state (not assumed). Reference modules used per concern are named explicitly.

## 1. API result / error mapping

| Mechanism | Location | State |
|---|---|---|
| `Result` / `Result<T>` + `SemanticError` | `Tooba.BuildingBlocks.Results` | used by all 4 ProductWorkspace queries |
| `ApiResponseFactory` (`From`, `From<T>`, `Created`, `FromFailure`, `FromException`) | `Tooba.BuildingBlocks.Presentation` | injected into every ProductWorkspace endpoint |
| `SafeErrorMapper` + `IErrorDefinitionCatalog` + `ErrorDescriptor` + `IErrorCatalogContributor` | `Tooba.BuildingBlocks.Presentation.Errors` | Catalog owns the `workspace.*` descriptors |
| `IProblemDetailsContextProvider` | `Tooba.BuildingBlocks.Presentation.ProblemDetails` | supplies `traceId`/`correlationId`/`requestId` |
| `ContractOperationException(string code)` | `Tooba.BuildingBlocks` | typed code-carrying fault for Domain/Infrastructure |

`SafeErrorMapper.Map(SemanticError)` behaviour that constrains W1: a **registered** code maps to its catalog `HttpStatus`; an **unregistered** code falls back to `400 Business` with `LocalizationKey = code`. Therefore moving a code from "registered" to "unregistered" is only behaviour-preserving when the catalog status is already `400`.

`ErrorDefinitionCatalog` fails fast on `duplicate_error_descriptor:<code>`; duplicate suppression is forbidden.

## 2. Localization

- machine-stable codes: `Tooba.<Module>.Contracts.Errors.<Module>ErrorCodes`
- validation codes: `Tooba.<Module>.Application.Validation.<Module>ValidationCodes` (AccessControl, Cart, BulkInquiry, Offer, Order, Payment precedent) — **not** registered in the error catalog; the canonical `ValidationBehavior` maps them through the Foundation `validation.failed` descriptor.
- descriptor ownership: `IErrorCatalogContributor` + `IErrorResourceSet` + `Errors.resx` / `Errors.fa.resx`
- `IErrorMessageLocalizer` / `ResourceErrorMessageLocalizer` / `IRequestLocaleResolver`

## 3. Typed-fault seam for a composing module

The repository's established ARCH-COMPLETE-002 seam for a module that composes other modules' faults is an `IsKnown`-filtered catch (ProductQnA `ProductQnAOperation`, OperatorProfile `OperatorProfileOperation`):

```csharp
catch (ContractOperationException ex) when (<Module>ErrorCodes.IsKnown(ex.Code))
{
    return Result.Failure<T>(new SemanticError(ex.Code));
}
```

Classification is typed-code-only; message/prose heuristics are forbidden; unknown codes propagate to the global exception boundary.

## 4. Cross-module operation port pattern (module BFF / composed mutation)

The repository's established Contracts-only pattern for "module A owns the HTTP surface, module B owns the write capability":

| Port | Owner | Consumer | Implementation |
|---|---|---|---|
| `IFulfillmentAdminOperations` | `Tooba.Fulfillment.Contracts.Operations` | `Order.Application`, `Order.Infrastructure` | `Fulfillment.Infrastructure/Adapters/FulfillmentAdminOperationsAdapter` |
| `IAdminOrderFulfillmentOperations` | `Tooba.Order.Contracts.Fulfillment` | Fulfillment work-queue bulk | `Order.Infrastructure/Admin/Fulfillment/AdminOrderFulfillmentOperations` |
| `IReturnAdminOperations` | `Tooba.Returns.Contracts.Operations` | `Order` | `Returns.Infrastructure/Adapters/ReturnAdminOperationsAdapter` |

Rules observed in all three: contracts expose narrow DTOs/enums only (never EF/domain/Application types); the port is implemented by the owning module's Infrastructure; the consumer resolves it by DI; expected failures surface as stable codes / typed faults, never as foreign Application exceptions; the consumer never references the owner's `Application`/`Infrastructure`/`Domain`.

## 5. Cross-module read seam already used by ProductWorkspace

`Catalog.Contracts.Ports.ICatalogAdminProductWorkspaceReadGateway` / `…ListGateway` return semantic projections (`CatalogAdminProductWorkspaceSnapshot`, `CatalogAdminProductListSlice`) and are implemented by `Catalog.Infrastructure/Directories/*`, registered in `CatalogModule.AddServices`. This is the model the W1 write seam must mirror.

## 6. CQRS foundation

`ToobaCqrsRegistration.AddToobaCqrsFoundation` (MediatR 12.5.0, `ValidationBehavior`, `LoggingBehavior`, `TracingBehavior`). Endpoints dispatch with `ISender`; Application registers validators via `AddValidatorsFromAssembly`.

## 7. Actor context

`Catalog.Application.Shared.ICatalogActorContext` is an Application-internal scoped port registered by `CatalogModule` (`AddScoped<ICatalogActorContext, CatalogActorContext>()`) and consumed by Catalog directories for history attribution. `ProductWorkspace.Endpoints` currently resolves it from `RequestServices` — a foreign-Application dependency.

## 8. Logging / observability

`ILogger<T>` + `ObservabilityLogScope` / `ObservabilityLogScopeKeys`; `ToobaTelemetry` (`ActivitySource`/`Meter` named `Tooba`); `IModuleCallTracer` for cross-module calls; `CorrelationIdConstants.HeaderName = X-Correlation-Id` via `ICorrelationIdProvider`. No second pipeline anywhere in ProductWorkspace (the module has no logging calls at all).

## 9. Architecture guards

`TmarSourceSizeGuard` + `Baselines/tmar-source-size-baseline.json`, `ARCH-SIZE-001`, `ARCH-MODULE-FILE-001`, `TmarCompleteReferenceStructureGateTests`, `TmarDurableGuardTests`, plus the Host Admin AMC guard family (`HostAdminAmcW13…W33`) that currently pins ProductWorkspace's **Catalog-command shape**.

## 10. Manifest / SoT vocabulary

`docs/architecture/tmar-module-structure-manifests.json` — top-level keys `modules` (certified), `uncertifiedHttpOwningModules`, `preCertModules`. `ProductWorkspace` currently sits in `preCertModules` with `structureCertified: false`.
`docs/architecture/tmar-current-state.json` — `structureLock.certifiedModules` (25 modules today) plus per-task checkpoint blocks.
