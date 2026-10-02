# cancellation-retry-deadletter — TB-TMAR-HOST-OUTBOX-AMC-001

## Cancellation safety — MATERIAL DEBT

| Path | OCE behavior |
|---|---|
| Hosted `ExecuteAsync` dispatch | `catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)` → break (**safe**) |
| Hosted Delay | same (**safe**) |
| Target poll catch | `catch (Exception)` — **swallows OCE** during shutdown if thrown from DispatchTargetAsync |
| Module claim catch | **swallows OCE** |
| Message processing catch | **converts OCE into retry/dead-letter** via MarkRetry/MarkDeadLetter |

Token is threaded to ClaimAsync / PublishAsync / Mark* / Delay.

**Classification:** `CANCELLATION_SAFETY_DEBT_MESSAGE_AND_CLAIM_CATCH` — W1 should rethrow `OperationCanceledException` when `cancellationToken.IsCancellationRequested` before sanitizing/retrying.

## Retry / dead-letter

- Sanitized LastError via `OutboxErrorSanitizer.Sanitize` (Persistence-owned).
- MaxAttempts / RetryBaseDelaySeconds / LockSeconds from options.
- Overflow: shift capped at 8.

## Broad exception policy

Worker-specific isolation: unknown faults logged by type name and continued. Acceptable for background poller; **not** an HTTP swallow violation. Cancellation exemption required as above.
