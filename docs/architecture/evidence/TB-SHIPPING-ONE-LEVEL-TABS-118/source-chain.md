TB-SHIPPING-ONE-LEVEL-TABS-118 — SOURCE-TO-UI CHAIN (bounded, one representative path)
=====================================================================================

Route (statically verified + observed live)
-------------------------------------------
| Hop | Location | Role |
| --- | --- | --- |
| 1 | `src/frontend/app/shipping/page.tsx:17-20` | storefront route `/shipping`; `ShippingPage` renders `StorefrontShell` + `StorefrontShopeivaShipping` |
| 2 | `src/frontend/app/storefront/storefront-shipping.tsx:105-106` | `StorefrontShopeivaShipping`; first statement is `useCheckoutAuthGate("/shipping")` |
| 3 | `storefront-shipping.tsx:947-951` | the visible ONE-LEVEL step/"tab" model: step-1 سبد خرید (`/cart` link), step-2 ارسال, step-3 پرداخت — rendered at `:965` as `data-testid="shipping-steps"` |
| 4 | `storefront-shipping.tsx:608-660` (`:632`) | option rows: `projection.methods.map(...)` → `data-testid="shipping-method-${m.methodCode}"`, label text = `{m.label}` |
| 5 | `storefront-shipping-api.ts:275-312` (`:286`) | `loadShippingProjection` → `POST /v1/storefront/shipping/projection` (read-only projection) |
| 6 | `StorefrontOrderEndpoints.cs:37` + `:144-165` (`:152`) | endpoint maps the body to `ProjectStorefrontShippingQuery` **after** `gate.EnsureCheckoutActorAsync(ct)` |
| 7 | `CheckoutIdentityGate.cs:41-56` | the exact gate: needs `GuestAllowed` policy OR an authenticated session, else `checkout.authentication_required` |
| 8 | `ProjectStorefrontShippingQuery.cs:33-91` | Order-owned handler → `StorefrontShippingService.ProjectAsync` → shadow observers (observe-only) |
| 9 | `StorefrontShippingService.cs:434-461` (`:445`) | `ApplyOneLevelDisplayNamesAsync` optionally overrides **only** `Label` via `IShippingOneLevelStorefrontLabelReader` |
| 10 | `FulfillmentModule.cs:111` → `ShippingOneLevelStorefrontLabelRead.cs:41-98` | Fulfillment-owned label seam; `:54` returns `Empty` immediately when `ShippingOneLevelCatalogPolicy.Evaluate(...) != Enabled` |
| 11 | `ShippingOneLevelCatalogContracts.cs:73-81` | `ShippingOneLevelCatalogPolicy.Evaluate` → `Enabled` only when `Tooba:ShippingOneLevelCatalog:Enabled == true` (default OFF) |

Selection / transition toward Payment (one representative path)
---------------------------------------------------------------
| Hop | Location | Role |
| --- | --- | --- |
| A | `storefront-shipping.tsx:317-325` | `onSelectMethod` sets client state `methodCode` and re-reads the projection with `methodCode` (POST projection = read-only). No persistence. |
| B | `storefront-shipping.tsx:327-358` (`:332`) | `persistAndContinue` → `saveShippingSelection(projection.cartId, projection.cartVersion, …)` — the Order-owned draft store |
| C | `storefront-shipping-api.ts:315-352` (`:320`) | `PUT /v1/storefront/shipping/selection` → `SaveStorefrontShippingSelectionCommand` (Order module) |
| D | `storefront-shipping.tsx:350` | only then `router.push(localizePath('/payment?cartId=…'))` |
| E | `storefront-shipping-api.ts:378-418` | the legacy early-commit helper `commitShippingToPayment` (`POST /v1/storefront/shipping/commit`) is explicitly retired and has **no** caller: grep in the whole frontend finds it only in its own definition and in negative-assertion tests |
| F | `src/frontend/app/payment/page.tsx:17-23` | `/payment` renders `StorefrontPaymentHandoff`; the order/reservation is created only by the final payment click, not by shipping navigation |

Config gate status under the CURRENT shared configuration (read-only)
---------------------------------------------------------------------
`Tooba:ShippingOneLevelCatalog`, `Tooba:ShippingRateAuthority` and `Tooba:ShippingOperationalShadow`
do not appear in `src/backend/Host/Tooba.Host/appsettings.json` or `appsettings.Development.json`
(0 matches — see `one-level-flag-absence.txt`). Therefore:
- the storefront label overlay is fail-closed OFF → the storefront keeps the LEGACY method labels;
- no query is issued to the one-level catalog (`ShippingOneLevelStorefrontLabelRead.cs:53-56`).

Shared config's shipping surface that IS enabled:
`Tooba:ShippingMethods:EnabledCodes = [post, tipax, snapp_courier, store_courier, in_person]`,
`DefaultSellerPreparationDays = 1`, `DeliveryHorizonDays = 7`.
