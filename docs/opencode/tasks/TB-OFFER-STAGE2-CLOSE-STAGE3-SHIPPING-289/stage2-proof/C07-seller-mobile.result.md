# C07 Playwright (TB-289-R5) — seller + mobile, REAL run (2026-11-24)
- Installed @playwright/test@1.51.1 (lock ^1.51.1) + chromium build 1161 (authorized install).
- Ran offer-safe-edit.spec.cjs against running frontend (3000), locale fa-IR (RTL).
- Result: 7/7 PASS (REQ-01..REQ-07), including REQ-06 "mobile dirty form is reachable" (mobile viewport).
- Covers seller offer safe-edit UI + mobile reachability. Admin-panel UI subset is not present as a spec here -> admin C07 NOT covered by this run.
