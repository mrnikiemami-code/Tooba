# W33-MERCHANDISING analyze

## Source (Host Admin)
- MerchandisingCampaignAdminEndpoints.cs (+ composer)
- MerchandisingCampaignDevelopmentSeed.cs
- MerchandisingStoreLandingReferenceGate.cs

## Destination
| Responsibility | Destination |
|---|---|
| Admin HTTP `/v1/admin/merchandising-campaigns*` | Promotion.Endpoints |
| Composer + member enrichment | Promotion.Infrastructure (ICatalogVariantLookup + IPartyLookup) |
| MediatR CQRS | Promotion.Application.Merchandising.Admin |
| Development seed | Promotion.Infrastructure.Development |
| Landing campaign gate | Host.CatalogAdapters thin adapter |

## Out of scope
StoreAppearance*, ProductWorkspace* shells, HoldPolicy
