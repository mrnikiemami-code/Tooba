# RECEIPTS — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R4

Human-readable exact receipts for the R4 correction slice. Times are local `+03:30` unless marked `Z`.
Canonical target: `D:\Users\User\source\repos\SarvNewVer`, FE `http://127.0.0.1:3000` → Host
`http://127.0.0.1:5088`, SingleStore / `store-alpha` / `tooba_alpha`.

## 0. Envelope SHAs

| Field | SHA |
| --- | --- |
| Expected base | `8617161188232acfc4f70d6e363b906e85e14953` |
| Code commit | `30c6fafd7d2a89a5ad9b7482f186c7b7d786f2c3` |
| Evidence commit | `03423fb4fe78d9d07f8d4edd0d1a2e0389d5caa3` (stamped, then amended by the envelope stamp commit; the final pushed HEAD is the review authority) |

## 1. Source inventory (code commit)

Modified:

```text
src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator.cs
src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/MarketplaceOperationalGroupWorkflow.cs
src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs
src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/OrderDbContext.cs
src/backend/Modules/Order/Tooba.Order.Tests/Admin/MarketplaceOperationalGroupWorkflowRelationalTests.cs
src/frontend/app/admin/admin-order-items-shipping-panel.tsx
src/frontend/app/admin/admin-order-operational-delivery-section.tsx
src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts
src/frontend/app/admin/operational-group-occurrence-store.ts
```

Added:

```text
src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Ports/OrderOperationalGroupOccurrencePorts.cs
src/backend/Modules/Order/Tooba.Order.Domain/Checkout/OrderOperationalGroupOccurrence.cs
src/backend/Modules/Order/Tooba.Order.Infrastructure/Admin/Operations/OrderOperationalGroupOccurrenceStore.cs
src/backend/Modules/Order/Tooba.Order.Infrastructure/Persistence/Migrations/20261108153000_AddOperationalGroupOccurrences.cs
src/backend/Modules/Order/Tooba.Order.Tests/Admin/MarketplaceOperationalGroupOccurrenceTests.cs
```

`git diff --stat` before commit: 9 files changed, 1129 insertions(+), 152 deletions(-) plus the 5 added files.

## 2. Disposable PostgreSQL evidence

Provisioned container (task-owned, cleaned after use):

```text
docker run -d --name tb253r4-pg -e POSTGRES_USER=tb253r4 -e POSTGRES_PASSWORD=local \
  -e POSTGRES_DB=order_tb253r4 -p 15433:5432 postgres:16
```

R3 regression target created inside the same disposable server only
(`CREATE ROLE tb253r3 LOGIN SUPERUSER PASSWORD 'local'`, `CREATE DATABASE order_tb253r3 OWNER tb253r3`) so the
existing R3 target guard (`order_tb253r3`) is satisfied without touching any shared database.

### 2.1 `Category=TB253R4` (6/6)

```text
$env:TOOBA_TB253R4_ORDER_POSTGRES="Host=127.0.0.1;Port=15433;Database=order_tb253r4;Username=tb253r4;Password=local"
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj --filter "Category=TB253R4"
→ Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 2 s
```

Cases: A reuse-only (2 parcels, 0 new) · B sibling-group parcel foreign · C crash after A → changed B/reuse ⇒
typed intent conflict, retry finishes exact A parcel · D unknown/duplicate/foreign/over-quantity refused with zero
partial writes · E two-context barrier claim idempotency · F mixed create+reuse replay (no third parcel).

### 2.2 `Category=TB253R3|Category=TB253R4` (15/15)

```text
$env:TOOBA_TB253R3_ORDER_POSTGRES="Host=127.0.0.1;Port=15433;Database=order_tb253r3;Username=tb253r3;Password=local"
dotnet test ... --filter "Category=TB253R3|Category=TB253R4"
→ Passed! - Failed: 0, Passed: 15, Skipped: 0, Total: 15, Duration: 4 s
```

The R3 matrix (origin, occurrence-vs-payload, preflight, failure recovery) still passes unchanged.

### 2.3 Authority/workflow subset (32/32)

```text
dotnet test ... --filter "FullyQualifiedName~MarketplaceOperational|FullyQualifiedName~OperationalDeliveryGroup"
→ Passed! - Failed: 0, Passed: 32, Skipped: 0, Total: 32, Duration: 4 s
```

One early run of this filter failed only because the R3 environment variable was not set in that shell
(`the order-evidence target database must be exactly order_tb253r3`); with both targets exported it is 32/32.

## 3. Frontend evidence

```text
cd src/frontend
npx tsx --test app/admin/admin-order-operational-delivery.mount.test.ts
→ tests 15, pass 15, fail 0

npm run typecheck   → TSC_EXIT=0
npm run build       → BUILD_EXIT=0, BUILD_ID Pa2w5BmMJCuC-79qEVcv_
```

R4 mount cases in that file:

1. complete prebound envelope persisted before the first POST;
2. retrying the SAME envelope reconciles it, a CHANGED review gets a new occurrence;
3. cancelled/undefined outcome keeps the exact envelope;
4. one viewer scope's recorded occurrence is never inherited by another;
5. partial server progress + lost response keeps the envelope even when create readiness is false.

Built-artifact check: `admin-order-operational-reconcile-` present in `.next/static/chunks/*.js` and
`admin-order-operational-reconcile-retry-` present in `.next/server/**/*.js`.

## 4. Migration ledger

```text
Migration: 20261108153000_AddOperationalGroupOccurrences (Order)
Tables:    order.order_operational_group_occurrences
           order.order_operational_group_occurrence_members
Backup:    D:\temp\tb253r4-backup\tooba_alpha-pre-tb253r4.dump (2,772,057 bytes, 16:33:54)
Applied:   tooba_alpha via the existing scoped MigrationRunner
Readback:  2 tables present, 0 occurrence rows
```

Additive only — no column drop, no reset/reseed, no business SQL, no manual DDL, no accepted-history rewrite.

## 5. Deployment ledger

```text
Host publish : dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts\publish-primary
               PUBLISH_EXIT=0 (16:32)
DataProtection: artifacts\publish-primary\App_Data\dataprotection-keys — present before and after (True/True),
               existing key file key-d82e31a6-...xml untouched
Host runtime : PID 29240, http://127.0.0.1:5088, GET /health → 200
FE rebuild   : npm run typecheck 0, npm run build 0 (17:0x), BUILD_ID Pa2w5BmMJCuC-79qEVcv_
FE runtime   : PID 41724, http://127.0.0.1:3000, GET /fa → 200
Ports        : 5088 → 29240, 3000 → 41724
```

The FE was rebuilt and restarted after the final source edit so the running bundle provably contains the R4
reconcile block; the previous bundle id was not used as evidence.

## 6. Canonical read-only checks

```text
GET  http://127.0.0.1:5088/health                                    → 200
GET  http://127.0.0.1:3000/fa                                        → 200
GET  http://127.0.0.1:5088/v1/admin/orders/01a1b92d-e97e-...-edaf    → 200, shippingGroups = 3
```

All three groups remain `Store`-owned; no Marketplace claim, no create/pack/dispatch/cancel/payment/merchant
write was issued on the canonical database. Every workflow/relational assertion ran on the disposable server.

## 7. Time and cleanup

| Event | Local time |
| --- | --- |
| Slice start (post R3 base) | 16:32 |
| Host publish / restart | 16:32 – 16:35:50 |
| Disposable DB provisioned | 16:29 |
| R4/R3 relational runs | 17:05 – 17:10 |
| FE mount tests | 17:00 |
| FE typecheck/build/restart | 17:12 – 17:15 |
| Evidence written | 17:16 |

Disposable container `tb253r4-pg` (and its two task-only databases) removed after the evidence was captured;
no other container, port, profile or user tab was touched.

No screenshots were taken; no visual-acceptance claim is made.
