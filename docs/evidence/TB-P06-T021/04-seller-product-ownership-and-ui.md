# 04 — Seller product ownership and UI (TB-P06-T021)

## Ownership verdict (current architecture + code)

**Model in force:** `Catalog Product != Seller Offer` (`docs/architecture/07-catalog-product-offer.md`, `docs/architecture/42-catalog-product-variant-foundation.md`).

| Concern | Owner | Seller may mutate today? |
|---|---|---|
| Canonical Product / Variant / category / brand / descriptive fields / media **references** | **Catalog** | **No** — seller surfaces mark Catalog as read-only |
| Commercial Offer (bind seller ↔ catalog variant, seller SKU, offer status) | **Offer** | **Partial** — patch SKU + Active/Suspended only |
| Base price | **Pricing** | **No** (read-only display) |
| Stock / availability quantities | **Inventory** | **No** (read-only display) |

**Practical ownership for sellable demo (matches locked separation):**

```text
Admin / Catalog ops creates & publishes Product (+ Variant, category, media refs)
→ Seller creates/owns Offer on that Variant
→ Seller (or authorized actor) writes price via Pricing
→ Seller writes stock via Inventory
→ Storefront composes purchasable listing/PDP
```

This is **Admin-created Product + Seller Offer**, not “Seller owns Catalog Product write model.”

Code explicitly encodes Catalog RO for sellers:

- `SellerOfferDetailPage.CatalogReadOnly` always `true` in `SellerPanelComposer.cs`
- Vendor UI section “زمینهٔ Catalog (فقط‌خواندنی)” — `src/frontend/app/vendor-panel/products/[offerId]/page.tsx`

TB-P06-T021 allows preserving Admin Product + Seller Offer if that is the architecture; **do not** collapse Price/Stock onto Product.

**Gap vs intended ownership:** Admin Catalog create and Seller Offer/Price/Inventory create-write HTTP/UI are still missing (see `03-sale-blockers.md`). Today both Catalog and Offer **creates** happen in Development bootstrap, not panels.

## Current vendor-panel product routes

| Route | File | Behavior |
|---|---|---|
| `/vendor-panel/products` | `src/frontend/app/vendor-panel/products/page.tsx` | Live DataGrid of **seller Offers** (`loadSellerOffers`). Columns: product title, seller SKU, offer status, price, available units, updated. Links to detail by **offerId**. **No create button.** |
| `/vendor-panel/products/[offerId]` | `src/frontend/app/vendor-panel/products/[offerId]/page.tsx` | Offer edit seam: editable `sellerSku` + `status` (Active/Suspended). Catalog title/brand/channel RO. Price + inventory displayed RO. |
| Nav | `src/frontend/app/vendor-panel/vendor-shell.tsx` | “محصولات” → `/vendor-panel/products` (`live: true`) |

Related sale ops (not product authoring): `/vendor-panel/orders`, `/vendor-panel/orders/[sellerOrderId]`, `/vendor-panel/fulfillments`, `/vendor-panel/fulfillments/[fulfillmentId]`.

## Seller product/offer APIs (current)

Client: `src/frontend/app/vendor-panel/seller-api.ts`  
Host: `src/backend/Host/Tooba.Host/Seller/SellerPanelEndpoints.cs`

| Method | Path | Purpose |
|---|---|---|
| GET | `/v1/seller/dashboard` | KPIs including `activeOffers` |
| GET | `/v1/seller/offers` | List own offers + composed price/availability |
| GET | `/v1/seller/offers/{offerId}` | Offer detail + Catalog RO context |
| PATCH | `/v1/seller/offers/{offerId}` | Body: `{ sellerSku?, status? }` only |
| GET | `/v1/seller/orders` | Own seller-orders |
| GET | `/v1/seller/orders/{sellerOrderId}` | Own order detail |
| GET | `/v1/seller/dev-contexts` | Dev actor/seller bootstrap contexts |

Auth/context headers (FE): `X-Tooba-Seller-Party-Id`, Dev `X-Tooba-Dev-Actor-User-Id` (`seller-api.ts`). Foreign seller Offer → 404/`seller.offer.missing` (isolation in composer).

**Absent (needed for ownership flow above):**

- `POST /v1/seller/offers` (create Offer on `catalogVariantId`)
- Seller Pricing write endpoints
- Seller Inventory adjust endpoints
- Any seller Catalog Product create/edit endpoints (correctly absent if Admin owns Catalog — but then Admin create HTTP must exist)

## Admin Catalog / product workspace (counterpart)

| Surface | Path / API | Role in ownership |
|---|---|---|
| Admin product list | `src/frontend/app/admin/products/page.tsx` → `product-list.tsx` | Lists products via `GET /v1/admin/products` |
| Admin product workspace | `src/frontend/app/admin/products/[productId]/page.tsx` → `product-workspace-screen.tsx` | Composed view: variants, media, offers, prices, inventory, tax — mostly **read**; title editable |
| Host | `ProductWorkspaceEndpoints.cs` | `GET /`, `GET /{productId}`, `PATCH /{productId}/catalog-title` only |

Admin **cannot** currently create Product/Variant/Offer/Price/Stock over HTTP; seed fills the workspace.

## Seed vs panel (how “products” exist today)

1. `StorefrontDemoCatalogBootstrap.cs` — creates Published products, variants, offers, prices, inventory for storefront demo matrix.
2. `ProductWorkspaceDevelopmentBootstrap.cs` — workspace sample product + dual-seller offers.

Panels operate on that pre-seeded commercial graph; they do not author a new sellable item.

## Implication for T021 implementation (documentation only)

Preserve **Admin Catalog Product + Seller Offer** ownership:

1. Expose minimal **Admin** Catalog authoring HTTP/UI (product + default variant + category + media ref + publish), **or** an Architect-approved seller-create-product policy (would change `CatalogReadOnly` posture).
2. Expose **Seller Offer create** + Pricing/Inventory writes bound to `OfferId` / seller ownership checks.
3. Keep vendor “محصولات” as Offer-centric merchandising UI (already aligned with Shopeiva vendor product list semantics).
