# Existing Landing audit — TB-P10-T018

- Model: StoreLandingPage + PageSection (T010–T013)
- Registry: Host `StoreLandingPageSectionRegistry.cs` + FE `landing-section-catalog.ts`
- Types: Hero, ProductCollection, CategoryGrid, BrandStrip, PromoBanner, ArticleList, Reviews, RichText, NavigationMenu
- Product sources Supported: Manual, Category, Brand, Newest; BestSelling/Featured/Discounted unsupported on Landing
- Admin: composer under `app/admin/landing-pages/` — controlled configs, no CSS
- Renderer: `storefront-landing-sections.tsx` + `landingSectionSurfaceRole`
- Draft/Published/Home selection: existing Page lifecycle preserved
- ProductCard: inherits root Appearance/skin
- Evolution: map PascalCase types → shared SectionType/Variant via `landing-adapter.ts`; keep published configs readable; no mass composer rewrite in T018
