# ProblemDetails final — TB-TMAR-FND-OBSERR-001-R4

Pipeline:

Exception → `SafeErrorMapper` (once) → descriptor catalog → `ProblemDetailsContextProvider` → `RequestLocaleResolver` → `ResourceErrorMessageLocalizer` → `ApiResponseFactory.CreateFromMapped` → `ExceptionPresentationService` → `IProblemDetailsService`.

`ToobaExceptionHandler` only calls `IExceptionPresentationService`. Offer seller endpoints have no local `catch`, no `ApiResponseFactory`, and no `Results.Json(new ProblemDetails...)`.

Body fields: `errorCode`, `correlationId`, `traceId`, and `requestId` when the ASP.NET trace identifier exists. Validation failures add structured `errors`.

Production (`HideExceptionDetails` when not Development): no stack trace, no `exception.Message`, no exception type. Development unexpected failures may set `Detail` to the exception type name only.

Verdict: PASS.
