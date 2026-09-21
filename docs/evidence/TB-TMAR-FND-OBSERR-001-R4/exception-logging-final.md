# Exception logging final — TB-TMAR-FND-OBSERR-001-R4

`ExceptionPresentationService` is the single Warning/Error presentation log.

- Semantic, validation, not-found, conflict, and forbidden descriptors use `ErrorSeverity.Warning`, so they log Warning, not Error.
- `platform.unexpected` and other Error severities log once at Error.
- The log template includes ErrorCode, Classification, StatusCode, and correlation/trace/span/request join keys.
- The public ProblemDetails title comes from resources or the safe fallback. `exception.Message` is not copied into the response.

`LoggingBehavior` logs MediatR failures at Debug only, including expected business failures. It does not emit a second Error. `TracingBehavior` sets span status and `exception.type`, then rethrows without logging.

Verdict: PASS.
