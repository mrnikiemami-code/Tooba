# cancellation-safety — TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT

Certified: requested `OperationCanceledException` rethrows at target, claim, and message paths when the task token is canceled.

- Does not increment tenant/claim failure metrics on requested OCE
- Does not MarkRetry / MarkDeadLetter on requested OCE
- HostedService breaks on DispatchOnce and Delay OCE with stoppingToken
- Non-cancellation isolation preserved (target/module/message)

Label: `REQUESTED_OCE_PROPAGATES_NO_RETRY_DEADLETTER_CERTIFIED`
