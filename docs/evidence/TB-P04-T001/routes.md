# TB-P04-T001 — Route inventory

Source: extracted Shopeiva App Router `src/app/**/page.jsx` (73 files). Demo variants `/index2` and `/index3` counted once as homepage demos.

| Route | Purpose | Layout | Major components | Data assumptions | Tooba relevance | Mobile | RTL | Class |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `/` | Home | Root header/footer | home sections, swipers | JSON products | High visual | carousel | rtl root | ADAPT |
| `/index2`, `/index3` | Alternate homes | same | home2/home3 | JSON | Low | carousel | rtl | DROP |
| `/categories` | Category index | root | categoriesClient | JSON categories | High | cards | rtl | ADAPT |
| `/category/[id]/[slug]` | Category listing | root | categoryDetail* | JSON | High | filters | rtl | ADAPT |
| `/category/[id]/sub/[subId]/[slug]` | Subcategory | root | same family | JSON | High | filters | rtl | ADAPT |
| `/brands`, `/brand/[id]/[slug]` | Brands | root | Brands*, BrandDetail* | JSON | Medium | cards | rtl | ADAPT |
| `/search` | Search | root | SearchClient | client Fuse + JSON | High | drawer `right-0` | rtl-biased | REBUILD |
| `/product/[id]/[name]` | PDP | root | ProductClient, JSON-LD | `products.json` price/stock | High | gallery | rtl | ADAPT/REBUILD |
| `/cart` | Cart | root | CartClient | zustand | High | stack | rtl | ADAPT |
| `/shipping` | Address/shipping | root | ShippingClient | mock | High as UX only | forms | rtl | REBUILD |
| `/payment` | Pay UI | root | PaymentClient | mock, indexable metadata | High as UX only | forms | rtl | REBUILD |
| `/sale` `/offers` `/best-seller` `/most-viewed` `/new-products` `/trending` `/coupons` | Merchandising lists | root | *Client grids | JSON | Medium | carousels | rtl | ADAPT |
| `/compare` | Compare table | root | CompareTable | JSON | Low/medium | table | rtl | DEFER |
| `/sellers`, `/seller-profile`, `/seller-profile/[id]/[slug]` | Seller storefront | root | SellerProfile* | JSON seller | High chrome | mixed | rtl | ADAPT |
| `/blogs`, `/blogs/[id]/[slug]` | Content | root | blogs* | JSON | Medium | article | rtl | ADAPT |
| `/login` `/register` `/forgot-password` | Auth | root | *Client | client authStore | High UX only | forms | rtl | REBUILD |
| `/user-panel/*` | Customer dashboard | panel | dashboard widgets | mock | Medium | dense | rtl | ADAPT |
| `(vendor)/vendor-register` | Become seller | root | register + Chart | mock | Medium | forms | rtl | REBUILD |
| `(vendor)/vendor-panel/*` | Seller ops | panel | CRUD + charts | mock | High need, weak impl | tables desktop-first | rtl | REBUILD |
| `(staticPages)/*` | About/faq/contact/legal/club | root | static | copy | Medium | readable | rtl | ADAPT |
| `/gift-card` `/premium` `/referral` `/warranty` `/site-survey` `/return-policy` | Template extras | root | marketing | mock | Pending product | mixed | rtl | DEFER/DROP |

User-panel routes: ``, wishlist, gift-cards, ticket(s), profile, settings, notifications, orders, wallet, addresses.

Vendor-panel routes: dashboard, analytics, products list/new/edit, orders list/detail, customers list/detail, reviews, coupons list/new/detail, gift-cards, wallet, tickets list/new/detail, settings.
