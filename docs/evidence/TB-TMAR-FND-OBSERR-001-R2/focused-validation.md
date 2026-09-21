# Focused validation — TB-TMAR-FND-OBSERR-001-R2

| Suite | Result |
| --- | --- |
| Tooba.BuildingBlocks.Tests | 29/29 PASS |
| Tooba.Offer.Tests | 41/41 PASS |
| Host CorrelationRuntime + ErrorContract (focused) | see full-validation |
| Tooba.Host build | PASS |

## New coverage

- TracingBehavior command span + error tagging
- ModuleCallTracer source/target/correlation
- MessagingCorrelation publish/consume restore
- ObservabilityLogScope keys without PII
- TracingBehavior registered exactly once
- HTTP correlation preserve/replace/ProblemDetails join
- Offer list/create module-call topology
- Architecture guards (no raw StartActivity in Offer handlers; gateway tracers; single OTel)
