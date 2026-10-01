# boundary-security — TB-TMAR-HOST-ERRORS-AMC-001

## Dependency boundary (Host/Errors)

| Check | State |
| --- | --- |
| Foreign Application / Infrastructure / Domain | ZERO |
| DbContext / persistence | ZERO |
| Business commands / module policy | ZERO |
| Allowed | ASP.NET Diagnostics + BuildingBlocks Presentation |

## Hard-coded runtime text (Errors files)

| String | Location | Classification |
| --- | --- | --- |
| "Bad Request" | PlatformExceptionMapper | dead-branch-but-live-type debt; prefer deletion after MultiTenancy rehome |
| "Internal Server Error" | PlatformExceptionMapper | same |
| PlatformHttpException.Title passthrough | PlatformExceptionMapper | legacy Title presentation |

ToobaExceptionHandler: ZERO hard-coded user-facing text.

## Sensitive detail

- Mapper: Development detail only when caller passes `developmentDetail`; MultiTenancy passes null → Detail omitted.
- ToobaExceptionHandler / ExceptionPresentationService: Production-safe via SafeErrorMapper/ApiResponseFactory (no stack in client contract; ErrorContractTests assert).
- Exception.Message not used for code selection in either Errors file.

## Observability

- Errors files: no ActivitySource/Meter/custom correlation.
- ExceptionPresentationService uses canonical logger + ProblemDetailsContextProvider (traceId/correlationId).
- Mapper writes `traceId` extension from caller-supplied string only.
