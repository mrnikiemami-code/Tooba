# TB-TMAR-PLATFORMPROBE-AMC-001-W0 — Recommended disposition

## Decision (exactly one)

`REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE`

## Rationale

1. Explicit disposable / non-business semantics in production source and architecture docs.
2. Zero production business consumers.
3. Value is almost entirely Host integration-test evidence for persistence/outbox/messaging foundations.
4. Keeping it as INTERNAL_ONLY production Infrastructure would preserve startup/migration surface for a non-capability and fight microservice/host thinning goals.
5. Creating ceremonial Domain/Application/Contracts/Endpoints would violate Analyze guidance.

## Test-fixture rehome (feasibility: FEASIBLE)

Preferred smallest destination aligned with “no BuildingBlocks production test tables”:

`src/backend/Host/Tooba.Host.Tests/Fixtures/PlatformProbe/`

Carry (or recreate) for tests:

- `PlatformProbeDbContext` + `PlatformProbeRecord` + `PlatformProbePersistence`
- `PlatformProbeOutboxRegistration` + probe events
- EF migrations **if** integration tests continue to `Migrate` the probe schema; alternatively a test-only context that migrates the same historical migrations from the fixture assembly

Preserve:

- same-transaction outbox via `OutboxSaveChangesInterceptor` + module registration (already generic)
- schema name `platform_probe` for parity with deployed DBs used by tests

Do **not** put probe tables into BuildingBlocks production.

## Bounded future wave plan (analyze only — do not implement)

| Wave | Scope |
|---|---|
| **W1** | Extract/recreate PlatformProbe types under Host.Tests Fixtures; point Outbox/Persistence/MassTransit tests at fixture; prove parity without changing production behavior |
| **W2** | Detach production: remove Host ProjectReference + `ToobaModuleComposition` entry + MigrationRunner descriptor; stop registering production migrator; **keep deployed schema**; update Messaging forbid only if needed (likely keep) |
| **W3** | Structure/cleanup: remove or archive `Modules/PlatformProbe` production project; fix `Tooba.slnx`; retire stale copies; adjust `ModuleSchemaMigrationOrder` / SoT migrationOrder narrative carefully |
| **W4** | Certify production absence + durable guards + SoT/recovery checkpoint (`USER_REVIEW_…`); no COMPLETE_REFERENCE_PATTERN module claim |

## Explicit non-goals for future waves

- No DROP SCHEMA by default
- No ceremonial five-project module scaffold
- No frontend / business schema changes
- No automatic start of W1 from this analysis commit
