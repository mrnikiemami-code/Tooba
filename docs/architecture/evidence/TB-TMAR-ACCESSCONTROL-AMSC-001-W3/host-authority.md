# Host authority — AccessControl (W3)

## Classification

| Aspect | State |
| --- | --- |
| `ILLEGAL_BUSINESS_AUTHORITY` | `ZERO` |
| `ILLEGAL_PERSISTENCE_AUTHORITY` | `ZERO` |
| `ILLEGAL_ENDPOINT_OWNERSHIP` | `ZERO` |
| `STRUCTURAL_DEBT_ONLY` | `ZERO` |
| `HOST_FINAL_CLOSURE_REGRESSION` | `ZERO` |
| `SINK_FOLDER_REGRESSION` | `ZERO` |
| Host production files added by the AMSC run | `ZERO` |

## Host references to AccessControl — classification

| Host file | Category | Justification |
| --- | --- | --- |
| `Composition/ToobaModuleComposition.cs` | `ALLOWED_COMPOSITION_ROOT` | registers `AccessControlModule` DI composition |
| `Program.cs` | `ALLOWED_COMPOSITION_ROOT` | maps module endpoints; bootstraps access control |
| `Development/DevelopmentSchemaMigrator.cs` | `ALLOWED_COMPOSITION_ROOT` | applies module migrations |
| `Development/MarketplaceDevelopmentBootstrap.cs` | `ALLOWED_COMPOSITION_ROOT` | dev seed orchestration |
| `Development/MarketplaceSellerDevBootstrap.cs` | `ALLOWED_COMPOSITION_ROOT` | dev seed orchestration |
| `Health/HostHealthEndpoints.cs` | `ALLOWED_COMPOSITION_ROOT` | readiness aggregation |
| `Health/HostReadinessEvaluator.cs` | `ALLOWED_COMPOSITION_ROOT` | readiness aggregation |
| `Admin/Access/Authorizers/HostOrderAdminEffectiveAccessReader.cs` | `ALLOWED_CONTRACT_CONSUMPTION` | Host admin authorizer consuming the module's effective-access contract |
| `Security/Seller/HostPartySellerAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` | Host seller security adapter consuming module capability checks |
| `Security/Seller/HostSupportSellerAuthorizer.cs` | `ALLOWED_SECURITY_ADAPTER` | Host seller security adapter |
| `Composition/SupportDevelopmentSeedHost.cs` | `ALLOWED_COMPOSITION_ROOT` | dev seed orchestration |
| `Tooba.MigrationRunner/ModuleMigrationRegistry.cs` | `ALLOWED_COMPOSITION_ROOT` | migration registry entry |

Every Host reference is composition, security adaptation or Contracts consumption. None contains
AccessControl business rules, persistence access, route handlers or policy logic.

## Host HTTP ownership

| Check | Result |
| --- | --- |
| `Host/Tooba.Host/AccessControl/` folder | does **not** exist |
| `Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` | does **not** exist |
| Host-owned AccessControl routes | `ZERO` |
| Module-owned AccessControl routes | 58 `Map*` registrations in 4 module files |

The size baseline still carries a `Host/Tooba.Host/AccessControl/AccessControlEndpoints.cs` entry
(984 LOC). That file is gone from disk — Host HTTP evacuation already completed. The stale baseline
entry is recorded in `residual-debt.md`; it is **not** a Host closure regression.

## Host final closure preservation

| Checkpoint | State |
| --- | --- |
| `HOST_TMAR_EVACUATION_FINAL_CLOSURE_CERTIFIED` | preserved |
| `HOST_ROOT_FINAL_CERTIFIED` | preserved |
| `currentHostCheckpoint = HOST_ROOT_FINAL_CERTIFIED` | intact in SoT |

## Closed-folder regression audit (§13b)

| Destination | Baseline state | Post-AMSC state | Verdict |
| --- | --- | --- | --- |
| `Host/Tooba.Host/AccessControl/` | closed / absent | absent | no regression |
| `Host/Tooba.Host/Admin/` | closed folder, protected retained set | unchanged | no regression |
| `Host/Tooba.Host/Storefront/` | closed (evacuated to Catalog) | unchanged | no regression |
| any other module folder | untouched | untouched | no regression |

The AMSC run wrote **no** file into any Host folder. `git diff a3ba1a4f HEAD -- src/backend/Host/Tooba.Host/`
is empty except for the `Tooba.Host.Tests` guard project (which is not a production Host destination).
