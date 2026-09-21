# Cart audit — TB-TMAR-NEXT-MODULE-BATCH-005

## Cross-module boundary
- Application/Infrastructure ProjectReferences: Catalog.Contracts, Inventory.Contracts, Offer.Contracts, Pricing.Contracts only (no Catalog/Inventory Application).
- New contracts: `ICatalogCartQuantityPolicyGateway`, `ICartInventoryHoldPort`.

## Physical
- Domain split: Aggregates/ShoppingCart, Entities/CartLine, Events/*, ValueObjects/*.
- Infrastructure: Directories/, Security/, Messaging/, DependencyInjection/.
- Application: Ports/, Conversion/, Lifetime/.
- Root dump = 0.

## Foundation
- CartDirectory injects IClock, IIdGenerator, IQuantityNormalizer (required; no `??` SystemUtcClock/QuantityNormalizer).
- Domain factories take explicit Guid ids (CreateGuest/CreateAuthenticated/CartLine.Open).
- No Guid.NewGuid / DateTimeOffset.UtcNow / UuidV7.New in Cart module production sources.

## Host
- Cart Host production authority: CartDbContext bootstrap/migrate only (unchanged pattern).
- Endpoint seam characterization paths updated for Directories/CartDirectory.cs.

## Residuals (batch)
- Dedicated Cart.Tests ArchitectureGuard project not added (characterization remains in Host.Tests).
