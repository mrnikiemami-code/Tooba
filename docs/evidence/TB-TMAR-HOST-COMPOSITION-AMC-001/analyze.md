# Analyze — Host/Composition AMC-001

Disposition: **KEEP_AS_GENERIC_HOST_COMPOSITION_ROOT** (not HOST_ZERO).

## Inventory (exact 5)

| File | Role | Disposition |
|---|---|---|
| ToobaModuleComposition.cs | Explicit IToobaModule list + AddToobaModules | KEEP (namespace Tooba.Host composition root) |
| SettingsFoundationDevelopmentSeedHost.cs | Thin binder → Party/UserPreference/OperatorProfile seeds | KEEP |
| SupportDevelopmentSeedHost.cs | Thin binder → SupportDevelopmentSeedBootstrap | KEEP |
| WalletDevelopmentSeedHost.cs | Thin binder → WalletDevelopmentSeedBootstrap | KEEP |
| ContentDevelopmentSeedHost.cs | Was fat (foreign DbContext migrate); thin after hygiene | KEEP after repair |

## Blockers repaired

1. ContentDevelopmentSeedHost owned Localization/Content/Media MigrateAsync → moved Content migrate+seed to Content.Infrastructure ContentDevelopmentSeedBootstrap.
2. Localization lacked IModuleSchemaMigrator → registered LocalizationModule migrator order 22 (before Content).
