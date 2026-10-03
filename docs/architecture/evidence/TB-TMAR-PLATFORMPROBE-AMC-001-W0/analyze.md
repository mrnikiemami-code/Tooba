# TB-TMAR-PLATFORMPROBE-AMC-001-W0 — Analyze

## Business capability

**NONE.** Source and architecture docs label PlatformProbe as disposable convention / persistence / outbox proof. No HTTP surface, no CQRS, no customer/admin/seller use-case.

## Responsibility classification

| Artifact | Classification |
|---|---|
| `PlatformProbeModule` | HOST_COMPOSITION_ROOT sample (`IToobaModule`) — not a business module root |
| `PlatformProbeDbContext` | PERSISTENCE (foundation probe schema) |
| `PlatformProbeRecord` | TEST_FIXTURE / FOUNDATION_PROBE entity (not DOMAIN_RULE business aggregate) |
| `PlatformProbePersistence.NewRecord` | FOUNDATION_PROBE factory |
| `PlatformProbeOutboxRegistration` | INTEGRATION_ADAPTER sample for `IOutboxModuleRegistration` |
| `ProbeRecordCreatedDomainEvent` | FOUNDATION_PROBE domain-event shape |
| `ProbeInternalNoteDomainEvent` | FOUNDATION_PROBE (proves non-translation) |
| `ProbeRecordCreatedIntegrationEvent` | FOUNDATION_PROBE integration contract sample |
| EF Migrations | PERSISTENCE history for probe schema |

## Ownership / structural anomalies

- Lives under `Modules/PlatformProbe/` on disk but **flat** `/Modules/` in `Tooba.slnx` (not `/Modules/PlatformProbe/`).
- Infrastructure-only; creating Domain/Application/Contracts/Endpoints for symmetry would be ceremonial and incorrect.
- Host + MigrationRunner still treat it as a production-registered module.

## Production necessity answers

| Question | Answer |
|---|---|
| Any production request/use-case dependent? | **No** |
| Any production business module dependent? | **No** |
| Any production data flow on probe events? | **No** (probe-only writes) |
| Outbox dispatcher dependent on PlatformProbe specifically? | **No** — generic `IOutboxModuleRegistration` / interceptor; probe is one registration |
| Required for application startup? | **Only because registered** in `ToobaModuleComposition` |
| Removal changes customer/admin/seller behavior? | **No** |

Separate:

- **GENERIC FOUNDATION DEPENDENCY:** `IOutboxModuleRegistration`, `OutboxSaveChangesInterceptor`, `IModuleSchemaMigrator`, MassTransit transport schema rules.
- **PLATFORMPROBE-SPECIFIC DEPENDENCY:** Host ProjectReference, `PlatformProbeModule` in composition, MigrationRunner descriptor, order constant usage at registration, Host tests typing probe types.

## Four-skill applicability

### Analyze

- Business capability: **NONE**
- Ownership: **FOUNDATION_PROBE** currently misplaced in production module graph
- Structural anomalies: flat slnx grouping; Infrastructure-only disposable sample

### Migrate

- Production evacuation: **REQUIRED**
- Bounded waves: see `recommended-disposition.md` (W1 fixture → W2 detach → W3 structure cleanup → W4 certify absence)

### Structure

- Final shape: **TEST_FIXTURE_ONLY** (not INTERNAL_ONLY production module under `/Modules/PlatformProbe/`)
- `/Modules/PlatformProbe/` should **not** remain as a production module after evacuation
- Structure handoff for future waves: **REQUIRED** (fixture location + slnx cleanup); Analyze does not claim READY_FOR_CERTIFY

### Certify

- `COMPLETE_REFERENCE_PATTERN` as a business module: **DOES NOT APPLY**
- Final target: production **ABSENCE** certification + durable guards (no Host reference, no composition entry, no MigrationRunner descriptor) while schema may remain orphaned
- Optional: certify test fixture location separately as test-support, not ARCH-COMPLETE-002 module cert

## Structure-Handoff-State

`REQUIRED` for future migrate/structure waves; **not** executed in W0.
