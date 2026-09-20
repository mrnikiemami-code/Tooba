# Usage rule

Do NOT read this file automatically for every task.

Consult it when a task introduces a new capability, changes ownership/schema, or needs to discover whether a capability already exists.

Update only the affected sections when architectural ownership/capability changes.

Evidence files remain the deep-detail source; this map is the quick index.

## Commerce
| Capability | Owner | Entity/Service | Module | Scope | Reuse | Verified |
|---|---|---|---|---|---|---|
| Sellable listing | Offer | SellerOffer | Offer | Tenant | Canonical listing identity | R14–R20 |
| Authored selling price | Pricing | AuthoredPrice / PriceDirectory | Pricing | Tenant | Single pricing engine | R18–R20 |
| Campaign cart price authority | Pricing↔Promotion | ICampaignCartPriceAuthority | Promotion+Pricing | Store | Cart/Checkout | R19 |
| Merchandising campaign | Promotion | MerchandisingCampaign + CampaignOffer | Promotion | StoreId | Runtime resolvers | R16–R20 |
| Admin campaign workspace | Host/FE | /admin/campaigns + /v1/admin/merchandising-campaigns | Host | Store | List/create/edit/publish/archive/members/prices | R20 |
| Campaign membership authoring | Promotion+Host | AddOffer/Reorder + offer-candidates | Host | Store | SellerOffer selector | R20 |
| Campaign price authoring | Pricing+Host | CreateCampaignPriceAsync / ChangeAmountAsync | Pricing | Offer+Campaign | No parallel store | R20 |
| Cart campaign context | Cart | CartLine.MerchandisingCampaignId | Cart | Line | R19 |
| Product Showcase sources | Catalog/Host | PromotionCampaign | Host/Builder | Page | CampaignId=null dynamic | R17+R20 |

## Deferred
- Admin Campaign restore-from-archive
- Explicit Builder campaignId picker
- P11 Access Control repair

## TMAR / Architecture Recovery
| Item | Fact | Verified |
|---|---|---|
| Strengths | Per-module schema/DbContext/migrations; no cross-schema FK/JOIN; ArchitectureBoundaryTests; Directory/Gateway write seams; Outbox/events; ICache foundation | TB-TMAR-ARCH-BASELINE |
| Debt | Host DbContext writes/decisions; Contracts live in Application; Catalog convenience storefront/settings/template types; IMemoryCache bypasses; Domain localized errors; legacy Directories | TB-TMAR-ARCH-BASELINE / FND-001 baselined |
| Target | Modular Monolith now → low-friction microservices; CQRS + MediatR 12.5.0; per-module Contracts; Host transport-only; strangler not Big Bang | TB-TMAR-ARCH-BASELINE |
| Foundation | MediatR 12.5.0 + FluentValidation pipeline; IClock; IIdGenerator; SemanticError; freeze guards (Host write / App→App / IMemoryCache) | TB-TMAR-FND-001 |
| Host Wave 1 Slice 1 | Store Landing/Page Composition writes behind CQRS; handlers live in Catalog.Application; Infrastructure owns Directory/DbContext/transactions; Host write baseline shrunk | TB-TMAR-HOST-W1-R1 |
| Next | Continue Host dangerous-write removal (next slice from audit) after HOST-W1 layering repair accepted | TB-TMAR-HOST-W1-R1 |

Last Verified Task = TB-TMAR-HOST-W1-R1
