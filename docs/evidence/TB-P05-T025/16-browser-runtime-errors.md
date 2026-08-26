# 16 — Browser runtime errors (TB-P05-T025)

CDP Log/Runtime capture during Home + PDP + critical surfaces (`scripts/capture-t025-runtime-evidence.mjs`).

Observed:

| Severity | Detail |
|---|---|
| Non-fatal | `favicon.ico` → 404 (twice) |
| Fatal console/hydration/500 | None after FE restart |
| Critical API failures on Home/PDP | None observed in smoke |

JSON dump: `16-browser-runtime-errors.json`

Note: running `next build` while `next dev` is alive recreates RSC/dev-cache 500s on `/`; final user preview requires a fresh `next dev` after build (documented in `17-runtime-final-user-preview.md`).
