# validation — TB-TMAR-HOST-PERSISTENCE-AMC-001-W2-CERT

## Focused builds

- `Tooba.Host`
- `Tooba.Host.Tests`

## Focused tests

| Suite | Role |
| --- | --- |
| `HostPersistenceAmcCertGuardTests` | Durable CERT tree/boundary/SoT lock |
| `HostPersistenceAmcGuardTests` | Analyze allowlist preserved |
| `PersistenceAmcCertBehaviorTests` | Valid/blank/missing/blank-value/malformed/legacy-root + non-leak |
| `PersistenceFoundationTests.Missing_connection_reference_fails_closed` | Existing miss coverage |
| `TmarDurableGuardTests` | Recovery pointer / stop coherence |
| `HostOutboxAmcCertGuardTests` | Prior Outbox CERT preserved (spacing-tolerant SoT asserts) |

No solution-wide suite.
