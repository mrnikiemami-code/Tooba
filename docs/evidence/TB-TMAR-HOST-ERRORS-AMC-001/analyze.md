# analyze — TB-TMAR-HOST-ERRORS-AMC-001

## Mode

ANALYSIS_ONLY — production code unchanged.

## Exact tree

| Path | Namespace | Types |
| --- | --- | --- |
| Errors/PlatformExceptionMapper.cs | `Tooba.Host` | `MappedPlatformError`, `PlatformExceptionMapper` |
| Errors/ToobaExceptionHandler.cs | `Tooba.Host` | `ToobaExceptionHandler` |

Production `.cs` count: **2**.

## Dispositions

| Type | Disposition |
| --- | --- |
| ToobaExceptionHandler | KEEP_AS_THIN_GLOBAL_HOST_EXCEPTION_ADAPTER |
| PlatformExceptionMapper | ACTIVE_LEGACY_PARALLEL_PRESENTATION (not Program-registered; consumed by MultiTenancy) |
| MappedPlatformError | ACTIVE (only via PlatformExceptionMapper) |

## Canonical chain (unhandled)

ASP.NET `UseExceptionHandler` → `ToobaExceptionHandler` → `IExceptionPresentationService` (`ExceptionPresentationService`) → `ISafeErrorMapper` + `ApiResponseFactory.CreateFromMapped` → `IProblemDetailsService`.

## Parallel path

`TenantResolutionMiddleware.WriteProblemAsync` → `PlatformExceptionMapper` → direct `ProblemDetails` write (PlatformHttpException only in practice).

## Skills

`.cursor/skills/tooba-architecture-analyze/SKILL.md` applied.

## Certification

NOT_CERTIFIED (Analyze only). Security/Admin certifications preserved.
