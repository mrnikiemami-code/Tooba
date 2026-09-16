# Admin Inventory — TB-P11-T001

Source: `src/frontend/app/admin/admin-shell.tsx` live nav + `app/admin/**/page.tsx`.

| Module | Route | Impl | Backend | DataGrid | Forms | Delete/archive | Permissions | i18n/RTL | Empty/error/loading | Tests | Production blockers | Debt |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Dashboard | `/admin` | LIVE | `/v1/admin/dashboard` | N/A cards | N/A | N/A | nav `admin.dashboard.view`; API `tenant#view` | RTL | loading ellipsis; Denied/Error | thin | coarse auth; O(n) status pull | marketplace tiles always shown |
| Orders | `/admin/orders` | LIVE | `/v1/admin/orders/query` | AppDataGrid **canonical** | detail | ops dialogs | `order.view` | RTL | loading | extensive P09/P10 | none for listing | no bulk |
| Fulfillments | `/admin/fulfillments` | LIVE | work-queue APIs | AppDataGrid + bulk | detail | ops | `fulfillment.view` | RTL | yes | work-queue tests | bulk without shared txn | — |
| Shipping services | `/admin/shipping-services` | LIVE | shipping APIs | ADG client weak filters | dialog | deactivate no confirm | `fulfillment.view` | RTL | yes | screen tests | weak grid ops | — |
| Returns | `/admin/returns` | LIVE | returns queue | ADG server | detail | ops | `return.view` | RTL | yes | queue tests | — | — |
| Stories | `/admin/stories` | LIVE | story APIs | ADG text actions | modal | review | `story.view` | RTL | yes | — | text ops | — |
| Page composition | `/admin/page-composition` | LIVE | pagecomposition | **custom list** | reorder | restore | `pagecomposition.view` | RTL | yes | — | raw sectionType | not ADG |
| Store Pages | `/admin/landing-pages` | LIVE | page APIs | ADG `orders-canonical` | composer | yes | `pagecomposition.view` | RTL | loading | R9–R13 guards | — | export off |
| Menus | `/admin/menus` | LIVE | menu APIs | **custom table** | editor | confirm | `pagecomposition.view` | RTL | yes | menu guards | no ADG; no dirty guard | — |
| Content articles | `/admin/content` | LIVE | content APIs | ADG server | editor | confirm | `content.view` | RTL | yes | content tests | — | — |
| Content categories | `/admin/content/categories` | LIVE | content APIs | tree | workspace | yes | `content.view` | RTL | yes | — | not ADG (tree OK) | — |
| Content authors | `/admin/content/authors` | LIVE | content APIs | ADG | editor | yes | `content.view` | RTL | yes | — | — | — |
| Content help | `/admin/content/help` | LIVE | static | N/A | N/A | N/A | `content.view` | RTL | n/a | — | — | — |
| Receipts/Payments | `/admin/receipts` | LIVE | `/v1/admin/payments/query` | ADG | n/a | n/a | `order.view` | RTL | yes | — | in-cell links | no dedicated payments page |
| Settlement | `/admin/settlement` | LIVE | settlement APIs | ADG client unbounded | n/a | n/a | `settlement.view` | RTL | yes | — | N+1 balances | marketplace copy |
| Payouts | `/admin/payouts` | LIVE | payout queue | ADG | process | n/a | `settlement.view` | RTL | yes | — | raw status 0/3 | GUID prefix |
| Catalog categories | `/admin/catalog/categories` | LIVE | catalog APIs | tree | workspace | yes | `product.view` | RTL | loading tree | category tests | full-tree load | not ADG |
| Attributes | `/admin/catalog/attributes` | LIVE | catalog APIs | ADG text actions | panels | disable | `catalog.attribute.view` | RTL | yes | — | GUID placeholders | — |
| Units | `/admin/catalog/units` | LIVE | catalog APIs | ADG `ops` not pinned | dialog | deactivate | `product.view` | RTL | yes | units tests | pin miss | — |
| Category schema | `/admin/catalog/category-schema` | DEFERRED nav | catalog | — | — | — | — | — | — | — | not live nav | — |
| Products | `/admin/products` | LIVE | product workspace | ADG server | workspace dirty | archive confirm | `product.view` | RTL | loading | product tests | — | strong peer |
| Product create | `/admin/products/new` | LIVE | same | N/A | create | — | `product.view` | RTL | — | — | — | — |
| Sellers | `/admin/sellers` | LIVE | `/v1/admin/sellers/query` | ADG no row ops | list only | none | `seller.view` | RTL | loading | — | no create/edit | edition always visible |
| Customers | `/admin/customers` | LIVE | customers query | ADG no row ops | list only | none | none on nav | RTL | yes | — | no detail | — |
| Reviews | `/admin/reviews` | LIVE | reviews | ADG text ops | inline | no confirm | `review.view` | RTL | yes | — | weak filters | — |
| Tickets | `/admin/tickets` | LIVE | support | **custom cards** | detail | — | `support.view` | RTL | page-of-8 | — | not ADG | GUID search |
| Gift cards | `/admin/gift-cards` | LIVE | wallet | ADG client | issue/revoke | revoke confirm | `giftcard.view` | RTL | yes | — | CardId GUID | — |
| Wallets | `/admin/wallets` | LIVE | wallet | inspect form | adjust | — | `wallet.view` | RTL | — | — | GUID actor input | not listing |
| Promotions | `/admin/promotions` | LIVE | promotion | ADG weak | deactivate | no confirm | `promotion.view` | RTL | yes | — | seller GUID | no create |
| Settings | `/admin/settings` | LIVE | settings APIs | N/A | tabs | — | none on nav | RTL | loading | settings tests | mixed EN | no global dirty |
| Languages | `/admin/languages` | LIVE | localization | ADG thin | edit | — | none on nav | RTL | yes | — | hardcoded locale lists elsewhere | — |
| Access control | `/admin/access-control` | LIVE | `/v1/admin/access-control/*` | custom matrix | roles/users | archive confirm | `accesscontrol.view` fail-open | RTL | loading | ACC tests | fail-open manage | no audit UI |
| Appearance | `/admin/settings` tab ظاهر فروشگاه | LIVE | appearance APIs | N/A | palette form | — | settings | RTL | yes | appearance tests | html attrs residual | — |

Absent dedicated Admin modules: Brands CRUD, Inventory module, Notifications, Banners CRUD, Store switcher, Store create.

Seller ACC subpage: `/admin/sellers/[sellerId]/access-control`.
