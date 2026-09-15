# Runtime

Host `:5088` + FE `:3000`. Actor login + Playwright capture via `capture.mjs`.

`runtime-report.json` → **ok=true**, errors=[].

Scenarios covered:

| Step | Result |
|------|--------|
| A orders grid reference | pass |
| B/C page language create + edit fixed | pass |
| D workspace + template layout previews | pass |
| E product section wizard + resource selector (grid/advanced/selected) | pass |
| F banner two-slot + mosaic slot editors | pass |
| G article dynamic + manual grid | pass |
| H story display settings; no add-story authoring | pass |
| M admin theme isolation after store palette change | pass (adminPrimary stays `37 99 235`) |
| N seller panel after store palette change | pass |
| O storefront after store palette change | pass |

Screenshots: `docs/evidence/TB-P10-T022-R1/screenshots/` (22 PNGs).
