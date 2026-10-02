# outbox-health-boundaries — TB-TMAR-HOST-MESSAGING-AMC-001

## Outbox

- `OutboxDispatcher` (Host) polls/stores via Persistence contracts and calls `IIntegrationEventPublisher.PublishAsync`.
- Messaging owns transport publication only.
- Publisher exceptions surface to Outbox retry/orchestration (Outbox AMC not opened).
- No duplicate consumer retry authority inside publishers.

## Health

- Health CERT reads `MessagingHostOptions` + `IEnumerable<IBusControl>` only.
- Health does **not** import Messaging types by name beyond options (same Host namespace today).
- Classification: adjacent Host platform dependency **ACCEPTABLE**; Health CERT **PRESERVED**.
- No forbidden dependency into Messaging publisher internals.
