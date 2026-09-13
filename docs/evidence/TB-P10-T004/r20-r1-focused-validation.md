# R20-R1 focused validation

Formatter/tests:

- 09m30s => `09:30`
- 59m59s => `59:59`
- 60m00s => `01:00:00`
- 23h59m => `23:59:00`
- 24h+ => `24:00:00` (not `1440`)
- accessible FA/EN labels
- expiry `shouldRefreshOnceAtZero` unchanged
- failed-payment mapping still uses server `holdEndsAt` (no timer invent)

Operational history:

- GATEWAY_REJECTED => FA ردشده توسط درگاه پرداخت
- GATEWAY_REJECTED => EN Rejected by payment gateway
- renderer uses `presentOperationalHistoryEntry`
- unknown `SOME_UNKNOWN_CODE` => human fallback
- human summaries unchanged

Regression: pending-payment, admin-reservation-cycle, reservation-policy-admin, admin-t007-history, recovery guard.

Result: 22 frontend + 4 recovery PASS. `git diff --check` clean (CRLF warning only).
