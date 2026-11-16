# TB-273 — RECEIPTS

Every command below was executed once, after the final code change. Base
`fcb2fe1cd2325b9378e85e55554bacadcef01b42` == `origin/main` at claim; code
`05cafa54aa4e2f9347ff37f4ce7295cfab1b12b2`. Final HEAD / origin parity are printed in the canonical Bridge result.

## 1. Focused behaviour suite (the named suite)

```
$ dotnet build src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --nologo
0 Error(s)   (3 pre-existing CS8625 warnings in ReturnsErrorAndGridTests.cs)

$ dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --no-build --nologo \
    --filter "FullyQualifiedName~ReturnsRecovery273Tests"
Passed!  - Failed: 0, Passed: 15, Skipped: 0, Total: 15, Duration: 922 ms
```

## 2. Disclosure — broader suites actually executed

```
$ dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --no-build --nologo
Failed! - Failed: 2, Passed: 52, Skipped: 0, Total: 54
  Failed ReturnsCharacterizationTests.Create_approve_refund_preserves_amount_currency_and_is_idempotent
  Failed ReturnsCharacterizationTests.Wallet_destination_credits_wallet_without_psp_refund
  -> both: InvalidOperationException EF-InMemory 'TransactionIgnoredWarning' from
     ReturnDirectory.RecordReturnDecisionAsync (line 38) via ApproveAsync (line 217)
     PRE-EXISTING (identical at base; recorded at 272/272-R1); not touched.

$ dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release --nologo \
    --filter "FullyQualifiedName~ReturnsModuleAmsc"
Failed! - Failed: 2, Passed: 32, Skipped: 0, Total: 34
  Failed ReturnsModuleAmsc001W3CertGuardTests.Returns_is_contracts_only_and_microservice_extractable
  Failed ReturnsModuleAmsc001W1MigrateGuardTests.Migration_did_not_change_the_returns_schema_migrations
  -> both: Assert.Equal() Collections differ on the expected MIGRATION FILE LIST
     (expected stops at 20260909130600_DecimalReturnQuantity.cs, actual also has the 265-era
     20261113162816_AddCourierOriginReturnsLinkage / 20261113174500_NormalizeCourierOriginColumnNames)
     PRE-EXISTING pins; every route/validator/error-catalog count pin updated by this task passes.
```

No whole-solution test run, no Order/Fulfillment/Shipping suite, no parity/baseline comparison.

## 3. Build and publish

```
$ cd src/backend; dotnet build Tooba.slnx -c Release --nologo
    293 Warning(s)
    0 Error(s)

$ cd <repo root>; dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary --nologo
  Tooba.Host -> ...\artifacts\publish-primary\
  PUBLISH_EXIT=0   (1 warning: CS0105 pre-existing duplicate using in AdminDevActorBootstrap.cs)
```

## 4. Runtime refresh (Host only, graceful stop first)

```
old artifacts\publish-primary\Tooba.Host.dll SHA256 3244E778B2B24884D85AE4537961105CA97729B0A9B36AF075534D1FEF8BEEEB
$ Stop-Process -Id 13160            -> listeners on 5088: 0
$ dotnet publish ... (one publish)
$ Invoke-CimMethod Win32_Process Create 'cmd.exe /c set ASPNETCORE_ENVIRONMENT=Development&
    set ASPNETCORE_URLS=http://127.0.0.1:5088& cd /d <publish>& dotnet Tooba.Host.dll >> D:\temp\tb273\host-run.log 2>&1'
ReturnValue=0 PID=38068             -> listener PID 28580 on 127.0.0.1:5088
new artifacts\publish-primary\Tooba.Host.dll SHA256 2E80BFFBB3D595A685430382F00ACFC61D09A13A9636C469CC2D963D4AD4802A

GET /health              -> 200 {"status":"ok"}
GET /health/ready        -> 200 {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured",
                                "authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
GET /__platform-commerce -> 200 SingleStore / dev-singlestore / store-alpha / tenant-alpha / theme-alpha / IR
```

`appsettings*.json` untouched; FE untouched (no FE test/build/restart, no screenshots).

## 5. Live read-only HTTP receipts for the new route (no business write)

Actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db` via `X-Tooba-Dev-Actor-User-Id` (Development seam).

| Request | Response |
| --- | --- |
| `POST /v1/admin/returns/<random>/recover` with **no** actor header, `{}` | **403** `admin.authorization.denied` |
| `{}` | **400** `return.recovery.kind_invalid` |
| `{"kind":"not_a_kind"}` | **400** `return.recovery.kind_invalid` |
| `{"kind":"resume_pending_refund"}` | **400** `validation.failed`, `ExpectedRefundAttemptId: returns.validation.recovery_attempt_required` |
| `{"kind":"resume_pending_refund","expectedRefundAttemptId":"0199dddd-0000-7000-8000-000000000003"}` | **404** `return.missing` |
| `{"kind":"settle_physical_disposition"}` | **404** `return.missing` |
| `POST /v1/admin/returns/query {"page":1,"pageSize":5,"filters":[],"sort":[]}` | **200** `{"items":[],"page":1,"pageSize":5,"totalCount":0}` |

Trace/request ids were returned by the Host for every call; the notable ones:
`a000d81f58b6cd9d060b2be9f204e044` (kind_invalid), `786b2953f9fa1e929a1adf8d53d248e9` (validation),
`504531a929027a3520efc2c228036ae7` (return.missing).

## 6. Data / financial boundary

- One canonical `tooba_alpha` `127.0.0.1:5432` (SingleStore / `store-alpha`), reached only through the running Host.
- No migration, DDL, seed, backfill, cleanup, delete, restore, reset or reseed.
- No live case approval/reject/refund/recovery mutation; no PSP/Wallet/Inventory/waybill/notification/money call.
- The live Returns table is empty (`totalCount=0`), matching the envelope's pre-declared state; no fixture was created
  to manufacture a walkthrough.

## 7. Scratch files preserved (not part of this change set)

`scripts/tb266r1-*.mjs`, `scripts/tb270r2-*.mjs` remain untracked; `D:\temp\tb273\*` holds the received task copy and
the Host run log outside the repository.
