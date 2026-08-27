# 21 — Visual regression audit (TB-P06-T021)

Scope: every **touched** route in the sellable product flow. Unauthorized Shopeiva drift must be repaired before PASS.

## Touched routes

| Route | Touch type | Audit |
|---|---|---|
| `/vendor-panel/products` | Extended (create CTA, live offer list) | Native-fit to Shopeiva ProductsList; accent `#E53935` |
| `/vendor-panel/products/new` | New (Offer + price + stock create) | Mapped from Shopeiva ProductForm commerce section — **not** generic admin form; no advanced variant matrix UI |
| `/vendor-panel/products/[offerId]` | Extended (price/inventory writable) | Preserve Catalog RO block; commerce fields live |
| `/fa/products`, `/fa/products/[slug]` | Unchanged presentation | Shopeiva-locked; no redesign this task |
| `/fa/cart`, `/fa/checkout`, `/fa/payment/*` | Unchanged presentation | Prior P05 fidelity; coupon path from T020 |
| `/vendor-panel/orders*`, `/fulfillments*` | Unchanged | Prior fulfillment UI |
| `/customer-panel/orders*` | Unchanged | Prior customer order UI |
| `/admin/products*`, `/admin/orders*`, `/admin/fulfillments*` | Catalog create HTTP may be used; UI workspace preserved | No mega-screen |

## Checklist (per touched surface)

| Concern | Vendor products* | Public storefront | Panels orders |
|---|---|---|---|
| CSS | No unauthorized restyle | Locked | Locked |
| JS / interaction | Create/save live APIs only | Unchanged | Unchanged |
| Carousel / gallery | N/A (panel) | Locked on PDP | N/A |
| Animation / transition / hover / focus | Match Shopeiva vendor patterns | Locked | Existing |
| Spacing / typography | Source-derived | Locked | Existing |
| Responsive | List/form wrap | Desktop+mobile | Existing |
| Tabs / modal / buttons / badges | List CTA + form actions | Locked | Existing |
| Card / table geometry | DataGrid/list native-fit | N/A | Existing |

## Drift disposition

| Finding | Action |
|---|---|
| None intentional for Catalog/Offer architecture (Admin Product + Seller Offer ≠ Shopeiva single Product.Price model) | Documented in `09` — presentation mapped; domain separation preserved |
| Advanced variant matrix absent | **`ADVANCED_VARIANT_DEFERRED`** — not a visual defect for this task |
| HOME/PDP user visual review | Still `OPEN_FOR_USER_FEEDBACK` — not claimed closed |

## Verdict

```text
VISUAL_CONTRACT = SHOPEIVA_LOCKED
UNAUTHORIZED_DRIFT = NONE_INTENDED
USER_VISUAL_ACCEPTED = NOT_CLAIMED
```
