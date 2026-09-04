# TB-P08-T012-R1 — Runtime smoke

| Check | Result |
|---|---|
| Focused FE tests (article admin/media/crud) | PASS (18) |
| Recovery staleness guard | PASS (3) |
| `git diff --check` | PASS (CRLF normalize warnings only) |
| Host `:5088` | Up (`/v1/admin/content/articles` → 401 unauthenticated) |
| FE `:3000` | Up (`/fa` 200, `/fa/admin/content` 200, `/fa/admin/content/articles/new` 200) |
| Interactive Article EDIT CKEditor (fa/en format, DAM, table, save/reload) | **BLOCKED** — browser MCP tab unavailable; no authenticated article EDIT session exercised in this Worker run |

Code wiring: client-only `dynamic(..., { ssr: false })` on Content tab EDIT. Static tests assert CKEditor (not TipTap) + DAM callback. Interactive CMS smoke deferred; do not claim visual ACCEPT (`USER_VISUAL_ACCEPTED=NO`).
