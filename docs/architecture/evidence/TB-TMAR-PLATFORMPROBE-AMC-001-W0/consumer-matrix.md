# TB-TMAR-PLATFORMPROBE-AMC-001-W0 — Consumer matrix

| Consumer | Surface | Production/Test/Docs | Why It Depends | Required If Probe Removed? |
|---|---|---|---|---|
| `Tooba.Host` csproj | `ProjectReference` → PlatformProbe.Infrastructure | Production | Load assembly for composition | No — remove reference |
| `ToobaModuleComposition` | `new PlatformProbeModule()` in module graph | Production | Registers DbContext + migrator + outbox | No — drop from graph |
| `PlatformProbeModule` | `IToobaModule.AddServices` | Production | Self-registration | N/A (moves or deleted) |
| `ModuleSchemaMigrationOrder.PlatformProbe = 13` | constant in `Tooba.Persistence` | Production foundation | Stable migration order key | Keep constant or retire carefully; other modules use higher numbers |
| `DevelopmentSchemaMigrator` (indirect) | discovers `IModuleSchemaMigrator` | Production/Dev | Applies order-13 migrator when module registered | No probe-specific code; loses one migrator when unregistered |
| `Tooba.MigrationRunner` / `ModuleMigrationRegistry` | explicit `PlatformProbeDbContext` descriptor | Production tooling | Applies EF migrations for schema | No — remove descriptor; leave deployed schema |
| `MessagingOptionsValidator` | forbids Messaging schema `platform_probe` | Production | Reserve name so bus transport ≠ probe schema | Keep forbid string (safe; no assembly dep) |
| `ArchitectureBoundaryTests` | asserts composition contains `PlatformProbeModule` | Test | Early foundation proof | Rewrite to absence or fixture assertion |
| `HostDevelopmentMigrationSeamGuardTests` | lists PlatformProbe composition root; counts 29 migrators | Test | Migration seam inventory | Update list/count after detach |
| `OutboxFoundationTests` | `PlatformProbeOutboxRegistration` translate/type-map | Test | Outbox contract proof | Rehome registration to test fixture |
| `OutboxPostgresTests` | `PlatformProbeDbContext` + records + schema claims | Test | Same-transaction outbox proof | Rehome DbContext/migrations or recreate test DbContext |
| `OutboxTestSupport` | factory for probe DbContext + registration | Test | Shared outbox test helper | Move with fixture |
| `MassTransitPostgresTests` | probe records + outbox registration | Test | Messaging integration proof | Rehome or substitute another module outbox |
| `PostgresIntegrationTests` | `PlatformProbePersistence.NewRecord` | Test | Persistence smoke | Rehome or replace sample |
| `PersistenceFoundationTests` | schema constant + context factory | Test | Schema ownership smoke | Rehome or replace |
| Architecture docs `33`/`34` | narrative “disposable sample” | Docs | Foundation explanation | Update wording after removal |
| Historical tasks / evidence | many TB-P01 / TMAR docs | Docs | Origin story | Retain as historical |
| `tmar-current-state.json` | `migrationOrder` includes `"PlatformProbe"` | SoT | Records historical order parity | Update when runtime detach lands |
| `tmar-module-structure-manifests.json` | — | SoT | Not listed | N/A |
| Business modules (Catalog/Order/…) | — | Production | **ZERO** ProjectReference / using | Not required |
| Frontend | — | Frontend | **ZERO** | Untouched |

## Production business consumers

**ZERO.** No customer/admin/seller request path, no business module ProjectReference, no production data flow on `platform_probe.*` events outside the probe’s own write path.
