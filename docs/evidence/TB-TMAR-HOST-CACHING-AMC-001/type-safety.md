# Type safety

Chosen behavior: remove incompatible non-null entry + TypeMismatch telemetry + miss.
NullSentinel remains a successful negative-cache hit (value null, hit true).
`as T` cast path removed; `is T typed` required for typed hit.
