# Runtime component coverage — TB-P10-T017-R4

Crawler: `src/frontend/scripts/storefront-theme-coverage.mjs` (R3 inventory + component inspect + pure-white island under PaletteTint).

Report: `docs/evidence/TB-P10-T017/r4-runtime-report.json` (`ok: true`).

| Run | Result |
| --- | --- |
| Neutral tooba-blue | 200, flags 0 |
| PaletteTint tooba-blue | 200, flags 0 + proof shots |
| PaletteTint wine-burgundy subset | 200, flags 0 |
| DarkOnly PaletteTint (home/PDP/customer) | 200, flags 0 |

Next compile timeouts on a first pass were retried with `domcontentloaded`; no unresolved pure-white component islands under PaletteTint. Ticket `[id]` remains layout-static. Canonical restored: tooba-blue / LightOnly / classic / Neutral.

Screenshots: `docs/evidence/TB-P10-T017/screenshots/r4/`.
