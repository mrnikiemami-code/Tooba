# 07 — Outbox operations proof (TB-P06-T004)

## Canonical path

Module transactional outbox → `OutboxDispatcherHostedService` → `IIntegrationEventPublisher` → MassTransit SQL Transport (when messaging enabled) or in-process test double.

MassTransit module EF outbox is **not** added; per-module tables + Host dispatcher remain canonical.

## Pending resume after restart

- Unprocessed rows: `processed_at IS NULL`, not dead-lettered, eligible by `next_attempt_at` / `locked_until`.
- Dispatcher starts automatically with Host when `Tooba:Outbox:Enabled=true`.
- **Proof:** `OutboxPostgresTests.Dispatcher_publishes_marks_processed_and_isolates_tenants`.

## Failure → retry → dead letter

- Handler failure schedules retry with exponential backoff.
- After max attempts → `dead_lettered_at` + sanitized error (no credential leak).
- **Proof:** `OutboxPostgresTests.Handler_failure_retries_then_dead_letters`.

## No duplicate delivery from concurrent claim

- **Proof:** `OutboxPostgresTests.Concurrent_claim_does_not_deliver_the_same_row_twice`.

## Stuck / visibility

| State | Visible via |
|---|---|
| Pending | Row: `processed_at` null, `dead_lettered_at` null |
| In-flight | `locked_until` in future |
| Retry waiting | `next_attempt_at` in future |
| Dead letter | `dead_lettered_at` set |
| Metrics | `tooba.outbox.processed`, `.retries`, `.dead_letters`, `.tenant_failures` |

## Retention / cleanup

**Not implemented in TB-P06-T004.** Processed and dead-lettered rows remain in module outbox tables until a future retention job or manual DBA cleanup. Operators should plan periodic archival/purge by `processed_at` / `dead_lettered_at` age.

## Safe replay

- Pending rows: fix root cause; dispatcher auto-resumes.
- Dead-letter rows: manual operator intervention required (no auto-replay endpoint in Host).
