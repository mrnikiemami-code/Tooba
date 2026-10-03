# TB-TMAR-PLATFORMPROBE-AMC-001-W1 — Test consumer parity

## Repointed consumers

| File | Change |
|---|---|
| `OutboxFoundationTests.cs` | fixture registration/events |
| `OutboxPostgresTests.cs` | fixture DbContext/persistence/events |
| `OutboxTestSupport.cs` | factory + DI registration + handlers |
| `MassTransitPostgresTests.cs` | fixture persistence/outbox registration |
| `PostgresIntegrationTests.cs` | fixture persistence via helpers |
| `PersistenceFoundationTests.cs` | fixture DbContext/schema |

## Intentionally unchanged (production graph until W2)

- `ArchitectureBoundaryTests` (still asserts production `PlatformProbeModule`)
- `HostDevelopmentMigrationSeamGuardTests` (still inventories production migrator)

## Parity preserved

- schema `platform_probe`
- tables `probe_records` + module-local `outbox_messages`
- UUID v7 records, NodaTime `CreatedAt`
- created domain event → integration event; internal note does not translate
- event type `platform_probe.record_created.v1`
