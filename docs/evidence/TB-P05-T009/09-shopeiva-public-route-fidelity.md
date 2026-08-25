# TB-P05-T009 — Shopeiva public route fidelity

The implementation preserves the accepted Shopeiva storefront shell, header, mega-menu, footer, responsive product cards, campaign hero/grid ordering, brand directory, and seller directory/profile composition. Only live bindings and Persian copy were added.

- `/new-products`: existing merchandising hero and product-card grid; ordered by `CatalogProduct.CreatedAt`.
- `/offers`, `/sale`: same campaign shell; cards exist only when Promotion returns an applied discount.
- `/best-seller`, `/most-viewed`, `/trending`: same public shell with an explicit unavailable state; no fabricated rankings.
- `/brands`, `/brand/[slug]`: directory and landing bind published Catalog brands and composed cards.
- `/sellers`, `/seller-profile/[publicId]`: seller directory/profile bind active Offer composition and public opaque identity.
- Shared header/footer and cards remain unchanged; the mega-menu remains navigation-only.
- Unsupported thin routes are `noindex,follow`; supported routes own self-referencing canonical metadata and semantic `h1`.
- Mobile layout uses the existing responsive grids (`2` product columns and single-column directory/profile fallbacks) and contains long labels with `min-w-0`/truncate behavior.

No new card system, header, seller-profile design, brand design, frontend price calculation, or frontend availability authority was introduced.
