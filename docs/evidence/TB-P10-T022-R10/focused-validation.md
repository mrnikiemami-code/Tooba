# Focused Validation — TB-P10-T022-R10

| Check | Result |
|-------|--------|
| Template Apply materializes all sections | PASS — `replaceAdminLandingComposition` + Fashion payloads; runtime landed with section cards |
| Exact order preserved | PASS — payload variant order matches template presets; Host SortOrder 0..n-1 |
| Repeat apply no silent duplicate | PASS — `template-replace-confirm` dialog; cancel leaves composition |
| Arrow reorder persists | PASS — `persistOrder` → `PUT …/sections/reorder` |
| Drag/drop same canonical persistence | PASS — HTML5 handle → same `persistOrder` |
| Insert requested index | PASS — `InsertAt` on POST + insert affordances |
| Delete compacts order | PASS — Host delete + renumber; FE reload |
| Disable preserves config/order; excludes published | PASS — existing `SetEnabled` + `publicOnly` filter |
| Re-enable restores | PASS — toggle path |
| Catalog unaffected | PASS — composition replace only touches page sections |
| Home/Landing share editor | PASS — shared `AdminLandingPageComposer` |
| critical-storefront | PASS |
| recovery-staleness | PASS — SoT updated; no R11/T023 |
| git diff --check | run at commit |

## Automated

- FE: `admin-store-pages-r10.guard.test.ts` + R9/R2 guards — 23 pass
- Host: `StoreLandingPageSectionTests` — 6 pass (incl. InsertAt + ReplaceComposition)
- `npm run test:critical-storefront` — pass
