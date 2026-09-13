# R15 runtime A–J

Server clock used in tests: `2026-09-13T10:00:00Z` (and offsets). Proven in `ReservationCycleFoundationTests` against the real directory/policy types.

| Case | Result | Server time |
| --- | --- | --- |
| A. Initial cycle | PASS Cycle #1 Active, Initial 10m, snapshot stored | 2026-09-13T10:00:00Z |
| B. Two failed payments inside Cycle #1 | PASS same cycle, ExpiresAt still 10:10 | 10:00 correlate; ExpiresAt 10:10 |
| C. Cycle #1 expiry | PASS Closed Expired + event | 10:10 |
| D. Retry with stock → Cycle #2 | PASS #2, Retry 5m, #1 immutable Expired | 10:11–10:16 |
| E. Retry without stock | PASS ReacquireFailed event, created count unchanged, no Active | 10:23 |
| F. Max cycle limit | PASS #3 then RetryLimitReached, no #4 | 10:17–10:24 |
| G. Payment success → CommittedPaid | PASS | +2m |
| H. Manual initial/review/confirm | PASS same CycleNumber on review transition; confirm = CommittedPaid | domain + restore/paid paths |
| I. Multi-line mixed policy | PASS 3m / 2m / max 2 | resolver |
| J. Decimal 1.25 | PASS numeric quantity, no float | domain hold |

No TB-P10-T005. No FE countdown authority.
