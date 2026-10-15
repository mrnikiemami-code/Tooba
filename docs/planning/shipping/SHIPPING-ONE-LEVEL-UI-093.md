# SHIPPING-ONE-LEVEL-UI-093 — reproducible Development Host + real admin auth + shared-stack UI proof

Task: `TB-SHIPPING-ONE-LEVEL-TABS-093` · Parent: `TB-SHIPPING-ONE-LEVEL-TABS-092`
Program: `TOOBA-SHIPPING-ONE-LEVEL-LIVE-CLOSURE` · Mode: `BOUNDED_REPRODUCIBLE_DEV_HOST_AND_ADMIN_AUTH_REPAIR`
Stop-State: `STOP_FOR_ARCHITECT` · `automaticNextImplementationTask: NONE`
Status: **PASS** for the bounded scope of this task (shared `:5088` now serves current code from a
repo-owned, reversible operator procedure, and the shared `:3000` admin UI genuinely shows the five
one-level tabs for an authenticated Development admin).

---

## 1. Base / drift

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `4bdd9901a38101fb4160a45cec7de7dee8306684` |
| Base-SHA (pre-work `HEAD`) | `9a362da1aee719997139a8c9f38fdf1970261328` |
| `origin/main` at preflight | `9a362da1aee719997139a8c9f38fdf1970261328` (equal) |
| Drift `4bdd9901a..HEAD` | 2 files, both 092 result bookkeeping only |

Non-overlapping 092 result-bookkeeping drift (`RESULT.bridge.txt`, `post-result.js`) — not
`RECOVERY_CONFLICT`. No `src/**` file differed at base.

---

## 2. Mission A — reproducible, reversible Development Host lifecycle

### 2.1 What was missing

`scripts/dev/` did not exist. The only recorded artefacts were task-scoped evidence scripts, and the
prior "relaunch" helper (`docs/architecture/evidence/TB-SHIPPING-FULL-OPERATIONS-079/relaunch-host.ps1`)
stops *whatever owns the port* with `Stop-Process -Force` and has no PID file, no ownership proof and no
health/provenance gate. That is exactly the pattern the Architect ruling forbids.

### 2.2 What was added

`scripts/dev/host-stack.ps1` — one script, actions `start | stop | restart | status`, with:

| Property | Implementation |
| --- | --- |
| Deterministic paths | repo root derived from `$PSScriptRoot`; project `src\backend\Host\Tooba.Host\Tooba.Host.csproj`; content root `src\backend\Host\Tooba.Host`; `OutDir` normalised to an absolute path (relative values resolve against the repo root) |
| Explicit environment | `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://127.0.0.1:<Port>` |
| State | PID file `.tmp-dev-host/host-<Port>.pid` (JSON: pid, url, executablePath, outDir, contentRoot, gate, startedAt); stdout/stderr logs beside it; `.tmp-*` is already git-ignored |
| Verification | `/health` must return 200 **and** `/__platform-commerce` must return the exact Development tuple `SingleStore / dev-singlestore / store-alpha / tenant-alpha`, otherwise the run is refused |
| Refusal | `start` refuses when the port is owned by a process this script did not start, unless the deliberate `-TakeOverProvenDevelopmentHost` flag is passed |
| Ownership proof on stop | recorded PID must be alive, must own the port, and its executable path (compared as normalised absolute paths) must equal the recorded one — otherwise the script throws and stops nothing |
| No force | `stop` is graceful; `Stop-Process -Force` is used only when `-Force` is explicitly passed |
| Gate | `-GateShippingOneLevelCatalog` sets `Tooba__ShippingOneLevelCatalog__Enabled=true` **in the process environment only**; no appsettings file is written. `ShippingRateAuthority` / `ShippingOperationalShadow` are never touched by the script |
| Secrets | no credential, token, connection string or password is written to any file or log |

Deliberate takeover path (`-TakeOverProvenDevelopmentHost`) additionally: requires the current owner to be
`Tooba.Host.exe` whose executable lives under this repository, requires the exact Development provenance
tuple, and **writes a prior-process record** (pid, executable path, command line, parent pid, creation
date, whether the parent is alive, the prior provenance, and the rollback command) to
`.tmp-dev-host/prior-host-<port>-<timestamp>.txt` *before* terminating anything. That record is what makes
the takeover reversible.

### 2.3 Measured lifecycle (shared `:5088`)

| Step | Result |
| --- | --- |
| `status` before takeover | pid=33548, exe `…\bin\Debug\net8.0\Tooba.Host.exe`, start 13:31:13, provenance OK, health 200 — and correctly reported as **not** script-managed |
| `start -TakeOverProvenDevelopmentHost -GateShippingOneLevelCatalog` | prior record written `.tmp-dev-host/prior-host-5088-20261015-210438.txt`; graceful stop of 33548; port free; current-code Host started pid=25236; health OK; provenance OK; PID file written |
| `restart` (no gate) | graceful stop of 25236; restart pid=34512; health OK; provenance OK; PID file rewritten |
| `restart -GateShippingOneLevelCatalog` | graceful stop of 34512; restart pid=33968; health OK; provenance OK |
| `status` after restart | pid=33968, exe `.tmp-093-host-out\Tooba.Host.exe`, `gate=ShippingOneLevelCatalog=true`, provenance OK, health 200 |

The stale orphan (`…\bin\Debug\net8.0\Tooba.Host.exe`, parent already dead, one-level route `404`) was
replaced **only** through this recorded, reversible procedure. Its full prior state is preserved in the
prior-process record, and the rollback command is the recorded executable path.

---

## 3. Mission B — the admin-auth discrepancy, explained without a bypass

### 3.1 Measured matrix on the **old** shared `:5088` (pre-change)

| Credential | `GET /v1/admin/shipping-services` |
| --- | --- |
| real Development login session (bearer, obtained through `:3000` BFF login) | **200** |
| `X-Tooba-Dev-Actor-User-Id: aaaaaaaa-aaaa-4aaa-8aaa-000000000009` | **403** |
| `X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db` (bootstrapped admin) | **200** |
| no credential | **401** |

### 3.2 Root cause

`aaaaaaaa-aaaa-4aaa-8aaa-000000000009` is not an admin identity. It is the **storefront/customer guest
actor**, declared in four backend modules (`StorefrontGuestActor.ActorId`, `GuestActorId` in
Payment/Order, `DemoCustomerActorUserId` in Wallet) and mirrored into the frontend as
`DEFAULT_CUSTOMER_DEV_ACTOR_ID` (`src/frontend/app/customer-panel/customer-api.ts`). It is also reused as
`DEFAULT_DEV_ACTOR_ID` in `src/frontend/lib/auth/constants.ts`, which
`src/frontend/lib/server/host-client.ts` sends as `X-Tooba-Dev-Actor-User-Id` **only when there is no
session cookie**. The Host correctly resolves it to a non-admin subject and denies with `403`
(`admin.authorization.denied`) — fail-closed, exactly as designed.

The admin path is different and already correct: `DEFAULT_ADMIN_ACTOR_ID` is intentionally the empty
string (`src/frontend/lib/admin/admin-result.ts`), and the client-side admin screens call
`prepareAdminDevActor()`, which reads the real actor from the Development-only `GET /v1/admin/dev-context`
(guarded by `IHostEnvironment.IsDevelopment()` + `AdminDevActorBootstrap.Snapshot`) and then send it.

So there is **no identity-wiring defect to repair**: the 403 in task 092 came from probing an admin route
with the guest actor. The legitimate authorized path is the real Development login session, and it works
on both the old and the new Host.

### 3.3 Proof on the shared stack with the **current-code** Host

| Credential | `GET …/shipping-methods/one-level` |
| --- | --- |
| no credential | **401** |
| guest dev actor `aaaaaaaa-…` | **403** |
| real login session (the BFF path) | **200**, `gate=Enabled`, `ready=true`, 7 flat method codes |
| bootstrapped admin actor `01a036c2-…` | **200** |

`GET /api/auth/me` through the shared BFF returns `userId=01a036c2-970e-7000-8eb7-94bf5cc2d8db`, i.e. the
authorized BFF session **is** the same actor that Fulfillment's admin read endpoints recognize. No
`AllowAnonymous`, no permission widening, no ACL disabling, no hardcoded production admin, no committed
actor id or password.

### 3.4 Documented residual finding (NOT changed — needs Architect approval)

The server-side fallback in `src/frontend/lib/server/host-client.ts` (line 32) sends the guest/customer
actor id for anonymous server-rendered admin reads. That is a narrow, real mismatch, but it **fails
closed** (403 → the page renders the legacy screen with the one-level gate disabled), and correcting it
would mean either giving the RSC a session-independent admin identity or having the server call the
Development dev-context endpoint — both are identity/security decisions outside this task's authority.
Recorded here and in the result; no source file was edited.

---

## 4. Mission C — shared Development UI proof

Origin used: **shared** `http://localhost:3000` (the pre-existing Next dev server, PID 31580, untouched)
against the **shared** `:5088` (now current code). No isolated pair was used.

Browser session: real Development login through the shared BFF
(`POST /api/auth/login`, `admin-actor@tooba.local`) → `/api/auth/me` 200 → then
`http://localhost:3000/fa/admin/shipping-services`.

### 4.1 Five-tab strip and seven flat methods (rendered DOM)

`dir=rtl`, `lang=fa`. Tabs: `shipping-one-level-tab-services` (سرویس‌ها),
`shipping-one-level-tab-rates-coverage` (تعرفه و پوشش), `shipping-one-level-tab-packaging-fare`
(بسته‌بندی و کرایه), `shipping-one-level-tab-schedule-capacity` (برنامه کاری و ظرفیت),
`shipping-one-level-tab-cost-review` (بررسی هزینه).

Rows — flat, one per method, no second-level editor:

```
پیشتاز post:express غیرفعال        معمولی post:standard فعال
پیشتاز tipax:express فعال          معمولی tipax:standard فعال
اسنپ / پیک آنلاین snapp_courier غیرفعال
پیک فروشگاه store_courier فعال     تحویل حضوری in_person فعال
```

`post:express` and `snapp_courier` are present, marked inactive, and were **not** activated.

### 4.2 Tabs 2–5 after selecting `tipax:standard` (selection preserved)

| Tab | Observed on shared stack |
| --- | --- |
| 2 rates/coverage | `روش یک‌سطحی tipax:standard → سرویس tipax · standard`, coverage `۱ از ۱`, active rate `۱۸۰٬۰۰۰ IRR` (unconditional), inactive `۱۲۰٬۰۰۰ IRR` (weight band 0–2000 g), third policy shown; inactive policies are displayed, never hidden or activated |
| 3 packaging/payer | `۱۵٬۰۰۰ IRR`, `کرایه: پیش‌کرایه`, inactive badge, plus the explicit note that real COD/collect-on-delivery settlement is not yet connected to Checkout/payment |
| 4 schedule/capacity | `Asia/Tehran`, `برنامه‌های فعال: ۱ از ۱ · مجموع بازه‌ها: ۲`, windows Sunday 13:00–14:00 and 16:30–18:00, `ظرفیت تنظیم‌شده (هنوز اعمال نمی‌شود): ۲۵` |
| 5 cost review | read-only evaluation completed: `داخل بازهٔ فعال حامل`, `Asia/Tehran`, Sunday 13:30, matching window id, `ظرفیت: فقط تنظیم شده؛ رزرو/اجرای ظرفیت وجود ندارد`, `کد دلیل کرایه: shipping.service.not_priced`, plus the standing note that Checkout still prices from its old reference and this number is written to no order |

### 4.3 Network evidence (real browser, shared origin)

```
name          : http://localhost:3000/v1/admin/shipping-services/tipax/operational-quote
initiatorType : fetch
duration      : 124 ms
transferSize  : 743
```

The quote is a **read-only POST** issued from the page against the shared origin; no merchant write, no
financial collection, no order mutation was performed.

Screenshots: `01-shared-3000-five-tabs-rtl.png`, `02-shared-3000-cost-review-rtl.png`.

---

## 5. Rollback proof (measured, not asserted)

`restart` **without** the gate flag returned the stack to the default state:

| Check | Result |
| --- | --- |
| `GET …/shipping-methods/one-level` (authorized) | `200 {"gate":"Disabled","ready":false,"methods":[]}` |
| legacy `GET /v1/admin/shipping-services` | `200` — the original public catalog is preserved |
| shared `GET http://127.0.0.1:3000/fa/admin/shipping-services` | `200`, `oneLevelTabStrip=False` — the legacy two-level screen is back |
| `status` | pid=34512, `gate=defaults (all gates off)`, provenance OK, health 200 |

So the gate flip is a genuine one-flag, reversible operation, and `-Action stop` + `-Action start`
(without the flag) is the documented rollback. The shared stack was then returned to the operator state
below.

---

## 6. Current operator state (kept deliberately, and reversible)

| Fact | Value |
| --- | --- |
| `:5088` | pid **33968**, exe `.tmp-093-host-out\Tooba.Host.exe` (current code), loopback only |
| Gate | `Tooba__ShippingOneLevelCatalog__Enabled=true`, **ephemeral process environment**; not in any `appsettings*.json` |
| `ShippingRateAuthority` / `ShippingOperationalShadow` | never set by the script → remain OFF |
| Provenance | `SingleStore / dev-singlestore / store-alpha / tenant-alpha` → `tooba_alpha` |
| `:3000` | untouched (pre-existing Next dev server, PID 31580) |
| State/rollback | `.tmp-dev-host/host-5088.pid`, prior-process record `.tmp-dev-host/prior-host-5088-20261015-210438.txt` |

Turn it off: `powershell -File scripts/dev/host-stack.ps1 -Action restart -Port 5088 -OutDir .tmp-093-host-out -NoBuild`
Turn it on:  `powershell -File scripts/dev/host-stack.ps1 -Action restart -Port 5088 -OutDir .tmp-093-host-out -NoBuild -GateShippingOneLevelCatalog`
Inspect:     `powershell -File scripts/dev/host-stack.ps1 -Action status -Port 5088`

It is safe to keep because the listener is loopback-only, authorization is unchanged (401/403 proofs
above), the gate is process-scoped and one flag away from off, and the prior process is recorded.

---

## 7. Data parity (read-only, after all work)

| Fact | Value |
| --- | --- |
| One-level methods / translations | `7` / `14` |
| Legacy `shipping_services` / `shipping_service_options` | `5` / `4` |
| `order.seller_orders` / `order.order_lines` / `fulfillment.fulfillments` | `26` / `37` / `9` |
| Migration `AddShippingOneLevelCatalog` | applied once (task 092 evidence, unchanged) |
| Checkout legacy authority | `POST /v1/storefront/shipping/projection` → `401 checkout.authentication_required` (unchanged) |

No seed, backfill, migration, order, payment or reservation write occurred.

---

## 8. Tests

| Suite | Result |
| --- | --- |
| `Tooba.Host.Tests` filtered `AdminPanelAuthorization` | **4/4 pass** |
| Frontend `lib/auth/csrf.test.ts` + `lib/auth/login-return-to.test.ts` | **7/7 pass** |

No production source file was changed, so no broader suite was required; the 092 evidence (56/56 frontend,
45/45 backend one-level) stands by reference.

---

## 9. Changed paths

- `scripts/dev/host-stack.ps1` (new) — the repo-owned Development Host lifecycle script
- `docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-093.md` (new)
- `docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-093/**` (new)
- `docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md` (append)
- `docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-093.task.md` (receipt, **unstaged**)

No `src/**` file was modified.

---

## 10. Remaining blockers (unchanged, none of them blocks the five-tab UI)

- Server-side anonymous admin fallback uses the guest actor id → anonymous RSC admin reads get `403` and
  the one-level gate reads as disabled. Needs an identity decision (documented in §3.4).
- Trusted packed-weight provenance; atomic capacity claim/release; COD ledger/settlement; true customer
  delivery promise; merchant-approved real tariffs/packaging/coverage plus an explicit Checkout cutover;
  `snapp_courier` completeness.
- No coverage-grant read list exists (create-only route).

## 11. Elapsed

~24 minutes (hard timebox 30 minutes, not exceeded).
