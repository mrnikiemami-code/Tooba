# 14 — Background integration tests (TB-P06-T004)

## Test infrastructure

- **Framework:** xUnit + Testcontainers PostgreSQL 16 (`postgres:16-alpine`)
- **Collection:** `[Collection("PostgresSerial")]` — serializes container-heavy tests
- **Skip:** Tests skip gracefully when Docker unavailable (`SkippableFact`)

## OutboxPostgresTests

| Test | Covers |
|---|---|
| `Same_transaction_writes_outbox_and_rollback_leaves_none` | Transactional outbox write |
| `Dispatcher_publishes_marks_processed_and_isolates_tenants` | Successful dispatch + tenant isolation |
| `Handler_failure_retries_then_dead_letters` | Transient retry → terminal dead-letter |
| `Concurrent_claim_does_not_deliver_the_same_row_twice` | Multi-instance claim safety (SKIP LOCKED) |
| `Worker_handler_sees_message_tenant_not_host_header` | Worker context from message, not host |
| `Marketplace_dispatcher_only_reads_marketplace_database` | Edition DB boundary |
| `Expired_lock_is_reclaimed_after_lease_elapses` | Stuck work / lease recovery |

## CartExpiryPostgresTests

| Test | Covers |
|---|---|
| `Duplicate_expiry_trigger_is_idempotent` | Duplicate trigger safe |
| `Concurrent_claim_processes_each_due_cart_once` | Multi-instance cart expiry |

## Related (messaging layer)

- `MassTransitPostgresTests` — real SQL Transport consumer delivery (TB-P06-T003-R1 lineage)
- `PaymentFoundationTests` — order `payment_inbox` dedup

## Not covered in dedicated Postgres tests (this task)

- Graceful shutdown timing (covered by design review + `stoppingToken` code paths)
- Authorization schema bootstrap (integration tests in authorization foundation suite)

## Run command

```bash
dotnet test src/backend/Tooba.slnx --filter "FullyQualifiedName~OutboxPostgresTests|FullyQualifiedName~CartExpiryPostgresTests"
```
