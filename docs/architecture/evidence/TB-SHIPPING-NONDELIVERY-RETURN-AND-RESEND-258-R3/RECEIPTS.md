# RECEIPTS — TB-SHIPPING-NONDELIVERY-RETURN-AND-RESEND-258-R3

All commands ran on the canonical Windows workspace, one module/database, one worker.
No disposable/test/parallel/Docker database, no 15433, no edition/connection/port change.

## Environment

| Fact | Value |
| --- | --- |
| Repository | `D:\Users\User\source\repos\SarvNewVer` (branch `main`) |
| Expected base | `a56814cb7ea97126bcab08da45b1886449fd3bab` (ancestor) |
| Work base | `5104375d5` (Architect issue/docs commits) |
| Database | ONE `tooba_alpha` @ 127.0.0.1:5432 (user admin) |
| Edition / tenant | `SingleStore` / `store-alpha` → `tenant-alpha` → `tooba_alpha` |
| Host | `artifacts/publish-primary`, PID **32080**, `http://127.0.0.1:5088/health` → 200 `{"status":"ok"}` |
| Frontend | `next start -p 3000 -H 127.0.0.1`, PID **47852**, BUILD_ID `Ljnu38AmJOn-eqg7FcKKq`, `/fa` 200 |

## Commands and observed output

1. `git pull --ff-only origin main` → fast-forward of the 258-R2 review + 258-R3 issue docs; HEAD `5104375d5`.
2. `dotnet build src/backend/Tooba.slnx -c Release` → **0 errors** (565 pre-existing warnings).
3. `dotnet test ...Tooba.Fulfillment.Tests --filter FullyQualifiedName~PostDispatchException258R1Tests`
   → **Passed 12, Failed 0** (11 retained + 1 new R3 cancelled-identity case).
4. `TB258_CONNECTION=<tooba_alpha> dotnet test ...Tooba.Fulfillment.Tests
   --filter FullyQualifiedName~PostDispatchExceptionRelational258Tests` → **Passed 11, Failed 0** on real
   `tooba_alpha` (A–K retained lifecycle/relational; ids in `tb258-relational.txt`).
5. `TB258_CONNECTION=<tooba_alpha> TB258_EVIDENCE_DIR=<258-R3 evidence> dotnet test
   ...Tooba.Order.Tests --filter FullyQualifiedName~AdminOrderPostDispatchReplayTests`
   → **Passed 9, Failed 0** (7 pure + 2 relational; the new case crosses
   orchestrator → adapter → `FulfillmentDirectory` on real storage). Evidence:
   `H cancel fulfillment=dc4a1ded-ff2e-4417-a62f-c19dc38b8a9f checkout=709b756d-c185-40ea-aedd-73f0cb44f04c
   source=3569b590-2014-44bf-aee1-0ae21a50ee75 cancelledResend=382e81c3-dc96-4ea0-adfe-6a17946dcc5c
   exact=resolvedCancelled changedSource=conflict changedQuantity=conflict rows=shipments2/receipts1/resends1`.
6. `npx tsc --noEmit` → **0**.
7. `npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts`
   → **tests 40, pass 40, fail 0** (39 retained + `TB-258-R3 mounted: a storage that cannot persist the intent
   refuses the NEW send and sends nothing`).
8. `npm run build` → success, BUILD_ID `Ljnu38AmJOn-eqg7FcKKq`.
9. FE restarted; previous BUILD_ID `mdzGQu5f7TP39KSWV_eKU` now **404**, current
   `/_next/static/Ljnu38AmJOn-eqg7FcKKq/_buildManifest.js` **200**; `/fa` **200**.
10. `dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts/publish-primary`
    → success; Host started (PID 32080), `/health` 200.
11. Served chunk check: `/_next/static/chunks/9127-ec36efb7727c0afc.js` 200 and contains
    `i.setItem(s,e),i.getItem(s)===e … {attempt:l,durable:!0} … {attempt:l,durable:!1}` (read-back durability).
12. Read-only canonical order route `http://127.0.0.1:3000/fa/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf`
    → **200**; nothing read or written for that order beyond the route response.

## Zero unintended mutations

No migration was added or applied. No raw business SQL, manual DDL, reset, reseed, backfill or deletion was issued.
Fixture rows are task-labelled (`TB258-*` / `tb258r3-*`) inside the SAME canonical database; the user's orders and
the reviewed order above were not mutated. DataProtection key file and CapturedPreview state untouched.
