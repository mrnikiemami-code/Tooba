# canonical-presentation — TB-TMAR-HOST-ERRORS-AMC-001-W1

## Active global path

```text
ASP.NET ExceptionHandler
→ ToobaExceptionHandler (Tooba.Host.Errors)
→ IExceptionPresentationService
→ ExceptionPresentationService
→ ISafeErrorMapper
→ ApiResponseFactory
→ IProblemDetailsService
```

## ToobaExceptionHandler

- Physical path: `src/backend/Host/Tooba.Host/Errors/ToobaExceptionHandler.cs`
- Namespace: `Tooba.Host.Errors` (EXACT path-derived)
- Remains thin `IExceptionHandler` only
- Delegates solely to `IExceptionPresentationService.WriteAsync`
- No local mapping, titles, catalog ownership, or duplicate logging

## Program registration

- `using Tooba.Host.Errors;`
- `AddExceptionHandler<ToobaExceptionHandler>()`

## Parallel ProblemDetails path

ZERO in Host/Errors and MultiTenancy resolution-failure path after mapper retirement.
