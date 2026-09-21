# Anti-pattern gate — TB-TMAR-FND-OBSERR-001-R2

| Pattern | Status |
| --- | --- |
| Polling / magic sleeps for telemetry | CLEAN |
| AsyncLocal without scope restore | CLEAN (BeginScope dispose) |
| ThreadStatic | CLEAN |
| Static current HttpContext | CLEAN |
| Service locator in foundation | CLEAN (Host enrichment injects deps) |
| Raw RequestServices in foundation for enrichment | CLEAN |
| Custom background timer for telemetry | CLEAN |
| Duplicate spans (HTTP/MassTransit) | CLEAN (enrich-first policy) |
| Unbounded tag cardinality | CLEAN (static operation names) |
| PII in tags/scopes | CLEAN |
| exception.Message in tags/API | CLEAN |
| Blanket catch-ignore | CLEAN |
| Hardcoded Offer-only module map in TracingBehavior | CLEAN (namespace derivation) |
| Separate correlation generators HTTP/MT/outbox | CLEAN (shared MessagingCorrelation / CorrelationIdContext) |

**AntiPattern-Gate: CLEAN**
