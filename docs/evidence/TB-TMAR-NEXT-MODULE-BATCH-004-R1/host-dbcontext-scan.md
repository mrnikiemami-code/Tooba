# Host DbContext Scan

Scan root: `src/backend/Host/Tooba.Host/**/*.cs` (production)

## FulfillmentDbContext
| Path | Classification |
|------|----------------|
| Admin/ProductWorkspaceDevelopmentBootstrap.cs | bootstrap-only (migrate) |

## ReturnsDbContext
| Path | Classification |
|------|----------------|
| (none in Tooba.Host production) | — |

## MigrationRunner (allowed bootstrap)
| Path | Classification |
|------|----------------|
| Host/Tooba.MigrationRunner/ModuleMigrationRegistry.cs | bootstrap-only (FulfillmentDbContext + ReturnsDbContext descriptors) |

## Architecture guards
Minimal allowlist only:
- Fulfillment: Program.cs, ProductWorkspaceDevelopmentBootstrap.cs, ModuleMigrationRegistry.cs
- Returns: Program.cs, ModuleMigrationRegistry.cs

Production business/query Host hits: **0**
