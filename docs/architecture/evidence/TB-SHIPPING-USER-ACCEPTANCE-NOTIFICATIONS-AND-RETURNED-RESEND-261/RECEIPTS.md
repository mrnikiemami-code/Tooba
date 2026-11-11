TB-261 RECEIPTS — exact commands and their observed results
==========================================================
Project root: D:\Users\User\source\repos\SarvNewVer
Date: 2026-11-11. One database: tooba_alpha @127.0.0.1:5432.

BASE / BRANCH
-------------
git rev-parse HEAD                        -> 0d1481e4d5463951a88efa9d107ae5877d553e4d (main)
git log -1                                -> docs(shipping): reopen user acceptance with261 and explicit damage followup262
git merge-base --is-ancestor 66242d2dd…   -> 0 (Expected-Base-SHA is an ancestor, not a divergence)

BACKEND
-------
$env:TB258_CONNECTION='Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha'
$env:TB258_EVIDENCE_DIR='…\evidence\TB-SHIPPING-USER-ACCEPTANCE-NOTIFICATIONS-AND-RETURNED-RESEND-261'
dotnet test Modules/Fulfillment/Tooba.Fulfillment.Tests -c Release --filter "FullyQualifiedName~PostDispatchException"
  -> Passed! Failed: 0, Passed: 36, Skipped: 0, Total: 36   (real tooba_alpha; receipt ids in tb258-relational.txt)

dotnet test Host/Tooba.Host.Tests -c Release --filter "FullyQualifiedName~AdminFulfillmentScopeSequenceTests"
  -> Passed! Failed: 0, Passed: 14, Skipped: 0, Total: 14
     (includes `Line_status_follows_the_parcels_that_actually_carry_it`:
      Returned / Mixed / Created-resend / PartialDispatched)

dotnet test Modules/Order/Tooba.Order.Tests -c Release --filter "FullyQualifiedName~AdminOrderDetail|FullyQualifiedName~AdminOrderPostDispatchReplay"
  -> Passed! Failed: 0, Passed: 15, Skipped: 0, Total: 15
     (the two public-command orchestration cases wrote their own receipts into this evidence directory:
      `tb258r2-orchestrator-relational.txt` = exact replay after the source returned,
      `tb258r3-orchestrator-relational.txt` = cancelled intent exact/conflict)

FRONTEND
--------
cd src/frontend
npx tsc --noEmit                            -> exit 0, no diagnostics

npx tsx --tsconfig tsconfig.mount-tests.json --test features/admin-shipping-authoring/tb261-notification.mount.test.ts
  -> tests 1, pass 1, fail 0
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
  -> tests 49, pass 49, fail 0   (TB-261 damaged / consumed-capacity / positive-capacity / mixed-line cases included)
npx tsx --tsconfig tsconfig.mount-tests.json --test "app/admin/orders/**/admin-order-operations-route.mount.test.ts"
  -> tests 4, pass 4, fail 0     (glob used because node's --test treats `[checkoutId]` as a pattern;
                                  the literal path form silently matched 0 files)

BUILD / PUBLISH / RESTART
-------------------------
npm run build (src/frontend)                -> ✓ Compiled successfully; .next/BUILD_ID=Iyke7WwKot_a3oQOuWbjJ
dotnet publish src/backend/Host/Tooba.Host -c Release -o artifacts/publish-primary -> exit 0
  (first attempt failed MSB3027/MSB3021 file locks owned by Tooba.Host PID 30260 — the running host was
   stopped and the publish repeated; no workaround, no forced copy)
Host restarted from artifacts/publish-primary -> PID 32736
  GET /health        -> 200
  GET /health/ready  -> 200 (database reachable)
FE restarted (`next start -p 3000 -H 127.0.0.1`) -> PID 39388
  GET /fa                                   -> 200
  GET /_next/static/chunks/8318-…js         -> 200 (carries «نیازمند تعیین تکلیف»)
  GET /_next/static/chunks/992-…js          -> 404 (previous BUILD_ID chunk gone; no stale client)

LIVE READ-ONLY VERIFICATION (see tb261-live-fixture.txt)
--------------------------------------------------------
GET /v1/admin/dev-context                                   -> 200
GET /v1/admin/orders/13feece0-…-4d4b1a5e0c97 (read-only)     -> 200, zero writes
  before deploy: line 8e0a897b op=InTransit (stale)
  after  deploy: line 8e0a897b op=Returned, sibling e4f00cd4 op=InTransit, receipt 1 Damaged/usable 0

NOT RUN ON PURPOSE
------------------
No whole-module/base parity sweep, no repeated successful run, no disposable/test/Docker/15433 database,
no edition or connection switch, no migration (no schema change in this task), no raw business SQL,
no reset/reseed/backfill, no fixture mutation, no live Tipax/provider call, no fake waybill.
