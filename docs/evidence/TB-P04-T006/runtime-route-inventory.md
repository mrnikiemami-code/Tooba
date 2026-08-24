# TB-P04-T006 — Shopeiva runtime route inventory

Resolved runtime (evidence only, not production config):

```text
D:\Users\User\source\repos\SarvNewVerRequirment\reference\shopeiva
```

Package: npm (`package-lock.json`). Next.js 16.2.6 / React 19.2.4 / Tailwind 4. Dev: `npx next dev -p 3013`. URL: `http://localhost:3013`.

Local atlas-only patches (not copied into Tooba): `checkAuth` bypass in `src/store/authStore.js` because login API is stubbed (`response = false`); root `src/app/loading.jsx` returns null so home skeleton does not cover vendor/customer panels.

Distinct Admin app: **NO**. Vendor panel is the operations-like surface.

Viewport used for desktop captures: CSS `1440×900`. Mobile target: `390×844`.

Decision vocabulary: REUSE / ADAPT / REBUILD / DROP / DEFER.

## Storefront (code + runtime)

| Route | Kind | Runtime | Desktop | Mobile | Dark | LTR | Pattern | Tooba target | Decision |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `/` | static | 200 | A01 | yes | theme toggle | html dir | home v1 slider/stories/cats | Storefront home | ADAPT |
| `/index2` | static | 200 | A02 | yes | yes | yes | home composition v2 | Storefront home variant | ADAPT |
| `/index3` | static | 200 | capture | yes | yes | yes | home composition v3 | Storefront home variant | ADAPT |
| `/categories` | static | 200 | A04 | yes | yes | yes | category mosaic | Catalog browse | ADAPT |
| `/category/[id]/[slug]` | dynamic | 200 | listing | yes | yes | yes | PLP + filters | Catalog listing | ADAPT |
| `/category/[id]/sub/[subId]/[slug]` | dynamic | 200 | listing | yes | yes | yes | nested PLP | Catalog listing | ADAPT |
| `/search` | static | 200 (Next 16 `searchParams` warning) | A05 | yes | yes | yes | search results | Search | ADAPT |
| `/product/[id]/[name]` | dynamic | 200 | A06 | yes | yes | yes | PDP gallery/buy box | PDP | ADAPT |
| `/sellers` | static | 200 (sparse mock) | A07 | yes | yes | yes | seller grid | Seller directory | ADAPT |
| `/seller-profile` `/seller-profile/[id]/[slug]` | mixed | 200 | profile | yes | yes | yes | seller storefront | Seller public page | ADAPT |
| `/cart` | static | 200 | A08 | yes | yes | yes | cart | Cart | ADAPT |
| `/shipping` | static | 200 | A09 | yes | yes | yes | checkout step | Checkout | ADAPT |
| `/payment` | static | 200 (empty cart → footer-heavy) | A10 | yes | yes | yes | payment step | Checkout payment | ADAPT |
| `/sale` `/offers` `/coupons` `/premium` `/trending` `/new-products` `/most-viewed` `/best-seller` | static | 200 | A11 family | yes | yes | yes | campaign merchandising | Promotions/content | ADAPT |
| `/blogs` `/blogs/[id]/[slug]` | mixed | 200 | A12 | yes | yes | yes | content | CMS | DEFER |
| `/brands` `/brand/[id]/[slug]` | mixed | 200 | listing | yes | yes | yes | brand PLP | Catalog brand | ADAPT |
| `/login` `/register` `/forgot-password` | static | 200 | auth | yes | yes | yes | auth cards | Auth UI | ADAPT |
| `/compare` `/gift-card` `/referral` `/warranty` `/return-policy` `/site-survey` | static | 200 | marketing | low | yes | yes | policy/marketing | Content | DEFER |
| `/club` `/about` `/contact` `/faq` `/privacy` `/rules` | static | 200 | A13 family / footer | yes | yes | yes | trust/footer | Storefront chrome | REUSE |
| `/vendor-register` | static | 200 | form | yes | yes | yes | seller onboarding | Seller apply | ADAPT |

## Vendor panel (no separate Admin)

| Route | Kind | Runtime | Pattern | Tooba target | Decision |
| --- | --- | --- | --- | --- | --- |
| `/vendor-panel` | static | 200 after auth bypass | dashboard KPIs/charts | Admin + Seller dashboards | ADAPT |
| `/vendor-panel/products` | static | 200 | product table/toolbar | Admin/Seller catalog | ADAPT + Tooba Data Grid |
| `/vendor-panel/products/new` `[id]/edit` | mixed | 200 | product form | Product workspace | ADAPT |
| `/vendor-panel/orders` `[id]` | mixed | 200 | order list/detail | Orders | ADAPT + Data Grid |
| `/vendor-panel/customers` `[id]` | mixed | 200 | customer list/detail | Customers | ADAPT + Data Grid |
| `/vendor-panel/analytics` | static | 200 HTML; client shell sometimes missing | charts | Analytics | MEDIUM_ADAPT / inspect charts |
| `/vendor-panel/coupons` `new` `[id]` | mixed | 200 | promo forms | Promotions | ADAPT |
| `/vendor-panel/reviews` | static | 200 | review moderation | Reviews | ADAPT + Data Grid |
| `/vendor-panel/wallet` | static | 200 | wallet | Payments/payout | ADAPT |
| `/vendor-panel/tickets` `new` `[id]` | mixed | 200 | support | Support | ADAPT |
| `/vendor-panel/gift-cards` | static | 200 | gift ops | Promotions | DEFER |
| `/vendor-panel/settings` | static | 200 | settings | Settings | ADAPT |

## Customer panel

| Route | Kind | Runtime | Pattern | Tooba target | Decision |
| --- | --- | --- | --- | --- | --- |
| `/user-panel` | static | 200 after auth bypass | account dashboard | Customer account | ADAPT |
| `/user-panel/orders` | static | 200 | order history | Customer orders | ADAPT |
| `/user-panel/wishlist` | static | 200 | wishlist | Wishlist | ADAPT |
| `/user-panel/wallet` | static | 200 | wallet | Customer wallet | ADAPT |
| `/user-panel/tickets` `new` `/ticket/[id]` | mixed | 200 | support | Customer support | ADAPT |
| `/user-panel/gift-cards` | static | 200 | gift cards | Promotions | DEFER |
| `/user-panel/addresses` | static | 200 | address book | Checkout addresses | ADAPT |
| `/user-panel/notifications` | static | 200 | inbox | Notifications | ADAPT |
| `/user-panel/profile` | static | 200 | profile | Identity profile | ADAPT |
| `/user-panel/settings` | static | 200 | prefs | Account settings | ADAPT |

## Admin

No `/admin` tree. Do not invent one. Adapt vendor-panel chrome + Tooba Data Grid for Tooba Admin.

## Counts

- `page.jsx` files under `src/app`: **73**
- Distinct Admin: **false**
- Template defects: missing `sizes` on fill images; some `/images/stories/*` 404; empty-cart payment is footer-dominant; login API stubbed; root `loading.jsx` is home skeleton (covers other routes until patched locally).
