# TB-279-R1 — receipts

## Repository

| Item | Value |
| --- | --- |
| Expected-Base-SHA | `0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32` |
| Base-SHA at claim | `0ef5ae21a048dc6e466d50d8c72bcbe7b9e27e32` (docs-only issuance delta; no source/config/schema drift) |
| Reviewed-Implementation-SHA (parent 279) | `9c0885a5943e7f1a4e1a09a17fac417d23037aa6` |
| Exact envelope | Bridge `BridgeTasks.Content` (channel `tooba-main`), 24 251 bytes, SHA256 `A5FABF0BC43546F4775F14018679813F8DF67B8374300A4F14A89C2F131494AF`, claimed and executed verbatim. The issuance commit (`fd0b24cc1`) had already landed the same task as `TASK.md` (24 446 bytes, SHA256 `8913a285…0435c9`) with identical requirements and only `##`-heading/bullet formatting differences; both `TASK.md` paths and `issuance-receipt.json` now carry the issued bytes, and this task's own working copy of the Bridge text stayed under `D:\temp\tb279-col-Content.txt` |

## Validation (run after the final code change)

```
dotnet test src\backend\Modules\Returns\Tooba.Returns.Tests\Tooba.Returns.Tests.csproj -c Release
  → Passed! Failed: 0, Passed: 172, Skipped: 0, Total: 172 (743 ms)

dotnet test src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsModuleAmsc001"
  → Passed! Failed: 0, Passed: 34, Skipped: 0, Total: 34 (226 ms)

TB279R1_CONNECTION=…tooba_alpha ; --filter FullyQualifiedName~ReturnsClosure279RelationalTests
  → Total tests: 3, all passed, committedRows=0 in each fact

dotnet build src\backend\Host\Tooba.Host.Tests\Tooba.Host.Tests.csproj -c Release
  → Build succeeded (0 errors)

dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
  → 0 errors

src\frontend: npx tsc --noEmit → 0 errors
src\frontend: npm run build → success, BUILD_ID 1qgSFICgCehoh6FzH3a0f
src\frontend: returns bundle (submission, submission-279, modal-278 mounted, workspace 276/277/279 mounted) → 44/44 pass
```

## Served artifacts

| Service | Identity | Receipts |
| --- | --- | --- |
| Host `5088` | PID `28300`, `artifacts\publish-primary\Tooba.Host.dll` SHA256 `9C5ECED52091F98A828EBDA0F7AB1F52009148F9C987439706F61623CD63F62D` | `GET /health` → `200`; `GET /health/ready` → `200` `{"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}}`; `GET /__platform-commerce` → `200` `store-alpha`, `dev-singlestore`, `tenant-alpha` |
| Next `3000` | PID `29448`, `BUILD_ID 1qgSFICgCehoh6FzH3a0f` | `GET /fa/admin/returns` → `200`; `GET /_next/static/chunks/7944-5791a99ccf276c21.js` → `200`, 197 026 bytes, contains `admin-return-custody-remaining`, `admin-return-requested-amount`, `record_custody`, `publish_resolution`, `reopen_for_review`, «بازگردانی» |

## Read-only live wire (GET only, no POST probe)

```
GET /v1/admin/returns/{01a1e6b0-f33a-7000-80ee-beffb6344490}                 (no actor)
  → 401 admin.actor.missing        traceId ef649284ee31f524ed1b50c3c9bfad66

GET /v1/admin/returns/{01a1e6b0-f33a-7000-80ee-beffb6344490}                 (dev actor 01a036c2-…)
  → 200 status=Completed approvedAmount=null requested=2407839.0000 custody=0
        decisionHistory=0 refundAttempts=1 recovery=Settled updatedAt=2026-11-17T00:31:26.384627+00:00

GET /v1/admin/returns/{00000000-0000-0000-0000-0000000000ff}                 (dev actor)
  → 404 return.missing             traceId 215c0c22096b7ae9d3332bef2899207b
```

## Database (read-only SELECT after every run)

```
returns.__ef_migrations_history (desc):
  20261118023434_Tb279R1PerConditionPortionsAndCustodyProof
  20261118004500_NormalizeApprovedAmountColumn
  20261117164135_Tb279AuditedDecisionCorrectionResolutionCustodyAndAllocation

decision_revisions=0  item_resolutions=0  custody=0  delivery_allocations=0
refund_attempts=3     return_requests=3 (Completed, approved_amount NULL, updated_at unchanged)
```

## Backup

`D:\temp\tb279r1-backup\tooba_alpha-pre-tb279r1.dump` — 3 031 123 bytes,
SHA256 `C025224B6C0BDC9F06F3A180AF02109DEF41915E1F07BB70D3EFEFBB17DEADEE`,
`pg_restore --list` exit 0 (1475 TOC lines, empty stderr), taken BEFORE the single apply.

## Declared deviations

* `dotnet publish` first failed (`MSB3026`, publish DLLs locked by the running Host PID 33892); the Host was stopped,
  the publish was re-run successfully, and the Host was restarted from `artifacts\publish-primary` (PID 28300). One
  earlier publish attempt is therefore recorded as failed.
* The Next server was restarted on the new `BUILD_ID`; the previous build (`8fLqIGZTLx0CUcUquOuUG`) is superseded.
* `Tooba.Host.Tests` build emits pre-existing analyzer warnings (`xUnit2013`, `NU1900` package-vulnerability index
  unreachable); 0 errors.
* No screenshot, no browser automation, no live POST, no provider/PSP/Wallet/Inventory/notification/waybill call, no
  new case/order/hold/fixture and no reset of the three completed cases.
