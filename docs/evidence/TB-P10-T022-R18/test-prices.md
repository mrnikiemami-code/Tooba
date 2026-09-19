# TB-P10-T022-R18 — Test Prices (Dev seed)

MerchandisingCampaignDevelopmentSeed EnsureCampaignPromoPricesAsync:
- ActivePrimary: first 3 active offers → MerchandisingCampaign AuthoredPrice @ 70% of Base
- ActivePrimary: remaining members without campaign price → Base fallback
- Future: one offer campaign price @ 50% with ValidFrom=campaign StartAt (must not apply now)
- Idempotent: skips existing Active campaign prices for same QualifierKey
- Development only
