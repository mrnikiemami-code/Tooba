# Migrate — Host/Persistence AMC-001 (HISTORICAL)

> HISTORICAL evidence from prior Persistence AMC execution. Superseded as certification authority by the fresh Analyze under parent Outbox W2-CERT. Production namespace EXACT outcome remains CURRENT.

Disposition (historical vocabulary): **KEEP_AS_GENERIC_HOST_PLATFORM_INFRASTRUCTURE** (no business evacuation).

## Changes

1. Namespace hygiene: `DatabaseConnectionResolver` → `Tooba.Host.Persistence` (path↔namespace EXACT).
2. Consumer usings for concrete type: `Program.cs`, Host tests, `MigrationRunner` (+ tests).
3. Behavior unchanged: same fail-closed codes, same DI registration `IDatabaseConnectionResolver → DatabaseConnectionResolver`, secrets never logged.

## Not done (correctly)

- No move into a business module.
- No HOST_ZERO (folder retained with 1 production file).
- No schema/route/frontend change.
- BuildingBlocks `IDatabaseConnectionResolver` contract unchanged.
