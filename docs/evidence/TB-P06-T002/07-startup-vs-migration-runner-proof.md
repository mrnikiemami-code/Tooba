# 07 — Startup vs migration runner proof (TB-P06-T002)

## Production Host — no silent EF migrate

`Program.cs`:

```csharp
if (app.Environment.IsDevelopment())
{
    await ProductWorkspaceDevelopmentBootstrap.ApplyAsync(app.Services);
    await StorefrontDemoCatalogBootstrap.ApplyAsync(app.Services);
}
```

Production path does **not** invoke `ProductWorkspaceDevelopmentBootstrap`.

## Development convenience

`ProductWorkspaceDevelopmentBootstrap` migrates all 19 module DbContexts via `MigrateAsync()` — Development only.

## Readiness interaction

`/health/ready` checks configuration/dependency availability (T001 baseline). Pending migrations do **not** automatically fail readiness — migration is an explicit deployment gate via runner, not runtime app mutation.

## Operational model

| Environment | Migration mechanism |
|---|---|
| Development | Auto-migrate on Host startup (bootstrap) |
| Production | Explicit `Tooba.MigrationRunner apply` |
