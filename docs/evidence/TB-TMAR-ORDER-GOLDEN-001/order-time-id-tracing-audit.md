# Order time, ID, and tracing audit

`CheckoutProcessManager` now receives canonical `IClock` and `IIdGenerator`; direct `DateTimeOffset.UtcNow` and `UuidV7.New()` were removed from that orchestration boundary with no fallback.

Residual direct clock/ID calls remain in Order Domain and Infrastructure (`CheckoutDirectory`, payment projections/bridges, and entity helpers). Each needs semantic review so deterministic Domain inputs are added without stylistic injection. Cross-module tracing coverage was not proven. Status: `PARTIAL_REPAIR`.
