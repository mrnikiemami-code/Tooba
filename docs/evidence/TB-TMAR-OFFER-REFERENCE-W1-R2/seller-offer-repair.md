# SellerOffer repair
- Removed Persian Activate exception
- Replaced InvalidOperationException string codes with SemanticException + OfferErrorCodes
- Create now requires offerId from IIdGenerator (no UuidV7 in Domain)
- Time remains argument-based (now)
