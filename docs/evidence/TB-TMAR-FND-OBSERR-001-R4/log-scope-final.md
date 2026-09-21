# Log scope final — TB-TMAR-FND-OBSERR-001-R4

`ObservabilityLogScope` emits, when present:

CorrelationId, TraceId, SpanId, RequestId, TenantId, StoreId, ActorId, ClientIp, HttpMethod, HttpPath.

Empty values are omitted. There is no claims dump, email, phone, or display name. Commerce ids come from `ICurrentCommerceContext` already on the request. No database query is made for logging.

## Where the scope is opened

1. `CorrelationIdMiddleware` opens correlation, request id, method, and path, then calls the rest of the pipeline.
2. `UseExceptionHandler` is inside that middleware, so the correlation scope is still active when `ExceptionPresentationService` logs.
3. `RequestObservabilityEnrichmentMiddleware` runs after tenant and session auth and adds tenant, store (same as tenant in single-store), actor id, and client IP for the endpoint pipeline.

## Client IP

`Program.cs` applies `UseForwardedHeaders` only when `Tooba:TrustedProxies` lists addresses. Known networks and proxies are cleared, then only those addresses are trusted. The scope stores `Connection.RemoteIpAddress` after that middleware. It does not read `X-Forwarded-For` directly.

## Exception presentation

`ExceptionPresentationService` logs ErrorCode, Classification, StatusCode, correlation, trace, span, request, tenant, store, actor, path, and method on the same call. Business failures are Warning and do not attach the exception object. Unexpected failures are Error and include the exception for operators only.

Verdict: PASS.
