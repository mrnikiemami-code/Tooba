# Request log scope — TB-TMAR-FND-OBSERR-001-R2

## Pipeline

1. `UseToobaCorrelationId` — base scope: CorrelationId, TraceId/SpanId (from Activity), RequestId, HttpMethod, HttpPath
2. After `TenantResolutionMiddleware` + `SessionAuthenticationMiddleware`:
   `RequestObservabilityEnrichmentMiddleware` opens a **nested** scope with TenantId / StoreId / ActorId / ClientIp when available

## Rules followed

- CorrelationId never replaced by enrichment
- Missing Tenant/Store/Actor is safe (keys omitted)
- No tokens, claims dump, email/phone/name
- No DB query for logging
- Client IP only via `Connection.RemoteIpAddress` (after forwarded headers when trusted proxies configured)

## StoreId

CommerceContext has no separate StoreId; Single-Store uses TenantId as store identity when present.
