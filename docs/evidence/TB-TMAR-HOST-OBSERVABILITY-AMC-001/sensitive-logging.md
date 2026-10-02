# sensitive-logging — TB-TMAR-HOST-OBSERVABILITY-AMC-001

Canonical keys: `ObservabilityLogScopeKeys` (BuildingBlocks). ClientIp key comment: *"فقط اگر trusted-proxy safe"*.

| Field | Classification | Notes |
|---|---|---|
| CorrelationId | ALLOWED | Canonical |
| RequestId (`TraceIdentifier`) | ALLOWED | Independent ASP.NET request id; complements CorrelationId |
| TenantId | ALLOWED | Platform commerce identity reference |
| StoreId | ALLOWED_WITH_CONDITION | Logging-only; currently mirrored from tenantId |
| ActorId (Guid N) | ALLOWED | No PII surface |
| ClientIp (raw `RemoteIpAddress`) | **SENSITIVE_DEBT** | Always passed when present; trusted-proxy gating not enforced in code; ForwardedHeaders only when TrustedProxies configured — without proxies IP is direct connection; with misconfigured proxies spoof risk |
| HttpMethod | ALLOWED | |
| HttpPath (`Path.Value`) | ALLOWED_WITH_CONDITION | Query string excluded (good); path segments may contain resource IDs — no redaction |
| Query string | ZERO (not logged) | Correct |

Foundation doc `29-observability-error-foundation.md`: do not log secrets/Authorization/cookies/passwords/OTP/connection strings/full query strings.

## Forwarded headers

`UseForwardedHeaders` only when `Tooba:TrustedProxies` length > 0. RemoteIpAddress is post-proxy normalized **only then**.

## Custom tracing/metrics in Host/Observability file

ZERO (`ActivitySource` / `Activity` / `Meter` / custom traceparent absent). Log enrichment only.
