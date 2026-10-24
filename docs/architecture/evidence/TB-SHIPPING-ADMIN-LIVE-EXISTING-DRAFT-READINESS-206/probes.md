# TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206 — probes

Read-only requests only. No request body, cookie, header value, connection string or credential is recorded.

## Ports / process provenance (before any action, shared processes untouched)

| Port | Listener | PID | Provenance |
| --- | --- | --- | --- |
| 3000 | shared frontend (`next` dev server) | 19108 | `node ...\src\frontend\node_modules\next\dist\server\lib\start-server.js`, started 2026-10-22 06:45 |
| 5088 | Host backend | 24304 | `src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe`, started 2026-10-22 06:41:42 |
| 3517 | — | — | free (no task-owned frontend started; not needed, see below) |
| 9555 | — | — | free (no browser started; not needed, see below) |

`src/frontend/.env.local`: `TOOBA_HOST_ORIGIN=http://127.0.0.1:5088`; `next.config.ts` rewrites `/v1/:path*` to that origin (default fallback is the same port). So the shared UI proxies `/v1` to the real Host, no mock.

## Read-only HTTP probes (`Invoke-WebRequest`, `-TimeoutSec 8..12`, no auth headers added)

| Request | Result |
| --- | --- |
| `GET 5088/health` | 200 `{"status":"ok"}` |
| `GET 5088/v1/admin/dev-context` | 200 dev actor/tenant context (dev identity, no sign-in wall) |
| `GET 3000/v1/admin/dev-context` | 200 (same, through the Next rewrite) |
| `GET 5088/v1/admin/shipping-services` | **401** (route exists in the RUNNING binary; auth enforced) |
| `GET 5088/v1/admin/shipping-services/authoring-drafts` | **404** |
| `GET 5088/v1/admin/shipping-services/authoring-types` | **404** |
| `GET 3000/v1/admin/shipping-services/authoring-drafts` | **404** (same 404 through the real rewrite; not a proxy problem) |
| `GET 5088/swagger/v1/swagger.json`, `/openapi/v1.json` | 404 (no API explorer exposed) |
| `GET 3000/admin/shipping-services` | 200 (page shell renders) |

## Source-of-truth comparison

- Current `main` registers the routes: `ShippingEndpointModule.cs:21` `app.MapGroup("/v1/admin")` → `ShippingEndpointModule.cs:33` `ShippingAuthoringDraftEndpoints.Map(admin)`, and `ShippingAuthoringDraftEndpoints.cs:26,67` map `"/shipping-services/authoring-drafts"` and `"/shipping-services/authoring-types"`.
- `ShippingAuthoringDraftEndpoints.cs` last changed by `be639a473` **2026-10-23 02:15 +0330** (TB-199 atomic editor document), i.e. after the running binary was built.
- Running binary artifacts: `Tooba.Host.exe` **2026-10-22 06:41:42**, `Tooba.Shipping.Endpoints.dll` / `Application.dll` / `Domain.dll` / `Contracts.dll` all **2026-10-22 06:41:10–20**.
- Symmetric mismatch: the running Host answers `401` for `/v1/admin/shipping-services`, while current `main` defines no route at that exact path (`MapGet("/shipping-services")` : no matches). The running route table therefore differs from current source in both directions.

Conclusion: the 404 is **route-not-present-in-running-backend**, not connection failure, not 401/403, not schema, not empty data, not response-contract, not a proxy/rewrite fault.
