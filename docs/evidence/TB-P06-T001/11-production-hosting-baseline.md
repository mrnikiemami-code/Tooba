# 11 — Production hosting baseline (TB-P06-T001)

| Concern | Assumption |
|---|---|
| Reverse proxy | TLS termination at proxy; Kestrel behind proxy |
| Forwarded headers | Enabled when `Tooba:TrustedProxies` configured |
| HTTPS | Enforced at edge; `UseHttpsRedirection` not forced in dev |
| Host header / tenant | `TenantResolutionMiddleware` uses normalized Host; forwarded host only from trusted proxies |
| Cookies | Session Bearer model; secure cookie policy at deployment layer |
| Canonical host/domain | Single-Store host allowlist in platform config |
| Provider lock-in | None — generic ASP.NET Core + PostgreSQL + optional SpiceDB + OTLP |

Documented for ops; no single-cloud hard-coding.
