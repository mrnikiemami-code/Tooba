# options-validation — TB-TMAR-HOST-OUTBOX-AMC-001-W1

`OutboxHostOptionsValidator` implements `IValidateOptions<OutboxHostOptions>`.

Fail-fast (<= 0) for:

- PollIntervalSeconds
- BatchSize
- RetryBaseDelaySeconds
- MaxAttempts
- LockSeconds

`Enabled=false` still validates numeric structure (consistent fail-fast).

Program registration:

- `AddOptions<OutboxHostOptions>().Bind(...Tooba:Outbox...).ValidateOnStart()`
- `AddSingleton<IValidateOptions<OutboxHostOptions>, OutboxHostOptionsValidator>()`

Section name unchanged: `Tooba:Outbox`.
