# middleware-order — TB-TMAR-HOST-OBSERVABILITY-AMC-001-W1

Program order preserved (import-only change for Observability namespace):

1. forwarded headers (conditional)
2. correlation
3. exception handler
4. CORS
5. SecurityHeaders
6. TenantResolution
7. SessionAuthentication
8. **RequestObservabilityEnrichment**
9. endpoint maps

Registration count: **exactly one**.
Order relative to SessionAuthentication: **AFTER** (unchanged).
