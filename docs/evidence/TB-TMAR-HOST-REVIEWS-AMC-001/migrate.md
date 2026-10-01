# Migrate — Host/Reviews AMC-001

## Waves

1. **Offer Contracts**: `IOfferSellerProductIdLookup` + Infrastructure adapter (parity with ListSellerOffers product ids)
2. **Foundation**: Reviews.Contracts.Errors, Reviews.Endpoints (Storefront/Customer/Seller/Admin)
3. **CQRS**: Queries/Commands over `ReviewsPresentationComposer`
4. **Auth**: admin authorizer module-owned; seller authorizer Host thin adapter; customer actor resolver (no guest)
5. **Host ZERO**: deleted `Host/Reviews`; Program maps `MapReviewsModuleEndpoints`

## Behavior preserved

- Routes/verbs unchanged
- Error codes: `customer.session.required`, `reviews.duplicate`, `reviews.rejected`, `reviews.moderation.rejected`
- SellerResponseSupported=false; Persian status labels
- Schema/migrations unchanged; frontend unchanged
