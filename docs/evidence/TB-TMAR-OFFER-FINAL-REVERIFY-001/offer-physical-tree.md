# Offer Physical Tree

- Domain, Application, Contracts, Infrastructure, Endpoints, and Tests projects exist.
- Price command: `Application/Commands/SetOfferPrice/SetOfferPriceCommand.cs`.
- Inventory command: `Application/Commands/SetOfferInventory/SetOfferInventoryCommand.cs`.
- `OfferStatus` and `SalesChannel` physically and logically belong to `Offer.Contracts.Dtos`.
- TypeForwarders files and `TypeForwardedTo`: absent.
- Stale type-forwarder namespace exemptions were removed from the physical guard.
