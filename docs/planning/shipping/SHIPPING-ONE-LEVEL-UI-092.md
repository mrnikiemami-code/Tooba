# SHIPPING-ONE-LEVEL-UI-092 — Development deploy + real five-tab smoke

Task: `TB-SHIPPING-ONE-LEVEL-TABS-092` · Parent: `TB-SHIPPING-ONE-LEVEL-TABS-091`
Program: `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Mode: `BOUNDED_DEVELOPMENT_DEPLOY_AND_REAL_FIVE_TAB_SMOKE`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Status: **PARTIAL** (live UI + all five tabs proven on a task-scoped current-code Host/FE pair; the
shared Development `:5088` process was **not** replaced because no operator-safe stop/start provenance
could be established — see §5)

---

## 1. Base / drift

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `903c5d839146874af88a5e0ce1bbe7b73b46b445` |
| Base-SHA (pre-work `HEAD`) | `1a63b46be85b9a3e1b4091b39df1a9f102306089` |
| `origin/main` at preflight | `1a63b46be85b9a3e1b4091b39df1a9f102306089` (equal → no divergence) |
| Drift `903c5d8..HEAD` | 2 files, both 091 result bookkeeping only |

Drift classification: **non-overlapping 091 result-bookkeeping drift**, not `RECOVERY_CONFLICT`.

```
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-091/RESULT.bridge.txt
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-091/log-post-091.txt
```

No source file under any `src/**` path differs between the expected base and the actual base.
Pre-existing dirty/untracked work was preserved; no reset/stash/rebase/force-push was performed.

---

## 2. Live Development provenance (`:5088` / `:3000`) — read-only, never restarted

| Fact | Observed value |
| --- | --- |
| Listener `:5088` | `127.0.0.1:5088`, PID **33548** (`Tooba.Host`) |
| Executable | `src\backend\Host\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe` |
| Command line | `"...\Tooba.Host.exe"` (no extra args; launched via `launchSettings.json` profile `Tooba.Host`) |
| Parent PID | 21968 — **no longer present**; the listener is orphaned (parent exited) |
| Creation time | `2026-10-15 13:31:13` |
| Working directory | not recoverable from the orphaned process; config resolves to the repo output dir |
| `GET /__platform-commerce` | `edition=SingleStore`, `deploymentId=dev-singlestore`, `tenantId=store-alpha`, `connectionReference=tenant-alpha`, `themeReference=theme-alpha`, `defaultMarketReference=IR` → `tooba_alpha` |
| `GET /health` | `{"status":"ok"}` |
| Listener `:3000` | PID **31580** (`node` → `next/dist/server/lib/start-server.js`), Next.js dev server |
| `TOOBA_HOST_ORIGIN` for `:3000` | unset → default `http://127.0.0.1:5088` (from `lib/server/host-client.ts`) |
| Live bin config vs repo | `appsettings.json` and `appsettings.Development.json` SHA-256 **identical** between repo and live bin dir |

### 2.1 What the shared `:5088` binary actually does (measured, not assumed)

| Probe | Result | Meaning |
| --- | --- | --- |
| `GET /v1/admin/shipping-services/shipping-methods/one-level` (no credential) | `404` | route not mounted in the running (stale) binary |
| `GET /v1/admin/shipping-services/shipping-methods/one-level` (dev actor header) | `404` | same |
| `GET /v1/admin/shipping-services` (dev actor header `01a036c2-…`) | `200` | legacy two-level catalog is alive and authorized |
| `GET /v1/admin/shipping-services` (FE default actor `aaaaaaaa-…-000000000009`) | `403` | the FE's hard-coded dev actor is **not** the bootstrapped admin → the real FE cannot authorize against `:5088` today |

`:5088` therefore remains on the pre-085 binary and is **not** the one-level host. This is the same
condition recorded by task 091; it was **not** changed.

### 2.2 Checkout-path evidence against the shared `:5088`

`POST /v1/storefront/shipping/projection` (gate-relevant public route) returns
`401 checkout.authentication_required` — the legacy public shipping/Checkout surface still answers on
its own unchanged code path. No order, payment, reservation or fulfillment row was written at any point
in this task.

---

## 3. Task-scoped current-code pair (isolated, non-disruptive)

Because §2 could not prove a safe stop/start procedure for PID 33548 (§5), a **task-scoped** pair was
run on free ports and torn down at the end. Originals `:5088` and `:3000` were never signalled.

| Component | Value |
| --- | --- |
| Host binary | `.tmp-091-host-out/Tooba.Host.exe` (current-code build produced and hash-pinned by task 091, reused unchanged) |
| Host port | `127.0.0.1:5100` |
| Host content root | `src\backend\Host\Tooba.Host` (`--contentRoot`), working dir `.tmp-091-host-out` |
| Host environment | `ASPNETCORE_ENVIRONMENT=Development` |
| Host DB scope | **real Development** `dev-singlestore` / `store-alpha` / `tenant-alpha` → `tooba_alpha` (verified via `GET /__platform-commerce` on `:5100`) |
| Ephemeral gate | `Tooba__ShippingOneLevelCatalog__Enabled=true` — **process env var only**, never written to any `appsettings*.json` |
| Background jobs | `Tooba__Outbox__Enabled=false`, `Tooba__CartExpiry__Enabled=false`, `Tooba__PaymentReconciliation__Enabled=false` (no outbox/polling interference with shared data) |
| FE | isolated copy `.tmp-092-fe` (junctioned `node_modules`), Next dev on `127.0.0.1:3100` |
| FE → Host wiring | `TOOBA_HOST_ORIGIN=http://127.0.0.1:5100` (server-side origin actually used by the page) |

A first launch attempt on `:5100` failed with an Npgsql connection error because `Start-Process`
inherited the shell's working directory as content root and read the scratch `appsettings` overlay
shipped in `.tmp-091-host-out`. Re-launching with an explicit `--contentRoot` pointing at the repo Host
project made the process read the real Development configuration; this is recorded as a runnable step
below.

### 3.1 Runnable reproduction

```powershell
# Host (current code, real Development DB, ephemeral gate ON, no background jobs)
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:ASPNETCORE_URLS='http://127.0.0.1:5100'
$env:Tooba__ShippingOneLevelCatalog__Enabled='true'
$env:Tooba__Outbox__Enabled='false'
$env:Tooba__CartExpiry__Enabled='false'
$env:Tooba__PaymentReconciliation__Enabled='false'
Start-Process '.tmp-091-host-out\Tooba.Host.exe' `
  -ArgumentList '--contentRoot','<repo>\src\backend\Host\Tooba.Host' `
  -WorkingDirectory '.tmp-091-host-out'

# Frontend (server-side origin pointed at the task-scoped Host)
$env:TOOBA_HOST_ORIGIN='http://127.0.0.1:5100'
$env:PORT='3100'
# from an isolated copy of src/frontend (node_modules junctioned):
node node_modules\next\dist\bin\next dev --port 3100 --hostname 127.0.0.1
```

Rollback = stop the two task-scoped PIDs; nothing else to undo (no config file changed, no DB write).

---

## 4. Real behaviour — measured

### 4.1 Backend, authenticated, gate ON (`:5100`)

| Probe | Result |
| --- | --- |
| `GET /health` | `200 {"status":"ok"}` |
| `GET /__platform-commerce` | `200` — dev-singlestore / store-alpha / tenant-alpha |
| `GET /v1/admin/shipping-services/shipping-methods/one-level` **unauthorized** | `401` |
| `GET …/one-level/post:standard/binding` **unauthorized** | `401` |
| `GET /v1/admin/shipping-services/shipping-methods/one-level` **authorized** | `200`, `gate=Enabled`, `ready=true`, **7** method codes, flat shape (no provider/options nesting) |

Authorized payload (verbatim):

```json
{"gate":"Enabled","ready":true,"methods":[
 {"methodCode":"post:express","name":"پیشتاز","isActive":false,"sortOrder":10},
 {"methodCode":"post:standard","name":"معمولی","isActive":true,"sortOrder":11},
 {"methodCode":"tipax:express","name":"پیشتاز","isActive":true,"sortOrder":20},
 {"methodCode":"tipax:standard","name":"معمولی","isActive":true,"sortOrder":21},
 {"methodCode":"snapp_courier","name":"اسنپ / پیک آنلاین","isActive":false,"sortOrder":30},
 {"methodCode":"store_courier","name":"پیک فروشگاه","isActive":true,"sortOrder":40},
 {"methodCode":"in_person","name":"تحویل حضوری","isActive":true,"sortOrder":50}]}
```

Inactive methods `post:express` and `snapp_courier` are present **and remain inactive**; none was activated.

### 4.2 Server-owned binding resolver

| Method code | `resolved` | `serviceCode` / `optionCode` | `isActive` |
| --- | --- | --- | --- |
| `post:standard` | true | `post` / `standard` | true |
| `post:express` | true | `post` / `express` | false |
| `snapp_courier` | true | `snapp_courier` / — | false |
| `unknown:code` | **false** | — | false |
| `post` (bare) | **false** | — | false |

Bare legacy codes are not accepted as one-level identities, and unknown codes fail closed with
`resolved=false` rather than being normalized into a new identity.

### 4.3 Tab-by-tab backend reads (authenticated, real data)

| Tab | Endpoint | Result |
| --- | --- | --- |
| 2 — rate/coverage | `GET /v1/admin/shipping-services/post/rates` | `200`, rate policies with `level=City`, `71000 IRR`, `preparationDays=3`, `leadDays=5`, `freeAboveThreshold=950000` |
| 2 — rate/coverage | `GET /v1/admin/shipping-services/tipax/rates` | `200`, `level=AdministrativeArea`, `260000 IRR` |
| 2 — coverage grants | `GET …/coverage-grants` | `405` — the route is **POST-only** by design (create grant); no read list exists. Read-only coverage evidence comes from the per-method rate policies above. |
| 3 — packaging/payer | `GET /v1/admin/shipping-services/tipax/packaging-policies` | `200`, `feeAmount=15000 IRR`, `freightPayerMode=Prepaid`, `isActive=false` |
| 4 — schedule/capacity | `GET /v1/admin/shipping-services/tipax/operating-schedules` | `200`, `timeZoneId=Asia/Tehran`, windows with `capacity=25`, `configuredCapacityTotal=25` |
| 5 — cost inspector | `POST /v1/admin/shipping-services/post/operational-quote` | `200`, `outcome=Quoted`, `freightAmount=120000`, `amountPayableAtCheckout=120000`, `amountCollectOnDelivery=0`, `packaging.reasonCode=shipping.packaging.not_configured` |
| 5 — cost inspector | `POST /v1/admin/shipping-services/tipax/operational-quote` | `200`, `outcome=Quoted`, `freightAmount=180000`, `schedule.availability=Unavailable`, `schedule.reasonCode=shipping.schedule.outside_windows`, `schedule.capacityEnforcement=NotEnforced`, `isOperationallyEligible=false` |
| 5 — unauthorized | `POST …/operational-quote` | `401` |

Every quote call is a **read-only evaluation**: no order, payment, COD ledger, capacity reservation or
promised-delivery record is created. `capacityEnforcement` is reported as `NotEnforced` and
`configuredCapacityTotal` is explicitly labelled "configured only — not enforced".

### 4.4 Browser-visible five-tab UI (real browser, real network)

Driven in a real Chromium tab against `http://localhost:3100`, authenticated through the frontend BFF
(`/api/auth/login` with the bootstrapped Development admin actor), so the page's **server-side** fetch
hit the task-scoped Host `:5100`.

Rendered tab strip (`role=tab`, exact DOM ids and labels):

| # | DOM id | Label (fa) |
| --- | --- | --- |
| 1 | `shipping-one-level-tab-services` | سرویس‌ها |
| 2 | `shipping-one-level-tab-rates-coverage` | تعرفه و پوشش |
| 3 | `shipping-one-level-tab-packaging-fare` | بسته‌بندی و کرایه |
| 4 | `shipping-one-level-tab-schedule-capacity` | برنامه کاری و ظرفیت |
| 5 | `shipping-one-level-tab-cost-review` | بررسی هزینه |

Tab 1 rendered the seven methods **flat, one row per method**, with the inactive badge on
`post:express` and `snapp_courier` — no second-level configuration was rendered:

```
پیشتاز post:express غیرفعال      معمولی post:standard فعال
پیشتاز tipax:express فعال        معمولی tipax:standard فعال
اسنپ / پیک آنلاین snapp_courier غیرفعال
پیک فروشگاه store_courier فعال   تحویل حضوری in_person فعال
```

Document direction during the run: `dir=rtl`, `lang=fa` (RTL verified at least once).

Selecting `tipax:standard` then visiting each dependent tab produced real server-backed content:

- **Tab 2** — `روش یک‌سطحی tipax:standard → سرویس tipax · standard`, coverage `۱ از ۱`, rate policies
  `۱۸۰٬۰۰۰ IRR` (active, unconditional), `۱۲۰٬۰۰۰ IRR` (inactive, weight-banded `0–2000g`),
  `۲۶۰٬۰۰۰ IRR` (inactive). Inactive policies are shown, never hidden or silently activated.
- **Tab 3** — packaging policies `۱۵٬۰۰۰ IRR`, `کرایه: پیش‌کرایه`, inactive badge; the screen states
  plainly that real COD settlement is not yet wired to Checkout/payment.
- **Tab 4** — `Asia/Tehran`, 2 windows, `ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود): ۲۵`; the copy states the
  windows are carrier hand-over availability, not a customer promise.
- **Tab 5** — inputs are explicitly labelled as manual diagnostics (not read from any order). Runs
  produced `خارج از همهٔ بازه‌های فعال حامل` (city scope, Tuesday 09:30) and
  `داخل بازهٔ فعال حامل` (province scope, Sunday 13:30, matching window id returned), with
  `ظرفیت: فقط تنظیم شده؛ رزرو/اجرای ظرفیت وجود ندارد` and the standing note that Checkout still prices
  from its old reference and this number is written to no order.

Screenshots (evidence folder):
`01-five-tabs-services-rtl.png`, `02-cost-review-tab-rtl.png`.

Also verified: the page HTML is served **server-rendered with the gate state and the seven methods
already in the payload** (`gate:{state:"enabled",source:"server-flag"}`), so there is no client-side
bypass and no client fetch that could enable the feature.

---

## 5. Deployment decision — why `:5088` was NOT replaced

The Architect ruling makes replacing the shared process **conditional** on a documented, repeatable
stop/start procedure with proved rollback. The following facts could **not** be established:

1. **Parent PID 21968 no longer exists.** The `:5088` listener is orphaned; there is no supervising
   shell, terminal, IDE debug session or service to reproduce the exact launch. Any restart would be a
   *different* process invocation, not a restart of the same one.
2. **No supported launch/stop script** exists in the repo for the shared Development host (only
   `launchSettings.json`, which is an IDE profile, not an operator script). No operator evidence of a
   managed stop/start was found.
3. **Rollback fidelity is unprovable**: the prior binary can be re-launched, but the exact original
   process command/parent/environment cannot be recreated, so "rollback to the prior process" cannot be
   demonstrated.
4. **The frontend cannot authorize against `:5088`** with its hard-coded dev actor (measured `403`), so
   even a successful binary swap would not by itself produce a browser-visible five-tab page.

Decision: **do not stop or replace `:5088`**, run the task-scoped pair instead, and stop with this
blocker. No kill, no config commit, no permission change, no destructive DB mutation was performed.
`ShippingRateAuthority` and `ShippingOperationalShadow` were never enabled (the one-level gate is
namespace-separated: `Tooba:ShippingOneLevelCatalog`).

### 5.1 Checkout isolation

- Enabling only `Tooba:ShippingOneLevelCatalog` is **structurally incapable** of cutting over Checkout:
  the gate section is owned by Fulfillment and is namespace-distinct from both price-authority and
  operational-shadow sections (`ShippingOneLevelMethodCatalogTests` asserts this separation), and no
  file outside `Tooba.Fulfillment.*` references `ShippingOneLevelCatalog` at all (verified by search).
- The gate default is **disabled by absence** and only flips on an explicit `Enabled=true`.
- Measured with the gate ON on `:5100`, the public storefront shipping projection still returned its own
  unchanged `401 checkout.authentication_required`; no Checkout price/paid snapshot changed.

---

## 6. Data parity (read-only, no writes)

| Fact | Value |
| --- | --- |
| Migration `…AddShippingOneLevelCatalog` rows in `fulfillment.__ef_migrations_history` | `1` (applied exactly once) |
| One-level methods | `7` (`post:express, post:standard, tipax:express, tipax:standard, snapp_courier, store_courier, in_person`) |
| One-level translations | `14` |
| Inactive methods | `post:express, snapp_courier` (unchanged) |
| Legacy public services / options | `5` / `4` (untouched) |
| Historical `order.seller_orders` / `order.order_lines` / `fulfillment.fulfillments` | `26` / `37` / `9` (untouched) |
| Pending checkouts | `13` (untouched) |

No seed or backfill was repeated; no row was inserted, updated or deleted.

---

## 7. Tests run (narrowest meaningful set — no source changed)

| Suite | Result |
| --- | --- |
| Frontend `app/admin/shipping-services/shipping-one-level-*.test.ts` | **56/56 pass** |
| Backend `Tooba.Fulfillment.Tests` filtered `ShippingOneLevel` | **45/45 pass** |

No production source file was changed, so no broader loop was run and no frontend typecheck was required
(the shipped frontend code is byte-identical to the committed tree).

---

## 8. Cleanup / rollback

- Task-scoped Host PID 37528 (`:5100`) and FE PID 32540 (`:3100`) — **stopped**; both ports verified free.
- Isolated FE copy `.tmp-092-fe` — removed (junction removed first so the shared `node_modules` was not
  followed).
- Scratch SQL file removed.
- Original listeners verified still healthy after cleanup: `:5088` → PID 33548, `:3000` → PID 31580.
- Original Development page re-checked in the browser after cleanup: it renders the **legacy two-level**
  screen (no one-level tab strip, no method codes) — i.e. the shared Development surface is exactly as
  it was before this task.
- Oversized host log trimmed to a redacted excerpt; credential scan over the new evidence folder
  returned no `Password=`/token/secret matches.
- Kept for reuse: `.tmp-091-host-out` (current-code Host build), which is outside all committed paths.

---

## 9. Answer to "is the new UI visible to the user NOW?"

**No, not on the shared Development stack.** On the shared `:5088` the one-level route is `404` (stale
binary) and the frontend cannot authorize against it. The five-tab UI **is** fully visible, authorized
and server-backed on the task-scoped pair, which has been shut down per cleanup rules.

## 10. Remaining exact blocker (ONE concrete next corrective action)

There is exactly one blocker: **no reproducible, operator-safe launch/stop procedure for the shared
Development `:5088` Host, plus an unusable frontend→Host credential on that same stack.**

Next corrective action (single, concrete): **restart the shared Development Host from a scripted,
operator-owned launch and align the frontend dev actor with the bootstrapped admin.**
Concretely: (a) add and use a repo-owned `scripts/dev/start-host.ps1` (env `Development`, URL
`http://localhost:5088`, `dotnet run --project src/backend/Host/Tooba.Host`) that records its own PID to
a pidfile, so stop/start is reproducible and reversible; (b) restart `:5088` from that script so the
current-code binary serves `/v1/admin/shipping-services/shipping-methods/one-level`; (c) resolve the
frontend dev-actor mismatch — the BFF currently sends `aaaaaaaa-aaaa-4aaa-8aaa-000000000009`, which the
Host answers with `403`, while the bootstrapped admin is `01a036c2-970e-7000-8eb7-94bf5cc2d8db`; either
the BFF must use the bootstrapped admin actor in Development or the dev actor must be provisioned. Only
then can the gate be flipped for the shared stack and the five-tab UI observed at
`http://localhost:3000/fa/admin/shipping-services` without a task-scoped pair.

## 11. Missing CRUD / commercial blockers (unchanged by this task)

- No coverage-grant **read** endpoint exists (create-only route) — tab 2 shows coverage through rate
  policies.
- Real COD/collect-on-delivery settlement is not connected to Checkout/payment.
- Capacity is configured-only; enforcement is `NotEnforced`, and no reservation exists.
- Checkout still prices from its legacy reference; nothing in this task changed it.

---

## 12. Elapsed & changed paths

- Elapsed: **~21 minutes** (hard timebox 30 min, not exceeded).
- Changed paths in this task:
  - `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-092.md` (new)
  - `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-092/**` (new)
  - `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` (append)
  - `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-092.task.md` (receipt, **unstaged**)
- No `src/**` file was modified.
