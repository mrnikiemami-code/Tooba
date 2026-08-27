# 10 — PDP live sale proof (TB-P06-T021)

Constraint: **do not visually redesign** PDP. Prove live sale surfaces for a seller-authored (or Admin Catalog + Seller Offer) purchasable item under the Shopeiva-locked PDP.

## Routes

| Surface | URL |
|---|---|
| Tooba PDP | `http://127.0.0.1:3000/fa/products/{slug}` |
| Shopeiva reference | `http://127.0.0.1:3001/product/{id}/{slug}` (original mock) |
| Host compose | `GET /v1/storefront/products/{slug}?variantId=` |

FE: `src/frontend/app/products/[slug]/page.tsx` → `storefront-pdp.tsx`  
Host: `StorefrontEndpoints.cs` + `StorefrontComposer.cs`

## Live capabilities proven (code + prior P05 PDP ACCEPT)

| Concern | Status | How |
|---|---|---|
| Gallery / media | LIVE | Catalog media refs → `GET /v1/storefront/media/{assetId}`; PDP gallery/carousel unchanged |
| Title | LIVE | Catalog localized title from compose |
| Seller / Offer presentation | LIVE | `primaryOffer` (seller party, SKU, amount, availableUnits) |
| Variant / option selector | LIVE (current model) | `variants[]` chips; `loadStorefrontDetail(slug, variantId)` — no advanced matrix (`ADVANCED_VARIANT_DEFERRED`) |
| Authoritative price | LIVE | Amount from Pricing via Offer compose — **not** Product.Price |
| Stock / availability | LIVE | `availableUnits` from Inventory (`OnHand - Reserved`); OOS disables ATC |
| Add to cart | LIVE | Posts Offer-keyed line to storefront cart |
| Description / specs | LIVE | Existing Shopeiva PDP sections |
| Reviews | LIVE | Existing review surface (T012 / T020 seller list) |
| Related sections | LIVE | Prior Shopeiva-locked slices preserved |

## Sale eligibility on PDP

Purchasable buy-box requires Published Catalog Product + Active Offer + Active tax-exclusive price + `availableUnits > 0` (see `08-storefront-sale-eligibility.md`). Seller create path (`05`–`07`) writes through Offer/Pricing/Inventory so a newly authored Offer can reach the same PDP compose as seed merchandise.

## Visual lock

CSS / JS / gallery / carousel / tabs / animation / hover / responsive: **no intentional drift** in this task. Any capture diffs belong in `20-browser-evidence.md` / `21-visual-regression-audit.md`. HOME/PDP user visual review remains `OPEN_FOR_USER_FEEDBACK` (functional live ≠ Visual ACCEPT).

## Verdict

```text
PDP_SALE_SURFACE = LIVE
VISUAL_CONTRACT = SHOPEIVA_LOCKED (unchanged)
```
