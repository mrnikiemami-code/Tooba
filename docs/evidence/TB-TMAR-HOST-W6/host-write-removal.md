# Host write removal — TB-TMAR-HOST-W6

Removed from Host ShippingServiceEndpoints: SaveChangesAsync, DbSet Add/RemoveRange for Create/Update/Deactivate/EnsureSeed.
EnsureSeed no longer static Host method; StorefrontShippingComposer + List tree call EnsureShippingCatalogSeedCommand.
Persistence owned by ShippingServiceDirectory.
