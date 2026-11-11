# TB-264 — receipts

Environment: Windows, PowerShell, `dotnet 8`, Node 24.15, Next.js 15.5.23.
ONE canonical DB `tooba_alpha` @ `127.0.0.1:5432` (SingleStore / `store-alpha`), FE `3000` → Host `5088`.

## 1. Pure tests

```
dotnet test src\backend\Modules\Shipping\Tooba.Shipping.Tests\Tooba.Shipping.Tests.csproj -c Release \
  --filter "FullyQualifiedName~ShippingCapturedRevisionQuoteTests|FullyQualifiedName~ShippingDispatchSchedulingAuthority264Tests"
→ Passed! Failed: 0, Passed: 20, Skipped: 0, Total: 20

dotnet test src\backend\Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AdminFulfillmentPreparationTiming264Tests"
→ Passed! Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

## 2. Real storage (`tooba_alpha`, task-labelled fixtures only)

```
$env:TB264_CONNECTION='Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha'
dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AdminDispatchPreparation264RelationalTests"
→ Passed! Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

`tb264-relational.txt`:

```
TB264-212116 checkout=0db2045a-… receiving=2026-11-16 v2Group=4a736708-… handoff=2026-11-12
prep=2026-11-12T11:15:00Z transfer=2026-11-12T12:45:00Z deadline=2026-11-12T13:30:00Z policyV2=2
v1Group=4af0eea8-… policyV1=1 v1Deadlines=null columns=13/8 partialTriple=23514-refused bridge=exact
```

```
$env:TB260R1_CONNECTION='Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha'
$env:TB264_EVIDENCE_DIR='docs\architecture\evidence\TB-SHIPPING-READINESS-AWARE-CUSTOMER-CALENDAR-AND-DISPATCH-DEADLINES-264'
dotnet test src\backend\Modules\Order\Tooba.Order.Tests\Tooba.Order.Tests.csproj -c Release \
  --filter "FullyQualifiedName~AdminFulfillmentWorkUnitProjection260R1RelationalTests"
→ Passed! Failed: 0, Passed: 6, Skipped: 0, Total: 6
```

`tb264-queue.txt` (new case E; A–D retained from TB-260-R1):

```
TB264-E-213454 checkout=a27e6f50-… rows=2 bucket=Upcoming prep=Overdue
prepStart=2026-11-11T21:04:54.8118217Z transfer=2026-11-11T22:04:54.8118217Z
handoffDeadline=2026-11-13T13:30:00Z legacyPrep=None legacyDeadlines=null preparationDue=1
filterPreparationDue=1 filterUpcoming=2 locator=group:5039c4ab-7ee8-4168-ae61-8cd8d90a3bdf
```

(Instants are compared at storage precision — microseconds; `HandoffDeadlineUtc` for a legacy policy-v1 row
is its own accepted close, while its preparation/transfer stay `null`.)

## 3. Mounted / frontend

```
cd src\frontend
node --experimental-strip-types --test features/admin-shipping-authoring/authoring-timing-264.test.ts
→ pass 4, fail 0

npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
→ tests 60, pass 60, fail 0   (incl. "TB-264 static: a preparation-aware plan shows its own deadline row in
                               the carrier zone" and "… a legacy policy-v1 plan renders no deadline row")

npx tsc --noEmit      → exit 0
npx next build        → 0 errors, BUILD_ID Hou23ETYm3l_DFIPdinpW
```

## 4. Schema

```
dotnet run --project src\backend\Host\Tooba.MigrationRunner -c Release -- plan --tenant store-alpha
→ Order: currentMigration = 20261111233000_AddPreparationAwareDispatchDeadlines, pendingMigrations: []
  (every module pendingMigrations: [])
```

Private backup before apply: `D:\temp\tb264-backup\tooba_alpha-pre-tb264.dump` (3,267,430 bytes,
2026-11-12 00:24:38). Applied by the repository runner only; no manual DDL, no business SQL, no reseed.

## 5. Runtime

```
Host:  PID 6276,  /health 200 → ok, /health/ready 200 → ready   (publish artifacts 2026-11-12 00:21)
FE:    PID 41488, /fa 200, /fa/admin/shipping-services 200
       BUILD_ID Hou23ETYm3l_DFIPdinpW, /_next/static/Hou23ETYm3l_DFIPdinpW/_buildManifest.js 200
       chunks/6899-a224cf719f19cb19.js (carries «زمان آماده‌سازی») → 200
```

Note: the previously running FE process (PID 43700, started 22:34) was serving the pre-264 build — the new
chunk returned 404 from it. It was stopped and the freshly built app was started (PID 41488) so the canonical
UI actually serves the 264 fields/labels.
