# Foundation structure final — TB-TMAR-FND-OBSERR-001-R4

Baseline: `1623b33886f22aa42c1622766e67cae6ce194321` (`main`, `HEAD == origin/main` at start).

## Folders

| Area | Path | Public responsibility |
| --- | --- | --- |
| Correlation | `Observability/Correlation` | `CorrelationIdContext` (AsyncLocal SSOT), middleware, provider facade, constants |
| Tracing | `Observability/Tracing` | Policy, MediatR `TracingBehavior`, `ModuleCallTracer`, enricher |
| Logging | `Observability/Logging` | `ObservabilityLogScope` + keys |
| Messaging join | `Observability/Messaging` | Publish/consume correlation and fallback activity |
| Errors | `Presentation/Errors` | `ErrorDescriptor`, catalog, `SafeErrorMapper` |
| ProblemDetails | `Presentation/ProblemDetails` | Context provider only |
| Response | `Presentation` | `ApiResponseFactory`, `ExceptionPresentationService` |
| Localization | `Localization` | `RequestLocaleResolver`, resource localizer, foundation `.resx` |
| DI | `DependencyInjection/ObservabilityFoundationRegistration.cs` | One registration method; does not add a second OpenTelemetry provider |

## Rules

- One major public type responsibility per file. Facades (`CorrelationIdProvider`, `CorrelationContextAdapter`, `ToobaProblemDetailsFactory`) delegate to the single SSOT; they are not second contexts or mappers.
- Largest handwritten file in these folders is under 200 lines. None exceed 800 LOC.
- Namespaces match folders (`Tooba.BuildingBlocks.Observability.Correlation`, `.Tracing`, `.Logging`, `.Messaging`, `.Presentation.Errors`, `.Presentation.ProblemDetails`, `.Localization`, `.DependencyInjection`).
- No second correlation context, trace policy, API response mapper, or Accept-Language parser.

Verdict: PASS.
