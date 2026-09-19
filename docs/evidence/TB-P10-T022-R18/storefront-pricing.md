# TB-P10-T022-R18 — Storefront Pricing

PromotionCampaign resolve embeds member PriceAmount/CompareAt into StoreLandingPageResolvedItem; ToPublicAsync overlays onto ComposeProductCardsAsync cards:
- OfferAmountExclusiveOfTax = compare-at (Base) when promo applies
- PromotionalAmountExclusiveOfTax = campaign selling
- PromotionLabel = campaign BadgeText
Manual/Category/Brand/Newest paths unchanged (null overlay fields).
ComposeProductAsync selects Base QualifierKind only so campaign rows never become primary listing price outside overlay.
