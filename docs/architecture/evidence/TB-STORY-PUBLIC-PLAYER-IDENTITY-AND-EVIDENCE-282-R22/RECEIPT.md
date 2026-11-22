# RECEIPT — TB-STORY-R22

| Item | Value |
| --- | --- |
| Code commit | `1f93e3baa` |
| Message | `fix(story): reject stale locale responses and harden date proof` |
| Previous reviewed commit | `c19e36ee5` |
| Next build ID | `44a5DfAjiw3oXj00iX5q-` |
| Next `/fa` | 200 |
| Host PID | `32068` |
| Host DLL SHA256 | `FC7853809CD260BCCC7741408188E77C0949B927228979ED90EE1B251ACD80D8` |
| Host `/health/ready` | 200 |
| Host `/v1/storefront/stories` | 200 |
| Story/Home tests | `test:stories` 90/90; `test:home` 7/7 |
| TypeScript | `tsc --noEmit` 0 |
| Backend solution build | 0 errors |
| PostgreSQL tests | seller grid 1/1 (search/status/date ranges/blank/date+title sort/count/shop isolation); cover kind 1/1 |
| Browser proof script | `browser-proof.mjs` |
| Browser proof output | `browser-proof.json` |
| Browser story | `01a1f9d8-31af-7000-8e47-5da09230ff0d` |
| Browser save | `CTA-internal-164417` → `CTA-internal-164417-R22`; reload confirmed |
| Browser restore | `CTA-internal-164417`; reload confirmed |
| Migration/schema | none |
| Acceptance | PASS pending Architect/user review |
