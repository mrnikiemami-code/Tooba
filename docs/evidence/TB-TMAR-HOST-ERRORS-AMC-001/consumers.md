# consumers — TB-TMAR-HOST-ERRORS-AMC-001

## PlatformExceptionMapper / MappedPlatformError

| Kind | Location | Notes |
| --- | --- | --- |
| Production | `Host/MultiTenancy/TenantResolutionMiddleware.cs` `WriteProblemAsync` | `Map` + `ToProblemDetails`; only `PlatformHttpException` passed |
| Program DI | NONE | No registration |
| Tests | `Host.Tests/ErrorContractTests.cs` `PlatformExceptionMapperTests` | unit map unknown → 500 omit Detail |
| Docs/guards | historical OBSERR evidence | not production |

**Is PlatformExceptionMapper production-dead?** NO — ACTIVE via MultiTenancy.

**Is MappedPlatformError production-dead?** NO — used by mapper return type on that path.

## ToobaExceptionHandler

| Kind | Location |
| --- | --- |
| Production DI | `Program.cs` `AddExceptionHandler<ToobaExceptionHandler>()` + `UseExceptionHandler()` |
| Implementation | delegates solely to `IExceptionPresentationService.WriteAsync` |
| Tests | OfferArchitectureGuardTests asserts file contains `IExceptionPresentationService` |
| Direct other production call sites | ZERO |

**Is ToobaExceptionHandler the sole active Host IExceptionHandler boundary?** YES.

**Is it the sole Host ProblemDetails writer?** NO — MultiTenancy still writes via PlatformExceptionMapper.

## Canonical presentation authority

`IExceptionPresentationService` → `ExceptionPresentationService` (BuildingBlocks) is the canonical global unhandled-exception authority. `SafeErrorMapper` already maps `PlatformHttpException` and `SemanticException` with catalog statuses.
