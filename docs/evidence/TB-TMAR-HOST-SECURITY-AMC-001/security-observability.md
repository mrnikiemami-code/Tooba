# security-observability — TB-TMAR-HOST-SECURITY-AMC-001

## Sensitive data / logging audit (all 19 Security files)

| Concern | Finding |
| --- | --- |
| ILogger / Log.* | ZERO |
| Authorization header logged | ZERO |
| Cookie / Bearer token logged | ZERO |
| Password logged | ZERO |
| Actor / tenant / seller ids logged | ZERO |
| Exception detail leakage via logs | ZERO (no logging) |
| Dev-only header | `X-Tooba-Dev-Actor-User-Id` (protocol header constant; Development-only actor resolve) — not logged |
| Seller party header | `X-Tooba-Seller-Party-Id` — protocol; not logged |

## Observability / correlation

| Mechanism | Finding |
| --- | --- |
| Custom ActivitySource | ZERO |
| Custom Meter | ZERO |
| Manual traceparent | ZERO |
| Custom correlation IDs | ZERO |
| Parallel ObservabilityLogScope | ZERO |

Target met: no parallel telemetry mechanisms under Host/Security. Host relies on global BuildingBlocks/Host pipeline (not reimplemented here).

## AuthSecurityHostOptions / SecurityHeadersMiddleware

- Options: CORS origins, HSTS (Production+HTTPS), report-only CSP, auth rate-limit window, max body size — Host platform configuration; no secrets embedded in code.
- Middleware: applies configured security headers; no module business authority; no sensitive logging.
