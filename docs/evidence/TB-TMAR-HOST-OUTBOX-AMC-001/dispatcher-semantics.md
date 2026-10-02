# dispatcher-semantics — TB-TMAR-HOST-OUTBOX-AMC-001

## Responsibilities (single cohesive platform orchestrator)

1. Enumerate poll targets (`IOutboxPollTargetSource`)
2. Per target: resolve DB connection (`IDatabaseConnectionResolver`)
3. Per module registration: `ClaimAsync` via `IOutboxDispatcherStore`
4. Per message: resolve correlation, start Activity, deserialize, optional Metadata CorrelationId repair via reflection
5. Create async DI scope; assign commerce + store commerce; publish via `IIntegrationEventPublisher`
6. Mark processed / retry / dead-letter
7. Canonical counters: tenant_failures / retries / dead_letters / processed

## Failure isolation

| Layer | Behavior |
|---|---|
| Target poll | `catch (Exception)` → log ErrorType + TenantId; continue other targets |
| Module claim | `catch (Exception)` → log + continue other modules |
| Message processing | `catch (Exception)` → sanitize → dead-letter or retry; continue other messages |
| Hosted loop | OCE when stopping → break; other Exception → log ErrorType; Delay |

Isolation is intentional worker resilience (not HTTP fail-closed). Does **not** violate HTTP unknown-fault propagation.

## Attempt semantics

- Claim SQL increments `attempt_count` before return.
- Dead-letter when `AttemptCount >= MaxAttempts` (default 5) → up to 5 claim attempts.
- Retry backoff: `RetryBaseDelaySeconds * (1 << Min(AttemptCount-1, 8))`, floored at base; clock = NodaTime `SystemClock`.
- Consistent with store claim semantics (not off-by-one vs claim increment).

## Metadata reflection

If `Metadata` property exists and CorrelationId blank, sets `meta with { CorrelationId }`. Acceptable platform compatibility seam; reflection failure falls into message catch → retry/DL. Serializer ownership alternative = future debt, not Analyze blocker.
