# R19 anti-pattern scan

CLEAN after commit-time category hold fix.

| Pattern | Result |
| --- | --- |
| Timer reset hacks | CLEAN; correlate does not rewrite ExpiresAt |
| Timeout magic constants in FE | CLEAN |
| Storefront polling | CLEAN; local countdown only |
| FE retries / EnsureOrderSupply | CLEAN; backend coordinator only |
| Duplicate EnsureOrderSupply | CLEAN; single composer |
| Resurrect Released | CLEAN |
| Cycle from Payment Attempts | CLEAN |
| Settings rewrite history | CLEAN |
| First-item/first-seller | CLEAN (R11) |
| Raw internal errors in UX | CLEAN; Status.ToString() stored internally only |
| Lifecycle error suppression | CLEAN |
| Test-only DB bypass of domain | CLEAN; directory/domain used |
| N+1 grids/pending | CLEAN |
| FE status derivation / precedence | CLEAN |
| Category ignored at commit hold | FIXED in R19 |

No new lock. LOCK-SF-001…102 sufficient.
