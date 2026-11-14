# TB-265 — RECEIPTS (commands actually run, in order, once each)

Base `7c32cdc2955d9c0b0c1dd4eef8b961b9a4acb904`. One database `tooba_alpha` @ 127.0.0.1:5432 (SingleStore,
tenant `store-alpha`). No disposable/test/Docker DB, no 15433, no edition/connection/port change, no manual DDL,
no raw business SQL, no reset/reseed/backfill, no global reclaimer. User/screenshot rows read-only.

## 1. Migration (additive, after a private backup)

```
private backup: D:\temp\tb265-backup\tooba_alpha-pre-tb265.dump  (3,381,173 bytes, 2026-11-13 20:35:38)
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- plan  --tenant store-alpha   # before apply
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- apply --tenant store-alpha   # succeeded
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- plan  --tenant store-alpha   # after apply (final)
```

Final plan state (all 33 modules `pendingMigrations: []`):

| Module | currentMigration |
| --- | --- |
| Fulfillment | `20261113162906_AddCourierReturnClaims` |
| Returns | `20261113174500_NormalizeCourierOriginColumnNames` |
| Order | `20261112130000_RelaxPreparationDispatchZeroAllowances` (unchanged) |
| Shipping | `20261112120000_AddProcessingHoursAndTimingPolicy` (unchanged) |
| Inventory | `20261012110133_AddReservationConsumptions` (unchanged) |

Why two Returns migrations: the first additive migration emitted PascalCase columns for the new linkage
(`Origin`, `ClosurePhase`, `ObligationClosedAt`, `StockSettledAt`, `ReleasedHeldQuantity`, `SourceClaimId`, …) while
the module maps snake_case, so the relational path failed with `column r.closure_phase does not exist`. The second
additive migration normalizes those column names idempotently; the first migration is kept so the history stays
honest. Old rows are unchanged and read as their original origin.

## 2. Focused backend checks (each once)

```
dotnet test .../Tooba.Fulfillment.Tests --filter "FullyQualifiedName~CourierReturnClaim265Tests"      -> 7/7 passed
dotnet test .../Tooba.Returns.Tests     --filter "FullyQualifiedName~CourierOriginReturns265Tests"     -> 5/5 passed
dotnet test .../Tooba.Inventory.Tests   --filter "FullyQualifiedName~ReturnObligationRelease265Tests"  -> 5/5 passed
dotnet test .../Tooba.Order.Tests       --filter "FullyQualifiedName~AdminOrderPostDispatchReplayTests"-> 16/16 passed
   (11 retained TB-258-R3 facts + 5 new TB-265 facts)
$env:TB265_EVIDENCE_DIR='D:\temp\tb265-proof'
dotnet test .../Tooba.Host.Tests        --filter "FullyQualifiedName~CourierOriginReturns265RelationalTests"
   -> 6/6 passed  (real tooba_alpha; ids in tb265-relational.txt)
```

Two earlier fixture runs of the same relational suite are why the evidence file contains A–F once per scenario;
nothing was re-baselined and no production rule was changed to make a check pass.

## 3. Builds / frontend (each once, after the final change)

```
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj   -> Build succeeded, 0 errors
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Debug -o artifacts/publish-primary -> 0 errors
cd src/frontend && npx tsc --noEmit   -> 0
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
   -> 62/62 pass (incl. 2 TB-265 facts)
npm run build  -> Compiled successfully, BUILD_ID wqPY0iLLazeSDXeSeyTtu
```

`dotnet build Tooba.slnx` is not usable with the installed SDK (MSB1009: `.slnx` unsupported); the Host project
build is the full-module compile and was used instead. No other project was skipped silently.

## 4. Runtime (restarted onto the new build)

```
Stop-Process 49024 (old Host) -> dotnet publish -> start Tooba.Host.dll with ASPNETCORE_URLS=http://127.0.0.1:5088
   Host PID 37424 ; /health 200 ; /health/ready 200 (SingleStore)
Stop-Process 37012 (old Next) -> npm run build -> start next start -p 3000
   Next PID 20232 ; /fa 200 ; BUILD_ID wqPY0iLLazeSDXeSeyTtu
   served chunk 7825-dadcc8101dfd6099.js carries «ایجاد پروندهٔ مرجوعی»
```

Note: the first restart bound `http://localhost:5000` because `ASPNETCORE_URLS` was not inherited; the process was
stopped and restarted with the explicit 5088 URL. The earlier process on 5088 (PID 49024) came from the previous
build and is not evidence for this change.

## 5. Live read-only projection (new build)

```
GET http://127.0.0.1:5088/v1/admin/orders/01a1d511-adbb-7000-8575-d3b957453d76/operations?fulfillmentId=9fe40e71-23cb-4014-8a35-e4c3231c9dab
Headers: Host: alpha.localhost, X-Tooba-Dev-Actor-User-Id: 01a036c2-970e-7000-8eb7-94bf5cc2d8db,
         Accept: application/json, Accept-Language: fa
-> 200, 2195 bytes, server-projected actions (mark_processing …). This checkout is PendingPayment, so it truthfully
   offers no courier-return action; the mutation path itself is proven by the public-command facts and the
   relational matrix, not by this GET.
```

No authenticated browser click-through of the create flow was performed (no live paid checkout with an unclaimed
returned parcel exists that is not user data); this is stated as a limit in RESULT.md, together with the exact
operator entry path.

## 6. Untouched

`255` parked; live Tipax deferred; `264/R1/R2/R3` accepted scope not reopened; DataProtection keys and
CapturedPreview state unchanged (local ON 7 dates / checked-in OFF); no provider call, no fake waybill, no money
movement; user method `01a17d0d-…` and all user/screenshot rows read-only.
