# Anti-pattern gate — TB-TMAR-FND-OBSERR-001-R4

Checked against the R4 reject list for foundation and Offer production:

| Reject | Result |
| --- | --- |
| Polling / magic delay as a test fix | Not used. Host flake was serialized, not retried |
| ThreadStatic / static HttpContext | Correlation is AsyncLocal. No static HttpContext |
| Service locator in the new pipeline | Constructor injection |
| Duplicate correlation SSOT | One AsyncLocal context |
| Duplicate spans | HTTP fallback suppressed; MassTransit enriched |
| Unbounded span cardinality | Names are type/module, not ids or SKUs |
| PII or tokens in telemetry | Actor id and tenant id only. No email, phone, name, or claims dump |
| exception.Message in public response | Production ProblemDetails omits it |
| Bilingual hardcoded error switch | Deleted with OfferEndpointLocalizer |
| Unknown locale → Persian | Default culture `en` |
| Heuristic status mapping | Catalog only |
| Duplicate Error logs | MediatR stays at Debug |
| Endpoint semantic try/catch | Offer seller endpoints have none |
| Cross-module Offer DbContext | Guard clean |
| Baseline widening / green suppressions | None added |

AntiPattern-Gate: CLEAN
