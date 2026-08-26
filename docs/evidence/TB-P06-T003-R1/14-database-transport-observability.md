# 14 — Database transport observability (TB-P06-T003-R1)

| Signal | Implementation |
|---|---|
| MassTransit ActivitySource | registered in `Program.cs` tracing |
| Publish span | `tooba.messaging.publish` |
| Consume span | `tooba.messaging.consume` |
| Outbox span | `tooba.outbox.dispatch` |
| Metrics | `tooba.messaging.published/consumed`, outbox retries/dead-letters |
| Logs | structured JSON; event type, tenant, edition, event id |
| Message/correlation IDs | `EventId` as CorrelationId on publish; trace tags on consume |

No secret payload logging. Queue depth scans not added (expensive).
