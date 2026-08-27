# 03 — Sale blockers only (TB-P06-T021)

Scope rule: **only** issues that prevent (a) a seller from offering a product for sale, (b) a customer from buying it, or (c) seller/admin from completing the resulting order.

Non-blockers (wallet, tickets, advanced variants, settlement payouts, stories, deep CMS, etc.) are **omitted** here.

## Blockers

### B1 — No HTTP/UI path to create Catalog Product (+ publish / category / media / default Variant)

**Prevents:** seller (or admin-on-behalf) from putting **new** merchandise into Catalog so it can become sellable.

| Gap | Detail |
|---|---|
| Directory exists | `ICatalogDirectory.CreateProductAsync`, `AssignCategoryAsync`, `AttachMediaReferenceAsync`, `PublishProductAsync`, `CreateVariantAsync` — `src/backend/Modules/Catalog/Tooba.Catalog.Application/CatalogContracts.cs` |
| Host HTTP | Admin product routes are **read + title only**: `GET /`, `GET /{productId}`, `PATCH /{productId}/catalog-title` — `src/backend/Host/Tooba.Host/Admin/ProductWorkspaceEndpoints.cs` |
| Seller HTTP | **No** `/v1/seller/products` (or equivalent) create/edit Catalog routes — `SellerPanelEndpoints.cs` |
| UI | Admin workspace compose UI has no create flow (`src/frontend/app/admin/products/**`, `product-workspace-screen.tsx`). Vendor “محصولات” is Offer list only (`vendor-panel/products/page.tsx`) — no “create product” CTA |
| Today’s only writer | Development seed: `StorefrontDemoCatalogBootstrap.cs`, `ProductWorkspaceDevelopmentBootstrap.cs` |

Without B1 (or an Architect-approved alternate), T021 cannot create a legitimate sellable item “through APIs/UI, no direct DB mutation.”

### B2 — No HTTP/UI path for Seller to create Offer bound to a Catalog Variant

**Prevents:** seller from offering a product for sale (Offer is the commercial bind).

| Gap | Detail |
|---|---|
| Directory exists | `IOfferDirectory.CreateOfferAsync` — `OfferContracts.cs` / `OfferDirectory.cs` |
| Host HTTP | Seller expose only `GET /offers`, `GET /offers/{id}`, `PATCH /offers/{id}` — **no POST create** (`SellerPanelEndpoints.cs`) |
| Patch surface | `SellerOfferPatchRequest(SellerSku, Status)` only — `SellerPanelModels.cs`; composer `PatchOfferAsync` activates/suspends + SKU (`SellerPanelComposer.cs`) |
| UI | `vendor-panel/products/[offerId]/page.tsx` edits SKU/status for **existing** offers; no create-offer form |

### B3 — No HTTP/UI path to write price through Pricing owner

**Prevents:** seller from configuring a valid sellable price (and therefore listing/checkout eligibility for new/changed offers).

| Gap | Detail |
|---|---|
| Directory exists | `IPriceDirectory.CreatePriceAsync` / `ChangeAmountAsync` / `ActivateAsync` — `PricingContracts.cs` |
| Host HTTP | **No** `/v1/seller/.../prices` or admin pricing mutate endpoints (search of Host `*Endpoints.cs` shows none) |
| UI | Vendor detail explicitly: price display + “قیمت و موجودی در این slice فقط‌خواندنی” — `vendor-panel/products/[offerId]/page.tsx` |
| Impact | Storefront requires Active price for purchasable composition (`StorefrontComposer.cs`); without write API, only seeded prices work |

### B4 — No HTTP/UI path to adjust sellable stock through Inventory owner

**Prevents:** seller from configuring sellable inventory / availability for an Offer.

| Gap | Detail |
|---|---|
| Directory exists | `OpenPositionAsync`, `AdjustAsync` — `InventoryContracts.cs` / `InventoryDirectory.cs` |
| Host HTTP | **No** seller/admin inventory adjust endpoints |
| UI | Available units / on-hand read-only on vendor offer detail |
| Impact | Checkout reservation needs stock; zero/unavailable blocks buy. New offers cannot receive stock without seed/tests |

### B5 — Seller cannot complete “manage sellable merchandise” for T021 even on seeded Offers

**Prevents:** proving Seller A “sets valid authoritative price” and “sets stock” on their Offer (task E/G/H/T), despite existing seed rows.

Concrete: seller can toggle Active/Suspended and SKU, but **cannot** mutate Pricing or Inventory via any current panel API. That is enough to block the required seller configuration steps of TB-P06-T021 even when Catalog rows already exist.

## Explicit non-blockers (for this file)

These do **not** prevent offer/buy/complete-order on the **seeded** path, so they are not listed as sale blockers:

- Customer wallet / tickets / gift cards / notifications
- Advanced variant matrix / attribute schema redesign (`ADVANCED_VARIANT_DEFERRED` — see `18-current-variant-boundary.md`)
- Settlement payout processing depth beyond seeing Paid order + fulfillment
- Full Media binary pipeline (placeholders already render via Catalog media refs + `GET /v1/storefront/media/{assetId}`) — **except** lack of any Catalog `AttachMediaReference` HTTP remains folded into **B1** when authoring a new product

## Buy/complete path status (context, not additional blockers)

Once an Offer already has Active status + Active price + available inventory (as Development seed provides):

- Listing/PDP/cart/checkout/sandbox payment/order/fulfillment are **not** currently broken for that seeded merchandise (`StorefrontEndpoints.cs`, Payment sandbox, Order panel endpoints, `FulfillmentEndpoints.cs`).

The **blocking gap for T021 PASS** is the **seller/admin authoring + price/stock write** chain (B1–B5), not the guest buy pipeline against seed data.
