# TB-P10-T004-R10 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| Magic timeout values | CLEAN — Settings resolver + clamp |
| Frontend timers deciding expiry | CLEAN — no setInterval on customer order page |
| Order hard-delete | CLEAN |
| Auto-cancel Succeeded/review | CLEAN — ExpireUnpaidTimeout no-op |
| Duplicate Order on retry | CLEAN — same PaymentId/CheckoutId |
| Resurrect Released | CLEAN — EnsureOrderSupply new hold |
| Raw config keys in UI | CLEAN |
| Raw reservation errors | CLEAN — business copy |
| Duplicated reacquire | CLEAN — EnsureOrderSupply only |
| First-item shortcuts | CLEAN |
| Polling workaround | CLEAN — Expired stops payment poll |
