# TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240 — RECEIPTS

All receipts were produced against the task-only Marketplace Host `http://127.0.0.1:5190`
(Development, DeploymentId `tb239-marketplace`, Tenant NULL, DB `tooba_marketplace_tb239`).
Secrets (passwords, tokens, guest secrets, connection strings) are deliberately omitted.

## R1 — Host identity at handoff

```
PID 23444  dotnet Tooba.Host.dll   (D:\temp\tb240-build\out, published from dc987ce4e)
GET /health -> 200
Edition Marketplace · Tenant NULL · ASPNETCORE_ENVIRONMENT Development
checked-in flags: StorefrontCapturedPreview Enabled/SelectionEnabled/CheckoutEnabled = false
task runtime only: all three = true, AvailableDateCount = 7
Tooba__Development__MarketplaceAdmin__Enabled = true      (239-R1 seam)
Tooba__Development__MarketplaceSellableSeed__Enabled = true  (this task)
Tooba__StoreCommerce__Market = IR
```

## R2 — Sellable fixture commissioning (measurement, read-only SQL)

Before: `party.parties = 0`, `offer.offers = 0`.
After the opt-in seed (no reset, no 566-product replay):

```
parties=2  offers=3  prices=3  taxcats=1  taxrules=1  classifications=3
stock=4    locations=3
```

Second full Host restart (same DB, same env) → `parties=2 offers=3` unchanged.

## R3 — Seller scope (real Identity tokens, normal seller panel header)

```
GET /v1/seller/offers  seller A token + own seller header     -> 200  (706 bytes)
GET /v1/seller/offers  seller A token + Seller B seller header -> 403
GET /v1/seller/offers  seller B token + own seller header      -> 200  (355 bytes)
```

## R4 — Admin authorization for shipping authoring on a fresh owner

```
POST /v1/admin/access-control/bootstrap          -> 204
GET  /v1/admin/access-control/me/capabilities    -> 200, includes fulfillment.manage + fulfillment.view
GET  /v1/admin/shipping-services/authoring-types -> 200 {"types":[]}
GET  /v1/admin/shipping-services/authoring-drafts-> 200
POST /v1/admin/shipping-services/authoring-drafts (1st, before re-bootstrap)
                                                 -> 403 {"errorCode":"admin.authorization.denied"}
POST /v1/admin/access-control/bootstrap (again)  -> 204
POST /v1/admin/shipping-services/authoring-drafts
                                                 -> 400 {"errorCode":"shipping.service_code.unknown"}
```

The second bootstrap pass is what changed the outcome; the same admin, token and body were used.
This is an observed platform seam behaviour, reported — not modified.

## R5 — Normal buyer + real mixed-owner cart

```
POST /v1/auth/register  -> 200 {"userId":"01a181b6-0b20-7000-895e-bd938f048575"}
POST /v1/auth/login     -> 200 (bearer token)
POST /v1/storefront/cart
  -> 200 {"cartId":"01a181b8-2640-7000-a27e-a9c926ca1cf1","version":1,"market":"IR",
          "defaultCurrency":"IRR","channel":"Marketplace","itemCount":0}
POST /v1/storefront/cart/{id}/lines  (ARM-LN-01)                    -> 200 v3 lines=1
POST /v1/storefront/cart/{id}/lines  (ARM-SC-01, same Seller A)     -> 200 v5 lines=2
POST /v1/storefront/cart/{id}/lines  (DGS-LN-01, Seller B)          -> 200 v7 lines=3
```

`X-Tooba-Cart-Version` and `X-Tooba-Guest-Secret` are required; `If-Match` yields
`cart.version.conflict` and a missing guest secret yields `cart.guest.invalid` (both observed).

## R6 — Captured projection (the mixed-owner read)

```
POST /v1/storefront/shipping/projection {"cartId":"01a181b8-…","language":"fa"}
 -> 200 cartVersion 7 itemCount 3.0 subtotalExclusiveOfTax 5,090,000 sellerCount 2
    capturedPreview.enabled true  currency IRR
    groups:
      cap|unassigned|01a181b05caf70009c44e82f2457a122  seller 01a181b0-5c87-7000-976e-ae5ce19864f6
      cap|unassigned|01a181b05e937000bcc885f8532bd345  seller 01a181b0-5c87-7000-976e-ae5ce19864f6
      cap|unassigned|01a181b05cd770008ba1a45b0df5425f  seller 01a181b0-5c9a-7000-bf9b-8c8e1c47da72
    every group: methods 0, deliveryDates 0,
                 unavailableReason "storefront.captured_preview.destination_required"
```

Re-run with legacy text `provinceName=تهران&cityName=تهران` → identical
`destination_required` refusal and still 0 methods / 0 dates.

## R7 — Reference-data gaps that stop the flow (read-only SQL)

```
countries=0  administrative_areas=0  cities=0          (geography schema)
languages=0                                            (localization schema)
shipping_services=0  shipping_methods=0  shipping_types=0  (fulfillment schema)
prices=3  taxcats=1  taxrules=1  classifications=3  stock=4  locations=3
```

Source search results supporting the conclusion:

- `git ls-files src/backend/Modules/Geography/**` contains Contracts/Domain/Infrastructure/Endpoints
  only — no `Development` folder, no seed, no importer.
- All mapped Geography routes are `MapGet` (admin: countries, administrative-areas, cities,
  service-areas; storefront: countries, administrative-areas, cities).
- The only language seeding seam is
  `Tooba.Localization.Infrastructure.Bootstrap.LanguageBootstrapDevelopmentSeed.PostMigration.EnsureAsync`,
  which the Marketplace bootstrap does not call (`LanguageBootstrapHostedService` skips at startup
  because no commerce context is bound: observed log line "Language bootstrap skipped: no commerce
  context is bound at host startup").
- `CreateOneLevelShippingMethodHandler` refuses before any write when no real registered language
  name is supplied.

## R8 — Focused code verification

```
dotnet build src/backend/Host/Tooba.Host.Tests  -> 0 errors
dotnet test  --filter "MarketplaceSellableSeedBootstrapTests|MarketplaceAdminDevBootstrapTests|HostDevelopmentAmcGuardTests"
  -> Passed 15, Failed 0, Skipped 0
```

No full Order/Host/Shipping/solution sweep was run. Known baseline reds (bg-white token,
`DateTimeOffset.UtcNow` guard, packaging allow-list) were neither repaired nor counted green.
