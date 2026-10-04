# TB-TMAR-CART-AMSC-001-W0 — Canonical Mechanisms (Discovered)

All entries were **discovered by reading current repository state** at
`HEAD = 17e95804aa0f261a2a98f55278f671aafd5e41d9`. No mechanism below was assumed from memory.

| # | Concern | Canonical mechanism (exact type) | Owning location | Cart current state |
| --- | --- | --- | --- | --- |
| 1 | Result / expected failure | `Tooba.BuildingBlocks.Results.Result`, `Result<T>`, `SemanticError` | `src/backend/BuildingBlocks/Tooba.BuildingBlocks/Results` | **USED** |
| 2 | Typed fault exception | `Tooba.BuildingBlocks.SemanticException` (`.Error.Code`) | `TmarFoundation.cs:84` | **NOT USED** — Cart throws `InvalidOperationException("<code>")` |
| 3 | Fault→Result composition | `Application/<X>ExceptionMapper.TryAsync(...)` + `ToSemanticError(...)` | `Payment.Application.Errors.PaymentExceptionMapper`, `Cart.Application.Errors.CartExceptionMapper` | **USED (legacy arm)** |
| 4 | API response mapping | `ApiResponseFactory.From(Result<T>)` / `From(Result)` / `Created` / `FromFailure` / `FromException` / `FromSemanticException` | `BuildingBlocks/Presentation/ApiResponseFactory.cs` | **USED** (all 7 routes) |
| 5 | Error catalog contributor | `IErrorCatalogContributor` + `ErrorDescriptor` | `BuildingBlocks/Presentation/Errors` | **USED** (`CartErrorCatalogContributor`, 12 descriptors) |
| 6 | Safe error mapping | `SafeErrorMapper` + `IErrorDefinitionCatalog` + `ErrorDefinitionCatalog` | `BuildingBlocks/Presentation/Errors` | **USED** |
| 7 | Localization resource set | `IErrorResourceSet` + `.resx` + `ResourceErrorMessageLocalizer` + `IErrorMessageLocalizer` | `BuildingBlocks/Localization/ResourceErrorMessageLocalizer.cs` | **USED** (`CartErrorResourceSet`, 13 keys × en/fa) |
| 8 | Locale resolution | `IRequestLocaleResolver` (never raw `Accept-Language` in endpoints) | BuildingBlocks | **COMPLIANT** — Endpoints never parse the header |
| 9 | ProblemDetails context | `IProblemDetailsContextProvider` supplying `traceId` / `correlationId` / `requestId` | `BuildingBlocks/Presentation/ProblemDetails` | **USED** by `ApiResponseFactory` |
| 10 | Stable module codes | `Cart.Application.Errors.CartErrorCodes` (Application-local, same as `PaymentErrorCodes`) | Cart.Application | **USED** |
| 11 | Validation codes | `Cart.Application.Validation.CartValidationCodes` (stable, non-localized) | Cart.Application | **USED** |
| 12 | CQRS foundation | `AddToobaCqrsFoundation` (MediatR 12.5.0) + `ValidationBehavior<,>` + `LoggingBehavior<,>` + `TracingBehavior<,>` | `TmarFoundation.cs` | **USED** |
| 13 | Logging | `ILogger<T>` + `ObservabilityLogScope` + `ObservabilityLogScopeKeys` | BuildingBlocks | **USED** (`CartExpiryWorker`) |
| 14 | Telemetry primitives | `ToobaTelemetry.ActivitySource` / `ToobaTelemetry.Meter` (named `Tooba`) | BuildingBlocks | **USED** (`CartExpiryWorker` counters) |
| 15 | Correlation | `ICorrelationIdProvider` / `CorrelationIdContext` / `X-Correlation-Id` | BuildingBlocks | **COMPLIANT** — no parallel mechanism |
| 16 | Cross-module tracing | `IModuleCallTracer` / `TracingBehavior<,>` | BuildingBlocks | **NOT REQUIRED** — Cart issues no cross-module command |
| 17 | Outbox | `IOutboxModuleRegistration` + `OutboxSaveChangesInterceptor` + `OutboxMessageMapping` | `Tooba.Persistence` | **USED** (`CartOutboxRegistration`) |
| 18 | Schema migration | `AddModuleSchemaMigrator<TDbContext>` + `ModuleSchemaMigrationOrder` | `Tooba.Persistence` / `Tooba.ModuleContracts` | **USED** (`ModuleSchemaMigrationOrder.Cart`) |
| 19 | Clock / ids | `IClock` / `IIdGenerator` (never `DateTimeOffset.UtcNow` / `Guid.NewGuid()`) | BuildingBlocks | **COMPLIANT** (guard-enforced) |
| 20 | Size / cohesion guard | `TmarSourceSizeGuard` + `Baselines/tmar-source-size-baseline.json` (`thresholdNewFileLoc = 800`) | `Tooba.Host.Tests` | **VIOLATED** (`CartDirectory.cs` 831 LOC, not in baseline) |

## Two fault mechanisms coexist in the repository

Discovered by comparing the modules that produce HTTP-reachable expected failures:

**Mechanism A — framework typed fault (`SemanticException`)**

```text
throw new SemanticException(new SemanticError(FoundationErrorCodes.CheckoutAuthenticationRequired));
```

Used by Host `Security/Checkout/CheckoutIdentityGate.cs`. Mapped by `SafeErrorMapper` via
`exception.Error.Code` → `MappedSafeError` → catalog descriptor → localized ProblemDetails.

**Mechanism B — module exception mapper over code-carrying exceptions**

```text
// Payment
catch (ContractOperationException ex) when (TryMapExact(ex.Code, out var mapped)) { ... }
catch (InvalidOperationException ex) when (TryMapExact(ex.Message, out var mapped)) { ... }

// Cart
catch (InvalidOperationException ex) when (TryMapExact(ex.Message, out var error))
{
    return Result.Failure<T>(error);
}
```

Used by `Payment` (27 known codes + alias dictionary + optional public-code override) and by `Cart`
(18 alias cases + `default: return false`).

**Cart uses Mechanism B in its narrower form.** The observable outcomes are correct today, but the
Analyze/Certify skills classify "failure classification done by parsing `ex.Message`" as
`STRING_HEURISTIC` and list `EXCEPTION_MESSAGE_BASED` as a defect state.

## Reference module per concern

| Concern | Reference used | Why |
| --- | --- | --- |
| AMSC flow / capability-first Application tree / `Result<T>` handlers | **AddressBook** | immediately preceding accepted AMSC run (`Addresses/{Commands,Queries,Validators}`) |
| Exact-code allowlist mapper shape | **Payment** | `PaymentExceptionMapper` is the most mature Mechanism-B implementation |
| Typed fault + `Application/Validation/` folder | **AccessControl** | `AccessControlException` + `AccessControlFluentRules`/`AccessControlValidationCodes` |
| Error catalog contributor + `Contracts/Errors` | **Offer** | canonical `OfferErrorCodes` + `OfferErrorCatalogContributor` + resx pair |
| Platform primitives | **BuildingBlocks** | `Result`, `SemanticError`, `SemanticException`, `ApiResponseFactory`, `SafeErrorMapper` |

`Offer` is used as a **principle** reference only; Cart keeps its Application-local `CartErrorCodes`
because `Payment` demonstrates that location is canonical for this module family.
