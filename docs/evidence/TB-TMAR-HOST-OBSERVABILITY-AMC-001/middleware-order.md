# middleware-order — TB-TMAR-HOST-OBSERVABILITY-AMC-001

Exact Program order (relevant segment):

1. `UseForwardedHeaders()` — **conditional** (`Tooba:TrustedProxies` non-empty)
2. `UseToobaCorrelationId()` — foundation correlation + base log scope
3. `UseExceptionHandler()`
4. `UseCors`
5. `SecurityHeadersMiddleware`
6. `TenantResolutionMiddleware`
7. `SessionAuthenticationMiddleware`
8. **`RequestObservabilityEnrichmentMiddleware`**
9. endpoint maps

## Verdict

**INTENTIONAL_AFTER_TENANT_AND_SESSION** — enrichment runs after commerce + session so `tenantId` / `storeId` / `actorId` can populate ObservabilityLogScope. Correlation middleware remains earlier authority for CorrelationId lifecycle.
