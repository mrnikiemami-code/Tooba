# Folder / ownership analysis — TB-TMAR-FE-BASELINE

Classification uses routes, imports, data owners, and behavioral ownership — not filenames alone.

| Area | Classification | Evidence |
| --- | --- | --- |
| `app/*/page.tsx` thin storefront routes (home, cart, category, PDP slug) | GOOD_BOUNDARY | Server pages compose `app/storefront/*` + SEO helpers |
| `app/storefront/*` storefront implementation | ACCEPTABLE_LEGACY | Feature code lives under app route tree; works but not `features/` |
| `app/admin/*-screen.tsx` + panels | FLAT_FEATURE_ACCUMULATION | 200+ admin sources; giants (category/product/content/landing) |
| `app/admin/admin-api.ts` | WRONG_OWNERSHIP / SHARED_DUMPING_GROUND | Cross-capability HTTP surface in one file (~1326 LOC) |
| `design-system/app-data-grid` | GOOD_BOUNDARY | Reusable grid primitive; capability-agnostic |
| `design-system` tests importing `app/admin/*` | NEEDS_DESIGN | Reverse shared→feature edges baselined |
| `lib/storefront-composition` → `app/storefront` / admin landing catalogs | ACCEPTABLE_LEGACY | Composition engine coupled to storefront/admin catalogs |
| `app/customer-panel`, `app/vendor-panel` | ACCEPTABLE_LEGACY | Panel-scoped; thinner than admin |
| `app/template-preview`, `app/evidence` | NEEDS_DESIGN | Preview/evidence routes; keep out of product ownership map |
| Checkout/cart UI | ACCEPTABLE_LEGACY | Cart page SSR shell; interactive cart client; `/checkout` redirects to `/shipping` |
| Campaigns admin (`app/admin/campaigns`) | GOOD_BOUNDARY | Newer capability folder under admin (R20 pattern) |

## Highest-debt ownership problems

1. Flat admin screen accumulation (category, product workspace, content article, landing composer).
2. Monolithic `admin-api.ts` as cross-capability client.
3. Shared lib/design-system reverse imports into admin/storefront (freeze via import guard).
4. Storefront feature modules still under `app/storefront` rather than `features/*` (defer physical move).

Do **not** move folders in this task.
