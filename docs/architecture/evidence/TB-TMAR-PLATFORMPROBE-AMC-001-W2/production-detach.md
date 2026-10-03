# TB-TMAR-PLATFORMPROBE-AMC-001-W2 — Production detach

## Changes

| Surface | Action |
|---|---|
| `Tooba.Host.csproj` | Removed `ProjectReference` to PlatformProbe.Infrastructure |
| `ToobaModuleComposition.cs` | Removed `using` + `new PlatformProbeModule()` |
| `ModuleMigrationRegistry.cs` | Removed PlatformProbe using + descriptor |

## Preserved

- `src/backend/Modules/PlatformProbe/**` source tree (W3 cleanup)
- PlatformProbe EF migrations on disk
- `ModuleSchemaMigrationOrder.PlatformProbe = 13`
- `Tooba.slnx` PlatformProbe project entry
- Host.Tests `Fixtures/PlatformProbe`
- MessagingOptionsValidator forbid of schema name `platform_probe`
- No DROP / no schema mutation
