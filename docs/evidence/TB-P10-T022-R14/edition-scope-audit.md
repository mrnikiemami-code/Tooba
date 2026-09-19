# Edition Scope Audit

## Editions present

Host health/ready reports `edition=SingleStore` in local Dev. BuildingBlocks / module composition support Marketplace vs Single-Store without duplicating commercial entities.

## Commercial entity scoping today

| Entity | Scope |
|---|---|
| `SellerOffer` | SellerPartyId + Channel (+ CatalogVariantId). Channel enum includes `Marketplace` and `Direct`. |
| `AuthoredPrice` | OfferId + Market + Channel + Currency + time window |
| `StockPosition` | OfferId + LocationId |
| `PromotionDefinition` | Optional SellerPartyId / OfferId / Category filters; Market / SalesChannel strings — **tenant DB scoped**, not a separate StoreId column |
| Landing pages | Per tenant catalog schema |

Tenant database (`tooba_alpha`, …) = store deployment boundary for Single-Store.

## Answers for Amazing campaigns

| Question | Recommendation |
|---|---|
| Store-scoped? | **YES** — campaign rows live in tenant DB (same as Landing/Offer). No extra StoreId required in Single-Store; Marketplace edition still one commercial DB per marketplace deployment unless future multi-store-in-DB appears. |
| Marketplace-wide? | Campaign can include Offers from **multiple sellers** via membership → `SellerOffer.SellerPartyId`. |
| Seller-scoped campaigns? | Optional later (`SellerPartyId` on campaign); not required for foundation. |
| Channel-scoped? | Reuse Offer/Price `Channel` filters at query time if needed; avoid duplicate campaign-per-channel tables initially. |
| Multi-seller members? | **YES** — membership references `OfferId`. |
| Single-Store without multi-store UX | Same model; Admin hides multi-seller chrome when edition is Single-Store; no edition-specific duplicate schema. |

**Do not** create MarketplaceCampaign vs SingleStoreCampaign tables.
