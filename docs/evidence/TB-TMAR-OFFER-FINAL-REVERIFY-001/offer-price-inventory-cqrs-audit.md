# Offer Price and Inventory CQRS Audit

- `SetOfferPriceCommandHandler` invokes `ISellerOfferPricingGateway`, preserves `Result` errors, reloads the owned Offer, and returns the same `SellerOfferDetailPage` composed by GetOffer.
- `SetOfferInventoryCommandHandler` does the equivalent through `ISellerOfferInventoryGateway`.
- POST/PUT route shapes and request DTOs are unchanged.
- Both successful mutations retain the post-write Offer re-read behavior.
