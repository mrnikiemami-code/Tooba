# TB-P10-T022-R18 — Price Precedence

1. Base AuthoredPrice remains default selling price outside campaign context.
2. When campaign is runtime-active (Published + StartAt<=now + EndAt null| >now) AND SellerOffer is a member AND matching MerchandisingCampaign AuthoredPrice is Active/effective for Market/Channel/Currency/At: campaign amount is selling price.
3. Future / expired / archived campaigns never apply campaign prices in projection (`applyCampaignPrices=false` or inactive window).
4. Wrong CampaignId QualifierKey does not match.
5. Wrong Market/Channel/Currency excluded by batch filters.
6. If no campaign price: Base only.
