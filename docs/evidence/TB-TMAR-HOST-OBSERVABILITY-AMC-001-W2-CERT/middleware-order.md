# middleware-order — TB-TMAR-HOST-OBSERVABILITY-AMC-001-W2-CERT

Certified Program order (relevant slice):

1. forwarded headers (conditional)
2. correlation middleware
3. exception handler
4. CORS
5. SecurityHeadersMiddleware
6. TenantResolutionMiddleware
7. SessionAuthenticationMiddleware
8. RequestObservabilityEnrichmentMiddleware
9. endpoint mapping

Classification: `AFTER_TENANT_AND_SESSION_CERTIFIED`
Registration count: exactly one.
