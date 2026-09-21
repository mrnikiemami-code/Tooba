# foundation-adoption-scan

Scoped to Inventory + Promotion + direct Host call sites.

| Pattern | Classification |
|---|---|
| TypeForwardedTo | CLEAN |
| UtcNow/Guid.NewGuid/UuidV7.New in Inventory/Promotion production | CLEAN (IClock/IIdGenerator) |
| StartActivity/ActivitySource raw | CLEAN |
| PlatformHttpException | CLEAN |
| Results.Json owned Result paths | N/A (no Inventory/Promotion owned Result HTTP) |
| Host InventoryDbContext/PromotionDbContext (production) | REPAIRED → CLEAN |
| Host DbContext in Host.Tests helpers | justified test harness |
| Seller SetInventoryAsync Result | canonical |
| Domain InvalidOperationException with stable codes | justified domain invariant (not localized prose) |
| Cart→Inventory.Application for release | justified residual / pre-existing App→App |