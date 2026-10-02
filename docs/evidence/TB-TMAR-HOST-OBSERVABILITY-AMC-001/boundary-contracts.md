# boundary-contracts — TB-TMAR-HOST-OBSERVABILITY-AMC-001

| Boundary | State |
|---|---|
| ObservabilityLogScope | CANONICAL_BUILDINGBLOCKS — no parallel schema |
| Correlation | CANONICAL_ICorrelationIdProvider |
| Authentication | CONSUMES_CurrentAuthenticatedSession_ONLY — no auth authority |
| Tenancy | CONSUMES_ICurrentCommerceContext_ONLY — no tenant resolution authority |
| Foreign Application | ZERO |
| Foreign Infrastructure | ZERO |
| Foreign Domain | ZERO |
| Foreign DbContext | ZERO |
| Business authority | ZERO |
| Hardcoded user-facing text | ZERO (comments only) |
| Exception handling | NO_CATCH — propagates to `UseExceptionHandler`; `using` disposes scope |
| Logger | BeginScope only — no completion message templates/PII logs |
| Duplicate enrichment middleware | ZERO (single Program registration) |
