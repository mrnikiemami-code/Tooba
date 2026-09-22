# Inventory Route Ownership Audit

The seller mutation route belongs to Offer because its URI and use case modify inventory for an Offer listing. `Tooba.Offer.Endpoints` owns HTTP and sends an Offer Application command. The command crosses into Inventory solely through `Tooba.Inventory.Contracts.Seller.ISellerOfferInventoryGateway`.

Inventory owns stock persistence and invariants, but has no presentation responsibility. Duplicating this route in Inventory would create conflicting HTTP ownership. Host has no Inventory HTTP or business authority.
