# tests-guards — TB-TMAR-HOST-OUTBOX-AMC-001

## Existing focused coverage

| Area | Coverage |
|---|---|
| Poll targets Marketplace/SingleStore | OutboxFoundationTests |
| Worker FromOutbox reconstruction | OutboxFoundationTests |
| Error sanitizer | OutboxFoundationTests |
| Serializer/correlation | OutboxFoundationTests |
| Disabled outbox in host fixtures | AuthSecurityHttpTests / ErrorContractTests |
| Postgres claim SKIP LOCKED / tenant isolation / retry / DL / Marketplace vs store | OutboxPostgresTests |
| Dispatcher services harness | OutboxTestSupport / OutboxTestPlatform |
| Worker seam contract presence | HostRootGlobalBoundariesGuardTests / HostCartResidualGuardTests |

## Gaps (for W1/W2)

- Path/namespace/file-cohesion durable guard (none for Host/Outbox)
- Options invalid values / ValidateOnStart
- Explicit OCE during message processing must not MarkRetry
- Persian FromPollTarget exception hygiene
- Dedicated HostObservability-style `HostOutboxAmcW1GuardTests` (to add in W1)

No solution-wide tests required for Analyze.
