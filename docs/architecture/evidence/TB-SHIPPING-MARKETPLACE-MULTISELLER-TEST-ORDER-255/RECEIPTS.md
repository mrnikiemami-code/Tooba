# RECEIPTS — TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255

All receipts are from this task's own session on the canonical machine, 2026-11-09 (UTC). Nothing below changed
runtime state; every database statement is read-only metadata/count verification.

## R1. Bridge claim/dispatch (BRIDGE-WAKE-V1)

```text
GET  http://127.0.0.1:17321/api/tasks?channelId=tooba-main   200
     -> first row: id=7c75271b-8dff-40fc-934e-eef17c74b6b2  taskId=TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255  status=Pending
GET  http://127.0.0.1:17321/api/tasks/next?channelId=tooba-main   200  (content = TASK.md payload, 7260 bytes persisted)
POST http://127.0.0.1:17321/api/workers/heartbeat  200
     {"workerId":"tooba-worker-01","channelId":"tooba-main","agentType":"cursor","status":"Working",
      "lastSeenAtUtc":"2026-11-09T02:48:59.9891907Z"}
```

The Bridge build exposes `POST /api/tasks/{id}/complete|fail` but no `claim` route (`/api/tasks/{id}/claim` → 404);
the single Pending row was therefore taken via `tasks/next` and kept as the one active task.

## R2. Git preflight

```text
git rev-parse HEAD            -> ab4ebd3ee1b28d20250291187695020ae3693729
git rev-parse origin/main     -> ab4ebd3ee1b28d20250291187695020ae3693729
git status --porcelain        -> (only this task's own new files after persistence)
git log --oneline -3          -> ab4ebd3ee docs(shipping): 254-R1 recovery TOPs + evidence code SHA
                                 08935f551 fix(shipping): wrap essential titles, feature-owned group dialog, ...
                                 5a11a4b8f fix(shipping): display-only operational delivery section + ...
```

## R3. Runtime identity (authoritative, served process)

```text
GET http://127.0.0.1:5088/health  200  {"status":"ok"}
GET http://127.0.0.1:5088/__platform-commerce  200
{"edition":"SingleStore","deploymentId":"store-alpha-canonical-tb253r4","tenantId":"store-alpha",
 "connectionReference":"tenant-alpha","resolvedHost":"127.0.0.1","themeReference":"theme-alpha",
 "defaultMarketReference":"IR","traceId":"4ae066180a9a47dfccb5e5540e18a28e"}

GET http://127.0.0.1:3000/fa      200  len=376150
```

Processes / ports:

```text
Get-NetTCPConnection -State Listen  -> 127.0.0.1:3000 (PID 45832 node) ; 127.0.0.1:5088 (PID 17344 Tooba.Host)
Get-CimInstance Win32_Process 45832 -> node ... next start -p 3000 -H 127.0.0.1
Get-CimInstance Win32_Process 17344 -> "D:\Users\User\source\repos\SarvNewVer\artifacts\publish-primary\Tooba.Host.exe"
Get-Process | ? { $_.ProcessName -match 'Tooba|dotnet|node' } -> only those two (no Marketplace process)
src/frontend/.env.local -> TOOBA_HOST_ORIGIN=http://127.0.0.1:5088
```

## R4. Served configuration (read-only)

`artifacts/publish-primary/appsettings.Development.json`:

```text
Tooba:Edition                          = "SingleStore"
Tooba:DeploymentId                     = "dev-singlestore"      (runtime deployment id reported: store-alpha-canonical-tb253r4)
Tooba:Marketplace:ConnectionReference  = "marketplace"          (no Marketplace tenants section exists)
Tooba:SingleStore:Tenants              = store-alpha (Active, tenant-alpha, hosts incl. 127.0.0.1), store-disabled
PostgreSQL:ConnectionReferences
  tenant-alpha  -> Host=127.0.0.1;Port=5432;...Database=tooba_alpha
  marketplace   -> Host=127.0.0.1;Port=5432;...Database=tooba_marketplace
  messaging     -> ...Database=tooba_messaging
```

## R5. Code authority for the owner kind

```text
StorefrontCapturedOwnerPolicy.cs
  138  ToobaEdition.SingleStore => ResolveStore(tenant, responsibility.Value)
  139  ToobaEdition.Marketplace => ResolveMarketplace(tenant, responsibility.Value, groupSellerPartyId)
  160  OfferFulfillmentResponsibility.Platform => Resolved(ShippingCapturedQuoteOwnerKind.Store, tenantId, null)
  163-164 OfferFulfillmentResponsibility.Seller => Failure(...SellerUnsupportedInSingleStore)

ShippingCapturedOwnerScopeResolver.cs
  38   ToobaEdition.SingleStore => ResolveStore(kind, tenantId, sellerPartyId, tenant)
  56   || kind != ShippingCapturedQuoteOwnerKind.Store     (SingleStore accepts Store scope only)

ShippingAuthoringOwnerAuthorizer.cs
  111-113 installation.Edition == ToobaEdition.SingleStore ? ForStore(tenantId!) : ForMarketplace()
  140      seller authoring -> SemanticException(shipping.authoring.owner_scope_unavailable) outside Marketplace

OfferFulfillmentResponsibility  -> Seller = 0, Platform = 1   (no Marketplace responsibility value)
ShippingCapturedQuoteOwnerKind  -> Store = 0, Marketplace = 1, Seller = 2
```

## R6. Database inventory (read-only)

```text
docker exec postgres-db psql -U admin -d postgres -c "select datname from pg_database where datistemplate=false"
  -> MyDatabase ShopDb TravelCore postgres sarvdb tooba_alpha tooba_codex_dev tooba_disabled tooba_easy_test
     tooba_first_test tooba_marketplace tooba_marketplace_tb239 tooba_messaging travelcore

tooba_marketplace        -> 74 tables ; schemas: access_control content identity media notification order
                            page_composition party payment public returns settlement story
                            (NO catalog / offer / cart / shipping / geography / fulfillment)
tooba_marketplace_tb239  -> 269 tables ; 34 schemas incl. catalog, offer, cart, shipping, geography, fulfillment
                            counts: products 568, offers 3, carts 24, checkouts 6  (previous task's trial data)
tooba_alpha              -> 35 schemas incl. catalog, offer, cart, order, shipping, geography, fulfillment
                            counts: carts 103, order.checkouts 22
```

## R7. Retained prior order (unchanged; no write performed)

```sql
select seller_party_id, method_code, service_code, option_code, accepted_owner_kind, accepted_owner_tenant_key,
       accepted_receiving_date, requested_delivery_time_window, shipping_charge_snapshot
from "order".order_shipping_groups
where checkout_id = '01a1bd30-7efe-7000-9abc-3bcc111d967c' order by seller_party_id, method_code;
```

```text
 seller_party_id                      | method_code      | service | option   | owner_kind | owner_tenant | date       | window    | fee
 01a030d1-40cb-7000-8abe-6d31739956c5 | post\x1F         | post    |          |     0      | store-alpha  | 2026-11-16 | 09:00-10:00 | 125000
 01a030d1-40cb-7000-8abe-6d31739956c5 | post\x1Fstandard | post    | standard |     0      | store-alpha  | 2026-11-11 | 09:00-17:00 | 125000
 01a0ffff-0001-7000-8000-000000000001 | post\x1F         | post    |          |     0      | store-alpha  | 2026-11-16 | 09:00-10:00 | 125000
```

```text
"order".checkouts for 01a1bd30-… : mode=OnlinePurchase  channel=Marketplace  currency=IRR
                                   shipping_amount=375000.00  submitted_at=2026-11-08T22:03:54Z  (unchanged)
```

## R8. Explicit negative receipts

```text
new carts created:            0
new checkouts/orders created: 0
reservations / payment:       0 attempts
migrations executed:          0
Host/FE rebuild or restart:   none
edition/tenant/connection/config edits: none
direct business SQL writes:   none  (all SQL above is SELECT on metadata/counts)
screenshots / browser runs:   none
```

## R9. Files written by this task

```text
docs/ai/tasks/TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255/TASK.md              (verbatim Bridge payload, 7260 bytes)
docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255/RESULT.md
docs/architecture/evidence/TB-SHIPPING-MARKETPLACE-MULTISELLER-TEST-ORDER-255/RECEIPTS.md
AGENTS.md · docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md ·
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md                       (CURRENT block prepended)
```
