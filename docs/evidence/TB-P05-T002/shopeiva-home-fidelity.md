# Shopeiva Home Fidelity — TB-P05-T002

## Scope

Fast-connect of purchased Shopeiva Home chrome to live Tooba storefront data. Not a redesign.

## Section map

| Shopeiva source section | Tooba data source | Adaptation |
| --- | --- | --- |
| Promo bar | Static Shopeiva chrome | Accent blue `#2563EB`; copy kept as storefront support strip |
| Header / logo / actions | Live cart badge via `loadStorefrontCart`; search suggestions from `/v1/storefront/home` featured products | Logo asset remains Shopeiva; brand text is Tooba |
| Search | `GET /products?q=` + live Offer amounts in suggest | No Product.Price |
| Mega Menu | Live Catalog categories (flat) | Fake `/jsons/menuCategories.json` removed; **no product cards/prices inside mega**; middle columns are category hierarchy chrome filled with live category links; nested Catalog tree still missing |
| Hero / Slider | Static Shopeiva slider assets + live `heroTitle` / `heroSubtitle` overlay | CMS/slider content not owned by Catalog |
| Stories | Live category names + Shopeiva story images | Images are chrome, labels are Catalog |
| Category tiles | `StorefrontHomePage.Categories` | Tile images rotate Shopeiva category assets |
| Special Offers | Slice of live featured Offer cards (prefer in-stock) | No fake % discount |
| Sale | In-stock Offer cards | Explicit note: no fake discounts |
| Mid banners | Static Shopeiva middleBanner assets | Unchanged chrome |
| New Arrivals | Reverse slice of live featured cards | No CreatedAt rail API yet |
| Best Sellers | In-stock cards sorted by `availableUnits` desc | Temporary until sales reporting exists |
| Product rail | Offset slice of live featured cards | Same Offer card contract / PDP `/products/{slug}` |
| Brands | `StorefrontHomePage.Brands` from Catalog brands | New thin DTO field on home |
| Footer / Trust | Shopeiva footer chrome + live category links | Trust tiles remain template chrome |

## Live contracts

- `GET /v1/storefront/home` → categories, featuredProducts, brands, hero copy
- Product card money from Offer/Pricing; availability from Inventory
- PDP route reused: `/products/{slug}`

## Missing dependencies / deferred

- Real promotion/% sale engine (Promotion module not wired into Home rails)
- True bestseller ranking (orders analytics)
- CMS-owned slider/banner content
- Nested Catalog category tree for mega columns deeper than flat list
- Carry-forward from prior phases: Payment missing IdempotencyKey → 500/NRE; Cart replace-hold release→reserve window

## Evidence files

- `01-home-desktop.png`
- `02-home-mobile-390x844.png`
- `03-mega-menu.png`
- `04-special-offers.png`
- `05-sale-section.png`
- `06-new-arrivals.png`
- `07-product-rail.png`
- `08-category-section.png`
- `09-footer-trust.png`
