# 08 — Marketplace migration proof (TB-P06-T002)

## Edition resolution

When `Tooba:Edition = Marketplace`:

- `MigrationTargetResolver` resolves single target from `ControlPlaneRegistry.MarketplaceConnectionReference`
- One operational marketplace database
- All 19 module migrations applied in registry order on that DB

## CLI examples

```bash
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- status
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- plan
dotnet run --project src/backend/Host/Tooba.MigrationRunner -- apply
```

No tenant flags required for Marketplace edition.

## Module ordering

Same deterministic order as dev bootstrap (Catalog → … → Content). See `ModuleMigrationRegistry.cs`.

## Failure semantics

- First module failure stops further modules for that target
- Non-zero exit code (1)
- Partial prior modules remain applied (standard EF behavior)

## Connection resolution

`DatabaseConnectionResolver` maps logical `ConnectionReference` → Npgsql connection string from `Tooba:PostgreSQL:ConnectionReferences` — secrets never echoed in output.
