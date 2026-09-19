# Recommended Architecture (minimal strong foundation)

## Decision

**Option 4:** introduce a **Merchandising Campaign** aggregate separate from checkout `PromotionDefinition`, keyed by `PromotionType` (start with `AMAZING`), with **Offer membership**, UTC window, priority/sort, and a Builder product source `PromotionCampaign`.

## Write model (proposed — not implemented)

1. **`PromotionType`** (table or closed enum registry): code `AMAZING` (+ future `LIGHTNING`, …). Prefer small type table or closed string registry — avoid boolean explosion.
2. **`MerchandisingCampaign`**: Id, TypeCode, operational Name, StartAt, EndAt?, Priority, LifecycleStatus (Draft/Published/Archived), TeasingVisible bit or derive teasing from StartAt, Created/Updated, optional concurrency token.
3. **`MerchandisingCampaignOffer`**: CampaignId, OfferId, SortOrder, optional `PromoAmount` + currency (promotional selling), optional per-member order limit override later, optional AllocationQty later.
4. **`MerchandisingCampaignTranslation`**: CampaignId, Locale, Title, Subtitle?, BadgeText?.

## Read path

`Get active AMAZING offers for tenant, locale L, now`:

- campaigns where Type=AMAZING AND Published AND StartAt<=now AND (EndAt null OR EndAt>now)
- join membership ordered by campaign.Priority DESC, member.SortOrder ASC
- join Offer Status=Active
- join current AuthoredPrice (Base, market/channel)
- join Inventory Available > 0 (marketable)
- project card DTO: listPrice=AuthoredPrice.Amount, selling=PromoAmount ?? Amount, discount%=derived, timerEnd=EndAt, badge=translation[L], stock=Available

## Reuse

- Offer, Pricing, Inventory, Catalog product card composition, Landing Page locale, existing HostedServices patterns for optional expiry sweeper later.

## Explicit non-goals (foundation)

Audience rules, budgets, workflow engines, per-second timer persistence, `Offer.IsAmazing`, forking checkout Promotion into merchandising.
