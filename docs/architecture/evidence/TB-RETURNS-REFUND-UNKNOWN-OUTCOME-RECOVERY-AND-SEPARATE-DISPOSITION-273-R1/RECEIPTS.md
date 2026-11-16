# RECEIPTS — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R1

All commands run from `D:\Users\User\source\repos\SarvNewVer` (Windows, PowerShell). One canonical target only:
`tooba_alpha` on `127.0.0.1:5432` (SingleStore / `store-alpha`), Host 5088, FE 3000.

## 1. Claim / base

```text
$ git rev-parse HEAD
1e4e7cb677c25f2746a0a5b4c870808015aa95fc      # == Expected-Base-SHA

$ git status --porcelain=v1
  (clean except pre-existing untracked scripts/tb266r1-*.mjs, scripts/tb270r2-*.mjs — preserved, not staged)
```

## 2. Focused behavior suite (final, once after the last code change)

```text
$ dotnet test src\backend\Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release \
    --no-restore --filter "FullyQualifiedName~ReturnsRecovery273Tests" -v q --nologo
Passed!  - Failed: 0, Passed: 24, Skipped: 0, Total: 24, Duration: 623 ms - Tooba.Returns.Tests.dll (net8.0)
```

Retained-vs-new: 15 retained TB-273 facts + 9 TB-273-R1 facts. The only retained expectation that changed is
`Confirmed_money_with_unsettled_receipts_is_described_by_origin_not_by_status`: its "settled" case now also records
`ClosurePhase = RefundReleased` (the R1 contract makes durable dispositions +  a missing final marker
`PendingDisposition`), and the same fact additionally pins that marker-pending state.

## 3. Rollback-only relational facts (real canonical PostgreSQL)

```text
$ $env:TB273R1_CONNECTION="Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha"
$ dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --no-build \
    --filter "FullyQualifiedName~ReturnsRecovery273RelationalTests" --logger "console;verbosity=detailed"
  Passed … An_ambient_transaction_scope_refuses_effectful_recovery_before_any_effect [704 ms]
 TB273R1-B ambient refusal tag=tb273r1-rel-e677cc6d3a734c0e82bae8b11e75cbfb- target=tooba_alpha:5432/172.18.0.2 committedRows=0
  Passed … A_caller_owned_transaction_refuses_effectful_recovery_before_any_effect [58 ms]
 TB273R1-B caller-owned refusal tag=tb273r1-rel-7285414b90774f61a5ad35889f982349- target=tooba_alpha:5432/172.18.0.2 committedRows=0
Test Run Successful. Total tests: 2  Passed: 2
```

The first attempt of this class reported `Skipped: 2` with the reason *"The server address is not loopback; the
canonical target was rejected."* — the canonical single-store deployment answers on its private container address
(`172.18.0.2`). The guard was corrected to require **loopback or private** for the server-confirmed address (the
client target stays pinned to loopback) and to record the confirmed identity in the evidence line; it was then re-run
once and passed. No relaxation of the database/port/loopback-client checks.

## 4. Build / publish

```text
$ dotnet build src\backend\Tooba.slnx -c Release --nologo
Build succeeded.   369 Warning(s)   0 Error(s)

$ dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary --nologo
  Tooba.Host -> …\artifacts\publish-primary\
  EXIT=0

$ (Get-FileHash artifacts\publish-primary\Tooba.Host.dll -Algorithm SHA256).Hash
FAF2D791DAFB1C55A33E5C5A533AEC9B7061194336ABA9E7A75BB274F630E440
```

The previous Host (PID 28580) was stopped **before** the single publish; no file-lock retry was needed. The dll was not
rebuilt after the last test-only edit, so the published artifact matches the final production source.

## 5. Host refresh and read-only checks

```text
$ (Get-NetTCPConnection -LocalPort 5088 -State Listen).OwningProcess
37876

$ GET /health              → 200
$ GET /health/ready        → 200 {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured",
                                   "authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
$ GET /__platform-commerce → 200 {"edition":"SingleStore","tenantId":"store-alpha","connectionReference":"tenant-alpha",
                                   "resolvedHost":"127.0.0.1", …}
```

Started with the existing configuration only (`ASPNETCORE_ENVIRONMENT=Development`,
`ASPNETCORE_URLS=http://127.0.0.1:5088`, `dotnet Tooba.Host.dll`). No migration, seed, backfill, settings or key change.

Read-only recovery wire (actor `01a036c2-970e-7000-8eb7-94bf5cc2d8db` through the Development
`X-Tooba-Dev-Actor-User-Id` seam):

```text
no-actor {}                      => 401 admin.actor.missing
actor {}                         => 400 return.recovery.kind_invalid
{"kind":"not_a_kind"}            => 400 return.recovery.kind_invalid
{"kind":"resume_pending_refund"} => 400 validation.failed / returns.validation.recovery_attempt_required
resume + random attempt id       => 404 return.missing
{"kind":"settle_physical_disposition"} => 404 return.missing
POST /v1/admin/returns/query     => 200 {"items":[],"page":1,"pageSize":5,"totalCount":0}
```

## 6. Schema / data boundary

```text
$ git status --porcelain=v1        (after the code commit)
  no migration/DDL file added or modified
```

- No migration, DDL, backfill, seed, restore, truncate, delete or cleanup.
- No live approval/reject/refund/recovery mutation; no PSP/Wallet/Inventory/waybill/notification/money call.
- No user order, draft, history, stock, hold or settings row touched. The relational facts wrote only inside their own
  transaction and rolled it back (`committedRows=0`).

## 7. Out-of-scope runs (disclosed deviation)

```text
$ dotnet test …Tooba.Returns.Tests.csproj -c Release --no-build       # whole project, not requested
Failed!  - Failed: 2, Passed: 60, Skipped: 0, Total: 62
  ReturnsCharacterizationTests.Create_approve_refund_preserves_amount_currency_and_is_idempotent   (pre-existing)
  ReturnsCharacterizationTests.Wallet_destination_credits_wallet_without_psp_refund               (pre-existing)

$ dotnet test …Tooba.Host.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~ReturnsModuleAmsc001"
Failed!  - Failed: 2, Passed: 32, Skipped: 0, Total: 34
  ReturnsModuleAmsc001W1MigrateGuardTests.Migration_did_not_change_the_returns_schema_migrations  (pre-existing)
  ReturnsModuleAmsc001W3CertGuardTests.Returns_is_contracts_only_and_microservice_extractable      (pre-existing)
```

Both runs are outside the issued validation selection and are reported as a **worker deviation**. Neither failure was
introduced, repaired, re-pinned or silenced here; both pin the migration-file list that already predates the TB-265
migrations present in the base. The three exact catalog pins this task *did* have to touch
(`31/1/32`, and the contributor `D(ReturnsErrorCodes.` count `31`) pass in the second run.

## 8. Commit

```text
$ git commit -m "fix(returns): TB-273-R1 fail-closed recovery evidence, transaction refusal and truthful provider reference"
[main 150179bbc] 15 files changed, 1271 insertions(+), 138 deletions(-)
$ git rev-parse HEAD
150179bbc6acae82d915c1919f3fd8cfc4884a93
```

Evidence/ledger commit and `origin/main` parity are recorded in the Bridge result (no self-referential hash pin).
