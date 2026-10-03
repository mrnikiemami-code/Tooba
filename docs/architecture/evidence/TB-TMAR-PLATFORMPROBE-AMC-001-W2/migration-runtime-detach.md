# TB-TMAR-PLATFORMPROBE-AMC-001-W2 — Migration runtime detach

## Development seam

PlatformProbeModule no longer registered → no `IModuleSchemaMigrator` for PlatformProbe at runtime.

Active composition-root inventory (HostDevelopmentMigrationSeamGuardTests) = **28** modules (PlatformProbe removed).

## MigrationRunner

`ModuleMigrationRegistry.All` no longer contains PlatformProbe descriptor. Relative order of remaining descriptors preserved.

## Deployed schema

`platform_probe` left untouched (SAFE_TO_DETACH_KEEP_SCHEMA). Historical migrations remain in source for W3 disposition.
