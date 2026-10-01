# Single-flight

MemoryToobaCache uses reference-counted InflightSlot per key.
AcquireInflight / ReleaseInflight; Gate never disposed.
CurrentCount==1 dictionary remove path removed.
Concurrent same-key GetOrCreate executes factory exactly once.
Cancelled waiter decrements RefCount without releasing a gate it never held.
Failed factory does not store; next call retries safely.
