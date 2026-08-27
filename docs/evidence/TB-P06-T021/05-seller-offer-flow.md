# 05 — Seller Offer flow (TB-P06-T021)

## Ownership

Admin Catalog Product (+ default Variant) → Seller creates Offer on `catalogVariantId` → Seller writes price/stock on own Offer.

Catalog remains read-only for sellers (`CatalogReadOnly: true`).

## HTTP

| Method | Path | Notes |
|---|---|---|
| GET | `/v1/seller/catalog-variants` | Published Catalog variants for picker (read-only) |
| POST | `/v1/seller/offers` | Body: `{ catalogVariantId, sellerSku?, status? }`. SellerPartyId **only** from authorized context headers — not from body |
| GET | `/v1/seller/offers` | Own offers |
| GET | `/v1/seller/offers/{offerId}` | Own only; foreign → 404 `seller.offer.missing` |
| PATCH | `/v1/seller/offers/{offerId}` | SKU / Active\|Suspended |

Access: `SellerPanelAccess.RequireAuthorizedAsync` on every route.

## Composer

- `CreateOfferAsync` → `IOfferDirectory.CreateOfferAsync` with authenticated `sellerPartyId`
- Optional Activate when `status=Active`
- Duplicate same seller+variant+channel → `seller.offer.create.rejected`

## UI

- `/vendor-panel/products` — Offer list + CTA «پیشنهاد جدید»
- `/vendor-panel/products/new` — pick Catalog variant, SKU, status, then price+stock writes
- `/vendor-panel/products/[offerId]` — edit Offer seam; Catalog RO

## Isolation

- Create body has no `SellerPartyId` (cannot create for foreign seller)
- Multi-seller Offers on the same Catalog Variant remain allowed
- Foreign seller cannot PATCH/price/inventory another seller’s OfferId

## Admin Catalog prerequisite

`POST /v1/admin/products` creates Published Product + default Variant (no price/stock on Product).
