# CQRS map

Commands: Create, Update, Activate, Suspend, Archive, SetReturnPolicy, SetOrderQuantityLimits.

Queries: GetOffer, ListSellerOffers.

Handlers use `IOfferStore`, `IOfferUseCaseGuard`, `IClock`, `IIdGenerator`, Catalog/Party contract lookups, and `IReturnPolicyResolver`. Infrastructure performs persistence only.
