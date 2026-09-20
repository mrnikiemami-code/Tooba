# Inventory boundary

The compatibility inventory URL delegates to `ISellerOfferInventoryGateway`. Inventory owns stock persistence and active/default-location selection, including creation of `SELLER-DEFAULT`. Host and Offer never access `InventoryDbContext`.
