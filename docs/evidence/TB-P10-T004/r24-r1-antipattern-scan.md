# TB-P10-T004-R24-R1 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| Cart line data in auth Session | CLEAN |
| Frontend-only cart persistence as SoT | CLEAN (session holds ids only) |
| localStorage Cart SoT | CLEAN |
| Duplicate storefront headers | CLEAN (one shell header) |
| Route-specific auth hacks | CLEAN |
| Manual badge invent | CLEAN (server itemCount) |
| Empty auth cart shadowing merge | REPAIRED |
| Merge on every navigation | CLEAN (guestSecret gate) |
| Optimistic cart clear | CLEAN |
| First/Last concatenated as sole storage | CLEAN (independent columns + composed display) |
| Guess FullName split | CLEAN |
| Duplicate customer panel | CLEAN |
| UI-only logout | CLEAN |
| Polling/retry hacks | CLEAN |

Scan: CLEAN.
