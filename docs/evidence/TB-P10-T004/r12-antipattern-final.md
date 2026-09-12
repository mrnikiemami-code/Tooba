# TB-P10-T004-R12 — Anti-pattern final

| Pattern | Result |
| --- | --- |
| Magic TTL | CLEAN — Settings |
| Polling hacks | CLEAN — gated poll + hard stop |
| Duplicated reacquire | CLEAN — EnsureOrderSupply |
| Released → Held | CLEAN — new reservation ids on retry/recover (C/G/H/O) |
| First-seller | CLEAN |
| Raw reservation errors | CLEAN — business FA copy |
| Client-trusted totals | CLEAN |
| N+1 list | CLEAN — grid query |
| Hidden exception swallow | KEEP R10 Paid-keep on Ensure fail (lost-money lock) |
| Test-only workarounds | CLEAN |
| Frontend capabilities | CLEAN |

CLEAN.
