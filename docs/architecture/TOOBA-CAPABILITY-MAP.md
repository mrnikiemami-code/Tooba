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
| Host Wave 1 Slice 1 | Store Landing CQRS; handlers in Application; Infrastructure Directory/EF | TB-TMAR-HOST-W1-R1 |
| Host Wave 2 | Store Menu Host writes behind CQRS; baseline shrunk | TB-TMAR-HOST-W2 |
| Boundary V1 | Independent review claims verified: Domain→Offer (3) + Payment.Infra→Wallet.Domain CONFIRMED; Order.Application hub mapped; Domain/Infra foreign Domain edges frozen with exact baselines | TB-TMAR-BOUNDARY-V1 |
| Boundary V1-R1 | God-file/source-size freeze + Infra→foreign Application freeze; ARCH-SIZE-001/002 + ARCH-REFACTOR-001; top-10 decomposition queue | TB-TMAR-BOUNDARY-V1-R1 |
| Contracts W1 | Offer.Contracts (SalesChannel); Wallet.Contracts (WalletCurrency); Domain→Offer Domain refs removed; Payment→Wallet.Domain removed; Domain/Infra foreign Domain baselines empty | TB-TMAR-CONTRACTS-W1 |
| Contracts W2 | Wallet order-payment port; Offer lookup gateway in Contracts; Infra/App baselines shrunk; ARCH-TX-001 + TransactionScope freeze; migration notes imported | TB-TMAR-CONTRACTS-W2 |
| Contracts W3 | Returns.Infra→Wallet.Contracts (IWalletRefundCreditPort); Order.App→Offer.Contracts (SalesChannel); Infra/App baselines shrunk | TB-TMAR-CONTRACTS-W3 |
| Contracts W4 | Cart.App→Offer.Contracts; Tax.Contracts (ITaxCalculator/TaxOutcome); Order.App→Tax.Contracts; App→App 14→12 | TB-TMAR-CONTRACTS-W4 |
| Contracts W5 | Inventory.App→Offer.Contracts; Pricing.Contracts (IPriceLookupGateway); Order.App→Pricing.Contracts; App→App 12→10 | TB-TMAR-CONTRACTS-W5 |
| Contracts W6 | Promotion.App→Offer.Contracts; Cart.App→Pricing.Contracts (+ campaign authority/CurrencyCode/ReturnPolicy in Contracts); App→App 10→8; FE READY | TB-TMAR-CONTRACTS-W6 |
| Frontend baseline | Canonical root `src/frontend`; FE-ARCH/SIZE/SEO/BOUNDARY locks; size+import+SEO guards; flat admin debt documented; no broad refactor | TB-TMAR-FE-BASELINE |
| Frontend F1 | FE-FOLDER-001/002 freezes; canonical test discovery (174); admin-languages → features/admin-languages | TB-TMAR-FE-F1 |
| Frontend ADMIN-W1 | admin-promotions → features/admin-promotions; admin-api 1321→1234; admin-screens 1230→1120 | TB-TMAR-FE-ADMIN-W1 |
| Frontend ADMIN-W2 | admin-reviews → features/admin-reviews; admin-api 1234→1145; exports 55→49; admin-screens 1120→1078; pattern PROVEN | TB-TMAR-FE-ADMIN-W2 |
| Frontend ADMIN-W3 | admin-sellers → features/admin-sellers; admin-api 1145→1107; exports 49→45; admin-screens 1078→1057 | TB-TMAR-FE-ADMIN-W3 |
| Frontend ADMIN-W4 | admin-customers → features/admin-customers; admin-api 1107→1069; exports 45→41; admin-screens 1057→1036 | TB-TMAR-FE-ADMIN-W4 |
| Next | Continue admin capability migration (receipts / dashboard candidates) | TB-TMAR-FE-ADMIN-W4 |

Last Verified Task = TB-TMAR-FE-ADMIN-W4
