# RECEIPTS — TB-SHIPPING-MARKETPLACE-OPERATIONAL-DELIVERY-GROUPS-253-R5

Human-readable exact receipts for the R5 correction slice. Times are local `+03:30`.
Canonical target: `D:\Users\User\source\repos\SarvNewVer`, FE `http://127.0.0.1:3000` → Host
`http://127.0.0.1:5088`, SingleStore / `store-alpha` / `tooba_alpha`.

## 0. Envelope SHAs

| Field | SHA |
| --- | --- |
| Expected base | `1dfdecbaf1365c21fbea86ab13922f63b358b74d` |
| Code commit | 4042e9d95129a1a0f0c40ebb6ed103412377c60e |
| Evidence commit | 7bafc2341d005de52be1ff7a4328a5846477e784 |

## 1. Source inventory

Modified:

```text
src/backend/Modules/Order/Tooba.Order.Application/Admin/Operations/Services/MarketplaceOperationalGroupWorkflow.cs
src/backend/Modules/Order/Tooba.Order.Tests/Admin/MarketplaceOperationalGroupOccurrenceTests.cs
src/frontend/app/admin/operational-group-occurrence-store.ts
src/frontend/app/admin/admin-order-operational-delivery-section.tsx
src/frontend/app/admin/admin-order-items-shipping-panel.tsx
src/frontend/app/admin/admin-order-operational-delivery.mount.test.ts
src/frontend/app/admin/admin-order-operations.ts
src/frontend/lib/admin/admin-result.ts
```

No new migration, no schema change, no business-data change.

## 2. Disposable PostgreSQL evidence

Fresh task-owned container (cleaned after use):

```text
docker run -d --name tb253r5-pg -e POSTGRES_USER=tb253r4 -e POSTGRES_PASSWORD=local \
  -e POSTGRES_DB=order_tb253r4 -p 15433:5432 postgres:16
```

The R3 regression target was created inside the same disposable server only
(`CREATE ROLE tb253r3 LOGIN SUPERUSER PASSWORD 'local'`, `CREATE DATABASE order_tb253r3 OWNER tb253r3`)
so the existing R3 target guard is satisfied without touching any shared database.

### 2.1 `Category=TB253R5` (2/2)

```text
$env:TOOBA_TB253R4_ORDER_POSTGRES="Host=127.0.0.1;Port=15433;Database=order_tb253r4;Username=tb253r4;Password=local"
dotnet test src/backend/Modules/Order/Tooba.Order.Tests/Tooba.Order.Tests.csproj --filter "Category=TB253R5"
```

```text
Passed ...G_failure_after_the_parcel_commit_but_before_the_member_link_recovers_that_exact_parcel [2 s]
Passed ...H_mismatched_occurrence_parcel_evidence_fails_closed_without_a_replacement [168 ms]
Test Run Successful. Total tests: 2, Passed: 2, Total time: 3.06 s
```

Case G fault seam (exact checkpoint, not the common-package one):

```text
FailFirstMemberLinkInterceptor : SaveChangesInterceptor on OrderDbContext
  throws when ChangeTracker has an Added entity named "OrderOperationalGroupOccurrenceMember"
  → the Fulfillment parcel create already committed, the durable member link did not
Asserted at failure: 1 shipment with the occurrence DispatchKey on GroupA1, 0 member links, 0 packages
Asserted after retry: 2 shipments (exact recovered id + one new), 1 package, 2 package members, 2 member links
Asserted changed request: workflow_intent_conflict, still 2 shipments / 1 package / 2 member links
```

Case H: a parcel seeded under `BuildMemberParcelDispatchKey(checkout, occurrence, GroupA1)` with quantity 2
against an envelope quantity of 1 →
`order.operational_group.workflow_intent_conflict`, exactly the one seeded shipment, 0 packages, 0 occurrence rows.

### 2.2 Adjacent `Category=TB253R4` (6/6)

```text
Test Run Successful. Total tests: 6, Passed: 6, Total time: 3.25 s
A-reuse-only, B-group-scoped-reuse, C-member-crash, D-preflight, E-concurrent-claim, F-mixed-create-reuse
```

### 2.3 Adjacent authority/workflow subset (34/34)

```text
dotnet test ... --filter "FullyQualifiedName~MarketplaceOperational|FullyQualifiedName~OperationalDeliveryGroup"
Passed! - Failed: 0, Passed: 34, Skipped: 0, Total: 34
```

Both `TOOBA_TB253R4_ORDER_POSTGRES` and `TOOBA_TB253R3_ORDER_POSTGRES` pointed at the disposable server.

## 3. Frontend mounted evidence (17/17)

```text
cd src/frontend
npx tsx --tsconfig tsconfig.mount-tests.json --test app/admin/admin-order-operational-delivery.mount.test.ts
```

```text
✔ R4/R5 mounted: retrying the SAME envelope reconciles it; a CHANGED envelope is blocked while unresolved
✔ R5 mounted: a viewer switch cannot act on the previous viewer's retained refs
✔ R5 mounted: only a proven no-claim refusal releases the retained envelope
tests 17 / pass 17 / fail 0
```

The rewritten R4 test now proves the R5 contract instead of the removed remint behaviour:
unchanged retry reuses the recorded occurrence; an edited quantity disables the normal Create path, sends no
request, and leaves the stored occurrence, selections and fingerprint byte-identical; the resolver returns
`blockedByRetainedAttempt: true` with the recorded occurrence.

The no-claim test proves: `undefined` outcome keeps the envelope; `{completed:false, noClaimRefusalCode:null}`
keeps it; `workflow_group_foreign` clears it and the next run mints a different occurrence.

## 4. Frontend build / typecheck

```text
npm run typecheck  → exit 0
npm run build      → exit 0
.next/BUILD_ID     → PJOIKkpv60txqyyb_oqfI
```

## 5. Canonical deployment

| Step | Result |
| --- | --- |
| Stop Host PID 29240 | stopped, port 5088 free |
| `dotnet publish src/backend/Host/Tooba.Host/Tooba.Host.csproj -c Release -o artifacts\publish-primary` | exit 0 |
| Start Host | PID **17344**, `/health` **200** |
| Stop FE PID 41724 → `next start -p 3000 -H 127.0.0.1` | PID **7960**, `/fa` **200** |
| Migration | `20261108153000_AddOperationalGroupOccurrences` unchanged; **no new migration applied** |
| DataProtection key directory | present/preserved in `artifacts\publish-primary\App_Data\dataprotection-keys` |

The FE was rebuilt and restarted after the final source edit, so the running bundle provably contains the
R5 blocked-remint + safe-transition code.

## 6. Canonical read-only checks

```text
GET  http://127.0.0.1:5088/health                                        → 200
GET  http://127.0.0.1:3000/fa                                            → 200
GET  http://127.0.0.1:3000/admin/orders/01a1b92d-...-edaf                → 200
GET  http://127.0.0.1:5088/v1/admin/orders/01a1b92d-...-edaf             → 200
     shippingGroups = 3, operationalDeliveryGroups = 3, every group cross=False (Store-owned)
```

No Marketplace claim, no create/pack/dispatch/cancel/payment/merchant write was issued on the canonical
database. Every workflow/relational assertion ran on the disposable server.

## 7. Time and cleanup

| Event | Local time |
| --- | --- |
| Slice start | 17:31 |
| Disposable DB provisioned (first attempt on 15434 rejected by the port guard) | 17:40 |
| Disposable DB recreated on 15433 | 17:41 |
| TB253R5 relational runs | 17:44 |
| TB253R4 + authority subsets | 17:45 – 17:46 |
| FE mount tests | 17:37 |
| Host publish / restart | 17:47 – 17:50 |
| FE build / restart | 17:51 – 17:52 |
| Read-only checks | 17:53 |

Cleanup: disposable container `tb253r5-pg` removed after the runs; no shared database was touched and no
canonical service was left stopped.
