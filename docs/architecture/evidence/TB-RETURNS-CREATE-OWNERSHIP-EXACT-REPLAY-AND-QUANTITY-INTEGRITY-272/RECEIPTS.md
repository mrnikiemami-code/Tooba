# TB-272 — RECEIPTS

Raw, reproducible receipts for every claim in `RESULT.md`. `D:\...\SarvNewVer` = repository root.
No credential is reproduced; the canonical connection string is the repository's Development configuration
(`Host=127.0.0.1;Port=5432;Username=admin;Password=***;Database=tooba_alpha`).

## 0. Base verification

```
$ git rev-parse HEAD                → 9d1a64e49102dd4e4429caad987beb7219f070ae
$ git rev-parse origin/main         → 9d1a64e49102dd4e4429caad987beb7219f070ae   (after fetch)
$ git status --short                → only pre-existing untracked scripts/tb266r1-*.mjs, scripts/tb270r2-*.mjs
```

Expected base == actual base == `origin/main`; no incoming delta, no RECOVERY_CONFLICT.

## 1. Pre-existing-failure attribution (base worktree, detached)

```
$ git worktree add --detach D:\temp\tb272-base 9d1a64e49102dd4e4429caad987beb7219f070ae
$ cd D:\temp\tb272-base ; dotnet test src\backend\Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsCharacterizationTests"
Failed!  - Failed: 2, Passed: 1, Skipped: 0, Total: 3
  System.InvalidOperationException : ... 'Microsoft.EntityFrameworkCore.Database.Transaction.TransactionIgnoredWarning'
  at Tooba.Returns.Infrastructure.Directories.ReturnDirectory.RecordReturnDecisionAsync(...)

$ dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsModuleAmsc"
Failed!  - Failed: 2, Passed: 32, Skipped: 0, Total: 34
  Failed ReturnsModuleAmsc001W1MigrateGuardTests.Migration_did_not_change_the_returns_schema_migrations
  Failed ReturnsModuleAmsc001W3CertGuardTests.Returns_is_contracts_only_and_microservice_extractable

$ git worktree remove D:\temp\tb272-base --force
```

⇒ both failure sets exist at the issued base, before any TB-272 code.

## 2. Focused unit/behavior suite (final change)

```
$ dotnet test src\backend\Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsCreateIntegrity272Tests"
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13
```

Cases: `Same_key_submitted_for_a_different_order_is_a_typed_conflict_with_zero_new_case`,
`A_non_owner_is_refused_before_any_stored_snapshot_is_answered`,
`Exact_replay_after_the_window_elapsed_answers_the_same_case_once`,
`Reordered_lines_are_the_same_intent_and_replay_the_same_case`,
`Changed_quantity_destination_or_reason_conflicts_with_zero_new_case`,
`Whitespace_only_reason_difference_is_the_same_intent`,
`A_stored_courier_origin_key_is_not_replayed_as_an_ordinary_case`,
`Admin_and_customer_creation_share_the_same_exact_intent_rule`,
`Duplicate_order_lines_are_refused_by_the_directory_and_by_the_aggregate`,
`The_transport_validator_refuses_duplicate_order_lines`,
`Refund_failed_keeps_its_capacity_while_a_rejected_case_releases_it`,
`A_partial_return_preserves_the_remaining_quantity_and_over_remaining_is_refused`,
`Advisory_lock_identity_is_stable_per_seller_order_and_namespaced`.

```
$ dotnet test src\backend\Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release
Failed!  - Failed: 2, Passed: 32, Skipped: 0, Total: 34     (the 2 pre-existing cases of §1)
```

## 3. Real relational suite (canonical `tooba_alpha`)

```
$ $env:TB272_CONNECTION="Host=127.0.0.1;Port=5432;Username=admin;Password=***;Database=tooba_alpha"
$ dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsCreateIntegrity272RelationalTests" --logger "console;verbosity=detailed"

Passed The_creation_decision_waits_for_the_seller_order_advisory_lock [1 s]
  TB272-D lock order=f0ca4b4c-aeb9-4b30-82bd-06ded7d925cd request=01a1e2cc-90fe-7000-9e09-7f4bc3001ff6
Passed Concurrent_creations_never_over_allocate_the_last_returnable_unit
  TB272-D round=1 order=d70d8f7f-... accepted=1 requestedUnits=3.000000
  TB272-D round=2 order=72f75b30-... accepted=1 requestedUnits=3.000000
  TB272-D round=3 order=b03c363f-... accepted=1 requestedUnits=3.000000
Passed Only_the_idempotency_index_is_classified_as_a_key_collision
Passed The_same_key_on_two_orders_collides_on_the_real_unique_index_and_conflicts
  TB272-A uniqueIndex winner=01a1e2cc-9331-7000-b831-c03f3cc7e29b loser=return.idempotency_conflict
Passed A_refused_creation_leaves_no_row_and_releases_its_lock
  TB272 refusal=rolled-back replay=01a1e2cc-981b-7000-881d-53ab67de4075
Test Run Successful. Total tests: 5, Passed: 5
```

Each test prints `TB272 cleanup tag=tb272-rel-<runTag>- rows=N` after deleting its own rows.

## 4. Post-run database state (read-only)

```
tb272_leftover_requests  [{"c":0}]
tb272_leftover_items     [{"c":0}]
total_requests           [{"c":0}]
total_items              [{"c":0}]
idempotency_index        [{"indexname":"ix_return_requests_idempotency_key",
                           "indexdef":"CREATE UNIQUE INDEX ix_return_requests_idempotency_key ON returns.return_requests USING btree (idempotency_key)"}]
```

## 5. Architecture guards (final change)

```
$ dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsModuleAmsc"
Failed!  - Failed: 2, Passed: 32, Skipped: 0, Total: 34
  Failed ReturnsModuleAmsc001W3CertGuardTests.Returns_is_contracts_only_and_microservice_extractable
  Failed ReturnsModuleAmsc001W1MigrateGuardTests.Migration_did_not_change_the_returns_schema_migrations
```

Same two names as §1 ⇒ zero new guard failures. The three code-count pins were updated
(`20 → 22` HTTP-reachable, `21 → 23` declared, `20 → 22` contributor entries) because the two new stable codes are
HTTP-reachable and catalogued.

## 6. Builds

```
$ dotnet build src\backend\Tooba.slnx -c Release
    0 Error(s)          (warnings only: pre-existing analyzers, offline NU1900)

$ dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
    (no error line; publish completed)
```

## 7. Runtime refresh

```
old artifacts\publish-primary\Tooba.Host.dll  SHA256 E204C33AA3765BD8F6073C30FA96646B433D60154DD621A8919822CC4AE18EDB  (2026-11-15 20:20)
$ Stop-Process -Id 24008     → port 5088 owners: 0, process 24008: 0
$ dotnet publish ... -o artifacts\publish-primary
new artifacts\publish-primary\Tooba.Host.dll  SHA256 389D5A7AC8D9112059F37626345207B999F083942B31DE6F40EEE1375B760954  (2026-11-16 08:57)
appsettings*.json in the published output: unchanged length/timestamp (3079 / 3670 / 2803, 2026-10-17)
$ Invoke-CimMethod Win32_Process Create 'cmd.exe /c set ASPNETCORE_ENVIRONMENT=Development& set ASPNETCORE_URLS=http://127.0.0.1:5088& cd /d <publish>& dotnet Tooba.Host.dll'
ReturnValue=0 PID=33468
port 5088 listener PID 51448
GET /health              → 200 {"status":"ok"}
GET /health/ready        → 200
GET /__platform-commerce → 200 {"edition":"SingleStore","deploymentId":"dev-singlestore","tenantId":"store-alpha","connectionReference":"tenant-alpha","resolvedHost":"127.0.0.1","themeReference":"theme-alpha","defaultMarketReference":"IR"}
startup log: "Legacy Catalog Development bootstraps skipped (Tooba:CatalogDemo:RunLegacyBootstraps=false)",
             "Language bootstrap skipped: no commerce context is bound at host startup",
             MassTransit "Schema transport created" / "Transport infrastructure in schema transport created (or updated)"
             no business migration, no seed, no backfill
```

## 8. Commit

```
$ git commit -m "fix(returns): authorize, dedupe and serialize ordinary return creation"
4d0ba4e169bff5c529ff387509c4fa920a31930f
$ git diff --cached --stat → 16 files changed, 1605 insertions(+), 74 deletions(-)
```
