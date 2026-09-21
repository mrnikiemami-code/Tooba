# Focused validation — TB-TMAR-FND-OBSERR-001-R4

| Suite | Result |
| --- | --- |
| `Tooba.BuildingBlocks.Tests` | Passed 36, failed 0, skipped 0 |
| `Tooba.Offer.Tests` (includes architecture guards, topology, catalog, localization) | Passed 57, failed 0, skipped 0 |
| Host `ErrorContractTests` + `CorrelationRuntimeTests` + `PlatformExceptionMapperTests` after `PostgresSerial` | Passed 7, failed 0, skipped 0 |

First parallel run of the two host factories failed one test with `Npgsql.PostgresException : XX000: tuple concurrently updated` inside `MassTransit.SqlTransport.PostgreSql.PostgresDatabaseMigrator.GrantAccess` during host start. Isolated, the same test passed. Both fixtures were added to existing collection `PostgresSerial` (`DisableParallelization = true`). No sleep, retry, skip, or assertion change.

Messaging and outbox correlation behavior is covered by `TracingAndMessagingFoundationTests` inside the 36 BuildingBlocks tests (publish resolve, consume scope restore, fallback activity only when current activity is absent, outbox correlation helper). Host SQL outbox suites were not re-run as a full postgres pack; the defect found was host-start migrator contention, not an outbox assertion.
