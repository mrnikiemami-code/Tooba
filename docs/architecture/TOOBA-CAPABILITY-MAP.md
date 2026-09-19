# Usage rule

Do NOT read this file automatically for every task.

Consult it when a task introduces a new capability, changes ownership/schema, or needs to discover whether a capability already exists.

Update only the affected sections when architectural ownership/capability changes.

Evidence files remain the deep-detail source; this map is the quick index.

## Commerce
| Capability | Owner | Entity/Service | Module | Scope | Reuse | Verified |
|---|---|---|---|---|---|---|
| Sellable listing | Offer | SellerOffer | Offer | Tenant | Canonical listing identity | R14–R18 |
| Authored selling price | Pricing | AuthoredPrice / PriceDirectory | Pricing | Tenant | Single pricing engine | R18 |
| Price dimensions | Pricing | Market, Channel, Currency, Valid window, QualifierKind/Key | Pricing | Tenant | Campaign uses QualifierKind=MerchandisingCampaign Key=CampaignId | R18 |
| Bulk price resolve | Pricing | IPriceLookupGateway.ResolvePricesBatchAsync + ResolveCampaignPricesBatchAsync | Pricing | Tenant | Showcase/campaign | R18 |
| Stock | Inventory | StockPosition | Inventory | Tenant | Canonical stock | R14–R17 |
| Checkout promotions | Promotion | PromotionDefinition / IPromotionEvaluator | Promotion | Tenant | NOT merchandising rails | R14–R18 |
| Merchandising type | Promotion | MerchandisingPromotionType (AMAZING) | Promotion | Shared codes | Seeded type | R16–R17 |
| Merchandising campaign | Promotion | MerchandisingCampaign + Translation + CampaignOffer | Promotion | StoreId | Runtime active/future resolvers | R16–R18 |
| Product Showcase sources | Catalog/Host | Manual, Category, Brand, Newest, PromotionCampaign | Host/Builder | Page | Source intent only | R17 |
| Page locale | Catalog | StoreLandingPage.Locale | Catalog | Page | Campaign translation fallback | R17 |
| Store scope | BuildingBlocks | Tenant / store-alpha → StoreAlphaId | Host | Marketplace+SingleStore | Shared model | R16–R18 |

## Deferred after R18
- Cart/Checkout campaign price context
- Admin Campaign authoring UI
- Explicit campaignId Builder picker
- P11 scope
