# RECEIPTS — TB-266-R1

All commands were run on the single canonical environment (`tooba_alpha` @ 127.0.0.1:5432 SingleStore
`store-alpha`, Host 5088, Next 3000). No credential is reproduced here.

## A. Repository

```
git rev-parse --abbrev-ref HEAD        -> main
git rev-parse HEAD                     -> 4d35ec1533e8b3304b3b819ee32ef6ac86ab1dec   (before the evidence commit)
git rev-parse origin/main              -> 4d35ec1533e8b3304b3b819ee32ef6ac86ab1dec   (origin parity verified before commit)
git merge-base --is-ancestor f4ca3cd4cc5b4f5c9e9cc0fb0eb818a09affcd9b HEAD  -> exit 0
git remote -v                          -> origin https://github.com/mrnikiemami-code/Tooba.git
```

## B. Changed files

```
 M src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Behavior/PaymentAuthorizedSupplyGate266Tests.cs
 M src/frontend/app/admin/admin-order-operational-delivery-section.tsx
 M src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts
 M AGENTS.md
 M docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
?? docs/ai/tasks/TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1/TASK.md
?? docs/architecture/evidence/TB-SHIPPING-EXECUTABLE-PARCEL-CREATION-AND-REAL-OPERATOR-HANDOFF-266-R1/
```

Production diffs: one FE copy branch (empty-projection split) and test-only additions
(`NoopInventory.ConsumeCalls`, `MultiOrderReader`, optional ids in `Handoff(...)`). No production backend change,
no migration, no schema change.

The operational helper scripts (`scripts/tb266r1-{admin,groups,seed,finish,probe*}.mjs`) are **deliberately not
committed**: they embed the Development admin login, and this task forbids exposing credentials in committed
receipts. They remain local working aids only; every fact they produced is recorded in this evidence set.

## C. Focused verification

```
dotnet test Modules\Fulfillment\Tooba.Fulfillment.Tests\Tooba.Fulfillment.Tests.csproj -c Release \
  --filter "FullyQualifiedName~PaymentAuthorizedSupplyGate266Tests"
  -> Passed! Failed: 0, Passed: 7, Skipped: 0, Total: 7   (921 ms)

npx tsx app/admin/admin-order-operational-delivery.mount.test.ts
  -> tests 63 / pass 63 / fail 0   (includes "TB-266-R1 static render: an uncaptured historical order states
     the permanent reason, not a reload")

npx tsc --noEmit                       -> exit 0
npm run build                          -> exit 0, BUILD_ID a07UMunhlqua0X_aHt-f5
Select-String .next/static/chunks/*.js -Pattern 'گروه ارسال ثبت‌شده ندارد'
  -> .next/static/chunks/7825-dc4d3435934a5ee0.js
Invoke-WebRequest /fa                                          -> 200
Invoke-WebRequest /_next/static/chunks/7825-dc4d3435934a5ee0.js -> 200
```

Build note: the first `npm run build` attempt exited with `0xC0000409` at "Generating static pages (30/123)" because
the previous `next start` process was still holding `.next`; the server was stopped (port 3000 verified free), the
same source was rebuilt once with exit 0, and a new server was started. No source change was involved.

## D. Runtime

```
Get-NetTCPConnection -LocalPort 5088 -> dotnet PID 12752   (started 20:57)
Get-NetTCPConnection -LocalPort 3000 -> node   PID 38224   (started 21:38)
Invoke-WebRequest http://127.0.0.1:5088/health/ready       -> 200
Invoke-WebRequest http://127.0.0.1:3000/fa                 -> 200
```

## E. Owning read-only API observations (admin actor)

```
node scripts/tb266r1-admin.mjs 01a1db20-ae31-7000-91d1-0b63649ac5c3 OPS
  -> 200; supplyStatus "Fulfilled"; actions deliver_shipment / record_non_delivery;
     lineCapabilities[0].shipmentEligibleQuantity 0
node scripts/tb266r1-admin.mjs 01a1db2f-e36e-7000-b7d3-afb2fe86e81a OPS
  -> 200; create_shipment present; sellerCapabilities[0].shipmentCreationPossible true;
     lineCapabilities[0].shipmentEligibleQuantity 1; supplyStatus "Reserved"
node scripts/tb266r1-groups.mjs 01a1daa2-519d-7000-bb9c-c4d0150a1971
  -> groupCount 0, groups [], shipments [], line operationalStatus "Packed"
node scripts/tb266r1-groups.mjs 01a1db2f-e36e-7000-b7d3-afb2fe86e81a
  -> groupCount 1, member createParcelEligible true, remainingPackedQuantity 1, plannedDispatch policyVersion 3
node scripts/tb266r1-groups.mjs 01a1db20-ae31-7000-91d1-0b63649ac5c3
  -> groupCount 1, member createParcelEligible false, blockReason «مقدار قابل ارسال باقی‌مانده‌ای …»,
     shipments [{ 01a1db2c-…, Dispatched, TRK-01A1DB2C5F9F70008149360BCBC27C3A }]
```

Raw JSON retained as `order-a-groups.json`, `order-b-groups.json`, `order-a-admin-detail.json`,
`order-b-admin-detail.json`.

## F. Browser verification (served build, read-only)

Three reads on the live page via `Runtime.evaluate` — texts reproduced verbatim in
`browser-live-verification.txt`:

1. dead-end order → `admin-order-operational-delivery-no-captured-group` present,
   `admin-order-operational-delivery-empty` null, textual buttons `[]`.
2. Order B → visible buttons `[{ "ایجاد مرسوله", w 102, h 44 }]`, document overflow `0`.
3. Order A → `hasTracking true`, `hasDispatched true`, create buttons `0`.

Screenshots retained: `order-a-dispatched-tracking.png`, `order-b-parked-actionable.png`,
`order-b-before-create-click.png`.

## G. Database counts after this task (read-only)

`db-counts-after-r1.txt`

```
carts 56 | checkouts 6 | fulfillments 5 | order_shipping_groups 2 | payments 5
reservations 9 | reservations_held 4 | seller_orders 6 | settlement_entries 0
shipments 1 | stock_positions 3577 | total_reserved 4
```

No count query wrote anything; no cleanup, delete, reset, reseed or restore was run in this round.

## H. Preserved 266 backups (unchanged, SHA-256 verified now)

| File | Bytes | SHA-256 |
| --- | --- | --- |
| `tooba_alpha-pre-tb266.dump` | 3,791,764 | `07AD46F5B6CBDAD16D152F23407645A0C81197DE942E96AEFCAB251E2395C8C1` |
| `tooba_alpha-pre-tb266-seed.dump` | 2,897,993 | `7865F86196CFA47968784E6A1632D28C2629C4FBA3ACB366D636A04A5B37905D` |
| `tooba_alpha-pre-tb266-repair.dump` | 2,902,992 | `CCF0279DA9A59556350AA2BF230813CAC049C3E25BE2F3DB0CB34F1CCC36A5BA` |
| `tooba_alpha-pre-tb266-final-cleanup.dump` | 2,908,545 | `FDFD25BFBBC853CADD9FE050725690BE93C8D4E9AA7C04BCBF31513B0391F1DE` |

Validation (read-only, no restore, no database created):

```
docker run --rm -i postgres:18.6 pg_restore --list  < each dump
  -> TOC_lines = 1425 for all four; header dbname tooba_alpha, Format CUSTOM, Dump Version 1.16-0,
     archive created 2026-11-14 12:15:02 UTC
```

## I. Constraints honoured

- One canonical database; no second / disposable / Docker / 15433 database.
- No edition, connection-string, port or appsettings change.
- No migration added or applied; no manual DDL; no business-SQL write.
- `DataProtection` keys and `CapturedPreview` untouched (local ON, checked-in OFF).
- User shipping draft `01a17d0d-e033-7000-af43-49a1daa28822` never written; the four original TB266 orders not
  deleted, reset, restored or retrofitted.
- No live carrier / Tipax / payment / refund / wallet call; no fake waybill; `255` parked.
