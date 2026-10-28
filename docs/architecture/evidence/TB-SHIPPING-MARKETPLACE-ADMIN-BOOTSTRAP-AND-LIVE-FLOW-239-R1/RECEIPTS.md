# RECEIPTS — TB-SHIPPING-MARKETPLACE-ADMIN-BOOTSTRAP-AND-LIVE-FLOW-239-R1

All requests are real HTTP against the task Marketplace target `http://127.0.0.1:5190`
(`Edition=Marketplace`, `DeploymentId=tb239-marketplace`, `Tenant=NULL`, DB `tooba_marketplace_tb239`).
Secrets (password/token/jwt) are omitted on purpose; no cookie or bearer value appears here.

## R1 — dedicated Development administrator, normal public registration

```
POST /v1/auth/register            -> 200  {"userId":"01a1818b-5257-7000-bada-84233a2b21fb"}
     body: identifierKind=Email, identifier=market-admin-tb239r1@example.invalid, random private password
```

Nothing privileged was given by registration itself:

```
POST /v1/auth/login               -> 200   (token omitted)
POST /v1/admin/catalog/demo/reset-and-seed -> 403
{"type":"about:blank","title":"Access to manage this store is not permitted.","status":403,
 "instance":"/v1/admin/catalog/demo/reset-and-seed","errorCode":"admin.authorization.denied",
 "traceId":"dc4bdc78298b4553a479a5481ed71eb5",
 "correlationId":"a93155b102ad43a5a494593816b3d434","requestId":"0HNPKOTR3UBI0:00000002"}
```

## R2 — private opt-in env + restarted task host

```
binary : D:\temp\tb239r1-build\out\Tooba.Host.dll   SHA256 prefix B31E1048BA6AB101
process: 5190 listener verified before restart (old PID 40544 -> 27428), /health 200
env    : Tooba__Development__MarketplaceAdmin__Enabled=true
         Tooba__Development__MarketplaceAdmin__Identifier=market-admin-tb239r1@example.invalid
         (identical Marketplace/gate env as 239; nothing checked in)
```

## R3 — protected official route now executes for the bound real principal

Host log (structured, one line, trimmed):

```
"Message":"Executed endpoint 'HTTP: POST /v1/admin/catalog/demo/reset-and-seed => ResetAndSeedAsync'"
"Message":"Request finished HTTP/1.1 POST http://127.0.0.1:5190/v1/admin/catalog/demo/reset-and-seed - 200 - application/json;+charset=utf-8 890620.3538ms"
```

Seed progress receipts (same log, sample):

```
CatalogDemo products progress: 50/584 … 150/584 … 300/584 … 475/584 … 550/584
```

## R4 — seller keeps the exact denial on the SAME route

```
POST /v1/auth/login (seller-actor-a@tooba.local) -> 200
POST /v1/admin/catalog/demo/reset-and-seed       -> 403
{"…","errorCode":"admin.authorization.denied","traceId":"2a7401a01661dccb7f163a3b29e503ae",
 "correlationId":"ab44302d620c4d00a5a8cbc89b9ff2b5"}
```

## R5 — a third normally registered account is not promoted by position

```
POST /v1/auth/register -> 200 {"userId":"01a181a0-57af-7000-b767-237b75b99e62"}
GET  /v1/admin/catalog/demo/status -> 403 admin.authorization.denied
```

## R6 — restart proves idempotent re-provisioning (InMemory authorization store is empty after restart)

```
old PID 27428 stopped, new PID 22832, GET /health 200
POST /v1/auth/login (bound admin) -> 200
GET  /v1/admin/catalog/demo/status -> 200
{"rootsDemo":15,"categoriesDemo":116,"categoriesTotal":116,"brandsDemo":22,"brandsTotal":22,
 "tagsDemo":36,"tagsTotal":36,"attributesDemo":41,"attributesTotal":41,"productsTotal":566,
 "productsDemo":566,"productsDraft":566,"productsPublished":0,"productsArchived":0,
 "allowResetAndSeed":true, …}
GET  /v1/admin/catalog/demo/status (seller-actor-a) -> 403
```

## R7 — the remaining owner-flow blocker, measured

```
catalog.products = 566 (all Draft)      party.parties = 0      offer.offers = 0
GET  /v1/storefront/home                     -> 200  (19 566 bytes)
GET  /v1/seller/offers (seller-actor-a)      -> 403  seller.authorization.denied
```

No Platform/Seller business scope exists, so no publication/offer/stock/shipping draft can be authored
and no cart line, projection, selection, payment preview or Order is reachable in this slice.
