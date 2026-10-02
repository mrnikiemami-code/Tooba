# retry-deadletter — TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT

Certified exact semantics:

- AttemptCount vs MaxAttempts threshold
- RetryBaseDelaySeconds * (1 << Min(AttemptCount-1, 8))
- NodaTime SystemClock
- OutboxErrorSanitizer.Sanitize for durable last-error
- no raw exception.Message / stack / message classification
