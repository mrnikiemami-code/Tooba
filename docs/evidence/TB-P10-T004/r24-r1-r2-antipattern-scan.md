# TB-P10-T004-R24-R1-R2 — Anti-pattern scan

| Anti-pattern | Result |
| --- | --- |
| surface-specific recipient precedence | CLEAN — no product precedence change |
| frontend guessed split | CLEAN |
| stale AddressBook reread after commit | CLEAN — pages show committed snapshot |
| auth hacks added only for screenshot capture | CLEAN — customer OTP UI; Admin uses existing `prepareAdminDevActor` storage key |
| shared browser cookie contamination | CLEAN — two Playwright contexts |
| duplicated formatter | CLEAN — no new formatter |

Scan: CLEAN.
