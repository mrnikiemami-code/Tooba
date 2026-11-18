# TB-280 — RECEIPTS (actual commands and answers)

Working tree: `D:\Users\User\source\repos\SarvNewVer` · branch `main` · one worker, one task, no delegation.

## Claim

```
git log -1 9aaf5bcd486ed63271ef431be20b7e24e8bb1951   -> 9aaf5bcd4 docs(returns): accept TB-279-R3 fixed-scope closure
git merge-base --is-ancestor 9aaf5bcd4 HEAD            -> ancestor=True
git log -1 HEAD                                        -> 2fb3791f0 docs(product): require merchant-friendly UX and issue focused Returns TB-280
                                                          (docs-only issuance; no source/config/schema delta)
```

## Source changes (6 files + 2 new tests)

```
M src/backend/Modules/Returns/Tooba.Returns.Application/ReturnRequests/Models/ReturnWorkDescription.cs
M src/frontend/app/admin/admin-return-workspace.tsx
M src/frontend/app/admin/admin-error-map.ts
M src/frontend/app/admin/admin-return-workspace-277.mount.test.ts
M src/frontend/app/admin/admin-returns-work-queue.test.ts
A src/backend/Modules/Returns/Tooba.Returns.Tests/Behavior/ReturnsPlainLanguageWorkflow280Tests.cs
A src/frontend/app/admin/admin-return-workspace-280.mount.test.ts
```

## Backend

```
dotnet test .../Tooba.Returns.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsPlainLanguageWorkflow280Tests"
  -> Passed! Failed: 0, Passed: 4, Skipped: 0, Total: 4
dotnet test .../Tooba.Returns.Tests.csproj -c Release --filter "FullyQualifiedName~ReturnsOperatorWorkflow275Tests|FullyQualifiedName~ReturnsDecisionCorrection279DirectoryTests"
  -> Passed! Failed: 0, Passed: 36, Skipped: 0, Total: 36
dotnet build src\backend\Tooba.slnx -c Release
  -> 6 Warning(s) (pre-existing NU1900 + XML-doc), 0 Error(s)
dotnet publish src\backend\Host\Tooba.Host\Tooba.Host.csproj -c Release -o artifacts\publish-primary
  -> Tooba.Host -> artifacts\publish-primary\ (0 errors)
Get-FileHash artifacts\publish-primary\Tooba.Host.dll -Algorithm SHA256
  -> 147D033E79F774EE2F24925C501CF0DCE823571529F3ACE0004EBA37C096605B
```

## Frontend

```
npx tsc --noEmit -p tsconfig.json
  -> exit 0
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-return-workspace-280.mount.test.ts
  -> 4/4 pass
npx tsx ... --test admin-return-workspace-{276,277,279,280}.mount.test.ts
  -> 27/27 pass
npx tsx ... --test app/returns/return-quantity.test.ts app/admin/admin-returns-work-queue.test.ts app/admin/admin-error-map.test.ts app/returns/return-api.test.ts
  -> 49/49 pass
npm run build
  -> compiled successfully; BUILD_ID aneVaAaZv-4AvKZggbQEJ
```

Fresh focused re-verify on this wake (same filters, no broad sweep):

```
dotnet test ... filter ReturnsPlainLanguageWorkflow280|ReturnsOperatorWorkflow275|ReturnsDecisionCorrection279Directory
  -> Passed! 40/40
npx tsc --noEmit -p tsconfig.json -> exit 0
npx tsx --tsconfig tsconfig.mount-tests.json --test
  admin-return-workspace-{276,277,279,280}.mount.test.ts
  + return-quantity + admin-returns-work-queue + admin-error-map + return-api
  -> 76/76 pass
npm run build -> BUILD_ID aneVaAaZv-4AvKZggbQEJ
dotnet build Tooba.slnx -c Release -> 0 Error(s)
dotnet publish Tooba.Host -c Release -o artifacts/publish-primary -> 0 errors
Get-FileHash artifacts/publish-primary/Tooba.Host.dll SHA256
  -> 147D033E79F774EE2F24925C501CF0DCE823571529F3ACE0004EBA37C096605B
```

One in-correction test edit is declared: `admin-return-workspace-277.mount.test.ts` asserted the OLD dialog sentence
«اولین تلاش بازگشت وجه»; TB-280 renames it, so the assertion now pins the new sentence while still asserting
approve ≠ money and the separate start-refund command. `admin-returns-work-queue.test.ts` source guards were updated from
the removed inline `id="…"` attributes to the ONE ordered action plan (same seven action identities).

## Served artifacts (refreshed, graceful)

```
Stop-Process 38424 (prior Host) -> publish -> Start-Process dotnet Tooba.Host.dll
  -> PID 31652; /health 200; /health/ready 200
     {"edition":"SingleStore","postgresql":"configured","authorization":"inmemory","messaging-transport":"postgresql-sql","messaging":"Healthy"}
  -> /__platform-commerce 200 {"tenantId":"store-alpha","resolvedHost":"127.0.0.1",...}
Stop-Process 36664 (prior Next) -> node next start -p 3000
  -> PID 26856; BUILD_ID aneVaAaZv-4AvKZggbQEJ; /fa/admin/returns 200; /fa/admin/returns/<case> 200
  -> served chunk /_next/static/chunks/7944-e29aaa760b08cd39.js 200
     contains «قدم بعدی», «بررسی مرجوعی»
     does NOT contain «برنامهٔ تفکیک» or «نسخهٔ بررسی»
```

## Live read-only checks (no POST, no write)

```
GET /v1/admin/returns/01a1eefb-ea96-7000-838d-9a0b37661c8b
  -> status=Requested updated=2026-11-18T14:07:29.430866Z received=0 attempts=0 receipts=0
     decisionVersion=0 canPublishResolution=false actionCodes=record_receipt
DOM 1440: document overflow 0; actions=["admin-return-action-record-receipt"]; history disclosure closed;
          role=alert count 0; no «تفکیک»/«نسخهٔ» in the working area;
          detail = «هنوز کالایی دریافت نشده است؛ ابتدا دریافت کالا را ثبت کنید.»
DOM 390 : document overflow 0; primary button 316x68; table keeps its own scroller (110px overflow inside it);
          receipt dialog 358px wide, 0 alerts, product «لنوو اولترابوک مدل 2», «واحد: عدد», «باقی‌ماندهٔ قابل دریافت: ۱»;
          cancelled -> 0 open dialogs, no unresolved-intent card, no pending command
GET /v1/admin/returns (queue) at 390: overflow 0, «همهٔ کارهای باز ۱», «دریافت کالا ۱», «بررسی و تصمیم ۰»
```

## Data / schema

```
zero migration · zero DDL · zero seed/backfill/reset/cleanup/restore
zero live business write · zero POST to any Returns/Order/Payment/Wallet/Inventory/provider route
the stored case keeps its original updated_at and zero receipts/attempts/decisions
```
