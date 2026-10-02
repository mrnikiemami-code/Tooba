# options-validation — TB-TMAR-HOST-OUTBOX-AMC-001

## OutboxHostOptions defaults

| Property | Default |
|---|---|
| Enabled | true |
| PollIntervalSeconds | 2 |
| BatchSize | 20 |
| RetryBaseDelaySeconds | 2 |
| MaxAttempts | 5 |
| LockSeconds | 30 |

## Binding

`Configure<OutboxHostOptions>(GetSection("Tooba:Outbox"))` — **no** `ValidateOnStart`, **no** `IValidateOptions<OutboxHostOptions>`.

## Runtime clamping today

- HostedService: `PollIntervalSeconds` → `Math.Max(1, …)` for Delay only.
- Retry path: `Math.Max(delay, RetryBaseDelaySeconds)`.
- BatchSize / MaxAttempts / LockSeconds: passed through to store **without** Host validation (zero/negative possible).

## Classification

`OPTIONS_VALIDATOR_ABSENT_DEBT` — W1 should add fail-closed startup validator (positive BatchSize/MaxAttempts/LockSeconds/PollInterval/RetryBase) mirroring Messaging/Cache pattern.
