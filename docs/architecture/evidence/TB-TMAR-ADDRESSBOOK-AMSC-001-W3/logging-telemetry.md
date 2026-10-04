# TB-TMAR-ADDRESSBOOK-AMSC-001-W3 — logging-telemetry

`LoggingState = CANONICAL`. `SensitiveLoggingState = NONE`. `OpenTelemetryState = CANONICAL`.
`CorrelationTraceState = CANONICAL`.

## Structured logging

| Check | Result |
| --- | --- |
| `ILogger<T>` only | PASS — the module uses the platform logging boundary; no module-local logger |
| `Console.WriteLine` / `Console.Write` | ZERO |
| `Debug.WriteLine` / `Debug.Write` | ZERO |
| second logging framework / second telemetry pipeline | ZERO |
| custom log-scope keys instead of `ObservabilityLogScopeKeys` | ZERO |

Asserted by `AddressBookModuleAmsc001W3CertGuardTests.AddressBook_has_no_raw_error_code_literals_ad_hoc_logging_or_typed_fault_residue`,
which scans every production `.cs` in the module for `Console.Write`, `Debug.Write`,
`ActivitySource.StartActivity` and `traceparent` — **zero offenders**.

The module currently emits no explicit log statement. That is the accepted state (the global Host boundary
logs request/failure outcomes through the canonical pipeline), and it matches the W0 Analyze finding
(`Logging-State = CANONICAL`, module logs nothing).

## Sensitive-data logging

| Sensitive category | Logged? |
| --- | --- |
| passwords / credentials | No |
| access / refresh tokens | No |
| OTP / reset secrets | No |
| `Authorization` headers / cookies | No |
| session secrets / security stamps | No |
| full query strings | No |
| payment payloads | No |
| full postal address / contact mobile in a log statement | No (no log statement exists) |

`SensitiveLoggingState = NONE`.

## OpenTelemetry / correlation continuity

| Check | Result |
| --- | --- |
| `ActivitySource.StartActivity` in Application/Endpoints | ZERO |
| `new Meter` / custom metric pipeline | ZERO |
| manual `traceparent` parsing/creation | ZERO |
| `AsyncLocal` custom correlation | ZERO |
| custom correlation header instead of `X-Correlation-Id` (`ICorrelationIdProvider`) | ZERO |
| `Guid.NewGuid()` used as a correlation id | ZERO |
| cross-module calls decorated with `IModuleCallTracer` | Not applicable — the module makes no outbound module calls; it is a callee (`Order.Application`, `CustomerProfile.Application` consume its Contracts ports) |
| ProblemDetails `traceId` / `correlationId` / `requestId` from the canonical context provider | PASS — responses are produced by `ApiResponseFactory`, which uses the canonical provider |

The module therefore inherits trace continuity from the canonical platform stack rather than creating a
parallel one. Asserted by the W3 cert guard's telemetry scan (zero offenders) and by the pre-existing
correlation guard set.

## Non-regression

W1–W3 introduced no logging, telemetry or correlation change:

- W1 changed fault **types** (`InvalidOperationException` → `SemanticException`) and handler return types,
  not logging;
- W2 was a pure path/namespace move;
- W3 is verification + guards + docs.

`DUPLICATE_TELEMETRY`, `SECOND_PIPELINE`, `PARALLEL_CORRELATION`, `LOST_PROPAGATION` — all absent.
