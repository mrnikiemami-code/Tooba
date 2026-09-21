# Reference gap analysis — TB-TMAR-FND-OBSERR-001-R1

External reference: `github.com/mrnikiemami-code/BuildingBlocks` branch `mastertest`
Clone location (outside repo): `%TEMP%\BuildingBlocks-mastertest`

| Reference capability | Classification | Reason |
| --- | --- | --- |
| `ICorrelationContext` | **ADAPT** | Adopted contract; Tooba namespace `Observability.Correlation`. |
| `ICorrelationIdProvider` | **ADAPT** | Same lifecycle API; wired via `AddToobaObservabilityFoundation`. |
| `CorrelationIdContext` (AsyncLocal) | **ADAPT** | Normalize/Ensure/BeginScope/nested restore; format N. |
| `CorrelationIdProvider` / `CorrelationContextAdapter` | **ADAPT** | Thin facades over AsyncLocal SSOT. |
| `CorrelationIdMiddleware` | **ADAPT** | Lightweight Host middleware: ensure header, enrich, log scope. Full MassTransit/client-IP wiring deferred to R2. |
| `BuildingBlocksActivitySource` | **ALREADY_EXISTS** | Tooba already has `ToobaTelemetry.ActivitySource` — do not duplicate. |
| `BuildingBlocksTraceEnricher` | **ADAPT** | `ToobaTraceEnricher` enrich-only; no duplicate HTTP span creation in R1. |
| `BuildingBlocksTracingPolicy` | **ADAPT** | `ToobaTracingPolicy` with health/ready exclusion + EnrichExisting vs CreateFallback. |
| `ObservabilityLogScope` / Keys / Tag names | **ADAPT** | Central scope + `tooba.*` tags; ClientIp optional. |
| `IProblemDetailsContextProvider` / Context / Provider | **ADAPT** | Richer Tooba context (Tenant/Store/Actor/Path/Method/HideDetails). |
| `ProblemDetailsFactory` / `ValidationProblemFactory` | **ADAPT** | `ToobaProblemDetailsFactory` + validation via `SafeErrorMapper`. Full Result&lt;T&gt; ValidationProblemFactory not forced in R1. |
| `ApiResults` / `DefaultResultHttpMapper` / `ResultHttpMapper` | **ADAPT** | `ApiResponseFactory` supports SemanticException/PlatformHttpException without forcing repo-wide Result&lt;T&gt; migration. Static ApiResults service-locator pattern **REJECT**. |
| Presentation DI `AddBuildingBlocksPresentation` | **ADAPT** | `AddToobaObservabilityFoundation` + Host keeps existing ExceptionHandler/OpenTelemetry. |
| `SafeErrorMapper` (Result Error catalog style) | **ADAPT** | Maps SemanticException / FluentValidation / PlatformHttpException / unknown; convention status; no module switch; no `exception.Message` to client. |
| Serilog CorrelationEnricher | **REJECT** | Tooba uses Microsoft.Extensions.Logging JSON console; scope keys cover join. |
| Full MassTransit correlation filters | **REJECT** (R1) | Deferred to R2 messaging correlation wiring. |
| Duplicate OpenTelemetry host wiring | **REJECT** | Host already has AddOpenTelemetry + ASP.NET/HttpClient/Runtime/OTLP. |
| FA-first Accept-Language Contains("en") | **REJECT** | Replaced by `IRequestLocaleResolver` with configurable fallback (default `en`). |

## Current Tooba already kept

- `ToobaTelemetry` ActivitySource/Meter
- Host `AddOpenTelemetry` instrumentation + OTLP
- `AddProblemDetails` + `ToobaExceptionHandler` (now delegated to central factory)
- `SemanticError` / `SemanticException`
- MediatR Validation/Logging behaviors
