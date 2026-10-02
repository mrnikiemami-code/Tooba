# path-namespace-cohesion — TB-TMAR-HOST-CONFIGURATION-AMC-001

| Check | State |
| --- | --- |
| Path | `Host/Tooba.Host/Configuration/` |
| Namespace | `Tooba.Host` |
| Match | **VIOLATION** |
| Target namespace | `Tooba.Host.Configuration` |
| Cohesion | **MUST_SPLIT** (9 types / 1 file) |

## Target tree (W1)

```text
Configuration/
  ToobaPlatformOptions.cs
  StoreCommerceOptions.cs
  MarketplaceOptions.cs
  SingleStoreOptions.cs
  TenantRecordOptions.cs
  PostgreSqlOptions.cs
  TenantRecord.cs
  ControlPlaneRegistry.cs
  PlatformOptionsValidator.cs
```

## Consumer import impact

Types today resolve via `namespace Tooba.Host` (same as Program root). After move, add `using Tooba.Host.Configuration;` (or fully qualify) in:

- `Program.cs`
- Persistence / MultiTenancy / Health / Outbox / Admin / Development / Composition consumers of registry/options
- Host tests constructing options/registry
- `Tooba.MigrationRunner` (+ tests)

No production behavior change intended.
