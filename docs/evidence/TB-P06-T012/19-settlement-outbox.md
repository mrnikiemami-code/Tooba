# 19 — Settlement Outbox

Task: `TB-P06-T012`

`SettlementOutboxRegistration` publishes after commit only (`settlement.entry.posted.v1`, payout events). No publish-before-commit. MassTransit SQL transport in dev when enabled.

See `SettlementDirectory` save + outbox interceptor on `SettlementDbContext`.
