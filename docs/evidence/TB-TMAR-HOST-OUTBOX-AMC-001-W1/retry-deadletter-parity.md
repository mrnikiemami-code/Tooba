# retry-deadletter-parity — TB-TMAR-HOST-OUTBOX-AMC-001-W1

Retry / dead-letter semantics preserved exactly:

- `AttemptCount >= MaxAttempts` => MarkDeadLetter
- else MarkRetry with `RetryBaseDelaySeconds * (1 << Math.Min(AttemptCount - 1, 8))`
- NodaTime SystemClock
- `OutboxErrorSanitizer.Sanitize(ex)` only (no raw Message / stack)

Focused tests cover retry and dead-letter paths under non-cancellation faults.
