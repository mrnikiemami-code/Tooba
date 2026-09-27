# Analyze — TB-TMAR-HOST-DEVELOPMENT-AMC-001

## Before / after tree
`src/backend/Host/Tooba.Host/Development/`

| File | Bytes (approx) |
| --- | --- |
| MarketplaceDevelopmentBootstrap.cs | present |
| MarketplaceAdminDevBootstrap.cs | present |
| MarketplaceSellerDevBootstrap.cs | present |

Production file count before = after = **3** (retain)

## Environment gating
`Program.cs`: `if (app.Environment.IsDevelopment())` then if Edition=Marketplace → `MarketplaceDevelopmentBootstrap.ApplyAsync`. Not reached in Production.

## Classification summary
| File | Class | Disposition |
| --- | --- | --- |
| MarketplaceDevelopmentBootstrap | DEVELOPMENT_HOST_COMPOSITION | RETAIN |
| MarketplaceAdminDevBootstrap | DEVELOPMENT_HOST_RUNTIME_SEAM | RETAIN |
| MarketplaceSellerDevBootstrap | DEVELOPMENT_HOST_RUNTIME_SEAM | RETAIN |

## Orchestration vs implementation
- MarketplaceDevelopmentBootstrap **orchestrates** `Database.MigrateAsync()` and module-owned `*DevelopmentSeed.ApplyAsync` / Host seller runtime Ensure — does not implement seed/domain mutation itself.
- Admin/Seller bootstraps write generic authorization tuples + seller snapshot publish via existing Host seller seam — no DbContext.
