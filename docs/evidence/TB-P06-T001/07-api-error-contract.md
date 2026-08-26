# 07 — API error contract (TB-P06-T001)

| Concern | Status |
|---|---|
| ProblemDetails / RFC 7807 | `AddProblemDetails` + `ToobaExceptionHandler` |
| Unhandled 500 | `application/problem+json`; no stack in Production |
| Validation | `BadHttpRequestException` mapped; domain validation via `PlatformHttpException` |
| Conflict / NotFound / Auth | Distinct status + `errorCode` extension |
| Trace ID | `traceId` extension on all ProblemDetails |
| Stack leakage | Blocked in Production (`ErrorContractTests`) |
| Connection string leakage | Blocked (`ErrorContractTests`, tenant middleware) |
| Auth trace consistency | **Fixed**: `AuthProblem` now uses W3C `Activity.TraceId` |

Centralized handler; no per-endpoint rewrite in this task.
