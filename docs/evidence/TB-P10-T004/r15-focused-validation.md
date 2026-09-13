# R15 focused validation

| Check | Result |
| --- | --- |
| ReservationCycleFoundationTests | 9/9 Passed (618 ms) |
| recovery guard | 4/4 (`CURRENT_TASK_ID=TB-P10-T004-R15`) |
| git diff --check | clean |

Covered: Cycle #1 + Initial TTL + snapshot, payment correlate same ExpiresAt, expiry close, Cycle #2 retry TTL, old cycle immutable, failed reacquire no number, max=3, retry-limit event, double start one cycle, paid/cancel close, projection side-effect-free, multi-line MIN policy, decimal 1.25, wiring/locks, anti-pattern scan.
