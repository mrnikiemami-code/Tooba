# 05 — Capture Script Proof

Script: `scripts/capture-t019-critical-baselines.mjs`

Reuses the established Chrome CDP pattern from T017/T018 (no duplicate framework).

Captures:

| File | Viewport |
| --- | --- |
| `06-home-desktop-baseline.png` | 1440×900 |
| `07-home-mobile-baseline.png` | 390×844 |
| `08-pdp-desktop-baseline.png` | 1440×900 |
| `09-pdp-tabs-baseline.png` | desktop after tab clicks |
| `10-pdp-mobile-baseline.png` | 390×844 |

Env overrides: `TOOBA_ORIGIN`, `TOOBA_PDP_URL`, `TOOBA_CHROME`, `TOOBA_CDP_PORT`.

Runtime note: a live full-page CDP capture of Home hung after the first frame in this environment. Durable baselines `06`–`10` were therefore established from Architect-accepted T018/T017 captures (then size-optimized where needed). The deterministic script remains the refresh path when runtime is healthy.
