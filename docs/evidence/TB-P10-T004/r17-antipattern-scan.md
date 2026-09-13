# R17 anti-pattern scan

| Pattern | Result |
| --- | --- |
| cycle == Payment Attempt | CLEAN — labels from cycle projection |
| frontend reconstructing history | CLEAN — history from server audit |
| per-row N+1 | CLEAN — GetProjectionsAsync on grids |
| raw enum rendering | CLEAN — StatusFa/ReasonFa only |
| raw GUID display | CLEAN — no CycleId; payment ref last-8 |
| admin timer extension | CLEAN — no تمدید/ExpiresAt editor |
| duplicated retry logic | CLEAN — Confirm remains existing path |
| local clock authority | CLEAN — remainingSecondsFromServer |
| per-second API polling | CLEAN — local interval only |
| first-cycle-only | CLEAN — Cycle #2 history kept |
| first-line-only shortage | CLEAN — all shortage lines |

CLEAN.
