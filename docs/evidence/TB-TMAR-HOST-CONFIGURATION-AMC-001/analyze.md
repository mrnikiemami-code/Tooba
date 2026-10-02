# Analyze — Host/Configuration AMC-001

Skills: `tooba-architecture-analyze`  
Mode: `ANALYSIS_ONLY` (production change ZERO)  
Parent: `TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT` (PRESERVED)

## Exact tree / type enumeration

| Path | File | LOC | Namespace | Top-level types |
| --- | --- | --- | --- | --- |
| `Host/Tooba.Host/Configuration/` | `ToobaPlatformOptions.cs` | 607 | `Tooba.Host` | **9** |

Types (all `internal sealed`):

1. `ToobaPlatformOptions`
2. `StoreCommerceOptions`
3. `MarketplaceOptions`
4. `SingleStoreOptions`
5. `TenantRecordOptions`
6. `PostgreSqlOptions`
7. `TenantRecord`
8. `ControlPlaneRegistry`
9. `PlatformOptionsValidator`

Nested types = **0**. Subfolders = **0**. Production file count = **1**.

## Path ↔ namespace

| Current | Target if retained |
| --- | --- |
| `namespace Tooba.Host` | `Tooba.Host.Configuration` |

**State: VIOLATION** — folder `/Configuration/` vs root Host namespace.

## File cohesion

**MUST_SPLIT** — 9 top-level types in one file. Default target = one top-level type per file (9 files).

## Disposition summary

| Type | Disposition |
| --- | --- |
| `ToobaPlatformOptions` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM |
| `StoreCommerceOptions` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM |
| `MarketplaceOptions` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM |
| `SingleStoreOptions` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM |
| `TenantRecordOptions` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM |
| `PostgreSqlOptions` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_PLATFORM |
| `TenantRecord` | KEEP_AS_GLOBAL_HOST_CONTROL_PLANE_SNAPSHOT |
| `ControlPlaneRegistry` | KEEP_AS_GLOBAL_HOST_CONTROL_PLANE_SNAPSHOT |
| `PlatformOptionsValidator` | KEEP_AS_GLOBAL_HOST_CONFIGURATION_VALIDATOR |

No MOVE / DEAD / BLOCKED for the nine types. Remainder audit: Configuration = `PLATFORM_KEEP`.

## Decisive plan

**recommendedWaveCount = 2**

1. `TB-TMAR-HOST-CONFIGURATION-AMC-001-W1` — namespace → `Tooba.Host.Configuration` + split 9 files + consumer usings (Host/tests/MigrationRunner).
2. `TB-TMAR-HOST-CONFIGURATION-AMC-001-W2-CERT` — CERTIFY_ONLY.

Offer.Contracts `SalesChannel` enum coupling = Contracts-only debt (document; do **not** force Offer relocate in W1).

`automaticNextImplementationTask` = **NONE**  
`workflowStop` = `USER_REVIEW_HOST_CONFIGURATION_AMC_001`
