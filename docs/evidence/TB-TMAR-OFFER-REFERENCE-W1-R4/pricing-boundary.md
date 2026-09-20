# Pricing boundary

The compatibility price URL delegates to `ISellerOfferPricingGateway`. `PriceDirectory` owns validation, ownership lookup through Offer Contracts, and Pricing persistence. Host and Offer never access `PricingDbContext`.
