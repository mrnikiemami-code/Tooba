# TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1 — RECEIPTS

All commands were executed on the canonical workstation (`D:\Users\User\source\repos\SarvNewVer`), Windows PowerShell.
Observed output is quoted, not paraphrased. One database only: `tooba_alpha` on `127.0.0.1:5432`.

## 1. Working tree / base

```
$ git log --oneline -5
fc4c5b44d docs(shipping): keep issue 3 open and hand off 258-R1 only
10b75c614 docs(shipping): keep issue 3 open and hand off 258-R1 only
42fe8023a docs(shipping): keep issue 3 open and hand off 258-R1 only
dbfa54a73 docs(shipping): keep issue 3 open and hand off 258-R1 only
93ee47a3d docs(shipping): issue 258-R1 exact resend and receipt repair
```

`git status --porcelain` before the code commit listed exactly the touched product paths (22 modified + 3 new backend
files + the new frontend `session/` store + this evidence folder). No unrelated file was modified. Issue/review/recovery
documentation commits after the issued base `00917021d` are its descendants, as the task allows.

## 2. Private backup before the migration

```
D:\temp\tb258r1-backup\tooba_alpha-pre-tb258r1.dump   2,846,806 bytes   2026-11-10 08:24:11
header magic: PGDMP (PostgreSQL custom-format logical dump)
SHA256: 620C3F4B4ED5E8ED79894246CCDD4CF4AAF8B17D0BFE0F137729B32849E19958
```

## 3. Migration (repository MigrationRunner, not manual DDL)

```
$env:ASPNETCORE_ENVIRONMENT='Development'; $env:DOTNET_ENVIRONMENT='Development'
Set-Location src\backend\Host\Tooba.MigrationRunner
dotnet run -c Release --no-launch-profile -- apply --tenant store-alpha
```

Observed plan lines (excerpt, full JSON list captured in the run):

```
Migration Apply module=Catalog      edition=SingleStore tenantId=store-alpha database=tooba_alpha connectionRef=tenant-alpha schema=catalog      current=20261103120000_AddProductPhysicalSpecification   pendingCount=0 result=ok
Migration Apply module=Order        edition=SingleStore tenantId=store-alpha database=tooba_alpha connectionRef=tenant-alpha schema=order        current=20261109120000_AddPlannedDispatchEvidence        pendingCount=0 result=ok
Migration Apply module=Fulfillment  edition=SingleStore tenantId=store-alpha database=tooba_alpha connectionRef=tenant-alpha schema=fulfillment  current=20261110160000_AddShipmentReturnOperatorAudit    pendingCount=0 result=ok
Migration Apply module=Returns      edition=SingleStore tenantId=store-alpha database=tooba_alpha connectionRef=tenant-alpha schema=returns      current=20260909130600_DecimalReturnQuantity             pendingCount=0 result=ok
Migration Apply module=Shipping     edition=SingleStore tenantId=store-alpha database=tooba_alpha connectionRef=tenant-alpha schema=shipping     current=20261109113000_AddCarrierAcceptanceSchedule      pendingCount=0 result=ok
```

Before the apply, the only module with a pending migration was Fulfillment
(`current=20261110120000_AddShipmentNonDeliveryAndReturnReceipt pendingCount=1`). After the apply every module reports
`pendingCount=0`, and the structured JSON tail reports `"succeeded": true` with empty `pendingMigrations` for each
module. No hand-written business SQL, no reset/reseed/backfill.

## 4. Backend — focused 258 proof

```
$ dotnet test src\backend\Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release \
    --nologo -v minimal --filter "FullyQualifiedName~PostDispatchException"
Passed!  - Failed: 0, Passed: 33, Skipped: 0, Total: 33, Duration: 32 ms - Tooba.Fulfillment.Tests.dll (net8.0)

$ ... --filter "FullyQualifiedName~PostDispatchException258Tests"
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 47 ms

$ ... --filter "FullyQualifiedName~PostDispatchException258R1Tests"
Passed!  - Failed: 0, Passed: 11, Skipped: 0, Total: 11, Duration: 46 ms
```

## 5. Backend — REAL relational proof on `tooba_alpha`

```
$env:TB258_CONNECTION='Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha'
$env:TB258_EVIDENCE_DIR='...\docs\architecture\evidence\TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1'
$ dotnet test ... --filter "FullyQualifiedName~PostDispatchExceptionRelational258Tests"
Passed!  - Failed: 0, Passed: 9, Skipped: 0, Total: 9, Duration: 2 s - Tooba.Fulfillment.Tests.dll (net8.0)
```

Per-case outcomes from the detailed logger:

```
Passed ...A_dispatched_parcel_returns_to_origin_and_a_resend_reaches_the_customer_without_erasing_history
Passed ...B_delivered_parcel_cannot_be_undelivered_and_a_foreign_or_pre_dispatch_parcel_is_refused
Passed ...C_non_delivery_grants_nothing_and_only_a_real_usable_receipt_funds_one_resend
Passed ...D_two_independent_contexts_cannot_consume_the_same_returned_unit_and_a_replay_returns_one_parcel
Passed ...E_delivery_is_per_parcel_truth_so_one_other_in_transit_parcel_keeps_the_unit_undelivered
Passed ...F_a_parcel_locked_in_an_active_consolidated_package_cannot_independently_record_nondelivery
Passed ...G_one_occurrence_replays_its_exact_intent_and_never_answers_a_changed_request
Passed ...H_two_returned_sources_of_one_line_cannot_lend_capacity_to_each_other
Passed ...I_one_parcel_line_may_be_received_split_across_conditions_and_grants_only_the_usable_part
Test Run Successful. Total tests: 9  Passed: 9
```

Three recorded runs with their `TB258-*` fixture identities (A–I, three sets) live in `tb258-relational.txt`. Nothing
was deleted and no user order/parcel/receipt row was read or written by these fixtures.

## 6. Module regression comparison (no re-baseline)

```
HEAD worktree:
$ dotnet test ...Tooba.Fulfillment.Tests.csproj -c Release --no-build
Failed!  - Failed: 19, Passed: 393, Skipped: 0, Total: 412

Base worktree D:\temp\tb258r1-base (fc4c5b44d, detached — the pre-change tree):
$ dotnet test ...Tooba.Fulfillment.Tests.csproj -c Release
Failed!  - Failed: 19, Passed: 379, Skipped: 0, Total: 398

diff of the distinct failing test names (duration suffix stripped):
HEAD_DISTINCT=19 BASE_DISTINCT=19
FAILING_SETS_IDENTICAL
```

`412 − 398 = 14` = 11 new `PostDispatchException258R1Tests` + 3 added relational cases (H, G, I). The 19 failures are
the pre-existing historical / type-catalog migration-parity family and were not touched or re-baselined.

## 7. Frontend

```
$ npm run typecheck
> tsc --noEmit            # no diagnostics

$ npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
ℹ tests 37
ℹ pass 37
ℹ fail 0
ℹ duration_ms 4297
  ✔ TB-258-R1 mounted: a lines refresh never resets the open review or mints a new occurrence
  ✔ TB-258-R1 mounted: two same-tick submits send exactly one mutation with one occurrence
  ✔ TB-258-R1 mounted: uncertain outcome + remount + consumed capacity reconciles the EXACT saved request
  ✔ TB-258-R1 store: another viewer never inherits the retained request

$ Get-Content src\frontend\.next\BUILD_ID
UvDFu9r8dErn3rqFmyyHX
```

Build freshness: `.next\BUILD_ID` mtime `2026-11-10 08:35:03`; no file under `app\admin` or
`features\order-operational-groups` is newer, so the served build corresponds to the committed frontend source
(`next build` exited 0 in this slice).

## 8. Host publish + runtime

```
$ (Get-Item artifacts\publish-primary\Tooba.Fulfillment.Domain.dll).LastWriteTime
Tuesday, November 10, 2026 8:22:12 AM
# no .cs under src\backend\Modules\Fulfillment|Order is newer than that assembly
```

```
GET http://127.0.0.1:5088/health  -> {"status":"ok"}
GET http://127.0.0.1:3000/fa      -> 200

LocalPort OwningProcess
     3000         41812   (Next.js frontend, BUILD_ID UvDFu9r8dErn3rqFmyyHX)
     5088          6336   (Tooba.Host, artifacts/publish-primary)
```

Edition/tenant/database resolved from the runner log above: `edition=SingleStore tenantId=store-alpha
connectionRef=tenant-alpha database=tooba_alpha`. No edition, connection, port or appsettings change; DataProtection
untouched; CapturedPreview local ON / checked-in OFF.

## 9. Authorized read-only DOM

`dom/tb258r1-dom.json` — fresh-profile headless Chrome over raw CDP, GET-only render of the existing review order
`01a1b92d-e97e-7000-999a-143350d0edaf`, no click, no form submit, no screenshot, no mutation.

```
capturedAt 2026-11-10T05:20:48.755Z
ready true · summary "۳ گروه ارسال · ۳ مرسوله" · groupCount 3 · parcelCount 3
parcel 01a1b93f-… : «ثبت تحویل» 44px + «ثبت عدم تحویل» 44px
parcel 01a1b942-ea82-… : «ثبت تحویل» 44px + «ثبت عدم تحویل» 44px
parcel 01a1b942-beef-… : «ثبت تحویل» 44px + «ثبت عدم تحویل» 44px
duplicateTestids [] · dialogOpen false · overflowX 0 · dir rtl
```

## 10. Evidence files produced

```
docs/architecture/evidence/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1/
  TASK.md                  (issued task, verbatim)
  RESULT.md
  RECEIPTS.md              (this file)
  result-contract.txt
  tb258-relational.txt     (three recorded relational runs + fixture ids)
  dom/tb258r1-dom.json
docs/ai/tasks/TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R1/TASK.md
```

TOPs and the closure plan were updated to state: issue 3 repaired pending Architect review, NOT the five-defect
completion, 259/260 still NOT ISSUED.

STOP_FOR_ARCHITECT — automaticNextImplementationTask: NONE.
