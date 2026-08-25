# TB-P05-T009 — Public route data map

| Public route | HTTP composition | Truth source | Honest limitation |
| --- | --- | --- | --- |
| `/new-products` | `GET /v1/storefront/merchandising/new-products` | published Catalog product `CreatedAt` + active Offer + Pricing + Inventory | Products without a composable active offer/price are absent |
| `/offers`, `/sale` | `GET /v1/storefront/merchandising/{kind}` | Promotion evaluation on backend-composed offer amount | Empty when no applicable automatic promotion exists; no countdown/% fabrication |
| `/best-seller` | merchandising endpoint | none currently accepted | `supported=false`, empty products, `noindex` |
| `/most-viewed` | merchandising endpoint | none currently accepted | `supported=false`, empty products, `noindex` |
| `/trending` | merchandising endpoint | none currently accepted | `supported=false`, empty products, `noindex` |
| `/brands` | `GET /v1/storefront/brands` | published Catalog brands and published-product counts | No invented marketing copy |
| `/brand/[slug]` | `GET /v1/storefront/brands/{slug}` | Catalog brand/product relation + composed cards | Only active composable offers appear |
| `/sellers` | `GET /v1/storefront/sellers` | active composed Offers grouped by seller Party | Party ID replaced by deterministic one-way public ID |
| `/seller-profile/[publicId]` | `GET /v1/storefront/sellers/{publicId}` | Party display name + active Offer/product cards | No legal/contact/auth/settlement/private fields |

Product-card ownership remains: identity and chronology from Catalog, listing/seller from Offer and Party lookup, amount from Pricing/Promotion, and availability from Inventory. Each module is queried through its own context/gateway; no cross-module SQL join or direct frontend database access exists.
