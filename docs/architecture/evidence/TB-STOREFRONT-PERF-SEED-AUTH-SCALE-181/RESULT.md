# TB-STOREFRONT-PERF-SEED-AUTH-SCALE-181 — Result

Program: TOOBA-STOREFRONT-PERFORMANCE-RELEASE-CLOSURE
Mode: BOUNDED_OWNER_SEED_AUTHORITY_AND_TWO_SIZE_SCALE_PROOF
Status: **BLOCKED** (external prerequisite: no owner-supported way to produce a *storefront-visible* scale fixture)
Stop-State: STOP_FOR_ARCHITECT · Automatic-Next-Task: NONE

Base-SHA: `b5af49751de3e03ebe3777fde94b58bcbb3365a5` (HEAD == origin/main verified before any write)

## 1. Authorization root cause (404 → 400, resolved to a per-process authority condition)

Two isolated runs of the same published binary against the same configuration shape, differing **only** in who
performs the Development bootstrap:

| Run | Bootstrap owner | `POST /v1/admin/catalog/demo/seed-only` with the real dev-actor id |
| --- | --- | --- |
| TB-180 | `Tooba.MigrationRunner` migrated/seeded first, Host started afterwards | **403** `admin.authorization.denied` |
| TB-181 | **Host itself** applies schemas + Development bootstraps at startup, no MigrationRunner | **400** `catalog.demo.failed` (post-authorization validation) |

A 400 from `CatalogDemoDevEndpoints.SeedOnlyAsync` can only be produced by its `catch (InvalidOperationException)`
block, which is **after** `await auth.RequireAuthorizedAsync(...)` — so authorization succeeded. That proves the
403 was *not* a missing actor, missing header, wrong tenant or a policy defect: with the same actor, same
tenant and same route, authority exists when the Host itself owns the bootstrap, and is absent when another
process performed it.

Source basis for the mechanism: `AuthorizationRegistration.AddToobaAuthorization` registers
`InMemoryAuthorizationAdapter` as a **singleton per process** and resolves `IAuthorizationService` /
`IAuthorizationTupleWriter` from that same singleton; the grant is written by
`AdminDevActorBootstrap.EnsureAsync` (`user:<actor>#member@tenant:store-alpha`). Development `Mode=InMemory`
therefore keeps authority in process memory only, so a grant written during the `MigrationRunner` process can
never exist in the Host process that serves the request. Not fully isolated: which of the Host's startup steps
did not re-write the tuple in the TB-180 ordering (the already-seeded branch does call the same bootstrap) was
not established; flagged UNVERIFIED rather than guessed.

No security weakness was introduced and none was worked around: no tuple was inserted by hand, no session was
minted, `X-Tooba-Dev-Actor-User-Id` was never treated as sufficient on its own, and no production policy changed.

## 2. The decisive blocker: the owner seeder cannot produce a storefront-visible scale fixture

With authorization resolved, the owner seeder was exercised under its bound (one 60 s attempt, then stopped):

* It **does** write at scale: during a single bounded attempt `catalog.products` grew **5 → 151** and
  `catalog.categories` **6 → 122** — see `fixture-after-seed-attempt-181.txt`.
* But the resulting catalog is **not storefront-visible**:
  * `CatalogDemoProductSeedService` hard-asserts every demo product `must remain Draft`
    (`if (product.Status != CatalogPublicationStatus.Draft) throw ...`).
  * `StorefrontComposer` composes Home **exclusively** from `CatalogPublicationStatus.Published`
    (products, brands, categories, reviews — lines 234/238/267/376/512/638).
  * Measured state after the attempt: `products_total=151, published=3, draft=147, archived=1`,
    `offers=6, prices=10, stock=8`. Home therefore still had essentially the size-A payload.
* The reset path is not an escape hatch either: `CatalogDemoResetAndSeedHost.EnsureProductLifecycleCleanAsync`
  **requires** `published == 0` and `draft == total`, i.e. the demo catalog is deliberately a Draft workspace.
* `SeedOnlyAsync` additionally enforces `ValidateIntegrity`: 420–720 demo products, `Roots == 15`,
  `Brands >= 20`, `Tags >= 30` — a fixed-range contract that assumes a clean catalog, not an additive one over
  pre-existing legacy products (the likely source of the fast 400, `catalog.demo.failed`; the exact message was
  not captured because that response body was empty).

Consequence: **no legitimate owner-supported action available to the worker can create a ≥4× storefront-visible
catalog.** Producing one requires either publishing hundreds of demo products (a product/publish decision, not a
measurement step) or adding a new Published-fixture mode to the owner seeder (new production code outside this
task's bounded scope), or hand-cloning Published rows (an invented fixture, explicitly forbidden).

## 3. What was and was not measured this task

* **Not measured:** size B at ≥4× — structurally unavailable, as above. No ratio, no per-item command growth and
  no B-side latency is claimed.
* Size A and the previously proven results from TB-180 remain the current numbers: 5 products / 6 offers /
  22 prices / 8 stock positions / 4 seller parties / 46 localized texts; warm `GET /v1/storefront/home?locale=fa`
  = 16,279 B, body `E4EB276E…`, **37 request-scoped EF commands per request**, ~406 startup commands,
  stdout 1,059,974 → 23,121 B, zero error lines, cross-leg bodies byte-identical.
* Tenant isolation was not newly exercised (the guard admits exactly one Active tenant); no claim is made.

## 4. Code / tests / configuration

**No production, test or configuration change.** Nothing was weakened, no flag flipped in committed config, no
baseline rebaselined. The only artifacts are this evidence folder and the task receipt. No focused test was
needed because no code changed.

## 5. Release gates

`PERF-CRIT-001` stays **OPEN — CRITICAL / RELEASE BLOCKER**. The measurement gap is now attributable to a
**product/fixture decision**, not to a runtime defect: the owner demo catalog is Draft-only by contract while the
storefront composes only Published data. Architect decision needed on exactly one of:

1. authorize a bounded publish step for a disposable demo catalog (publish demo products, then measure), or
2. authorize an owner-side `Published` fixture mode for the seeder, or
3. accept size-A evidence plus a command-accounting test at a synthetic unit level instead of a second runtime size.

Until one is chosen, no further worker measurement can legitimately close the gap. Architect review and
user-perceived responsiveness remain separate gates.

## 6. Isolation and cleanup

Task-owned only: container `tooba-tb181-pg`, db `tooba_181_scratch` / `tooba_181_messaging`, ports `55501`/`5452`
(verified closed), exact-PID Host processes killed, `.tmp-181-run` and the local scratch-password file deleted.
Shared `:3000`/`:5088`/`:5432`, `tooba_alpha`, shared `store-alpha`, the pre-existing shared Host and Next,
Redis/RabbitMQ and `.next` caches were untouched. No credential or connection string is in the evidence.
