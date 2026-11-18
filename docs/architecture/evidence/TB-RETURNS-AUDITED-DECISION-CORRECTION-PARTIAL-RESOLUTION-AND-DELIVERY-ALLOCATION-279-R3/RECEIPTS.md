# TB-279-R3 — RECEIPTS

Date: 2026-11-18 (Asia/Tehran). Commands were run from the repository root
`D:\Users\User\source\repos\SarvNewVer` unless noted. No command printed a secret.

## Git

```
git fetch origin --prune
git rev-parse HEAD            -> e7db94398e663b604a536b82bd86670bb50f72db
git rev-parse origin/main     -> e7db94398e663b604a536b82bd86670bb50f72db
git merge-base --is-ancestor dd5c1d31fcd65bce7164c115599aacb5a4e33083 HEAD -> exit 0
git log --oneline -3          -> e7db94398 docs(returns): issue TB-279-R3 fixed-scope final closure
                                 dd5c1d31f docs(returns): retain TB-279-R2 repairs and narrow remaining owner gates
                                 f867dcb71 test(returns): prove portion occupancy, unit quantum and unproven-carrier refusal…
```

Claim HEAD is the permitted docs-only issuance commit on top of the expected base; the incoming delta contained no
source/config/schema change.

## Historical posting (D)

```
git ls-files --error-unmatch docs/architecture/evidence/TB-RETURNS-MODAL-QUANTITY-GUARDS-AND-EXPLICIT-REFUND-SEPARATION-277-R1/RESULT.bridge.txt
  -> tracked at claim time (the R2 report was wrong)
git rm --cached docs/architecture/evidence/…-277-R1/RESULT.bridge.txt
  -> rm 'docs/architecture/evidence/…-277-R1/RESULT.bridge.txt'
Test-Path docs/architecture/evidence/…-277-R1/RESULT.bridge.txt        -> True   (local copy preserved)
git check-ignore -v docs/architecture/evidence/…-277-R1/RESULT.bridge.txt
  -> .gitignore:88:docs/architecture/evidence/**/RESULT.bridge.txt   docs/architecture/evidence/…-277-R1/RESULT.bridge.txt
```

## Build / publish

```
dotnet build src/backend/Tooba.slnx -c Release --nologo -v q
  -> 0 Error(s), 642 Warning(s)   (pre-existing warnings; NU1900 = offline nuget feed only)
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary
  -> 0 errors (exit 0)
```

## Focused tests

```
dotnet test src/backend/Modules/Returns/Tooba.Returns.Tests/Tooba.Returns.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~ReturnsDecisionCorrection279DirectoryTests"
  -> Passed! - Failed: 0, Passed: 21, Skipped: 0, Total: 21

… --filter "FullyQualifiedName~ReturnsFinalClosure279R3Tests"
  -> Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4

dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj -c Release --no-build \
  --filter "FullyQualifiedName~ReturnsModuleAmsc001"
  -> Passed! - Failed: 0, Passed: 34, Skipped: 0, Total: 34
```

Declared deviation (over-run, not a substitute for the facts above): `dotnet test …Tooba.Returns.Tests.csproj -c Release
--no-build` without a filter was executed once → `Passed! - Failed: 0, Passed: 187, Skipped: 0, Total: 187`. The issued
scope asked for focused classes only; this extra whole-project run is disclosed rather than presented as the proof.

## Served (GET only — no POST of any kind was sent)

```
Stop-Process -Id 7808        # previous Host (dll 7B8AAB85…)
dotnet publish …             # see above
Start-Process dotnet Tooba.Host.dll   (WorkingDirectory = artifacts\publish-primary)
  -> PID 39776, listener on 5088

(Get-FileHash artifacts\publish-primary\Tooba.Host.dll -Algorithm SHA256).Hash
  -> 04CD98A718E4BCB9702AF12F54BA4872E6B11FEB6956604AF9A65E4055ED34C5

GET http://127.0.0.1:5088/health               -> 200
GET http://127.0.0.1:5088/health/ready         -> 200
   {"status":"ready","checks":{"edition":"SingleStore","postgresql":"configured","authorization":"inmemory",
    "messaging-transport":"postgresql-sql","messaging":"Healthy"}}
GET http://127.0.0.1:5088/__platform-commerce  -> 200
   {"edition":"SingleStore","tenantId":"store-alpha","connectionReference":"tenant-alpha",
    "resolvedHost":"127.0.0.1","defaultMarketReference":"IR"}
GET http://127.0.0.1:3000/fa/admin/returns     -> 200 (19002 bytes)
   Next 3000 PID 9904 (unchanged — no frontend production source changed)
```

## Read-only existing history (GET with the dev actor from `/v1/admin/dev-context`)

```
GET /v1/admin/dev-context                       -> 200 actorUserId 01a036c2-970e-7000-8eb7-94bf5cc2d8db
GET /v1/admin/returns/01a1e6b0-f33a-7000-80ee-beffb6344490 -> 200
   status=Completed refund=2407839.0000 attempts=1 updatedAt=2026-11-17T00:31:26.384627+00:00 approvedAmount=<null>
GET /v1/admin/returns/01a1e9c5-c729-7000-b614-79daf3d0e812 -> 200
   status=Completed refund=2407839.0000 attempts=1 updatedAt=2026-11-17T14:03:06.669021+00:00 approvedAmount=<null>
```

Both `updated_at` values and the attempt counts are the pre-existing ones, so no live business write occurred. No third
case exists; the returns grid query is a POST and was therefore not sent (no live POST is authorized by this task), so
no served operator walkthrough is claimed.

## Schema / effects

No `dotnet ef`, migration runner, SQL, seed, backfill, reset, restore or cleanup command was executed in this task.
No payment/order/shipment/stock/hold/waybill/notification/Wallet/PSP call was made; the only money/Inventory activity is
the scripted in-process ports of `ReturnsFinalClosure279R3Tests`.
