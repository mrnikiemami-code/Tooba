# Order conflict resolution audit

The unique-cart winner read is a bounded post-conflict visibility algorithm, not an unbounded poll. This pass encapsulated its policy as 10 attempts with a 20 ms linear delay, documented the commit-visibility reason, retained cancellation, and made the maximum delay explicit (900 ms).

Typed `CheckoutConflictException` replaced string identity. Deterministic policy tests are still missing, so the final criterion `DETERMINISTIC_AND_BOUNDED` is not accepted until R1 adds characterization for winner-found, exhaustion, and cancellation.
