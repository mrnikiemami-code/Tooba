# Analyze — Host/Persistence AMC-001

Skills: `tooba-architecture-analyze`  
Transport: `ARCHITECT_DIRECT_ANALYZE_MIGRATE_CERTIFY`  
Scope: `src/backend/Host/Tooba.Host/Persistence/` only

## Target inventory (re-enumerated)

| File | LOC | Namespace (before) |
| --- | --- | --- |
| `Persistence/DatabaseConnectionResolver.cs` | 49 | `Tooba.Host` (path mismatch) |

Production file count = **1**. No other files in folder.

## Responsibility map

| Responsibility | Classification | Owner |
| --- | --- | --- |
| Map `ConnectionReference` → Npgsql connection string from `ToobaPlatformOptions.PostgreSQL.ConnectionReferences` | HOST_COMPOSITION_ROOT / platform infrastructure | Host |
| Fail-closed 503 `platform.connection.unconfigured` on missing/invalid | PLATFORM | Host |
| Contract `IDatabaseConnectionResolver` | CONTRACT (neutral) | `Tooba.BuildingBlocks` |

## Structured state

| Field | State |
| --- | --- |
| Foundation-State | N/A (no business module destination) |
| Ownership-State | correct — Host platform connection resolution |
| File-Cohesion-State | COHESIVE |
| Localization-State | CANONICAL (machine code only; no user-facing text) |
| API-Result-Pattern-State | N/A (no HTTP endpoint) |
| Stable-Error-Code-State | CATALOGUED (`platform.connection.unconfigured` via `PlatformHttpException`) |
| Logging-State | CANONICAL (no logging of connection strings) |
| Sensitive-Logging-State | NONE |
| CQRS-State | N/A |
| Contracts-Boundary-State | CLEAN (implements BuildingBlocks seam) |
| Cross-Module-Coupling-State | NONE |
| Cross-Module-Join-State | NONE |
| Persistence-Ownership-State | HOST_OWNED platform seam (not module DbContext authority) |
| Endpoint-Ownership-State | N/A |
| Host-Residue-State | `KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE` |
| Schema-Migration-State | UNCHANGED |
| Behavior-Preservation-Risk | LOW |
| Canonical-Reference-Used | `docs/architecture/30-tenant-edition-database-foundation.md`; BuildingBlocks `IDatabaseConnectionResolver`; remainder audit `PLATFORM_KEEP` |
| Final-Disposition | **KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE** |

## Why KEEP (not evacuate)

1. Remainder audit `TB-TMAR-HOST-REMAINDER-AUDIT-001` classifies `Persistence` as `PLATFORM_KEEP`.
2. Architecture lock: modules must not parse Host config or pick connection strings; Host owns reference→string resolution.
3. Implementation depends on Host-owned `ToobaPlatformOptions` — cannot move into a business module without illegal Host options ownership.
4. Destination cannot be `Tooba.Persistence` BuildingBlocks without pulling Host options into BuildingBlocks (wrong layer).

## Hygiene required (ownership ≠ quality)

- Path↔namespace: folder `Persistence/` must use `Tooba.Host.Persistence` (currently root-dumped as `Tooba.Host`).
- Durable allowlist guard + SoT block locking the 1-file retained set.
- No business evacuation; no HOST_ZERO claim.

## Destination integrity

No move into other Host folders. No new Host folder. Reviews/PageComposition/Security retained sets untouched.
