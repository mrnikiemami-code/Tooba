# Tooba — Shopeiva Study & Reuse Map

Status:

```text
Analysis only — not UI ACCEPT, not Design System ACCEPT
```

Task:

```text
TB-P04-T001
```

Shopeiva is UI/reference/reuse input. It is not domain, security, tenant, SEO, commerce, or authorization truth. Backend/module boundary is not the UI boundary.

## Executive assessment

Shopeiva is a Persian RTL Next.js App Router storefront plus vendor and customer panels, built on static JSON and client Zustand stores. Visual density, cards, skeletons, and RTL-first header/footer are the strongest reuse inputs. Commerce semantics (price, stock, seller, tax, promotion, payment) are collapsed into mock product records and must not be copied.

Recommended posture: extract design language and layout patterns later; rebuild commerce surfaces against Tooba contracts; do not vendor the template into `src/frontend`.

## Template source/version

```text
Location: sibling workspace reference tree (not inside the Tooba git tree)
Observed package.json: next 16.2.6, react/react-dom 19.2.4, tailwindcss 4
App Router pages: 73 page.jsx files
Components under src/components: 323 files
node_modules: not installed in the inspected reference tree
```

The archive/zip copies exist alongside an extracted `reference/shopeiva` tree. Absolute machine paths are not architecture truth and are not recorded here.

Tooba packages were not upgraded. Purchased source was not committed.

## Route inventory summary

73 App Router `page.jsx` files. Demo home variants `/index2` and `/index3` are duplicates of homepage composition.

Storefront: `/`, listing families (categories, category, brands, brand, search, sale, offers, best-seller, most-viewed, new-products, trending), PDP `/product/[id]/[name]`, cart, shipping, payment (not a Tooba checkout), compare, coupons, gift-card, premium, referral, sellers, seller-profile, blogs, static legal/content, auth (login/register/forgot-password), warranty, site-survey.

Customer: `/user-panel` plus profile, addresses, orders, wishlist, wallet, gift-cards, notifications, tickets, settings.

Vendor: `/vendor-register` plus `/vendor-panel` dashboard, analytics, products CRUD, orders, customers, reviews, coupons, gift-cards, wallet, tickets, settings.

Evidence: `docs/evidence/TB-P04-T001/routes.md`.

## Component taxonomy

Primitives: buttons/badges mostly Tailwind utility classes, not a tokenized kit.

Navigation: `Header`, `Footer`/`DynamicFooter`, breadcrumbs helper.

Commerce cards / media / price: product grids, Swiper carousels, gallery on PDP.

Filters / forms / tables: listing filters; react-hook-form + zod on some forms; three `<table>` usages (compare, best-seller breakdown, vendor dashboard).

Feedback: react-toastify, many skeleton folders, empty cart.

Dialogs: story modal, mobile search drawer (`right-0` sheet).

Account / dashboard / charts: user-panel and vendor-panel; Chart.js on vendor register/analytics.

Content: blogs, magazine, static pages.

Utilities: Zustand stores (`cartStore`, `authStore`), Fuse.js search, axios.

## REUSE / ADAPT / REBUILD / DROP / DEFER map

See the decision matrix below and `docs/evidence/TB-P04-T001/reuse-matrix.md`.

Headline:

- REUSE: skeleton vocabulary, RTL layout shell ideas, local IRANSans loading pattern, some card/hero visual rhythm.
- ADAPT: Header/Footer, listing/search UX, PDP gallery chrome, cart layout, seller public profile chrome, blog layout, theme/dark utilities.
- REBUILD: checkout/payment, cart state, product workspace, admin/seller operations, Data Grid, offer/price/tax/inventory presentation, auth.
- DROP: template demo JSON as product data; collapsed `product.price`/`product.stock`; hardcoded Shopiva SEO host; `/index2`/`/index3`; fake auth interval polling.
- DEFER: wallet, gift card, referral, premium, site-survey, vendor capabilities pending product confirmation.

## Dependency KEEP / REPLACE / REMOVE / DEFER map

| Dependency | Class | Why |
| --- | --- | --- |
| next / react / react-dom | REPLACE | Template is Next 16; Tooba frontend is Next 15. Do not jump Tooba in this task. |
| tailwindcss 4 | DEFER | Candidate for later Design System work; Tooba currently Tailwind 3. |
| lucide-react | KEEP | Icon set quality; RTL-safe if directional icons are adapted. |
| next-themes | KEEP | Dark mode primitive is usable. |
| react-hook-form | KEEP | Form foundation. |
| zod | KEEP | Validation; major version may differ from Tooba later. |
| @hookform/resolvers | KEEP | Pairs with RHF/zod. |
| zustand | DEFER | Fine for client UI state later; not a commerce source of truth. |
| framer-motion | DEFER | Motion optional; respect reduced-motion. |
| swiper | REPLACE | Heavy carousels; prefer lighter/accessible Tooba media later. |
| fuse.js | REPLACE | Client fuzzy search over JSON; Tooba search is PostgreSQL FTS then later OpenSearch. |
| axios | REPLACE | Prefer fetch + Tooba API client; extra bundle. |
| chart.js / react-chartjs-2 | DEFER | Analytics charts later; not operations grid. |
| react-toastify | REPLACE | Notifications should match Design System; a11y mixed. |
| react-loading-skeleton | ADAPT as pattern | Keep skeleton *idea*; implementation can be local. |
| react-otp-input | DEFER | OTP UI later; Identity already has OTP semantics. |
| react-paginate | REPLACE | Too weak vs mandatory Data Grid / listing pagination. |
| persian-date / persian-datepicker | REPLACE | Date UX needed, but these packages are maintenance/a11y risks; rebuild later. |

Do not add these to Tooba now.

## Design token findings

Raw Shopeiva:

- Brand: `--color-theme: #E53935`
- Surfaces: white / zinc-950 dark
- Font: IRANSansXNoEn local faces (weight mapping in `layout.jsx` is inconsistent: woff listed as 700)
- Container: `max-w-[1800px]`, padding `px-4 sm:px-6`
- Radius/shadow: ad-hoc Tailwind (`shadow-2xl`, rounded utilities)
- Motion: Swiper autoplay 3.5–5s; fake 400ms cart skeleton timeout
- z-index: drawer overlays, sticky `lg:top-24` summary

Candidate Tooba tokens (not declared final):

- color.brand, color.surface, color.fg
- space.page-x, width.shell
- font.sans (Persian-capable)
- radius.md, shadow.overlay
- z.header, z.drawer

## RTL/LTR findings

Root is `lang="fa" dir="rtl"`. Many Swipers hard-code `dir="rtl"`. Phone/email/order-code use `dir="ltr"` islands. Mobile filter drawer is `right-0` (RTL-native, LTR-unsafe). CSS `dir` alone is not proof of bilingual production readiness. Pagination, tables, and carousels need a later bidirectional audit.

## Mobile findings

Listing and home carousels use `slidesPerView: auto` / `2.2` — mobile-first-ish. Cart stacks `grid-cols-1 lg:grid-cols-3` with sticky summary on large screens only. Vendor tables are desktop-first (`text-[10px] md:text-xs`) and will fail as professional operations UX. Header is a dense mega-nav risk on small screens (must be studied again at Design System extraction). Desktop-first vendor CRUD would produce weak mobile seller UX if copied.

## Commerce fit

| Tooba concept | Shopeiva representation | Fit |
| --- | --- | --- |
| Catalog Product | JSON product with name/brand/category | Visual PDP/listing only |
| Variant | Not a first-class model | Missing |
| Seller Offer | Single product record; seller profile is marketing | Missing multi-offer |
| Multiple sellers on one variant | Absent | Must rebuild |
| Pricing | `product.price` on catalog-like JSON | Forbidden as architecture |
| Promotion | Discount percent from old vs new cart totals in Zustand | Must rebuild vs Promotion module |
| Tax | Absent on cart | Must rebuild |
| Inventory | `product.stock` on product JSON | Forbidden |
| Cart | Zustand `cartStore`, lines are products | ADAPT layout, REBUILD state |
| RequestToReserve vs OnlinePurchase | Absent | Must rebuild |
| Seller-scoped order lifecycle | Vendor orders list is demo CRUD | Rebuild |
| Payment | `/payment` client page; not PSP verification | Rebuild on Tooba Payment |

Hard rule: do not emit one CRUD screen per backend module.

## Admin / Seller / Customer fit

Customer panel is a conventional dashboard of demo widgets — insufficient as polished customer workspace.

Vendor panel is basic CRUD + Chart.js — not professional seller operations (no typed filters, saved views, bulk, export, keyboard grid).

No true marketplace Admin operations surface.

Missing patterns: product workspace composing catalog+offer+price+tax+inventory+SEO; bulk tools; audit history; reservation vs purchase modes.

## Data Grid readiness

Classification: **REBUILD**.

Evidence: only three HTML tables; no column resize/reorder/hide, no saved views, no bulk actions, no export, no sticky columns, no keyboard grid, no server-side dataset contract. `react-paginate` is listing pagination, not an operations grid.

Do not implement the Data Grid in this task.

## SEO / rendering risks

Some routes export `metadata` / `generateMetadata` and Product JSON-LD. PDP still hydrates a large `ProductClient`. Cart/payment/auth are client-heavy. `images.remotePatterns hostname: '**'` is overly open. Hardcoded canonical hosts (`shopeiva.ir`, demo metadataBase) must not be copied. Fake loading timers harm LCP. Shopeiva does not override Tooba SEO architecture.

Do not copy client-only PDP/cart as the Tooba default.

## Accessibility findings

Source-level, not WCAG certification:

- Toast + Swiper autoplay without clear reduced-motion
- Drawer likely missing focus trap/restore (pattern risk)
- Tables without established grid keyboard model
- Icon-only controls in header/cart need label audit
- Contrast of red brand on dark is plausible but unverified
- Touch targets on `text-[10px]` tables fail

## Recommended extraction order

Locked P04 sequence (no extra task IDs invented):

1. This study (done as analysis)
2. Design System extraction (tokens + primitives) — next authorized envelope only
3. Professional Data Grid
4. Workspace interaction patterns
5. Serious UI implementation
6. Visual evidence / Architect visual ACCEPT

## Known gaps

- No Offer/Price/Tax/Inventory separation in UI
- No RequestToReserve
- No durable auth (client store + interval `checkAuth`)
- No professional grid
- Demo JSON and public images are not Tooba content
- LTR/bilingual not designed
- Template Next 16 vs Tooba Next 15

## Required final reuse map

| Area | Shopeiva asset | Classification | Why | Required Tooba adaptation | Future task dependency |
| --- | --- | --- | --- | --- | --- |
| Storefront shell | `layout.jsx` + max-width shell | ADAPT | RTL column shell is useful | Drop hardcoded mega-host metadata; Server Component First | Design System |
| Header | `components/common/Header` | ADAPT | Dense Persian nav | Rebuild search/auth/cart badges on Tooba APIs; LTR variant | Design System, then UI |
| Search | `SearchClient` + Fuse.js | REBUILD | Client JSON search | PostgreSQL FTS / later OpenSearch; accessible overlay | Serious UI |
| Category listing | category pages + filters | ADAPT | Filter chrome reusable | Bind Category descriptive model, not purchasability | Serious UI |
| Product card | grids/carousels | ADAPT | Visual rhythm | Show Offer price/availability, not Product.Price/Stock | Serious UI |
| PDP gallery | `ProductClient` + JSON-LD page | ADAPT chrome / REBUILD data | Gallery/SEO hints useful | Variant + multi-offer selector; tax-exclusive price | Serious UI |
| Offer selector | none | REBUILD | Missing | Multi-seller offer list | Serious UI |
| Cart | `CartClient` layout | ADAPT | Item/summary split | Cart lines = OfferId; quote non-authoritative | Serious UI |
| Checkout | shipping + payment pages | REBUILD | Not Tooba checkout | Pricing→Promotion→Tax→snapshot; modes | Serious UI |
| Account shell | `user-panel/*` | ADAPT | IA of orders/addresses | Replace wallet/gift unless product confirms | Workspace patterns |
| Admin shell | absent | REBUILD | No admin product | Compose workspaces, not module CRUD | Workspace patterns |
| Seller shell | `vendor-panel/*` | REBUILD | Demo CRUD | Seller-scoped orders/offers/inventory | Workspace patterns |
| Tables/Grid | 3 HTML tables | REBUILD | Misses mandatory grid | Professional Data Grid later | Data Grid |
| Forms | RHF + zod samples | ADAPT | Pattern OK | Persian a11y, Tooba validation errors | Design System |
| Modal/Drawer | story modal, search sheet | ADAPT | Useful patterns | Focus trap, LTR, a11y | Design System |
| Theme | next-themes + dark utilities | ADAPT | Dark works | Tokenize; do not copy red as final brand | Design System |
| Typography | IRANSans localFont | ADAPT | Persian-ready | Fix weight mapping; licensed font pipeline | Design System |
| Charts | Chart.js vendor analytics | DEFER | Decorative | First-party analytics later | later envelope |
| Notifications | react-toastify | REPLACE | Not design-system | Tooba toast/live region | Design System |
| Skeletons | many `skeleton/*` | REUSE | Strong loading language | Tokenize sizes; no fake timeouts | Design System |

## No visual acceptance yet

This task does not claim Tooba UI, Design System, storefront, admin, seller portal, or customer dashboard accepted.
