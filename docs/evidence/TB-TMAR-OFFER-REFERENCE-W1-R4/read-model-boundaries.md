# Read-model boundaries

Offer composes seller DTOs through `ICatalogOfferReadGateway`, `IPriceLookupGateway`, `ISellerOfferInventoryGateway`, and `IPartyLookup`. Owner Infrastructure implementations query only their own DbContexts; Offer Application and Host perform no cross-module EF query.
