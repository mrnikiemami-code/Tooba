# Migration design — TB-TMAR-HOST-DEVELOPMENT-PRODUCTWORKSPACE-MIGRATION-SEAM-001

## New neutral seam

`src/backend/BuildingBlocks/Tooba.Persistence/ModuleSchemaMigrator.cs`

### Ordering authority (single model — no contradiction)

```csharp
public static class ModuleSchemaMigrationOrder
{
    public const int Catalog = 1;
    ...
    public const int Support = 28;
}
```

The explicit `int Order` is the **only** authoritative ordering source. There is no second
parallel ordering model (no priority tokens, no per-module constants living elsewhere).

### Contracts

```csharp
public interface IModuleSchemaMigrator
{
    string Module { get; }   // stable identity, no DbContext type name
    int Order { get; }       // explicit deterministic order
    Task MigrateAsync(CancellationToken cancellationToken = default);
}

public interface IModuleSchemaMigrationStep
{
    string Module { get; }
    int AfterOrder { get; }  // ties the step to the module migration it must follow
    Task RunAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
}
```

### Implementations

| Type | Purpose |
|------|---------|
| `EfModuleSchemaMigrator<TContext>` | generic `TContext.Database.MigrateAsync(ct)` — removes 23 trivial hand-written adapters |
| `DelegateModuleSchemaMigrator` | wraps one of the five accepted special module migrators |
| `DelegateModuleSchemaMigrationStep` | wraps a module-owned post-migration step |

### Registration extensions

```csharp
services.AddModuleSchemaMigrator<TContext>(module, order);
services.AddModuleSchemaMigrator(module, order, (sp, ct) => ...);
services.AddModuleSchemaMigrationStep(module, afterOrder, (sp, ct) => ...);
```

## Special migrator contracts preserved

`IOfferSchemaMigrator`, `IPricingSchemaMigrator`, `IInventorySchemaMigrator`,
`ITaxSchemaMigrator`, `IPromotionSchemaMigrator` keep their public signatures. Their modules
adapt them into the neutral seam by delegate — the seam does **not** re-implement their logic.

## Host replacement

`src/backend/Host/Tooba.Host/Development/DevelopmentSchemaMigrator.cs`

- resolves `IEnumerable<IModuleSchemaMigrator>`, orders by `Order`, fails fast on a duplicate order;
- after each module migrate, runs the `IModuleSchemaMigrationStep`s whose `AfterOrder` matches,
  ordered by module identity (ordinal) — this is how the two Promotion-owned steps stay in place;
- assigns the Development commerce context on `store-alpha` before migrating;
- then runs the Development seed composition (Catalog owned `IWorkspaceDemoSeed` + module-owned seeds)
  exactly as the deleted bootstrap did.

`Host/Development/DevelopmentSchemaMigrator.cs` also keeps the Catalog attribute-schema
prerequisite reachable: `CatalogModule` registers
`AddModuleSchemaMigrationStep("Catalog", Order, CatalogDevelopmentSeed.PostMigration.EnsureAsync)`
which is guarded by Development **and** `RunLegacyBootstraps`, exactly the previous `Program.cs`
guard.

Both method names are preserved so no behavior call-site changes semantics:
`ApplyAsync` (seed path) and `MigrateSchemaOnlyAsync` (schema-only path).

## Host classification

`Host/Development` still contains **exactly 5** files:

| File | Classification |
|------|----------------|
| `DevelopmentTenantCommerceContext.cs` | ALLOWED_DEVELOPMENT_COMPOSITION |
| `DevelopmentSchemaMigrator.cs` | ALLOWED_DEVELOPMENT_COMPOSITION (neutral seam; no foreign DbContext) |
| `MarketplaceDevelopmentBootstrap.cs` | ALLOWED_DEVELOPMENT_COMPOSITION |
| `MarketplaceAdminDevBootstrap.cs` | ALLOWED_DEVELOPMENT_RUNTIME_SEAM |
| `MarketplaceSellerDevBootstrap.cs` | ALLOWED_DEVELOPMENT_RUNTIME_SEAM |

`ProductWorkspaceDevelopmentBootstrap.cs` is **deleted**. Zero foreign `DbContext` and zero
foreign persistence namespaces remain anywhere under `Host/Development`.

## Cross-module boundary

- Platform seam: `Tooba.Persistence` (already referenced by all 28 module infrastructure projects).
- `Host/Development` references no module internal namespace for migration purposes.
- No new cross-module project reference was added.
- No schema, route, or frontend change.
