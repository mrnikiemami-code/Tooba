# Logging / telemetry / correlation — AccessControl (W3)

## Logging

| Aspect | State |
| --- | --- |
| Mechanism | `ILogger<T>` structured logging only |
| `Console.WriteLine` / `Console.Error` | `ZERO` |
| `Debug.WriteLine` | `ZERO` |
| New/second logging framework | `ZERO` |
| Second telemetry pipeline | `ZERO` |
| `ObservabilityLogScopeKeys` reuse | ✅ |
| Sensitive-data logging | `NONE` |

### Sensitive-data scan

| Sensitive class | Logged |
| --- | --- |
| passwords | no |
| refresh tokens | no |
| access tokens | no |
| OTP secrets | no |
| reset secrets | no |
| `Authorization` headers | no |
| cookies | no |
| session secrets | no |
| security stamps | no |
| private credentials | no |
| full query strings | no |
| payment payloads | no |

## OpenTelemetry / correlation

| Aspect | State |
| --- | --- |
| OpenTelemetry integration | preserved |
| `Activity` / `TraceId` / `SpanId` semantics | intact |
| Custom correlation header | `ZERO` |
| Custom correlation middleware | `ZERO` |
| Raw `AsyncLocal` correlation | `ZERO` |
| Canonical provider | `ICorrelationIdProvider` (`X-Correlation-Id`) |
| `ActivitySource.StartActivity(...)` in Application/Endpoints | `ZERO` |
| Manual `traceparent` parsing | `ZERO` |
| `LOST_PROPAGATION` | `ZERO` |
| `PARALLEL_CORRELATION` | `ZERO` |

## Module meter

`Infrastructure/Observability/AccessControlInstrumentation.cs` (25 LOC) is a thin facade over the
shared `ToobaTelemetry` `Meter`. It declares **no** private `ActivitySource` / `Meter` instance, so
there is no competing telemetry pipeline.

`Infrastructure/Authorization/AuthorizationInstrumentation.cs` (60 LOC) is a distinct
authorization-scoped meter facade with the same canonical shape — not a duplicate pipeline.

## Cross-module trace continuity

The module issues **no** synchronous cross-module *call* requiring `IModuleCallTracer` decoration.
Its foreign interaction is through in-process DI ports implemented by foreign Contracts
(`ICatalogCartPresentationLookup`, `IPartySellerDirectory`, `IOperatorProfileDirectory`,
`IIdentity*`), so trace continuity is carried by the ambient `Activity`.

## Verification method

Repo-wide scan of `Tooba.AccessControl.*` production `.cs` for `Console.(Write|Error)`,
`Debug.Write`, `ActivitySource.StartActivity` and `traceparent` returned **zero** hits.
