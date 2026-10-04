# TB-TMAR-ADDRESSBOOK-AMSC-001-W0 — Canonical Mechanism Discovery

Discovery performed **before** any classification, per the Analyze skill's mandatory
"Canonical Mechanism Discovery" step. No type name was assumed from memory.

## 1. Result / expected failure

| Mechanism | Location | Evidence |
| --- | --- | --- |
| `Result` / `Result<T>` (`IsSuccess`, `IsFailure`, `Errors`) | `Tooba.BuildingBlocks.Results` | consumed by Offer, AccessControl, UserPreference |
| `SemanticError(string Code, Arguments)` | `Tooba.BuildingBlocks` (`TmarFoundation.cs`) | throws `ArgumentException("error_code_required")` on blank code |
| `SemanticException(SemanticError Error)` | `Tooba.BuildingBlocks` (`TmarFoundation.cs`) | `base(error.Code)` — technical message is the code, never localized text |

## 2. Fault → Result composition (the decisive discovery)

`Application/Composition/<Module>Operation.cs` exists in **13 modules**:

```text
AccessControl   BulkInquiry   Content   Identity   Localization   Media
OperatorProfile PageComposition Party    ProductQnA   Story       UserPreference   Wishlist
```

Canonical shape (verbatim from `UserPreferenceOperation`):

```csharp
public static async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> action)
{
    try { return Result.Success(await action()); }
    catch (SemanticException ex) { return Result.Failure<T>(ex.Error); }
}
```

`AccessControlOperation` adds two variants used by the immediately preceding AMSC run:

```csharp
public static async Task<Result> ExecuteAsync(Func<Task> action)
public static Result<T> NotFoundIfNull<T>(T? value, string missingCode) where T : class
```

**AddressBook has no `Composition/` folder and no `AddressBookOperation`.**

## 3. API response mapping

`Tooba.BuildingBlocks.Presentation.ApiResponseFactory` (verified on disk):

| Member | Behavior |
| --- | --- |
| `From(Result)` | failure → ProblemDetails; success → `Results.NoContent()` (204) |
| `From<T>(Result<T>)` | failure → ProblemDetails; success → `Results.Json(value)` (raw DTO, no envelope) |
| `Created<T>(string location, Result<T>)` | failure → ProblemDetails; success → `Results.Created(location, value)` (201) |
| `FromFailure(SemanticError)` / `FromFailure(IReadOnlyList<SemanticError>)` | ProblemDetails from the catalog |
| `FromException` / `FromSemanticException` / `FromPlatformException` | context-based ProblemDetails |

`BuildProblemDetails` emits `Title` (localized via `IErrorMessageLocalizer`), `Instance`,
and extensions `errorCode`, `traceId`, `correlationId`, `requestId`, `errors`. Culture is resolved
through `IRequestLocaleResolver` + `IHttpContextAccessor` — **not** raw `Accept-Language` parsing.

**AddressBook currently injects `ApiResponseFactory` but only calls `FromFailure`; all six success
paths use raw `Results.*`.**

## 4. Error catalog

| Mechanism | Location |
| --- | --- |
| `IErrorCatalogContributor.Contribute()` | `Tooba.BuildingBlocks.Presentation.Errors` |
| `ErrorDescriptor(Code, Classification, HttpStatus, LocalizationKey, Severity, SafeTitleFallback)` | same |
| `IErrorDefinitionCatalog.TryGet(code, out descriptor)` | same |
| `SafeErrorMapper.Map(Exception)` | same — switch on `SemanticException` / `ValidationException` / `PlatformHttpException` / `_ => MapUnexpected()` |
| `SafeErrorMapper.Map(SemanticError)` | catalog hit → descriptor; miss → `400 Business` "Request rejected." |

`SafeErrorMapper.MapUnexpected()` resolves `platform.unexpected` → **HTTP 500**, severity `Error`.
There is **no** heuristic on code names.

Module contributors found: `OfferErrorCatalogContributor`, `AccessControlErrorCatalogContributor`,
`AddressBookErrorCatalogContributor`.

## 5. Localization

| Mechanism | Location |
| --- | --- |
| `IErrorResourceSet.Owns(key)` / `GetString(key, culture)` | `Tooba.BuildingBlocks.Localization` |
| `IErrorMessageLocalizer.Localize(key, culture, args, fallback)` | same |
| `IRequestLocaleResolver.Resolve(header)` | same |
| `.resx` + `.resx.fa` | `Endpoints/Resources/` |

AddressBook has `AddressBookErrors.resx` + `AddressBookErrors.fa.resx` with **one** key
(`customer.address.missing`) and `AddressBookErrorResourceSet.Owns` scoped to the
`customer.address.` prefix.

## 6. Stable error codes

- Module-owned: `Tooba.<Module>.Contracts.Errors.<Module>ErrorCodes`.
- AddressBook: `AddressMissing = "customer.address.missing"`,
  `SessionRequired = "customer.session.required"`.
- `customer.session.required` is a **shared cross-cutting code** declared (as a constant) by 8
  modules but owned by the Foundation catalog contributor; consuming modules correctly do **not**
  re-register its descriptor. The AddressBook contributor already documents this in a comment.

## 7. Validation

| Mechanism | Location |
| --- | --- |
| `AbstractValidator<T>` + `AddressBookFluentRules` | `Application/Validators/` |
| Stable codes | `AddressBookValidationCodes` (in `AddressBookFluentRules.cs`) |
| Pipeline | `ValidationBehavior<TRequest,TResponse>` in `TmarFoundation.cs` |

`Offer` / `AccessControl` place these under `Application/Validation/`; AddressBook uses
`Application/Validators/`. Both names are accepted by the module's physical-structure guard.

## 8. CQRS

`AddToobaCqrsFoundation(assemblies)` → MediatR 12.5.0, `LoggingBehavior<,>`, `TracingBehavior<,>`,
`ValidationBehavior<,>`. Registered in `Program.cs` with
`typeof(IAddressBookDirectory).Assembly`.

## 9. Logging / telemetry / correlation

| Concern | Mechanism | AddressBook usage |
| --- | --- | --- |
| Structured logging | `ILogger<T>` + `ObservabilityLogScope` / `ObservabilityLogScopeKeys` | not used (nothing to log) — canonical |
| Activity source | `ToobaTelemetry` (`ActivitySource`/`Meter` named `Tooba`) | not touched — canonical |
| Cross-module call tracing | `IModuleCallTracer` / `TracingBehavior<,>` | AddressBook makes no cross-module calls |
| Correlation id | `ICorrelationIdProvider`, header `X-Correlation-Id` | via `ProblemDetailsContextProvider` — canonical |
| Global exception boundary | `ExceptionPresentationService` (`IExceptionPresentationService`) | supplies `traceId`/`correlationId`/`requestId` |

## 10. Size / cohesion guard

`TmarSourceSizeGuard` + `Baselines/tmar-source-size-baseline.json`. **No AddressBook file is listed
in the baseline** — nothing in the module is oversized.

## Conclusion

Two canonical fault paths exist and AddressBook uses neither:

1. `SemanticException(SemanticError)` — framework-level, already fully supported by
   `SafeErrorMapper`.
2. `<Module>Exception` + `<Module>Operation.ExecuteAsync` — module-local, used by 13 siblings.

`AddressBook` uses `System.InvalidOperationException` with hard-coded Persian messages, which
`SafeErrorMapper` classifies as `Unexpected` → HTTP 500. This is a **non-canonical mechanism**, not
an accepted ownership exception.

The correct repair (W1) is convergence onto mechanism **(1)** wrapped by a module
`AddressBookOperation` composition mirroring `UserPreference` / `AccessControl` — no new mechanism
is invented.
