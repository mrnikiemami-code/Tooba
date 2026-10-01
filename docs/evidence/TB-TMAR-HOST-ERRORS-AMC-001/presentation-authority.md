# presentation-authority — TB-TMAR-HOST-ERRORS-AMC-001

## Active global unhandled path

```text
UseExceptionHandler
→ ToobaExceptionHandler.TryHandleAsync
→ IExceptionPresentationService.WriteAsync
→ ExceptionPresentationService
   → ISafeErrorMapper.Map
   → ApiResponseFactory.CreateFromMapped
   → IProblemDetailsService.WriteAsync
```

## Parallel early-middleware path (not via IExceptionHandler)

```text
TenantResolutionMiddleware.WriteProblemAsync(PlatformHttpException)
→ PlatformExceptionMapper.Map / ToProblemDetails
→ IProblemDetailsService.WriteAsync
```

## Authority conclusion

- Canonical unhandled authority: **IExceptionPresentationService** (single active handler registration).
- Parallel ProblemDetails construction in Host/Errors mapper: **YES** (legacy), still reachable from MultiTenancy.
- ToobaExceptionHandler: no local classification, no hard-coded titles, no ex.Message selection, no catalog/localizer duplication.
- ExceptionPresentationService logs structured operational fields; uses `IProblemDetailsContextProvider` for trace/correlation.
