# TB-STOREFRONT-PERF-QUERY-SCALE-183 — request-scoped EF command counts at two real Published sizes

**Status: PASS (technical evidence complete, blocker NOT closed)**
Base SHA: `b5b9644ef6c01e7b8f7ee48fac2de416d95f2beb` (HEAD == origin/main before work)

## 1. Headline numbers

| | Size A | Size B |
|---|---|---|
| Published products in scratch DB | 3 | 12 |
| Distinct product IDs in Home payload | 3 | 12 |
| Published growth | 1.0x | **4.0x** |
| EF `Database.Command` events attributed to the request | **38 / 38 / 38** | **47 / 47 / 47** |
| EF command growth | 1.0x | **1.237x** |
| Request wall time (3 measured) | 136.2 / 89.6 / 91.7 ms | 341.4 / 363.0 / 95.8 ms |
| Payload bytes | 33,962 | 51,892 |
| Bounded diagnostic log bytes per request | ~110 KB | ~139 KB |

Both legs were served by the **same Host process**, same binary, same tenant
(`store-alpha`), same route (`/v1/storefront/home?locale=fa`) and same instrumentation:
`Logging__LogLevel__Microsoft.EntityFrameworkCore=Information` set **only** for this
diagnostic process (committed `appsettings.json` and `appsettings.Development.json`
keep `Warning`).

## 2. How commands were attributed (not inferred)

`Program.cs` configures `ActivityTrackingOptions.TraceId|SpanId|ParentId` with
`IncludeScopes = true`. Each measured request therefore sent a unique W3C
`traceparent` header, and ASP.NET Core adopted that TraceId for the request Activity.
`attribute-commands-183.js` streams the 181 MB JSONL once and counts only lines whose
`Scopes[].TraceId` equals the request's TraceId **and** whose `Category` is
`Microsoft.EntityFrameworkCore.Database.Command` (EventId 20101). No window-based or
console-total inference is used.

Independent totals from the same pass (`ef-command-counts-183.json`):

```
totalEFCommandExecuted      = 57,469
attributedToMeasuredRequests =    340   (12 requests × 28–47 commands)
unattributed                 = 57,129
  ├ startup (before first request) = 52,460
  └ background during request window = 4,669
```

The 52,460 startup commands are Host bootstrap/migration/schema probes; the 4,669
background commands during the ~12 s measurement window are the outbox dispatchers and
hosted services (classified by first table hint: catalog 48,608 total, messaging 1,013,
inventory 543, offer 388, pricing 336, party 220, schema 90 over the whole log).

## 3. Size A → Size B command arithmetic

```
A: 38 = 35 request-fixed + 3 × 1 per published card
B: 47 = 35 request-fixed + 12 × 1 per published card
```

The model fits exactly (`+9` commands for `+9` published items): Home carries a
**one-query-per-card** term, no other item-linear fan-out. Sellers, prices, stock,
media, names, categories and variants are already batched (`PrepareBatchAsync`), which
the near-constant 35-command floor at both sizes confirms.

## 4. Owning call site of the per-card query

`Tooba.Catalog.Infrastructure/Storefront/StorefrontComposer.cs:1071` calls
`_promotions.EvaluateForCheckoutAsync(...)` **inside** the per-product
`ComposeProductAsync` loop. That port resolves to
`Tooba.Promotion.Infrastructure/Directories/PromotionDirectory.cs`, whose
`EvaluateAsync` executes `_db.Promotions.AsNoTracking().ToListAsync(...)` on every
call. The promotion table is empty in this fixture (`promotions=0`), so the cost is a
fixed per-card round-trip regardless of promotion data, and it repeats N times per Home
render.

**Smallest follow-up slice (recommended, NOT implemented here):** add a batched
promotion-evaluation entry to the Promotion Contract (e.g. load candidates once per
request and evaluate the card set in memory, or extend `StorefrontBatchData` with a
single promotion snapshot), keeping the Promotion module as the owner. Scope: one
contract member + one adapter/directory method + one call-site change in
`StorefrontComposer`. No lifecycle, pricing or lifecycle-rule change.

## 5. Latency and payload honesty

- Steady state: A `m2/m3` = 89.6 / 91.7 ms vs B `m3` = 95.8 ms — the 4x published
  growth did **not** produce 4x latency.
- B `warmup/m1/m2` = 313.5 / 341.4 / 363.0 ms were taken immediately after 40
  publication write requests against a just-written catalog (pool/plan warmup), so the
  B mean (266.7 ms) is reported but the steady-state comparison uses `m3`.
- A's mean (105.8 ms) includes its own cold warm-up (166.1 ms).

## 6. Quiet default leg (suppression verified on the same size-B state)

A second process was started **without** the diagnostic override:

```
log bytes for the entire session = 105,258  (vs 181,000,000+ in the diagnostic leg)
Microsoft.EntityFrameworkCore.Database.Command lines = 0
```

So the committed `Warning` default suppresses the command flood completely while the
Home endpoint still returns HTTP 200 — the TB-178 fix holds and was not touched.

Payload parity caveat, stated explicitly: quiet-leg body is 60,005 B vs the diagnostic
51,892 B, with **identical 12 distinct product IDs** and identical
featured/newArrivals/productRail/mostViewed counts; only `specialOffers` (1 → 7) and
`campaignProducts` (0 → 2) sections differ. This is the same section-population
transition already documented in TB-180 (eligibility/population of secondary rails over
time), not a code, binary or logging effect. It is why the byte-for-byte A/B comparison
above uses the single diagnostic process.

## 7. Fixture provenance (owner authority only)

Disposable `postgres:16-alpine` on `127.0.0.1:55503` (db `tooba_183_scratch`) and a
private Host on `127.0.0.1:5454`; `prepare-isolated-target-183.js` wrote
`isolation-guard-183.json` (PASS: single Active tenant, no `tooba_alpha`, no
`tenant-alpha`, no `Port=5432`, no `:3000`/`:5088`).

- One bounded `POST /v1/admin/catalog/demo/seed-only` (Development-only,
  `AllowResetAndSeed`, authorized admin actor) grew the scratch catalog
  5 → 200 products.
- Size B was produced by **40 successful owner-route calls** with genuine Development
  seller memberships: `POST /v1/seller/offers`, `PUT …/price`, `PUT …/inventory`, then
  `POST /v1/admin/products/{id}/publish` (readiness-gated). No SQL status write, no row
  cloning, no forged grant, no validator suppression, zero HTTP errors in the attempts
  log; products that failed real publish readiness simply stayed Draft.
- Final scratch fixture: `products=201 published=12 draft=188 variants=663
  categories=122 active_offers=46 active_prices=53 stock=48 promotions=0`.

## 8. PERF-CRIT-001 disposition (recommended, not self-approved)

| Gate | State |
|---|---|
| Logging flood / log-volume acceptance | **Met** — committed Warning default, 0 command lines in quiet leg |
| Query growth | **Met with a caveat** — 4x published items costs +23.7% commands; residual +1 query per card identified with owner call site |
| Performance | **Met for steady state** — 95.8 ms at 4x size, HTTP 200 |
| Response parity | **Met** — deterministic body per data snapshot; section-population delta explained |
| Real user browser responsiveness | **NOT met** — requires the user/architect |
| Architect register acceptance | **NOT met** — this task may not close it |

Recommended technical disposition: the N+1 class defect is resolved; one bounded,
owner-correct per-card promotion query remains and is worth a small scoped slice if
Home item counts grow further. The release blocker itself stays **OPEN** pending
architect sign-off and the user's browser verdict.

## 9. Isolation and cleanup

Task-owned only: container `tooba-tb183-pg` (+ volume), db `tooba_183_scratch` /
`tooba_183_messaging`, ports `55503` and `5454`, `.tmp-183-*` artifacts, logs, password
file. Never touched: `:5432`/`:3000`/`:5088`, `tooba_alpha`, `store-alpha`, running
shared Host/Next, Redis, RabbitMQ, `.next`, the unrelated dirty Admin file or the
TB-075/079/173 evidence. Cleanup verified: container removed, temp files deleted, no
listener on `55503`/`5454`, no scratch Host process; the user's pre-existing containers
(`postgres-db`, `rabbitmq`) were left untouched.

## 10. Files

- `stage1-183.ps1` — disposable PG + private Host + bounded owner seed
- `stage2-183.ps1` — size A, owner publication, size B (same process, trace-scoped)
- `stage4-quiet-183.ps1` — quiet default leg on the same size-B state
- `attribute-commands-183.js` — TraceId/interval attribution of EF command events
- `ef-command-counts-183.json` — exact counters, per-request rows, background split
- `home-size-a.json`, `home-size-b.json`, `home-size-b-quiet.json`, `fixture-183.txt`,
  `publication-attempts-183.json`, `isolation-guard-183.json`, `progress-183.log`
