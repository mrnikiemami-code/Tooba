# Evidence — TB-P06-T017

**Stories End-to-End — Real Story Backend/Admin + exact Shopeiva Story UI**

| Field | Value |
|---|---|
| Task-ID | `TB-P06-T017` |
| Bridge UUID | `43fff592-fb07-476a-9485-c8a526d79a13` |
| Predecessor | `9b4ff5c26981aa876565a6812da91bebafec169d` |
| Worker / Channel | `tooba-worker-01` / `tooba-main` |
| Commit target | `feat connect live stories to Shopeiva UI [TB-P06-T017]` |
| May report readiness | `STORY_LIVE_WITH_EXACT_SHOPEIVA_UI` (NOT `PRODUCT_FULLY_READY`) |

## Files

| # | File | Topic |
|---|---|---|
| 01 | `01-runtime-before-stories.md` | Baseline before live stories |
| 02 | `02-shopeiva-story-source-map.md` | External Shopeiva source paths |
| 03 | `03-story-domain-model.md` | Statuses, fields, CTA rules |
| 04 | `04-public-api-contract.md` | `GET /v1/storefront/stories` |
| 05 | `05-admin-api-contract.md` | Admin CRUD / enable / schedule / items |
| 06 | `06-seed-and-lifecycle-proof.md` | StoreAlpha seed + visibility |
| 07 | `07-host-registration-and-migration.md` | Module, migrate, endpoints |
| 08 | `08-frontend-live-binding.md` | Live API; fake data removed |
| 09 | `09-shopeiva-ui-port-notes.md` | Exact chrome; AddStory omitted |
| 10 | `10-locale-and-composition-compatibility.md` | Locale filter + composition |
| 11 | `11-admin-ui-proof.md` | `/admin/stories` DataGrid |
| 12 | `12-backend-tests-proof.md` | `StoryFoundationTests` |
| 13 | `13-frontend-tests-proof.md` | `story-api.test` + home guard |
| 14 | `14-browser-side-by-side.md` | Captures 01–04 |
| 15 | `15-unsafe-cta-rejection.md` | `javascript:` / forbidden schemes |
| 16 | `16-fake-data-removal-audit.md` | `STORY_IMAGES` gone from UI |
| 17 | `17-final-validation.md` | Restore / build / test placeholders |
| 18 | `18-final-runtime.md` | Host / FE / Shopeiva probes |
| 19 | `19-sot-update.md` | PROJECT-STATE / ROADMAP notes |
| 20 | `20-zero-redesign-declaration.md` | No visual redesign |
| 21 | `21-acceptance-checklist.md` | Acceptance matrix |
| 22 | `22-worker-result-draft.md` | Draft `BEGIN_TOOBA_WORKER_RESULT` |

## Artifacts

- Proof script: `scripts/prove-t06-t017-stories.mjs`
- Machine proof: `_acceptance-proof.json` (written by proof script)
- Browser captures: `captures/` (`01`–`04` PNGs from proof script)
