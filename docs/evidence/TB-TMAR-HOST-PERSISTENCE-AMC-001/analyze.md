# Analyze — Host/Persistence AMC-001

Skills: `tooba-architecture-analyze`  
Mode: `ANALYSIS_ONLY` (production change ZERO)  
Parent: `TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT` (PRESERVED)

## Exact tree / type enumeration

| Path | File | Visibility | Namespace | Types |
| --- | --- | --- | --- | --- |
| `src/backend/Host/Tooba.Host/Persistence/` | `DatabaseConnectionResolver.cs` | `internal sealed` | `Tooba.Host.Persistence` | `DatabaseConnectionResolver` |

- Production file count = **1**
- Production type count = **1**
- Duplicate `DatabaseConnectionResolver` under `Tooba.Host` = **ZERO**
- Path ↔ namespace = **EXACT**

## Type disposition

| Type | Disposition |
| --- | --- |
| `DatabaseConnectionResolver` | **KEEP_AS_GLOBAL_HOST_PERSISTENCE_PLATFORM** |

Why KEEP (not MOVE_TO_CONFIGURATION / MOVE_TO_TOOBAPERSISTENCE / DEAD):

1. Remainder audit classifies Persistence as `PLATFORM_KEEP`.
2. Modules must not parse Host options or own reference→string mapping.
3. Implementation depends on Host-owned `ToobaPlatformOptions` — cannot enter BuildingBlocks/Tooba.Persistence without illegal Host options ownership.
4. Configuration owns the catalog; Persistence owns the thin resolution adapter (intentional split).

## Structured state

| Field | State |
| --- | --- |
| Ownership | Host global persistence platform seam |
| File cohesion | COHESIVE (1 type / 1 file) |
| Localization | CANONICAL machine code `platform.connection.unconfigured` |
| Stable error | CATALOGUED Foundation (`FoundationErrorCodes` + EN/FA resx) |
| Logging / sensitive | ZERO connection-string / reference leakage |
| Contracts boundary | CLEAN — implements BuildingBlocks `IDatabaseConnectionResolver` |
| Cross-module coupling / join | NONE |
| Foreign Application / Domain / Infrastructure / DbContext | ZERO |
| Business authority | ZERO |
| Schema / frontend | UNCHANGED |
| Host residue | KEEP platform (not HOST_ZERO) |

## Decisive plan

Recommended wave count = **1** (DIRECT CERT).

- Historical migrate already applied (namespace EXACT).
- `PlatformHttpException` accepted as canonical fail-closed at this BuildingBlocks-documented seam.
- Remaining durable work = CERTIFY_ONLY SoT/guard vocabulary + optional focused test gaps (non-blocking for disposition).

`recommendedNextTask` = `TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT`  
`automaticNextImplementationTask` = **NONE**  
`workflowStop` = `USER_REVIEW_HOST_PERSISTENCE_AMC_001`
