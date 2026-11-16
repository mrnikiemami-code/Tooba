# TB-RETURNS-CREATE-OWNERSHIP-EXACT-REPLAY-AND-QUANTITY-INTEGRITY-272-R1 — RECEIPTS

All commands were run from `D:\Users\User\source\repos\SarvNewVer` unless stated. Every run below happened **once**,
after the final code change. No whole-module, shipping/order/fulfillment, composition or parity suite was run, and no
baseline worktree re-run was performed.

## 1. Base

```
$ git fetch origin
$ git rev-parse HEAD                      -> 71f5170f7a534b8d54facbcac4475efaa11672b3
$ git rev-parse origin/main               -> 71f5170f7a534b8d54facbcac4475efaa11672b3
$ git cat-file -t 71f5170f7...            -> commit
$ git merge-base --is-ancestor 71f5170f7... HEAD        -> True
$ git merge-base --is-ancestor 71f5170f7... origin/main -> True
```

Expected base == actual base == origin/main; incoming delta none. The working tree carried the in-progress R1 edit set
described below; nothing was stashed, reset, rebased or amended, and the unrelated `scripts/tb266r1-*.mjs` /
`scripts/tb270r2-*.mjs` scratch files were left untracked and uncommitted.

## 2. Harness inspected before running (the issued pre-run inspection)

```
$ rg -n "CommitAsync|ExecuteDelete|ExecuteUpdate|TRUNCATE|DELETE FROM" \
      src/backend/Host/Tooba.Host.Tests/ReturnsCreateIntegrity272RelationalTests.cs
(no matches — exit 1)
```

The class contains only caller-owned transactions rolled back by `await using`/explicit `RollbackAsync`, a
`ValidateCanonicalTarget()` gate (loopback + port 5432 + database `tooba_alpha` + server-confirmed identity,
`SkippableFact` on any mismatch) and a per-fact `AssertNoRolledBackRows` check.

## 3. Focused unit suite

```
$ cd src\backend\Modules\Returns\Tooba.Returns.Tests
$ dotnet test -c Release --filter "FullyQualifiedName~ReturnsCreateIntegrity272Tests" --logger "console;verbosity=normal"
...
  Passed ... A_non_owner_is_refused_before_any_stored_snapshot_is_answered
  Passed ... Same_key_submitted_for_a_different_order_is_a_typed_conflict_with_zero_new_case
  Passed ... Exact_replay_after_the_window_elapsed_answers_the_same_case_once
  Passed ... Reordered_lines_are_the_same_intent_and_replay_the_same_case
  Passed ... Changed_quantity_destination_or_reason_conflicts_with_zero_new_case
  Passed ... Whitespace_only_reason_difference_is_the_same_intent
  Passed ... A_stored_courier_origin_key_is_not_replayed_as_an_ordinary_case
  Passed ... Admin_and_customer_creation_share_the_same_exact_intent_rule
  Passed ... Duplicate_order_lines_are_refused_by_the_directory_and_by_the_aggregate
  Passed ... The_transport_validator_refuses_duplicate_order_lines
  Passed ... Unrepresentable_quantities_are_refused_before_any_write_or_replay
  Passed ... The_aggregate_refuses_unrepresentable_quantities_and_keeps_no_bypass
  Passed ... A_stored_case_with_another_checkout_or_seller_binding_is_not_replayed
  Passed ... A_refused_creation_preserves_unrelated_caller_tracked_work
  Passed ... The_transport_validator_refuses_unrepresentable_quantities
  Passed ... Refund_failed_keeps_its_capacity_while_a_rejected_case_releases_it
  Passed ... A_partial_return_preserves_the_remaining_quantity_and_over_remaining_is_refused
  Passed ... Advisory_lock_identity_is_stable_per_seller_order_and_namespaced
Test Run Successful.
Total tests: 18   Passed: 18
```

## 4. Rollback-only relational suite (real PostgreSQL, canonical `tooba_alpha`)

```
$ cd src\backend\Host\Tooba.Host.Tests
$ $env:TB272_CONNECTION = "Host=127.0.0.1;Port=5432;Username=admin;Password=***;Database=tooba_alpha"
$ dotnet test -c Release --filter "FullyQualifiedName~ReturnsCreateIntegrity272RelationalTests" --logger "console;verbosity=detailed"
TB272R1 target database=tooba_alpha serverPort=5432 serverAddress=172.18.0.2
  Passed The_creation_decision_waits_for_the_seller_order_advisory_lock [1 s]
    TB272R1-D lock order=260b979b-4756-46da-b57b-761d4cd111c4 request=01a1e2f2-261f-7000-8351-25c0546dab11
    TB272R1 rollback-only ... committedRows=0
  Passed Only_the_idempotency_index_is_classified_as_a_key_collision
    TB272R1 rollback-only ... committedRows=0
  Passed A_key_collision_removes_only_this_attempt_and_keeps_the_caller_transaction_usable
    TB272R1 rollback-only ... committedRows=0
  Passed A_real_unique_violation_inside_a_caller_transaction_is_exactly_classified_and_savepoint_recoverable
    TB272R1 rollback-only ... committedRows=0
  Passed A_refused_creation_leaves_no_row_keeps_the_caller_transaction_and_replays_exactly
    TB272R1 refusal=refused replay=01a1e2f2-273b-7000-ac4b-26e67cbd61ba
    TB272R1 rollback-only ... committedRows=0
  Passed Persisted_quantity_precision_and_the_full_binding_hold_inside_one_transaction
    TB272R1 rollback-only ... committedRows=0
Test Run Successful.
Total tests: 6   Passed: 6
```

`serverAddress=172.18.0.2` is the container's own interface address observed through the published loopback port; the
authoritative client endpoint (loopback, 5432) and the server-confirmed database/port are what the gate validates, and
the address is recorded as an observation rather than required to be loopback.

## 5. Scoped canonical error-set guards (because one descriptor was added)

```
$ dotnet test -c Release --no-build --filter "FullyQualifiedName~Declared_code_catalog_splits_http_reachable_from_platform_faults|FullyQualifiedName~ReturnsModuleAmsc001W3CertGuardTests|FullyQualifiedName~Certification_truth_and_stable_code_split_are_preserved"
  Passed ReturnsModuleAmsc001W1MigrateGuardTests.Declared_code_catalog_splits_http_reachable_from_platform_faults
  Passed ReturnsModuleAmsc001W3R1SetEqualityGuardTests.Certification_truth_and_stable_code_split_are_preserved
  Passed SettlementModuleAmsc001W1MigrateGuardTests.Declared_code_catalog_splits_http_reachable_from_platform_faults
  Passed SettlementModuleAmsc001W3R1SetEqualityGuardTests.Certification_truth_and_stable_code_split_are_preserved
  Failed ReturnsModuleAmsc001W3CertGuardTests.Returns_is_contracts_only_and_microservice_extractable
    Assert.Equal() Failure: Collections differ
    Expected: [... "20260909130600_DecimalReturnQuantity.cs", "ReturnsDbContextModelSnapshot.cs"]
    Actual:   [... "20261113162816_AddCourierOriginReturnsLinkage...", "20261113174500_NormalizeCourierOriginColumnNames...", ...]
  Passed ReturnsModuleAmsc001W3CertGuardTests.Returns_is_arch_complete_002_certified_in_sot_and_manifest
  Passed ReturnsModuleAmsc001W3CertGuardTests.Returns_owns_its_http_surface_with_host_route_count_zero
  Passed ReturnsModuleAmsc001W3CertGuardTests.Returns_api_results_localization_and_catalog_are_canonical
  Passed ReturnsModuleAmsc001W3CertGuardTests.Returns_cqrs_and_validator_matrix_are_exhaustive_and_discoverable
  Passed ReturnsModuleAmsc001W3CertGuardTests.Returns_wave_evidence_and_host_closure_are_preserved
Total tests: 10   Passed: 9   Failed: 1
```

The single failure is the **pre-existing** migration-file-list pin (its pinned list predates the TB-265 migrations);
parent 272 recorded the same two pins failing identically at base. It is reported, not repaired. The three code-count
pins that this repair actually changes were updated and pass.

## 6. Build and publish

```
$ dotnet build src\backend\Tooba.slnx -c Release
Build succeeded.
    211 Warning(s)
    0 Error(s)
```

## 7. Migration plan before launch (no schema change in this task)

```
$ $env:ASPNETCORE_ENVIRONMENT = "Development"
$ dotnet run --project src\backend\Host\Tooba.MigrationRunner -c Release --no-build -- plan --tenant store-alpha
   Migration Plan module=Returns ... schema=returns current=20261114170000_AddReturnRequestDecisions pendingCount=0
$ (regex over the full output) planLines=66 pendingZero=66 pendingNonZero=0     # 33 modules × (message + state)
```

## 8. Runtime refresh (Host only)

```
old artifacts\publish-primary\Tooba.Host.dll SHA256 389D5A7AC8D9112059F37626345207B999F083942B31DE6F40EEE1375B760954
$ Stop-Process -Id 51448          → listeners on 5088: 0, process 51448: absent
$ dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
    (no error line; publish completed)
new artifacts\publish-primary\Tooba.Host.dll SHA256 3244E778B2B24884D85AE4537961105CA97729B0A9B36AF075534D1FEF8BEEEB
appsettings*.json in the published output: unchanged (3079 / 3670 / 2803; 2026-10-17)
$ Invoke-CimMethod Win32_Process Create 'cmd.exe /c set ASPNETCORE_ENVIRONMENT=Development& set ASPNETCORE_URLS=http://127.0.0.1:5088& cd /d <publish>& dotnet Tooba.Host.dll'
ReturnValue=0 PID=48436 → listener PID 13160 on 127.0.0.1:5088
GET /health              → 200 {"status":"ok"}
GET /health/ready        → 200 {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
GET /__platform-commerce → 200 {"edition":"SingleStore","deploymentId":"dev-singlestore","tenantId":"store-alpha","connectionReference":"tenant-alpha","resolvedHost":"127.0.0.1","themeReference":"theme-alpha","defaultMarketReference":"IR"}
```

Startup log scan over the 116 captured lines:

```
applyingMigration=0   seeded/backfill=0   LogLevel Error=0   Critical=0
"Legacy Catalog Development bootstraps skipped (Tooba:CatalogDemo:RunLegacyBootstraps=false)"
"Language bootstrap skipped: no commerce context is bound at host startup."
MassTransit "Schema transport created" / "Transport infrastructure in schema transport created (or updated)"
```

The only schema activity is the pre-existing MassTransit transport ensure on `tooba_messaging`; no business
migration, seed or backfill ran.

FE untouched: `GET http://127.0.0.1:3000/fa` → 200 on the existing build (no FE test, build or restart).

## 9. Commits

```
$ git commit
[main e2a69f9] fix(returns): refuse unrepresentable quantities, scope collision recovery to the caller transaction
 15 files changed, 934 insertions(+), 266 deletions(-)
```

Code-SHA `e2a69f931b03caf9d9fa8189c10ea798359a2960`; the evidence commit and final HEAD/origin parity are reported in
the canonical Bridge result (a contract cannot truthfully embed its own commit).

## 10. Standing boundary of this evidence

- The negative cases that need a *different intent* (`checkout`, `seller party`, quantity, destination, reason, line
  set, order) are proven at the owning directory boundary; the `checkout`/`seller party` pair is additionally proven
  on real PostgreSQL inside a rolled-back transaction.
- The collision recovery branch is reached in the relational fact with a controlled interceptor carrying the exact
  `23505` shape that the *same run* proves against the real index, because a rollback-only boundary cannot see a
  committed winner.
- No live return case exists on the canonical database, so no served create/replay walkthrough was produced.
