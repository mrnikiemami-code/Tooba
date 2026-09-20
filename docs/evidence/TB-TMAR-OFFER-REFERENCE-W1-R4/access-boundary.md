# Access boundary

Endpoints extract the authenticated seller identity. Every seller command/query carries `SellerPartyId`; handlers fail closed with `offer.not_found`. The production `OpenOfferUseCaseGuard` registration and implementation were removed.
