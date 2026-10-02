# Middleware and endpoints — TB-TMAR-HOST-ROOT-FINAL-CERT-001

## Middleware order (certified)

1. UseForwardedHeaders (only when TrustedProxies configured)
2. UseToobaCorrelationId
3. UseExceptionHandler
4. UseCors("ToobaCors")
5. SecurityHeadersMiddleware
6. TenantResolutionMiddleware
7. SessionAuthenticationMiddleware
8. RequestObservabilityEnrichmentMiddleware

No reorder in this task.

## Trust / CORS / body

- TrustedProxies: KnownNetworks/KnownProxies cleared; `IPAddress.Parse` fail-fast; no TryParse skip
- CORS: empty origins fail closed (`SetIsOriginAllowed(_ => false)`); no AllowAnyOrigin
- MaxRequestBodyBytes from AuthSecurityHostOptions

## Observability

JSON console/scopes, AddToobaObservabilityFoundation, ToobaTelemetry ActivitySource/Meter, health/ready tracing exclusion, correlation middleware preserved.

## Host health

`HostHealthEndpoints.Map(app, enableCors: true)` — platform health composition; HOST_HEALTH_AMC_CERTIFIED preserved.

## Platform diagnostics (Development/Testing only)

- /__platform-error
- /__platform-conflict
- /__platform-commerce

Gated: `IsDevelopment() || IsEnvironment("Testing")`. No Production exposure. Platform diagnostics only.

## Development bootstrap

Gated by `IsDevelopment()`; composition/seed invocation only; best-effort logged seed failures preserved.

## Labels

`HOST_MIDDLEWARE_ORDER_CERTIFIED`
