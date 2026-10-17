# TB-STOREFRONT-PERF-SCALE-ACCEPTANCE-179 — Result

Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_TWO_SIZE_STOREFRONT_PERFORMANCE_AND_PARITY_ACCEPTANCE
Channel: tooba-main · Worker: tooba-worker-01 · Agent: cursor
Status: **PARTIAL** (size A measured for real; the ≥4× size and cross-leg byte parity are not delivered)
Stop-State: STOP_FOR_ARCHITECT · Automatic-Next-Task: NONE

Base-SHA: `26b9c90daf79fa61493e91c9dba725e72562ae0c` (HEAD == origin/main verified before any write)

## 1. What was actually measured

Fixture: **application-owned** Development catalog bootstraps only (`CatalogDemo.RunLegacyBootstraps=true`,
`AllowResetAndSeed=false`) on a disposable loopback Postgres 16 (`127.0.0.1:55499`, db `tooba_179_scratch`)
with one Active scratch tenant and one published Host (`127.0.0.1:5450`). No invented catalog rows.

Authoritative row counts read back from the scratch database:

| Relation | Rows |
| --- | --- |
| `catalog.products` | 5 |
| `catalog.variants` | 4 |
| `offer.offers` | 6 |
| `pricing.prices` | 10 |
| `inventory.stock_positions` | 8 |
| `party.parties` | 4 |
| `catalog.localized_texts` | 46 |

Route: `GET /v1/storefront/home?locale=fa`, `Host: alpha.localhost`, same published binary for both legs.
Each leg: **1 warm-up + 3 measured requests**. Two logging legs on the identical binary/database:

* **diagnostic leg** — task-owned opt-in `Logging__LogLevel__Microsoft.EntityFrameworkCore=Information`
* **default leg** — committed default (TB-178 `Warning`)

| Metric | Diagnostic leg | Default leg |
| --- | --- | --- |
| HTTP status (3 measured) | 200, 200, 200 | 200, 200, 200 |
| Warm-up status / ms / bytes | 200 / 222.2 / 14,851 | 200 / 231.6 / 16,279 |
| Measured wall ms | 71.0 / 60.4 / 50.4 (median **60.4**) | 60.4 / 65.0 / 50.8 (median **60.4**) |
| Response bytes (measured) | 14,851 each (3/3 identical) | 16,279 each (3/3 identical) |
| Body SHA-256 | `DFB61CC5…` 3/3 identical | `50A31168…` 3/3 identical |
| `"slug"` occurrences (item signal) | 22 | 22 |
| Host stdout bytes | **7,681,486** | **24,608** |
| stdout lines | 1,400 | 21 |
| EF-category lines | 1,320 | 2 (startup only) |
| `Microsoft.EntityFrameworkCore.Database.Command` lines | **1,287** | **0** |
| Error/Critical lines | 0 | 0 |

This confirms TB-178's fix at a **nonempty** catalog (its scratch catalog was empty) and shows the same
magnitude: ≥1,287 command lines and 7.68 MB of stdout for one startup + 4 Home requests collapse to 0
command lines and 24.6 KB, with **no** error suppression and **no** latency change (median identical at
60.4 ms).

## 2. Two-size scale check — NOT delivered (the blocking gap)

Size B (≥4×) is missing, and the concrete attempt is recorded here because it failed for a **discoverable
reason** rather than a lack of time:

* **Attempted path (application-owned, additive):** `POST /v1/admin/catalog/demo/seed-only` with the
  Development dev-actor header `X-Tooba-Dev-Actor-User-Id` (resolved from `identity.users` for
  `admin-actor@tooba.local`) against a scratch Host with `CatalogDemo.AllowResetAndSeed=true` in the scratch
  overlay only (`measure-scale-b-179.ps1`; the destructive `/reset-and-seed` endpoint was never called).
  The request **did not complete within 7 minutes** and wrote **no** catalog rows — `catalog.products` stayed
  at 5, `offer.offers` at 6, `inventory.stock_positions` at 7 for the whole window. The run was killed and
  the container removed. The exact cause was **not** diagnosed; this is a real observation, not a timeout
  excuse, and the endpoint's stall on a 5-product database is itself worth an Architect look.
* The startup-owned Development bootstrap set (`CatalogDevelopmentSeed.EnsureLegacyBootstrapsAsync`) is a
  **fixed** set of four template seeds plus landing/menu — it produced the 5-product fixture above and has no
  size knob.
* Authoring synthetic 4× catalog rows (cloning products/variants/offers/prices/stock/localisations) was
  **not** done: it would be an invented fixture and an unverified relation graph, which this task forbids.

Consequence: **no commands-per-item growth ratio, no small→large ratio and no fixed-overhead decomposition
exist**. The only derived figure available from size A is 1,287 command lines across one startup + 4 requests
(≈257/request including startup overhead), and command lines must **not** be equated with executed query count
in the quiet leg — the quiet leg reports 0 by construction.

## 3. Parity — partially proven, one honest gap

* **Within each leg:** 3/3 responses byte-identical (same SHA-256), status 200, identical `"slug"` count (22).
* **Across legs:** payload differed — **14,851 vs 16,279 bytes (+1,428, +9.6%)** while both legs ran the same
  binary, data and route. This is **not** attributable to the logging level (which cannot change query
  results) and is recorded as an **open observation, not a pass**. A likely cause is a non-deterministic field
  in the Home composition (e.g. absolute media URL/port or a composition section that differs between a cold
  and a pre-warmed catalog cache), but that was **not** proven and is explicitly marked UNVERIFIED.
* **Item-level field comparison (product IDs, localized names, seller identity, offer/price/currency,
  stock/availability, media refs): NOT captured** — the measured bodies were not persisted before the
  timebox expired. UNVERIFIED.
* **Cross-tenant bleed:** not exercisable — the guard permits exactly one Active tenant by design, so a
  second-tenant leak test was out of scope for this run.

## 4. Code changes

**None.** No production, test, or configuration file changed: the measured configuration is the committed
TB-178 one, the route and readers are untouched, and no N+1 repair was attempted because no growth trend
could be established from a single size. Nothing was rebaselined and no guard was suppressed.

## 5. Focused tests

Not run in this task: no source file changed, so there is nothing new to cover, and the task explicitly asks
only for the essential set. The prior task's guard suite (`HostEfLoggingConfigurationGuardTests`, 6/6) remains
the coverage for the logging defaults that this run exercised live.

## 6. Release register

`docs/planning/MUST-FIX-BEFORE-RELEASE.md` — PERF-CRIT-001 stays **OPEN — CRITICAL / RELEASE BLOCKER**. The
entry now records: TB-178 log-flood fix confirmed on a **nonempty** catalog (1,287 → 0 command lines,
7.68 MB → 24.6 KB stdout, median latency unchanged at 60.4 ms), and the still-missing scale/parity evidence
named above. Not closed; Architect review and user responsiveness remain separate gates.

## 7. Isolation and cleanup

Task-owned only: container `tooba-tb179-pg` (removed), databases `tooba_179_scratch` / `tooba_179_messaging`
(disposed with the container), ports `55499`/`5450` (confirmed closed after teardown), exact-PID Host
processes (the two measured legs and the stalled seed leg, all killed by PID). Shared `:3000`/`:5088`/`:5432`,
`tooba_alpha`, `store-alpha`, shared RabbitMQ/Redis, the running shared Host/Next processes, `.next` caches,
the unrelated dirty Admin file, TB-075/079/TB-173 files and all stashes were untouched. No credential or
connection string is in evidence (the scratch password came from the environment, was never written to
evidence, and the local password file was deleted); stdout excerpts are sanitised. `.tmp-179-run` (published
binaries) is local scratch, deleted, and not committed.

## 8. Elapsed / stop

Bounded execution ended inside the 45-minute box. Status **PARTIAL** — the log-flood fix is now proven on a
nonempty catalog, the two-size scale proof and the cross-leg/item-level parity that the task requires are not
delivered and are named above. `STOP_FOR_ARCHITECT`; no automatic next task; PERF-CRIT-001 remains OPEN.
