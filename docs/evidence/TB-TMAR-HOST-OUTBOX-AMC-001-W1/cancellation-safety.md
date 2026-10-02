# cancellation-safety — TB-TMAR-HOST-OUTBOX-AMC-001-W1

Requested `OperationCanceledException` when `cancellationToken.IsCancellationRequested` rethrows at:

1. target loop around `DispatchTargetAsync`
2. module claim path
3. per-message publish/process path

Invariants proven by `OutboxAmcW1BehaviorTests`:

- claim OCE propagates; MarkRetry/MarkDeadLetter not called
- publish OCE propagates; MarkRetry/MarkDeadLetter/MarkProcessed not called
- non-cancellation publish failure still retries
- max-attempt failure still dead-letters
- claim/module and target non-cancellation failures remain isolated

HostedService OCE break semantics preserved (DispatchOnce + Delay).
