# TB-SHIPPING-TRACKING-UNIQUENESS-AND-QUANTITY-AUTHORITY-257 — RECEIPTS

Point-in-time observations only. Branch `main`, repository `mrnikiemami-code/Tooba`, channel `tooba-main`,
worker `tooba-worker-01`.

## R0. Base, tree and issue descendants

```text
git rev-parse HEAD (start)              662254a1efe2a1eb42534b7f9ccb9f2a28e6c55f   == Expected-Base-SHA
git fetch origin                        origin/main had advanced to 00dc91bcb
  00dc91bcb docs(shipping): advance recovery to sequential tracking and quantity task257
  a442539bc / 816c340d4 / d6d1b68b9 (same subject)
  3020f450b docs(shipping): issue 257 unique tracking and remaining quantity correction
  e508e51cf docs(shipping): accept bounded carrier-only save repair
Files touched by those descendants: AGENTS.md, docs/ai/tasks/<257>/TASK.md,
docs/architecture/evidence/<256-R2>/ARCHITECT-REVIEW.md,
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md,
docs/planning/shipping/SHIPPING-OPERATIONAL-CLOSURE-PLAN-256-260.md,
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md  -> documentation only, legitimate issue/review descendants.
```

The issued TASK.md was adopted from `origin/main` byte-for-byte (no worker edit of the task contract).

## R1. Starting runtime (before any change)

```text
3000  PID 20404  node (next start -p 3000 -H 127.0.0.1)  BUILD_ID uXwhPh0ZnBqFWcpxujHPd
5088  PID 21536  artifacts\publish-primary\Tooba.Host.exe  ./health 200
```

## R2. Reproduce defect 2 on the canonical order BEFORE the fix (read-only DOM)

`node scripts/tb257-order-quantity-dom.mjs before-fix` → `dom-order-before.json`

```text
route      http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
ready      true      groups 3      overflowX -15      dir rtl
group 1    items: "نایک شلوار مردانه سری 1 ۱ عدد ارسال‌شده", "نایک پیراهن مردانه سری 1 ۱ عدد ارسال‌شده"
           parcel: "مرسولهٔ ۱ پست · ۲ قلم ارسال‌شده  کد رهگیری: TRK-01A1B93F"
           createEntries: ["admin-order-operational-create-01a1b92d-b7e1-7000-97a1-93f3bc3bcf0c"]
group 2    items: "نایک شلوار مردانه سری 1 ۱ عدد ارسال‌شده"
           parcel: "… کد رهگیری: TRK-01A1B942"
           createEntries: ["admin-order-operational-create-01a1b92d-b7e2-7000-bfd6-800eaf211fa9"]
group 3    items: "سامسونگ هدفون سری 1 ۱ عدد ارسال‌شده"
           parcel: "… کد رهگیری: TRK-01A1B9423"
           createEntries: ["admin-order-operational-create-01a1b92d-b7e2-7000-a518-5d5718aee482"]
```

Defect reproduced: three groups whose every line is already shipped still offered «ایجاد مرسوله».

## R3. Focused tests

```text
dotnet test Tooba.Fulfillment.Tests --filter "FullyQualifiedName~TrackingAndRemainingQuantityAuthority257"
  Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10   (29 ms)

$env:TB257_CONNECTION="Host=127.0.0.1;Port=5432;Username=admin;Password=123456;Database=tooba_alpha"
$env:TB257_EVIDENCE_DIR="<evidence dir>"
dotnet test Tooba.Fulfillment.Tests --filter "FullyQualifiedName~AutomaticTrackingRelational257"
  Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4     (1 s)

dotnet test Tooba.Order.Tests --filter "FullyQualifiedName~OrderCommittedCarrierShipmentFactsEligibility"
  Passed! - Failed: 0, Passed: 5, Skipped: 0, Total: 5     (25 ms)

dotnet test Tooba.Host.Tests --filter "FullyQualifiedName~AdminOrderFulfillmentOperationsTests"
  Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6     (15 ms)

cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-create-shipment-modal.mount.test.ts
  tests 7  pass 7  fail 0                                  (3 pre-existing + 4 TB-257)
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
  tests 27 pass 27 fail 0
npx tsc --noEmit -p tsconfig.json
  exit 0
npm run build
  exit 0   BUILD_ID KRvftNHH3cYhdoBybBqej
```

`tb257-relational.txt` (append-only evidence of the two relational runs, run 1 then run 2):

```text
A first=48f8b049-93aa-45c3-95ca-57da83430882 TRK-48F8B04993AA45C395CA57DA83430882
             second=bc653d3d-37c6-41e2-9695-3638332c1779 TRK-BC653D3D37C641E296953638332C1779
B target=d2ecbc9b-84fa-4b0b-94c3-6d1bc36b3b7f TRK-D2ECBC9B84FA4B0B94C36D1BC36B3B7F-1
  squatter=e43fc0c5-75e0-45cb-848d-69cc53c3a654 TRK-D2ECBC9B84FA4B0B94C36D1BC36B3B7F
C 23505 retried shipment=f84a922b-… / 9c753e16-…
D unrelated failure propagated shipment=d753eee0-… / 6f54238b-…
E left=c639c370-079b-41ac-910c-f145dc24e0bf TRK-C639C370079B41AC910CF145DC24E0BF
  right=18cbb7c5-afc2-4dc8-8395-997b662e946e TRK-18CBB7C5AFC24DC88395997B662E946E
```

Task fixtures: fulfillment units named `TB257-A/B/C/D/E1/E2`, created through `CreateFromPaidOrder` → processing →
packing → `CreateShipmentAsync` in `tooba_alpha`; parcels are `Created`/untracked except the assignment under test.
No user order/parcel/tracking/published-method row was written and nothing was deleted.

## R4. Architecture guards (unchanged pre-existing RED)

```text
node --experimental-strip-types --test lib/architecture/frontend-source-size.guard.test.ts
  18 findings (identical family: admin-api.ts, admin-error-map.ts, admin-order-items-shipping-panel.tsx,
  admin-order-operational-delivery.mount.test.ts, category-admin-screen.tsx, host-client.ts,
  landing-pages/admin-landing-page-composer.tsx, product-workspace-screen.tsx, storefront-landing-blocks.tsx,
  storefront-shipping*.ts(x), authoring-messages.ts, authoring-screen.tsx, order-group-carrier-execution*,
  shared-composition-renderer.tsx, seller-api.ts)     -> no finding added by this task
node --experimental-strip-types --test lib/architecture/frontend-flat-folder.guard.test.ts
  11 app/admin files (same base-parent set)           -> no finding added by this task
```

The TB-257 mounted cases were deliberately placed inside the already-counted
`app/admin/admin-create-shipment-modal.mount.test.ts` instead of a new `app/admin` file, so both counts stay at base.

## R5. Backend deploy

```text
Stop-Process -Id 21536 -Force
data-protection keys before: B6985A10085AC272 key-d82e31a6-0269-4818-8766-c8267198bde9.xml
dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts\publish-primary   -> exit 0
data-protection keys after : B6985A10085AC272 key-d82e31a6-0269-4818-8766-c8267198bde9.xml   (unchanged)
Start-Process artifacts\publish-primary\Tooba.Host.exe   (ASPNETCORE_ENVIRONMENT=Development,
  ASPNETCORE_URLS=http://127.0.0.1:5088, logs D:\temp\tb257-host.out.log / .err.log)
HOST_PID 38976 ; /health 200 after ~15 s ; logs: Edition=SingleStore TenantId=store-alpha
  DeploymentId=store-alpha-canonical-tb256
```

## R6. Reproduce the SAME canonical order AFTER the fix (read-only DOM)

`node scripts/tb257-order-quantity-dom.mjs after-fix` → `dom-order-after.json`

```text
ready true   groups 3   overflowX -15   dir rtl
group 1  createEntries []   items identical   parcel identical (TRK-01A1B93F)
group 2  createEntries []   items identical   parcel identical (TRK-01A1B942)
group 3  createEntries []   items identical   parcel identical (TRK-01A1B9423)
```

No click, no request with a body, no mutation; the order's parcels, statuses and tracking codes are byte-identical to
R2 apart from the now-absent dead-end create entries.

## R7. Frontend deploy

```text
cd src/frontend ; npm run build   -> exit 0 ; .next/BUILD_ID = KRvftNHH3cYhdoBybBqej
Stop-Process -Id 20404 -Force
Start-Process node node_modules/next/dist/bin/next start -p 3000 -H 127.0.0.1
  (WorkingDirectory src/frontend, TOOBA_HOST_ORIGIN=http://127.0.0.1:5088,
   logs D:\temp\tb257-fe.out.log / .err.log)
FE_PID 44384 ; GET /fa 200
GET /_next/static/chunks/6411-fdb48bcccfc432f7.js  200, 233150 bytes, contains the TRK- derivation
GET http://127.0.0.1:5088/health                     200
```

## R8. Database / migration identity

```text
One database only: tooba_alpha on the canonical SingleStore instance, 127.0.0.1:5432.
No disposable/test/parallel/Docker PostgreSQL, no port 15433, no second runtime or edition change.
No migration, no schema/DDL change, no manual business SQL, no reset/reseed/backfill.
No checkout, payment, packing, parcel, dispatch, delivery, cancellation or publication mutation on user data.
No screenshot; DOM/HTTP facts only. DataProtection preserved. CapturedPreview local ON / checked-in OFF.
255 parked; live Tipax deferred.
```

## R9. Git trail

```text
798cf01d3  257: whole-parcel automatic internal tracking with bounded DB retry, and one packed-shipped-open quantity authority
e888eb3ae  Merge remote-tracking branch 'origin/main'   (docs descendants; TASK.md resolved to the origin version)
<DOCS-SHA> 257: record worker result, receipts and TOP/closure-plan ledger
```

Local `main` == `origin/main` after the final push; no force, amend, reset, stash or rebase.
