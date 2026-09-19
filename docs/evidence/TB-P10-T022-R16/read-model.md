# TB-P10-T022-R16 — Read Model

## Surface

`IMerchandisingCampaignQuery` + DTOs in `Tooba.Promotion.Application`:

- `MerchandisingCampaignRuntimeModel` — CampaignId, StoreId, PromotionTypeCode, localized Title/Subtitle/BadgeText, StartAt/EndAt, Priority, IsTeasing, RemainingDuration (derived), Members
- `MerchandisingCampaignMemberRuntimeModel` — SellerOfferId, CatalogVariantId, SortOrder, IsMarketable, PriceAmount/Currency, AvailableQuantity, Min/Max order qty

## Non-goals

- Not EF entities
- No full ProductCard / slug/title duplication (offer-centric for next Builder task)
- No PromoAmount / discount / compare-at fields

## Limits

`MerchandisingCampaignRuntimeLimits.MaxMemberTake = 48` (aligned with Product Showcase MaxTake).
