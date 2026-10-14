# TB-SHIPPING-MVP-OPERATIONAL-VERIFY-058 — bounded operational readiness verification

Status: **PARTIAL** (worker). Real HTTP verification of route registration and the authorization boundary
was obtained on an isolated disposable clone of the new code; authenticated admin CRUD and the frontend
authorized round-trip remain BLOCKED. The running Development Host was not restarted.

- Parent task: `TB-SHIPPING-MVP-CHECKOUT-SHADOW-057`
- Expected base SHA: `716717464ead027de98e8a0ca1150e8a62579457`
- Base SHA: `54a9f6e99e887781966a027fdfd12008c163dc19` (allowed 057 evidence/bookkeeping fast-forward only)
- Mode: `BOUNDED_SHIPPING_OPERATIONAL_READINESS_VERIFICATION`
- Code-SHA: NONE (no production source changed in this task)
- Automatic next implementation task: **NONE**

## 1. Gate results

| # | Gate | State | Evidence |
| --- | --- | --- | --- |
| G1 | Local Development target identity (tenant store-alpha -> `tooba_alpha`, local container, running PID 37028 on 5088) | **VERIFIED (real, read-only)** | `log-target-identity-and-db-inventory.txt` |
| G2 | Geography migration + real rows (1 country `IR`, 12 areas, 47 cities, 0 service areas) | **VERIFIED (real read)** | same |
| G3 | Shipping migration + `rate_policies=0`, `coverage_grants=0` | **VERIFIED (real read)** | same |
| G4 | Existing Fulfillment catalog intact (5 services, 4 options, translations, 3 languages) | **VERIFIED (real read)** | same |
| G5 | Running process actually lacks the new routes | **VERIFIED_REAL_HTTP** (control route 401, negative control 404, new routes 404) | `log-runtime-attempts.txt` §1 |
| G6 | New routes registered and served by the new code | **VERIFIED_REAL_HTTP** (isolated clone: 4 Geography reads + 2 Shipping routes answer 401 / POST 415) | `log-runtime-attempts.txt` §2 |
| G7 | Admin authorization boundary fail-closed, no bypass | **VERIFIED_REAL_HTTP** (`401 admin.actor.missing`; Development actor seam + no capability -> `403 admin.authorization.denied`) | same |
| G8 | Authenticated admin CRUD (GET list w/ data, POST rate, POST grant, PUT, PATCH active) | **BLOCKED** — no admin credential and no authorization-tuple source; inventing one is forbidden | `log-runtime-attempts.txt` §2 |
| G9 | Frontend `/admin/shipping-services` real authorized round-trip | **BLOCKED** (depends on G8) — existence of the page/dialog files and their unit tests recorded, behaviour UNVERIFIED | — |
| G10 | Checkout price authority legacy-owned; shadow gate OFF by default | **VERIFIED_OFFLINE** | `log-source-verification.txt` |
| G11 | Zero writes to `tooba_alpha`; isolated clone dropped | **VERIFIED (real read, before/after identical)** | `log-target-identity-and-db-inventory.txt`, `log-runtime-attempts.txt` §4 |

No gate is claimed PASS for authenticated admin behaviour, UI behaviour, or checkout cutover.

## 2. Root cause of the two failed clone startups (disclosed)

Stale `Tooba__*` environment variables left in the worker shell session by an earlier unrelated task (046:
`Tooba__Edition`, `Tooba__DeploymentId=geo046-verify`, `Tooba__SingleStore__Tenants__0__*`,
`Tooba__PostgreSQL__ConnectionReferences__geo046scratch`) overrode the JSON tenant array, so the process saw
tenant `geo046` instead of `store-alpha`. No repository defect: after clearing those variables the same
binary started normally. The password-bearing stale variable was removed during cleanup; session leftovers
are now 0.

## 3. Release verdict

**NOT_READY_FOR_CHECKOUT_CUTOVER**, on data and authority grounds independent of the runtime gap:

* `shipping.rate_policies = 0` and `shipping.coverage_grants = 0` — the fail-closed engine has nothing to
  price with; switching customer pricing now would make shipping unavailable;
* the storefront projection carries only a province (no country/city/service-area), so requests are coarser
  than rate-policy scopes;
* no measured shadow parity exists — the observation seam has never run against real coverage (empty);
* no Architect authorization for cutover exists and 058 introduces no switch.

Preconditions before any cutover: per-service rate policies + explicit coverage grants (seeded through the
verified admin surface), a city/service-area-bearing destination input, measured zero-mismatch shadow
parity, then an explicit Architect-authorized cutover task.

## 4. Closure verdict — six-task programme + final three

* **MVP admin usability:** page, geography selector, destination picker and rates/coverage dialog exist with
  real HTTP clients and no fake-save stub; actual usability is **UNVERIFIED** (G8/G9 blocked).
* **Backend activation/live status:** code present, compiled and route-served on a clone of the current tree;
  the user-facing Development process is **NOT activated** — it predates 055-057 (`404` on the new routes)
  and was deliberately not restarted.
* **Pricing authority:** unchanged, legacy-owned; the new authority is OFF by default with no cutover switch.
* **Known failing guard:** the 057-noted Host guard path-relative issue remains; running Host architecture
  guards truthfully still requires a Host restart. No guard was weakened.
* **Smallest necessary next steps:** (1) seed real per-service rates + coverage grants through the verified
  admin surface, (2) add the city/service-area destination input, (3) run the shadow seam live with
  prerequisites confirmed and record parity, (4) then request cutover authorization.
