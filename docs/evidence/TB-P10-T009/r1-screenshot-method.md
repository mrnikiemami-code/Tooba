# TB-P10-T009-R1 — Screenshot method

Reused the project Playwright install at `.tmp-r24r1r2-pw` (same helper used by TB-P10-T004 visual runtimes).

Script: `docs/evidence/TB-P10-T009/r1-capture.mjs`

- Chromium headless, viewport 1440×1100
- Writes directly to `docs/evidence/TB-P10-T009/screenshots/`
- Deterministic filenames
- Files remain after the Worker run
- No Cursor browser temp artifacts
- No manual user capture

Raw log: `r1-runtime-raw.json` (`ok: true`, 15 files).
