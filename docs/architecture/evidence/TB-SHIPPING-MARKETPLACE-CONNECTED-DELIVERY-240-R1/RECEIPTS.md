# RECEIPTS — TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R1

Secrets (passwords, tokens, guest secrets) are intentionally omitted. Host: `127.0.0.1:5190`,
`ASPNETCORE_ENVIRONMENT=Development`, `Tooba__Edition=Marketplace`, `DeploymentId=tb239-marketplace`,
DB `tooba_marketplace_tb239`; process command line verified (`dotnet Tooba.Host.dll`) before the task-only restart.

## R1 — reference data (one host start, opt-in local env only)

```
Tooba__Development__MarketplaceReferenceData__Enabled=true   (checked-in configuration stays absent/OFF)
after start:
  localization.languages               = 2   (fa-IR default, en-US)
  geography.countries                  = 1
  geography.administrative_areas       = 1
  geography.cities                     = 1
GET /v1/admin/geography/countries -> 200 [{"id":"01a181c5-f010-7000-9d19-8290cdacb5d7","code":"IR","label":"ایران","isActive":true}]
IDs: country 01a181c5-f010-7000-9d19-8290cdacb5d7
     area    01a181c5-f01b-7000-aeee-36edde29aa92
     city    01a181c5-f02f-7000-9554-fb47fa3aab6b
```

## R2 — fulfillment catalog through normal admin APIs

```
POST /v1/admin/shipping-types                                  -> 201 heavy (isActive false, revision 1)
PUT  /v1/admin/shipping-types/{id}                             -> 200 heavy isActive true revision 2
POST /v1/admin/shipping-services                               -> 200 post + option standard
PUT  /v1/admin/shipping-services/{id}                          -> 200 FA/EN names corrected (UTF-8 body)
POST /v1/admin/shipping-services/shipping-methods/one-level    -> 200 post:standard (inactive)
PUT  /v1/admin/shipping-services/shipping-methods/one-level/post:standard/activation -> 200 isActive true
```

## R3 — owner assignments

```
PUT /v1/admin/offers/01a181b0-5caf-7000-9c44-e82f2457a122/fulfillment {responsibility:Platform, heavy} -> 200
PUT /v1/admin/offers/01a181b0-5e93-7000-bcc8-85f8532bd345/fulfillment {responsibility:Seller,   heavy} -> 200
PUT /v1/admin/offers/01a181b0-5cd7-7000-8ba1-a45b0df5425f/fulfillment {responsibility:Seller,   heavy} -> 200
```

## R4 — Platform publication (normal authoring APIs)

```
POST /v1/admin/shipping-services/authoring-drafts          -> 201 draft 01a181ca-e730-7000-bfaf-a5d9e5778dee
PATCH …/{draft}/presentation                               -> 200 (fa/en method names, group titles)
PUT   …/{draft}/weekly-schedule                            -> 200 (Asia/Tehran, Sat–Thu 09:00–17:00, Fri closed)
PUT   …/{draft}/method-settings                            -> 200 (Prepaid + Manual)
PUT   …/{draft}/method-type                                -> 200 (heavy)
PUT   …/{draft}/region-groups/{group}/rate-rows            -> 200 (freight 95,000 / packaging 5,000 / 2..4 days)
POST  …/{draft}/publication                                -> 200 state=Selected
GET   …/{draft}/publication -> state Selected, revision 6,
      snapshot 01a181ca-e8f8-7000-840c-1a09cbb105e5, sourceRevision 6, selectionVersion 1, matchesCurrent true
```

## R5 — seller-scope authoring (BLOCKED, measured)

```
POST /v1/seller/shipping-services/authoring-drafts
     Bearer seller-actor-a + X-Tooba-Seller-Party-Id 01a181b0-5c87-7000-976e-ae5ce19864f6
  -> 403 shipping.authoring.owner_scope_denied
     traceId 35a162f2c4ad478cc7f6c66b5da15fb7
DB: access_control.roles            = platform-admin only (55 permissions)
    access_control.user_role_assignments = 1 (platform admin)
    access_control.platform_seller_ceilings = 0 rows
=> no Seller-scope grant exists, and the admin create-role/assign endpoints are hard-coded to Platform scope.
```

## R6 — buyer flow, Platform-owned group only (cart reduced to that one real line)

```
POST /v1/storefront/shipping/projection  (destination IR / Tehran / Tehran) -> 200
  3-line cart: groups 3 -> platform PRICED, seller x2 no_selected_capture
  after removing the two seller lines (normal DELETE line API, cart v9):
    group cap|platform|marketplace|01a181b05c877000976eae5ce19864f6|01a181c778637000925fb6f64ed4694b
      typeLabel Heavy, goods 2016500, methods 1
      method: freight 95000.00, packaging 5000.00, payable 100000.00, currency IRR
              earliest 2026-10-31, snapshot 01a181ca-e8f8-…, sourceRevision 6, selectionVersion 1,
              areaGroup 01a181ca-e731-7000-9037-0a1d0f7354e5, rateRow 01a181ca-e8a1-7000-b24b-ab819db7a043
PUT /v1/storefront/shipping/selection  (capturedGroups + expected echoes, expectedSelectionRevision "0")
  -> 200 revision 1, 1 group
  (first attempt 400 storefront.captured_selection.intent_invalid because legacy shorthand was also present —
   the captured route refuses any shorthand; re-sent with blank shorthand -> 200)
GET /v1/storefront/cart/{id}/payment-draft -> 200, 1 shipping group
POST /v1/storefront/cart/{id}/start-payment (idempotencyKey, providerCode manual, expectedSelectionRevision "1")
  -> 200 checkoutId 01a181cf-afd3-7000-a33f-7ea2262fbe6e
         paymentId  01a181cf-b189-7000-81db-90623918e858, attempt 01a181cf-b18b-7000-bcc5-5645135aa706
         paymentStatus Pending, providerCode manual, requiresPspRedirect false
         amount 2116500.0000000000 IRR, reservationCount 1, awaitingManual=1
repeat with the SAME idempotencyKey -> 200 checkoutId 01a181cf-afd3-… (same checkout, checkoutReplayed false)
         NEW paymentId 01a181cf-cd1b-7000-ad50-30267c6c9802 / attempt 01a181cf-cd1b-7000-9c00-6625b45bddf6
```

## R7 — persisted order evidence (read-only query)

```
order.checkouts = 1
order.order_shipping_groups (accepted_snapshot_id not null) = 1
post:standard | Platform delivery 240 | 95000.00 | 5000.00 | 100000.00 | 0.00 | ownerKind 1 | tenant - | seller -
              | sourceRevision 6 | selectionVersion 1 | receiving 2026-10-31
              | ticks 324000000000..612000000000 | Asia/Tehran | requested 2026-10-31
```

## R8 — verification of the new seam

```
dotnet build src/backend/Host/Tooba.Host.Tests -> 0 errors
dotnet test  MarketplaceReferenceDataBootstrapTests -> Passed 6/6
```

## R9 — not produced

No UI PNG (no time), no mixed-owner order, no coupon run, no seller publication, no concurrency race probe.
