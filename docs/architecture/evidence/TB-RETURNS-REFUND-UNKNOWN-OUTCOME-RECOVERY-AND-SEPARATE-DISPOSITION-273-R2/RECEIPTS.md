# RECEIPTS — TB-RETURNS-REFUND-UNKNOWN-OUTCOME-RECOVERY-AND-SEPARATE-DISPOSITION-273-R2

Date: 2026-11-16. All commands run from `D:\Users\User\source\repos\SarvNewVer`.

## 0. Recovery / base

```
git fetch origin main
   a2ffbf60d..02af22610  main -> origin/main
git merge --ff-only origin/main
   HEAD = 02af2261032adf3aeb299fa9c369ee3b5154718d   (== Expected-Base-SHA)
   incoming delta: 5 docs-only Architect review files (AGENTS.md, TOOBA-RECOVERY-CONTEXT.md,
   …273-R1/ARCHITECT-REVIEW.md, RETURNS plan, START-HERE-IF-CHATGPT-IS-LOST.md)
   worktree: only the known untracked scripts/tb266r1-*.mjs and scripts/tb270r2-*.mjs scratch files
   no reset/stash/rebase/amend/force, no unrelated dirty work lost
```

## 1. Focused controlled tests (once after the final code change)

```
dotnet test src\backend\Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release \
  --filter "FullyQualifiedName~ReturnsRecovery273Tests"

Passed!  - Failed: 0, Passed: 29, Skipped: 0, Total: 29, Duration: 637 ms
```

29 = 24 retained R1 facts + 5 new R2 facts:

```
Incoherent_courier_source_blocks_pending_money_in_the_same_policy_the_command_uses
Pending_courier_money_with_a_broken_source_is_refused_before_any_money_or_owner_call
A_coherent_pending_courier_case_resumes_the_same_bound_attempt_and_key
A_recorded_final_marker_without_its_prior_closure_timestamps_is_inconsistent_not_settled
A_final_marker_missing_its_prior_evidence_refuses_physical_recovery_and_repairs_nothing
```

Declared deviation: the FIRST run of this suite failed 1/29 because the new helper used `Single()` over the two-item
courier fixture (`Sequence contains more than one element`) — a test-harness defect only. The helper was corrected and
the suite re-run ONCE; the passing run above is the reported one.

## 2. ONE real rollback-only relational fact (exact method filter; R1 facts NOT re-run)

```
dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~ReturnsRecovery273RelationalTests.An_explicitly_enlisted_caller_transaction_refuses_effectful_recovery_before_any_effect" \
  --logger "console;verbosity=detailed"

Passed Tooba.Host.Tests.ReturnsRecovery273RelationalTests.An_explicitly_enlisted_caller_transaction_refuses_effectful_recovery_before_any_effect [695 ms]
Standard Output Messages:
 TB273R2-C explicit-enlistment refusal tag=tb273r1-rel-a365c2f8ff3841498492d7d3c6a2f3c2- target=tooba_alpha:5432/172.18.0.2 committedRows=0

Total tests: 1  Passed: 1
```

(The `tb273r1-rel-` prefix is the retained R1 run-tag field of this test class; the staged rows were rolled back.)

## 3. Backend build (once)

```
dotnet build src\backend\Tooba.slnx -c Release
   679 Warning(s)
   0 Error(s)
Time Elapsed 00:00:36.54
```

## 4. Host deploy (backend-only, no migration, FE untouched)

```
startup effects inspected: the diff touches Returns application policy + Returns infrastructure directory + tests only.
No DI registration, options, endpoint, catalog, migration, config or startup file changed; MigrationRunner has no new
migration to apply.

Get-NetTCPConnection -LocalPort 5088  -> PID 37876 (old build)
Stop-Process -Id 37876                -> stopped=True
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
   -> Tooba.Host -> D:\Users\User\source\repos\SarvNewVer\artifacts\publish-primary\   (0 errors)
SHA256 Tooba.Host.dll = 3DA1453E7B8F77A592D1AC5F41E5BA95189A4CEE77423AAB1485FBCD8535C60C  (222,720 bytes)
start: dotnet Tooba.Host.dll  (ASPNETCORE_URLS=http://127.0.0.1:5088, current config)
PID = 13284

GET /health                 -> 200 {"status":"ok"}
GET /health/ready            -> 200 {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}
GET /__platform-commerce     -> 200 {"edition":"SingleStore","deploymentId":"dev-singlestore","tenantId":"store-alpha","connectionReference":"tenant-alpha","resolvedHost":"127.0.0.1","themeReference":"theme-alpha","defaultMarketReference":"IR",...}
```

No migration/seed/backfill/DDL/reset/reseed/cleanup/delete/restore. FE 3000 not built, tested, restarted or captured.

## 5. Served read-only wire (real HTTP, NOT recovery proof)

```
POST /v1/admin/returns/01a1db2f-0000-7000-8000-000000000000/recover
no actor, {}                    => 401 admin.actor.missing
dev actor, {}                   => 400 return.recovery.kind_invalid
{"kind":"resume_pending_refund"} => 400 validation.failed / returns.validation.recovery_attempt_required
{"kind":"resume_pending_refund","expectedRefundAttemptId":"<random>"} => 404 return.missing
{"kind":"settle_physical_disposition"}                                => 404 return.missing
POST /v1/admin/returns/query   => 200 {"items":[],"page":1,"pageSize":20,"totalCount":0}
```

The Returns table is empty: no live case exists, so no served recovery walkthrough and no demo case was created.

## 6. Git

```
git add <the 4 changed source/test files>
git commit -m "fix(returns): TB-273-R2 courier source before pending money, prior closure evidence and explicit enlistment refusal"
   -> 2481d40b98182bd17cf72614ff5c45173ef310ad   (Code-SHA)
```

Documentation/evidence commit and `git push origin main` follow in §7 of this evidence commit; final HEAD and fresh
origin parity are recorded in `result-contract.txt`.

## Not executed (deliberately)

```
- whole Returns project / whole Host project / broad ReturnsModuleAmsc* filter / cross-module or parity suites
- caller-owned + ambient relational facts (unchanged, retained receipts from R1)
- stress/concurrency duplicate-race harness; the FOR UPDATE outcome branch
- any architecture-guard / catalog / route / validator count run (no new error code)
- FE build/test/restart/screenshots; any migration, business SQL, provider call or fixture
```
