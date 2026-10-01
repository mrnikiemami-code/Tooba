# semantic-status — TB-TMAR-HOST-ERRORS-AMC-001

## PlatformExceptionMapper behavior

| Exception | Status | Title | Risk |
| --- | --- | --- | --- |
| PlatformHttpException | platform.StatusCode | platform.Title | Uses Title as presentation (legacy) |
| SemanticException | **forced 400** | Error.Code as title | Material semantics bug vs catalog 401/403/404/409/429/503 |
| BadHttpRequestException | 400 | hard-coded "Bad Request" | Hard-coded EN prose |
| unknown | 500 | hard-coded "Internal Server Error" | Hard-coded EN prose |

## Current consumer exercise

MultiTenancy only passes `PlatformHttpException` (FailClosed 404). SemanticException→400 and hard-coded EN branches are **not exercised by current production call sites** but remain **stale dangerous residue** inside an active type.

## Classification

- If mapper were the global handler: material SemanticException status bug.
- Given active MultiTenancy-only consumer: **ACTIVE_LEGACY_PARALLEL_PRESENTATION** with **STALE_DANGEROUS_RESIDUE** branches.

## PlatformHttpException compatibility

- Remaining production throw sites exist outside Host/Errors (Media, Catalog, Persistence, Program probes, Content authorizer, etc.).
- `ExceptionPresentationService` / `SafeErrorMapper.MapPlatform` already supports PlatformHttpException.
- PlatformExceptionMapper adds **no unique required compatibility** beyond MultiTenancy’s local early write; that consumer can rehome to canonical presentation.
