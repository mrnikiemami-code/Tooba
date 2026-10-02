# options-validation — TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT

`OutboxHostOptionsValidator` + Program `ValidateOnStart` certified.

Fail-fast when any of PollIntervalSeconds / BatchSize / RetryBaseDelaySeconds / MaxAttempts / LockSeconds <= 0.

Defaults certified: Enabled=true, Poll=2, Batch=20, RetryBase=2, MaxAttempts=5, Lock=30.

Section `Tooba:Outbox` unchanged. Operator prose only; user-facing presentation ZERO.
