# Usage rule

Do NOT read this file automatically for every task.

Consult it when a task introduces a new capability, changes ownership/schema, or needs to discover whether a capability already exists.

Update only the affected sections when architectural ownership/capability changes.

Evidence files remain the deep-detail source; this map is the quick index.

## Commerce
| Capability | Owner | Entity/Service | Module | Scope | Reuse | Verified |
|---|---|---|---|---|---|---|
| Sellable listing | Offer | SellerOffer | Offer | Tenant | Canonical listing identity | R14–R19 |
| Authored selling price | Pricing | AuthoredPrice / PriceDirectory | Pricing | Tenant | Single pricing engine | R18–R19 |
| Price dimensions | Pricing | Market, Channel, Currency, Valid window, QualifierKind/Key | Pricing | Tenant | Campaign uses QualifierKind=MerchandisingCampaign Key=CampaignId | R18–R19 |
| Bulk price resolve | Pricing | IPriceLookupGateway.ResolvePricesBatchAsync + ResolveCampaignPricesBatchAsync | Pricing | Tenant | Showcase/campaign | R18–R19 |
| Campaign cart price authority | Pricing↔Promotion | ICampaignCartPriceAuthority / CampaignCartPriceAuthority | Promotion+Pricing | Store | Validates Store/active/member then campaign AuthoredPrice | R19 |
| Stock | Inventory | StockPosition | Inventory | Tenant | Canonical stock | R14–R17 |
| Checkout promotions | Promotion | PromotionDefinition / IPromotionEvaluator | Promotion | Tenant | NOT merchandising rails | R14–R18 |
| Merchandising type | Promotion | MerchandisingPromotionType (AMAZING) | Promotion | Shared codes | Seeded type | R16–R17 |
| Merchandising campaign | Promotion | MerchandisingCampaign + Translation + CampaignOffer | Promotion | StoreId | Runtime active/future resolvers | R16–R19 |
| Cart campaign context | Cart | CartLine.MerchandisingCampaignId (nullable) | Cart | Line | Context only; never client amount | R19 |
| Cart reprice | Cart | CartDirectory.ValidateOfferAndQuoteAsync + RevalidateCampaignQuotesAsync | Cart | Active cart | Campaign then Base fallback | R19 |
| Checkout revalidation | Order | CheckoutDirectory.ResolveCheckoutLineQuoteAsync | Order | Commit | PRICE_CHANGED if quote drifts | R19 |
| Order price snapshot | Order | OrderLine UnitPriceSnapshot + PriceId | Order | Immutable | Not live campaign tables | R19 |
| Product Showcase sources | Catalog/Host | Manual, Category, Brand, Newest, PromotionCampaign | Host/Builder | Page | Source intent only | R17 |
| Storefront ATC campaign hint | Host/FE | StorefrontAddCartLineRequest.MerchandisingCampaignId | Host | Request | Hint only; server validates | R19 |
| Page locale | Catalog | StoreLandingPage.Locale | Catalog | Page | Campaign translation fallback | R17 |
| Store scope | BuildingBlocks | Tenant / store-alpha → StoreAlphaId | Host | Marketplace+SingleStore | Shared model | R16–R19 |

## Deferred after R19
- Admin Campaign authoring UI
- Explicit campaignId Builder picker
- P11 scope

Last Verified Task = TB-P10-T022-R19
