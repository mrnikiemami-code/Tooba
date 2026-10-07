# TB-TMAR-PAGECOMPOSITION-AMSC-001-W1 — Migrate (tooba-architecture-migrate)

## Scope

`src/backend/Modules/PageComposition/Tooba.PageComposition.*` — canonical mechanism completion (typed-fault seam), zero ownership moves, zero schema change, starting head `652036cc` (W0), parent `TB-TMAR-PAGECOMPOSITION-AMSC-001-W0`.

## Changes

1. `Contracts/Errors/PageCompositionErrorCodes.cs` — added the certified declared-code guard: private `KnownCodes` `HashSet<string>` (StringComparer.Ordinal) over all 8 module codes + `public static bool IsKnown(string? code)`. Mirrors the certified Media/Inventory/OperatorProfile precedent verbatim (OperatorProfile is the newest Architect-accepted AMSC W1 shape).
2. `Application/Composition/PageCompositionOperation.cs` — `ExecuteAsync<T>` now maps BOTH typed fault mechanisms by declared stable code: `catch (ContractOperationException ex) when (PageCompositionErrorCodes.IsKnown(ex.Code))` → `Result.Failure<T>(new SemanticError(ex.Code))` FIRST, then `catch (SemanticException ex)` → `Result.Failure<T>(ex.Error)`; added the value-less `ExecuteAsync(Func<Task>)` overload with the same dual mapping and `ArgumentNullException.ThrowIfNull` guards (OperatorProfile W1 shape). Unknown codes and every other exception propagate untouched to the canonical global exception boundary (`IExceptionPresentationService`). The existing sync `Execute<T>` overload (used by Admin/Storefront endpoints for tenant resolution) keeps its `SemanticException` mapping — the module raises only `SemanticException` today and no `ContractOperationException`-raising path exists inside the module; the sync overload is therefore intentionally left in its certified minimal shape to avoid inventing behavior, while the async path (all CQRS handlers) carries the full certified dual-mechanism seam.

## Behavior delta

`ACCEPTED_NO_BEHAVIOR_DELTA` — the module raises only `SemanticException` today (Domain aggregate + `RequireTenantId`); the `ContractOperationException` mapping is a dormant safety seam for module-level infrastructure faults; HTTP routes/shapes/status codes/stable codes/localization are unchanged; no log call sites exist so nothing else changes.

## Validation

- `dotnet build` PageComposition Endpoints/Infrastructure/Application/Contracts/Domain → 0 errors.
- Focused guards: PageCompositionModuleAmcW2StructureGuardTests + HostPageCompositionAmcGuardTests + PageCompositionModuleAmcW4CertGuardTests + TmarCompleteReferenceStructureGateTests PASS.
- New durable W1 guard: `PageCompositionModuleAmsc001W1MigrateGuardTests` (2 facts: IsKnown over all 8 declared codes; dual-mechanism seam shape incl. value-less overload and no `ex.Message` classification).

## Preservation

Routes, DTOs, stable codes, resx keys, catalog descriptors, DI lifetimes, authorization seam, schema/migrations, seed behavior: unchanged. Host untouched. Frontend untouched.
