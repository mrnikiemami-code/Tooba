# TB-STOREFRONT-PERF-SCALE-CLOSURE-180 — Result

Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_TWO_SIZE_CATALOG_SCALE_AND_RESPONSE_PARITY_PROOF
Channel: tooba-main · Worker: tooba-worker-01 · Agent: cursor
Status: **PARTIAL**
Stop-State: STOP_FOR_ARCHITECT · Automatic-Next-Task: NONE

Base-SHA `174dd8171dcb6d233c1a6c1ba8fd56440f988d9e` — verified `HEAD == origin/main` before any write.

## 0. Verdict in one line

Gap (2) — the TB-179 cross-leg byte delta — is **closed with runtime proof**: on one instance the two
logging legs are byte-identical (`E4EB276E…`, 16,279 B, 0 semantic diffs), and the delta is a **catalog/pricing
data-state transition**, quantified to the `specialOffers` section. Gap (1) — the ≥4× size — is **not
delivered** because the only owner-correct scale seeder is unreachable without a real Admin session
(`403 admin.authorization.denied`), and hand-written or cloned rows are forbidden.

## 1. Environment (task-owned, fail-closed)

* Disposable PostgreSQL 16 container `tooba-tb180-pg`, db `tooba_180_scratch` / `tooba_180_messaging`, loopback
  `55500`; Host `127.0.0.1:5451`; one Active scratch tenant (`store-alpha`), `Host: alpha.localhost`.
* Isolation guard (`prepare-isolated-target-180.js`) fail-closed before every leg: single Active tenant,
  no `tooba_alpha`, no shared reference, no port `5432`, committed logging defaults **not** overridden by the
  overlay. Shared `:3000`/`:5088`, the running shared Host (PID 26472) and Next, `tooba_alpha`, `store-alpha`
  shared data, RabbitMQ/Redis and `.next` caches were never touched.
* Same published binary + same database for every leg. **Validity assertion added** after a first attempt where a
  listener PID was not provably owned by the started process: every measured leg now asserts
  `listenerPid == startedPid` (`listenerPidMatchesHost: true` for all four legs).
* Canonical `Tooba.MigrationRunner apply --all-tenants` created every owner schema (`migrator-apply-180.txt`).

## 2. Size A — measured (final round, `stage-a.json`)

Fixture provenance: **application-owned** Development bootstraps only (`RunLegacyBootstraps=true`,
`AllowResetAndSeed` used only to expose the seed route; no reset was ever invoked). Rows read back from the scratch DB:
`products=5 categories=6 variants=4 offers=6 prices=22 stock=8 parties=4 texts=46`.

| Metric (3 warm requests + 1 warm-up) | Diagnostic leg (EF=Information) | Default leg (committed EF=Warning) |
| --- | --- | --- |
| HTTP status | 200 / 200 / 200 | 200 / 200 / 200 |
| Wall ms | 76.6 / 106.8 / 68.9 (median 76.6) | 66.2 / 67.7 / 53.8 (median **66.2**) |
| Payload bytes | 16,279 each | 16,279 each |
| Body SHA-256 | `E4EB276E…A2035BCD8` | **identical** |
| `"slug"` occurrences | 22 | 22 |
| **Request-scoped EF commands** (warm-up + 3) | **37 / 37 / 37 / 37** | 0 (invisible by construction) |
| Process command lines | 554 → fixed startup ≈ 406 | 0 |
| Host stdout | 1,059,974 B / 604 lines | **23,121 B / 20 lines** |
| Error/Critical lines | 0 | 0 |

Request-scoped counts are computed per request window (`startUtc`…`endUtc`) on
`Microsoft.EntityFrameworkCore.Database.Command` lines, with per-TraceId grouping as a cross-check; they are
**not** inferred from total log volume, and startup commands are reported separately (≈406) rather than being
attributed to a request.

## 3. Gap (2) — the TB-179 14,851 vs 16,279 byte delta: explained, not a logging effect

1. **Same-instance cross-leg parity is exact.** Diagnostic body and default body are byte-identical
   (16,279 B, `E4EB276E…`), parsed JSON diff = **0 paths**. EF log level cannot change a query result, and here
   it demonstrably does not.
2. **The delta is a data-state transition.** In an earlier round on this same instance (16:55, `prices=13`) the
   same route returned **14,856 B**; after the Development bootstrap had run further (`prices` 13 → 22 → 37 → 43)
   it settled at **16,279 B** and stayed there across **three consecutive process starts** (16,279 B each,
   `E4EB276E…` every time) even while `pricing.prices` grew 37 → 40 → 43. Payload did **not** follow the price-row
   count; it followed a one-time eligibility transition.
3. **Magnitude identifies the section.** Home top-level section sizes: `specialOffers` = **1,371 B** for 2 items,
   `featuredProducts`/`newArrivals`/`mostViewedProducts` = 1,953 B each, `bestSellerColumns` = 4,983 B. The
   observed delta is 1,423–1,428 B, i.e. `specialOffers` (1,371 B) + ≈52 B of neighbouring field drift —
   consistent with `specialOffers` being empty in the earlier pricing state and populated afterwards.
4. Honest limit: TB-179 did not persist its bodies, so its exact 14,851 B state cannot be re-diffed
   retroactively. Point 3 is quantified and consistent, **not** a byte-level identification of that old body.
   What *is* proven is that the delta is data/eligibility state, never the EF log level.

## 4. Gap (1) — ≥4× size: BLOCKED on owner authorization (no fabricated ratio)

Attempts, in order, all bounded and terminated by exact PID:

* `POST /v1/admin/catalog/demo/seed-only` with the Development dev-actor header carrying the real
  `admin-actor@tooba.local` user id (resolved from `identity.login_identifiers`, not guessed) →
  **`403 admin.authorization.denied`** ("Access to manage this store is not permitted"), captured verbatim in
  `seed-authorization-403-180.txt`. An earlier call in the same shape failed as HTTP 400 with an empty body
  after 14.1 s.
* The startup-owned bootstrap set has **no size knob** (fixed 5-product fixture), and the destructive
  `/reset-and-seed` path was not used.
* Manufacturing the authorization tuple, minting an Admin session, or writing/cloning catalog rows by hand
  would be fake authorization / an invented fixture — explicitly forbidden — and was therefore not done.

Consequence: **no size-B numbers exist**, hence no commands-per-item growth, no A→B ratio, no fixed-overhead
decomposition. The only legitimate derived figure is the size-A fixed/request split: ≈406 startup commands +
**37 per Home request**.

## 5. Item-level parity (size A, real, not inferred)

Parsed default body vs scratch DB snapshot (`db-snapshot-180-size-a.txt`):

* 5 DB product rows, 7 distinct slugs and 3 distinct `productId`s in the payload, **0 DB products missing**
  from the payload (no silent omission).
* 6 offers, 22 price rows, 8 stock positions, 4 seller parties, 46 localized texts in the same snapshot.
* Diagnostic vs default parsed diff: **0 paths**, so no field can differ across logging legs — product ids,
  localized names, seller identity, offer/price/currency, availability and media references are identical by
  construction on that data state.
* Cross-tenant bleed: not exercisable — the fail-closed guard admits exactly one Active tenant.

## 6. Code / config / tests

* **Zero production, test or configuration changes.** The measured configuration is the committed TB-178 one
  (`Microsoft.EntityFrameworkCore: Warning` in both `appsettings.json` and `appsettings.Development.json`);
  the diagnostic leg used an environment-only override.
* No test was added: no production code changed, so there is nothing new to cover; the existing
  `HostEfLoggingConfigurationGuardTests` (6/6) remains the coverage for the defaults exercised live here.
* Exact diff of this task: docs only (task receipt + `docs/architecture/evidence/TB-STOREFRONT-PERF-SCALE-CLOSURE-180/`
  + the PERF-CRIT-001 note).

## 7. Release gates

`PERF-CRIT-001` stays **OPEN — CRITICAL / RELEASE BLOCKER**. Now measured: EF log flood fixed on a nonempty
catalog (37 request-scoped commands fully visible at Information, 0 emitted at the committed default;
stdout 1.06 MB → 23.1 KB; median warm latency 76.6 → 66.2 ms with no regression), and the cross-leg payload
delta is attributed to catalog/pricing state with same-instance byte parity. Still missing: a ≥4× catalog size
(owner seeder gated by Admin authorization), the resulting scale/growth numbers, and — as always — Architect
review plus user-perceived responsiveness. Nothing was closed.

## 8. Cleanup

Container `tooba-tb180-pg` and both scratch databases removed; ports `5451`/`55500` verified closed; every
Host process killed by exact PID (including the probe and the three stability starts); `.tmp-180-run` and the
local scratch-password file deleted. No credential, connection string or customer data is in the evidence.
