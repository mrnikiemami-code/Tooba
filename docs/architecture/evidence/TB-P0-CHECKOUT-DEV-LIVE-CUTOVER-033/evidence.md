# TB-P0-CHECKOUT-DEV-LIVE-CUTOVER-033 — authorized same-environment Host cutover + read-only smoke

Base-SHA (== origin/main == HEAD at claim): fd17e4b623916e0820caf278b4f7ece35de54774
Stop-State: USER_REVIEW · automaticNextImplementationTask: NONE
Changed paths: evidence only (no source/build/config change).

## 1. Preflight (read-only)

| Fact | Value |
| --- | --- |
| Old Host PID | 39992 (`Tooba.Host.exe`) |
| Old binary | `...\Tooba.Host\bin\Debug\net8.0\Tooba.Host.exe`, version `1.0.0+207dae32fd899584d8057405901593aecf222e45` |
| Bind | `127.0.0.1:5088` (loopback only) |
| Launch method | `cmd /c %TEMP%\tooba-host-008-launch.cmd` → `dotnet run --project src\backend\Host\Tooba.Host --no-launch-profile --no-build` (parent `dotnet.exe` PID 10512) |
| Launch env | `ASPNETCORE_ENVIRONMENT=Development`, `ASPNETCORE_URLS=http://127.0.0.1:5088` |
| Config in output | `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json` (no "Urls"/Kestrel/ConnectionStrings keys at root; DB/queue come from the `Tooba` section and Development config) |
| Dependencies | `postgres-db` (postgres:17, :5432) and `rabbitmq` (:5672/:15672) up 2 days; Next dev node PID 31580 on :3000; Bridge PID 23716 on :17321 |
| Drift | HEAD == origin/main == Expected-Base-SHA; no rebase/stash/reset needed |

## 2. Reversible backup (verified)

* External backup: `D:\Users\User\cutover-backups\TB-P0-CHECKOUT-DEV-LIVE-CUTOVER-033\old-host-207dae32\net8.0`
  — robocopy of the whole live output: **583 files / 88.1 MB**, identical to source.
* `Tooba.Host.dll` SHA256 (src == backup): `DCB520C1B7C93933D26524E2F3FD25F9DD2A6E365DAA944ECDA3DB515B92483B`
* Launch script backed up at `...\old-host-207dae32\tooba-host-008-launch.cmd`.
* In-place previous output retained as `...\bin\Debug\net8.0.pre-033-20261013-181119` (instant rename, instant rollback).

### Rollback (exact)
```powershell
Stop-Process -Id <current-new-host-pid> -Force
cd D:\Users\User\source\repos\SarvNewVer\src\backend\Host\Tooba.Host\bin\Debug
Move-Item net8.0 net8.0.failed-033
Move-Item net8.0.pre-033-20261013-181119 net8.0      # or robocopy the external backup back
Start-Process cmd.exe -ArgumentList "/c","C:\Users\User\AppData\Local\Temp\tooba-host-008-launch.cmd" -WindowStyle Hidden
# verify: netstat 127.0.0.1:5088 + version 1.0.0+207dae32...
```
No DB restore is needed (no schema/migration change). Business rows created later by the user are **not** rolled back by any of this.

## 3. Staging build (live PID kept serving the whole time)

```powershell
New-Item -ItemType Directory -Force -Path D:\Users\User\cutover-staging\TB-033\net8.0
dotnet build src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Debug -o D:\Users\User\cutover-staging\TB-033\net8.0 --nologo
```
Result: **0 Error(s), 11 Warning(s), 34.4 s**, exit 0. Staged output verified: `Tooba.Host.exe` present,
776 files / 90.5 MB, all three `appsettings*.json` copied, **identical `Tooba.*.dll` name set (163)** as the
live output, `ProductVersion = 1.0.0+fd17e4b623916e0820caf278b4f7ece35de54774` (matches HEAD).
No migration, seed, DB write, PSP call or wallet operation was performed at any point.

## 4. Cutover (same environment, same port)

```powershell
Stop-Process -Id 39992 -Force            # old Host; parent dotnet run exited by itself
Move-Item <live net8.0> <net8.0.pre-033-20261013-181119>
Move-Item D:\Users\User\cutover-staging\TB-033\net8.0 <live net8.0>   # same volume -> atomic rename
Start-Process cmd.exe -ArgumentList "/c","C:\Users\User\AppData\Local\Temp\tooba-host-033-launch.cmd" -WindowStyle Hidden
```
New launcher is a byte-for-byte copy of the 008 launcher except the log file name (`tooba-host-033.log`):
same `cd`, same env vars, same `--no-launch-profile --no-build`, same port.

| | Old | New |
| --- | --- | --- |
| PID | 39992 (stopped) | **37028** (`Tooba.Host`) |
| Version | `1.0.0+207dae32…` | **`1.0.0+fd17e4b62…`** |
| Bind | 127.0.0.1:5088 | 127.0.0.1:5088 |
| Health | n/a | listening within 12 s, MassTransit transport consuming (`pricing.*` events), no startup error |

## 5. Read-only HTTP smoke (no mutation; random non-existent cart GUID)

| Probe | Status | Interpretation |
| --- | --- | --- |
| `GET /v1/storefront/payment-methods` (5088) | **200** | catalog OK, offers `gateway` + `manual` (no fabricated wallet) |
| `GET /v1/storefront/cart/{random}/payment-draft` | **401** | **route registered**, rejected at the identity gate (not 404) |
| `GET /v1/storefront/cart/{random}/wallet-quote` | **401** | **route registered**, identity gate |
| `POST /v1/storefront/cart/{random}/start-payment` body `{}` | **401** | **route registered**, identity gate — rejected before any commit/hold/payment |
| `GET /v1/storefront/cart/{random}/definitely-not-a-route` (control) | 404 | proves the 401s above are route presence, not a catch-all |
| `GET /v1/storefront/payment-methods` via Next proxy :3000 | **200** | `next.config.ts` rewrite `/v1/:path*` → `127.0.0.1:5088` still correct |
| `GET /v1/storefront/cart/{random}/wallet-quote` via Next proxy :3000 | **401** | same route through the browser-facing origin |

401 = `checkout.authentication_required` (Development checkout-identity policy) — expected fail-closed
behaviour for anonymous probes, and identical body shape for all three new routes.

## 6. Non-actions (explicit)

* No database migration/seed/write; no schema change; no business-data deletion.
* No PSP request, no gateway call, no manual-evidence submit, no wallet debit, no checkout commit,
  no fake order created by this task (all probes failed closed at the identity gate).
* PostgreSQL, RabbitMQ, Next dev (:3000) and Bridge (:17321) were never stopped or restarted;
  only the single verified Tooba Development Host process was replaced.
* No new environment, port, database or container was created.
* Frontend source/build unchanged; no feature or architecture change in this task.

## 7. Environment after cutover

```
127.0.0.1:5088  PID 37028  Tooba.Host        (new binary)
127.0.0.1:3000  PID 31580  node             (untouched)
127.0.0.1:17321 PID 23716  CursorChatBridge (untouched)
5432 / 5672 / 15672 docker (postgres-db, rabbitmq) up 2 days (untouched)
```

## 8. Test readiness verdict

**LIVE — READY_FOR_USER_REVIEW.** The updated Development Host (`1.0.0+fd17e4b62…`) is running on the same
`:5088` with the new `payment-draft`, `wallet-quote` and `start-payment` routes registered and the frontend
proxy working. The worker performed **read-only** checks only; the real Checkout / Wallet / PSP click tests
are the user's to run, and no live-money PASS is claimed.
