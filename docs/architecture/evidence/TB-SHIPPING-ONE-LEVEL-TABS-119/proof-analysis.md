TB-SHIPPING-ONE-LEVEL-TABS-119 — ONE ISOLATED AUTHORIZED PROOF (SOURCE CHAIN OF THE MISSING DEPENDENCY)
=====================================================================================================

Why the one-level label half cannot be completed with existing sanctioned seams (static proof)
---------------------------------------------------------------------------------------------
| Seam | Behaviour on a fresh scratch database |
| --- | --- |
| `Migration 20261015120000_AddShippingOneLevelCatalog` | creates `fulfillment.shipping_methods` + `shipping_method_translations` **tables only** — no data |
| `ShippingOneLevelCatalogActivation.SetActivationAsync` (`ShippingOneLevelCatalogActivation.cs:75-81`) | `MethodNotFound` when the row is absent; it never creates a row |
| `ShippingOneLevelCatalogAdminWrite.UpdateMethodNameAsync` (`:75`, `:96`) | `MethodNotFound` when the row is absent; it never creates a row |
| `ShippingOneLevelBackfillStore` / `ShippingMethodBackfillPlanner` | the ONLY code that can create rows — `grep IShippingOneLevelBackfillStore` finds it **registered in DI (`FulfillmentModule.cs:117`) and used only by tests**; there is no production/Host caller |
| `IShippingCatalogSeedPort` → `ShippingCatalogSeedAdapter` → `Directory.EnsureSeedAsync` | seeds only the LEGACY services/options (`shipping_services`, `shipping_service_options`), never the one-level tables |
| `ShippingOneLevelStorefrontLabelRead.ResolveDisplayNamesAsync` (`:41-98`) | returns `Empty` when the gate is off OR when no row matches → the storefront keeps the LEGACY label |

Conclusion: with the gate ENABLED, the one-level catalog is *readable but empty*, and there is **no
sanctioned runtime path to populate it**. Therefore the acceptance clause "proves one-level display
labels originate from the enabled catalog under scratch provenance" is unreachable without direct SQL
DML (forbidden) or new production code (forbidden). This is the exact canonical missing dependency.

What WAS proven, live, on the isolated Host (PID 23440, :5399, single launch, scratch tenant-119-scratch)
-------------------------------------------------------------------------------------------------------
1. Isolated provenance: `GET /__platform-commerce` → `deploymentId=isolated-119`,
   `connectionReference=tenant-119-scratch`, `resolvedHost=127.0.0.1`, `tenantId=store-alpha`.
2. Offline fail-closed guard PASS (`isolation-guard.json`): 1 tenant / 1 active, no `tooba_alpha`,
   no `tenant-alpha`, no Port=5432, no prior scratch port, RunLegacyBootstraps=true,
   AllowResetAndSeed=false, ShippingRateAuthority/ShippingOperationalShadow absent.
3. Admin identity through the canonical Development seam only: `GET /v1/admin/dev-context` →
   actor `01a1429f-b110-7000-a670-da221ac929ef` (`admin-actor@tooba.local`). No invented header value.
4. Identity policy through the canonical Catalog policy seam:
   `GET /v1/storefront/checkout-identity-policy` → `AuthenticatedOnly` (baseline);
   `PUT /v1/admin/settings/checkout-identity {"policy":"GuestAllowed"}` → 200;
   re-read → `{"policy":"GuestAllowed","cartAnonymousAllowed":true,"checkoutAuthenticationRequired":false}`.
   This is the *sanctioned* way to let the storefront projection pass its identity gate — no bypass.
5. Real scratch cart through the canonical Cart API: `POST /v1/storefront/cart` → 200 cartId
   `01a142a0-23dc-7000-a1c0-3f7d1dd1f468` (+ ephemeral guest secret, never recorded here);
   `POST /v1/storefront/cart/{id}/lines` with the real offer `01a1429f-ad3b-7000-b874-7f3c7ed8111e`
   (from `GET /v1/storefront/products/schema-mobile-demo-phone` → `variants[].primaryOffer.offerId`) → 200,
   `itemCount=1`, subtotal 12 500 000 IRR.
6. **The proof**: authorized `POST /v1/storefront/shipping/projection` → **200** with
   `itemCount 1`, `sellerCount 1`, `maxSellerPreparationDays 1`, **7 methods** and **12 provinces**:
   `post:express` «پست — پیشتاز» 200000 / lead 2, `post:standard` «پست — معمولی» 120000 / lead 4,
   `tipax:express` «تیپاکس — پیشتاز» 260000 / lead 1, `tipax:standard` «تیپاکس — معمولی» 180000 / lead 3,
   `snapp_courier` «اسنپ / پیک آنلاین» 90000 / lead 0, `store_courier` «پیک فروشگاه» 75000 / lead 1,
   `in_person` «تحویل حضوری» 0 (free, lead 0). Time windows 9-12/12-15/15-18/18-21. A second projection
   with `provinceName=تهران` returned the identical 7 methods (destination filter did not remove any).
   The option identity is carried explicitly (`optionCode=express|standard|null`), proving the
   "service:option" composite is never treated as a service code.
7. The distinguishing observation for this task: the gate WAS enabled in the isolated overlay
   (`ShippingOneLevelCatalog.Enabled=true`), and `GET /v1/admin/shipping-services/shipping-methods/one-level`
   returned **`{"gate":"Enabled","ready":true,"languageId":"01a1429f-af2a-7000-8f70-71ecb0870e3d","methods":[]}`**
   — plumbing healthy, catalog empty. Consistently, every projected label above is the **legacy**
   composed label, i.e. `ApplyOneLevelDisplayNamesAsync` correctly overrode nothing and invented nothing.

Requested-but-corrected detail
------------------------------
The first probe (proof attempt #1) extracted offers from the wrong JSON branch, so the cart stayed empty
and the projection returned a well-formed `400 shipping.cart.empty`; the corrected pass used
`variants[].primaryOffer.offerId` and succeeded. Both ran against the SAME single Host launch — no relaunch,
no restart, no extra process.
