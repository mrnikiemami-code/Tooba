# canonical-presentation — TB-TMAR-HOST-ERRORS-AMC-001-W2-CERT

## Active chain (verified)

```text
UseExceptionHandler()
→ ToobaExceptionHandler (Tooba.Host.Errors)
→ IExceptionPresentationService
→ ExceptionPresentationService
→ ISafeErrorMapper
→ ApiResponseFactory
→ IProblemDetailsService
```

`ExceptionPresentationService` also uses `IProblemDetailsContextProvider` for trace/correlation.

## Program

- `using Tooba.Host.Errors;`
- Exactly one `AddExceptionHandler<ToobaExceptionHandler>()`
- `app.UseExceptionHandler()` present

## Handler thinness

- Implements `IExceptionHandler`
- Injects only `IExceptionPresentationService`
- Delegates `WriteAsync` then returns `true`
- No local status mapping, ProblemDetails, titles, catalog, logging, or business logic

## MultiTenancy resolution seam (behavior protection only)

- Uses `IExceptionPresentationService`
- No local `ProblemDetails` / `IProblemDetailsService` / mapper
- MultiTenancy structure certification: **NOT_OPENED**
