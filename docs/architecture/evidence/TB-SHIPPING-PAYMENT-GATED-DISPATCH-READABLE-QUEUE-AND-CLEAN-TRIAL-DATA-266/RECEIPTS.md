# TB-266 receipts

Every line is a command actually executed in this session on this machine.

## Backups (private, `D:\temp\tb266-backup`)

```
docker exec postgres-db sh -c "PGPASSWORD=123456 pg_dump -h host.docker.internal -p 5432 -U admin -Fc -d tooba_alpha -f /tmp/tb266-final.dump"
docker cp postgres-db:/tmp/tb266-final.dump D:\temp\tb266-backup\tooba_alpha-pre-tb266-final-cleanup.dump
Get-FileHash ... -Algorithm SHA256
```

| File | Bytes | SHA256 |
| --- | --- | --- |
| `tooba_alpha-pre-tb266.dump` | 3,791,764 | `07AD46F5B6CBDAD16D152F23407645A0C81197DE942E96AEFCAB251E2395C8C1` |
| `tooba_alpha-pre-tb266-seed.dump` | 2,897,993 | `7865F86196CFA47968784E6A1632D28C2629C4FBA3ACB366D636A04A5B37905D` |
| `tooba_alpha-pre-tb266-repair.dump` | 2,902,992 | `CCF0279DA9A59556350AA2BF230813CAC049C3E25BE2F3DB0CB34F1CCC36A5BA` |
| `tooba_alpha-pre-tb266-final-cleanup.dump` | 2,908,545 | `FDFD25BFBBC853CADD9FE050725690BE93C8D4E9AA7C04BCBF31513B0391F1DE` |

## Canonical target verification

```
appsettings.Development.json → tenant-alpha: Host=127.0.0.1;Port=5432;Database=tooba_alpha
docker ps → existing containers only (no DB created by this task)
```

## Cleanup

```
Get-Content D:\temp\tb266-backup\cleanup.sql -Raw | docker exec -i postgres-db psql "<tooba_alpha>" -f -
```

Final scoped transaction (this task's own cancelled repair fixture only):

```
BEGIN
DELETE 0   fulfillment.shipment_items
DELETE 0   fulfillment.shipments
DELETE 1   fulfillment.items
DELETE 0   fulfillment.shipping_groups
DELETE 1   fulfillment.fulfillments
DELETE 0   order.order_shipping_group_lines
DELETE 0   order.order_shipping_groups
DELETE 1   order.order_lines
DELETE 1   order.seller_orders
DELETE 1   order.checkouts
DELETE 0   payment.proof_assets
DELETE 1   payment.attempts
DELETE 1   payment.payments
DELETE 1   cart.cart_lines
DELETE 1   cart.carts
COMMIT
```

Counts after (`counts-after.sql`, read-only):

```
checkouts 4 | seller_orders 4 | order_lines 4 | fulfillments 3 | shipments 0 | receipts 0
courier_return_claims 0 | packages 0 | damaged_resolutions 0 | returns 0
reservations 6 | total_reserved 3 | payments 3 | carts 8 | settlement_entries 0
stock_positions 3577 | offers 3223 | parties 10 | identity_users 86
shipping_methods 7 | shipping_drafts 4
```

## Queue projection (read-only)

```
POST http://127.0.0.1:5088/v1/admin/fulfillments/work-queue/query  → totalCount 3
  Packed          group:01a1daa2-5929-…  next create_shipment
  Processing      group:01a1daa2-50e6-…  next mark_packed
  ReadyToFulfill  group:01a1daa2-48be-…  next mark_processing
```

## Operations projections (read-only)

```
GET http://127.0.0.1:5088/v1/admin/orders/{checkoutId}
GET http://127.0.0.1:5088/v1/admin/orders/{checkoutId}/operations
```

- unpaid `01a1daaf-…`: `PendingPayment`, actions `[cancel]`, `fulfillments [null]`
- ready `01a1daa2-430d-…`: `Paid`, actions `mark_processing` (whole) + `mark_processing|{orderLineId}` (line) + `cancel`
- processing `01a1daa2-4a2d-…`: `Paid`, `mark_packed` + `pack_selected`/`pack_selected|line` + `unprocess`/`unprocess|line` + `cancel`
- packed `01a1daa2-519d-…`: `Paid`, `unpack`/`unpack|line` + `create_shipment` + `cancel`

The whole-order and per-line entries are distinguished by `orderLineId` and by their own labels («شروع پردازش» vs «شروع پردازش این قلم»); they are not duplicate buttons — the whole-order one is the seller toolbar action, the per-line one belongs to the row menu.

Unpaid physical command (negative proof, zero effect):

```
POST /v1/admin/orders/01a1daaf-…/operations {"code":"mark_processing"}
→ 404 {"errorCode":"order.operation.invalid","title":"Order was not found."}
```

## Frontend

```
npx tsc --noEmit                              → 0
npm run build                                 → 0   BUILD_ID yuIokX6A6PV337VQbzU1k
npm run start (port 3000)                     → 200 PID 25852
```

## Tests

```
dotnet test ...Tooba.Fulfillment.Tests.csproj -c Release --filter FullyQualifiedName~PaymentAuthorizedSupplyGate266Tests
  → Passed! Failed: 0, Passed: 4, Skipped: 0

npx tsx --tsconfig tsconfig.mount-tests.json --test "app/admin/orders/*/operations/admin-order-operations-route.mount.test.ts"
  → tests 5, pass 5, fail 0

npx tsx --tsconfig tsconfig.mount-tests.json --test "features/order-carrier-execution/*queue*.test.ts" "app/admin/*work-queue*.test.ts"
  → tests 17, pass 17, fail 0
```

## Browser (served production build)

```
navigate http://127.0.0.1:3000/fa/admin/fulfillments
Emulation.setDeviceMetricsOverride 1440x900 / 390x844
Runtime.evaluate → row heights, cell scrollHeight/clientHeight, banner clipping, document overflow
click [data-testid="admin-fulfillment-open-operations-group:01a1daa2-48be-…"]
→ /admin/orders/01a1daa2-430d-…/operations?fulfillmentId=01a1daa2-48be-…&from=queue
→ target card present, target-missing absent, back link /admin/fulfillments, overflow 0
```

## Host

```
GET http://127.0.0.1:5088/health        → 200
GET http://127.0.0.1:5088/health/ready  → 200 (SingleStore)
```

No migration added; `dotnet ef`/migration runner was not invoked in this task.
