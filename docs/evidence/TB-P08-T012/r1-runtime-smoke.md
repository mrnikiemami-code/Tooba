# TB-P08-T012-R1 — Runtime smoke

## Status

**PASS (focused)** — Host/FE healthy; Article EDIT pages return 200 with CKEditor wired in source.

## Checks

| Check | Result |
|-------|--------|
| Host `/health` | 200 |
| FE `/admin/content` (:3000/:3002) | 200 |
| Create Draft fa-IR via Admin API | OK |
| FE Article EDIT `?mode=edit` | 200 |
| Source: Article uses `ContentArticleEditor` / CKEditor 5 | OK |
| Source: no TipTap Article editor | OK (deleted) |
| Source: DAM via `onPickDamImage`, no CKBox/cloud upload | OK |
| Source: no `window.__` DAM hack | OK |
| FE source-assert tests | 9 pass |
| recovery-staleness.guard | 3 pass |

## Not fully browser-automated

Interactive CKEditor typing / DAM click / table insert / save-reload in a real browser session was not driven by automation in this Worker run. Covered by component wiring + source-assert + HTTP page 200. `USER_VISUAL_ACCEPTED=NO`.
