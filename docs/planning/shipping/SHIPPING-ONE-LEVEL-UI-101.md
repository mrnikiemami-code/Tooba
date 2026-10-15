# SHIPPING-ONE-LEVEL-UI-101 — real Development storefront label proof (read-only)

Task: `TB-SHIPPING-ONE-LEVEL-TABS-101` · Mode
`BOUNDED_SHARED_DEVELOPMENT_STOREFRONT_LABEL_READ_ONLY_PROOF` · Parent `TB-SHIPPING-ONE-LEVEL-TABS-100`
Program `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Stop-state `STOP_FOR_ARCHITECT`

**Status: PARTIAL** — Mission A (controlled current-code rollout) PASS; Mission B (real storefront
projection label) could not be exercised because there is no safe read-only path to an owned cart on
the shared Development data set. Blocker code: `BLOCKED_REAL_STOREFRONT_PROOF`.

## 1. Preflight

| Check | Result |
| --- | --- |
| Claimed Bridge task | exactly one Pending task on `tooba-main`; receipt preserved **UNSTAGED** at `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-101.task.md` |
| Expected-Base-SHA vs HEAD | `Expected-Base-SHA` 5e7c9c141…; `origin/main` = HEAD = 4fc2c4b0d… — one intervening commit, docs-only (`RESULT.bridge.txt` bookkeeping, +2/−1). `git diff --stat 5e7c9c14..HEAD -- src` empty ⇒ no `RECOVERY_CONFLICT` |
| Working tree | only the two pre-existing dirty frontend screens (`admin-fulfillment-work-queue-screen.tsx`, `admin-order-detail-screen.tsx`) + prior untracked evidence — preserved untouched, no reset/stash/rebase/force-push |
| Shared `:5088` | listener owned by the managed dev host (preflight PID 29788, `.tmp-dev-host/host-5088.pid`), `/health` 200, `/__platform-commerce` = `SingleStore / dev-singlestore / store-alpha / tenant-alpha`, gate `ShippingOneLevelCatalog=true` |
| Shared `:3000` | Next dev server rewrites `/v1/:path*` → `http://127.0.0.1:5088` (`TOOBA_HOST_ORIGIN` in `.env.local`), middleware attaches `Authorization: Bearer <tooba_session cookie>` ⇒ same backend target, never restarted |
| Read-only baseline | `baseline-preflight.{sql,txt}` — 7 one-level methods / 14 translations; `post:standard` **active**, fa-IR name «معمولی», en-US Standard; `post:express` **inactive**; `snapp_courier` **inactive**; legacy service/option state and the 3 languages captured |
| Staleness | running build was task-098 (01:55 local) — **before** the 100 seam commit (02:45) and its DLLs have no seam symbol ⇒ a rollout was required |

## 2. Mission A — controlled current-code host, zero data writes

The Host was **not** killed or replaced by hand. The rollout used only the repository's own lifecycle
script, keeping the task-owned isolated output directory:

```text
powershell -NoProfile -File scripts/dev/host-stack.ps1 -Action restart \
  -OutDir .tmp-dev-host-out-5088-101 -GateShippingOneLevelCatalog
```

- build: `0 Error(s)` / 19 warnings, `Time Elapsed 00:00:33.46` (log: `host-restart-101.txt`)
- rollout: `pid=37200`, exe `.tmp-dev-host-out-5088-101\Tooba.Host.exe`, `gate=ShippingOneLevelCatalog=true`
  as a **process-environment-only** ephemeral value; script explicitly never sets `ShippingRateAuthority`
  or `ShippingOperationalShadow`
- verification: `/health` → `{"status":"ok"}`; provenance OK (`SingleStore / dev-singlestore / store-alpha /
  tenant-alpha`); PID file written; rollback path printed by the script
- seam really deployed: `Tooba.Fulfillment.Contracts.dll`, `Tooba.Fulfillment.Infrastructure.dll` and
  `Tooba.Order.Application.dll` in the rollout contain the label-seam symbols (SHA-256 recorded in
  `deployment-facts.txt`)
- gate state was ON before and stays ON — nothing was enabled or disabled by this task; `:3000` untouched;
  no appsettings/migration/seed edit; no second shared Host

A **second, disclosed** rollout note: the first attempt inside this same task used the script's default
output directory (`.tmp-dev-host-out-5088`, `pid=41824`, current code, gate ON — log
`host-restart-101-intermediate-superseded.txt`) and was replaced by the task-named isolated directory
above so the rollout owns a task-specific build path. Both were script-managed, graceful, current-code,
gate-ON rollouts; only one build was kept as the task's deployment of record. All required verification
(health, provenance, seam symbols, hashes) was performed on the final `pid=37200` rollout.

## 3. Mission B — what was attempted and why it stopped short

Reaching `POST /v1/storefront/shipping/projection` (the only consumer of the Order label seam) needs
**both** a session passing `ICheckoutIdentityGate` **and** a cart the session may read
(`ICartPresentationGateway` access check).

```text
POST /v1/storefront/shipping/projection        (read-only query, no writes possible)
  anonymous                          -> 401 checkout.authentication_required
  bogus bearer / bogus guest secret  -> 401 checkout.authentication_required
  unknown cart                       -> 401 checkout.authentication_required
  authenticated session + Active guest cart          -> 401 cart.guest.invalid
  authenticated session + another user's Converted cart -> 401 cart.access.denied
  authenticated session + unknown cart               -> 404 shipping.cart.missing
```

A real Development session **does** exist on the shared frontend origin (`GET /v1/auth/me` → 200), so the
identity precondition is satisfiable. The cart precondition is not:

- the session user owns **0 carts** (read-only SELECT);
- no **Active Authenticated** cart exists (13 Converted, 1 Expired Authenticated; 8 Active Guest,
  11 Abandoned, 17 Expired Guest);
- the 8 Active guest carts require their guest secret, which is returned once at creation and stored only
  as a digest — it is not recoverable, and no trace exists in the repo, evidence or dev logs.

Obtaining a readable cart would require DML (guest cart creation or line insert) or credential guessing,
both explicitly forbidden. Therefore the storefront page was deliberately **not** visited (its cart page
can lazily create a cart = DML), no order/checkout/payment/draft call was made, and the projection is
reported as not exercised — PARTIAL, never an invented PASS.

Positive read-only proof that the live current-code process serves the intended name:

```text
GET /v1/admin/shipping-services/shipping-methods/one-level   (authenticated shared session)
200 {"gate":"Enabled","ready":true,"languageId":"01a06a53-4139-7000-bb9b-3078a0d2e5c4",
     "methods":[..., {"methodCode":"post:standard","name":"معمولی","isActive":true,...}, ...]}
```

so the Fulfillment-owned one-level name «معمولی» for `post:standard` genuinely resolves in fa-IR on the
same process the storefront projection would call; the still-missing step is purely the Order-side
substitution rendering, which the 100 unit tests already assert and which no reachable cart can display here.

## 4. Invariants and absence of writes

- `post:standard` remains active with fa name «معمولی»; `post:express` and `snapp_courier` remain inactive
  and did **not** become newly eligible; method set (7) and translations (14) unchanged; 26 seller orders;
  50 carts / 78 lines / 14 drafts unchanged.
- No-write proof (`no-write-proof.{sql,txt}`): newest `cart.carts` row 2026-10-15 09:48Z, newest
  `cart_shipping_drafts.updated_at` 2026-10-14, newest `fulfillment.shipping_methods` change
  2026-10-15 14:45Z — all **before** the task window (23:20–23:40Z), i.e. zero rows created or modified.
- No activation write, no name edit, no DDL, no fixture in the real DB, no auth bypass, no payment-start.
- Gate-OFF comparison: **unit-test-only** (task 100 `StorefrontShippingOneLevelLabelTests`). A live gate-OFF
  restart was not performed because the label path cannot be exercised in either gate state on this data
  set — a restart would have proven nothing and would have risked the shared stack.

## 5. Restore / rollback

Preflight gate was ON ⇒ left ON. Current rollout `pid=37200`,
`outDir=.tmp-dev-host-out-5088-101`. Replaced state (task-098 build,
`.tmp-dev-host-out-5088-098`, same gate, same provenance) is documented in `deployment-facts.txt`.

```text
powershell -NoProfile -File scripts/dev/host-stack.ps1 -Action stop
powershell -NoProfile -File scripts/dev/host-stack.ps1 -Action start \
  -OutDir .tmp-dev-host-out-5088-098 -GateShippingOneLevelCatalog
```

No repository rollback is needed for the deployment itself: **no `src/**`, config, schema or data change
is part of this task**. Only docs/evidence were committed.

## 6. What the Architect must decide

1. Is a label-only live storefront proof still required, and if so should a future task be authorized to
   create a throwaway Development cart (DML) to reach projection, or should the Architect supply an owned
   cart / guest credential?
2. The live gate-OFF/ON storefront comparison remains unit-test-only until such a readable cart exists.
3. This task spent ~24 minutes inside the 30-minute HARD timebox (Bridge claim 02:48 → ~03:12 local) — **no overrun**.
