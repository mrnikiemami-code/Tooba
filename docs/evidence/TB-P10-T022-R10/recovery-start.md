# Recovery Start — TB-P10-T022-R10

- Branch: `main`
- Expected previous HEAD: `94391f5d7353f44c94b71c72c74151d680df6fee`
- Local HEAD: `94391f5d7353f44c94b71c72c74151d680df6fee`
- origin/main: `94391f5d7353f44c94b71c72c74151d680df6fee` (match)
- Tracked tree: clean vs HEAD
- Unrelated preserved: `stash@{0}` `unrelated-pre-r10` holds `package.json` / `package-lock.json` (`server-only` only) — not popped, not committed
- Unrelated local `?? .tmp-*` / scratch artifacts preserved (not staged)
- Locks preserved: LOCK-SF-001…327
- Appearance YES / Builder NO (carry forward)
- Current Task: TB-P10-T022-R10 — Store Page Editor Workspace

## Preflight audit

| Area | Finding |
|------|---------|
| Store Page create flow | `/admin/landing-pages/new` → blank or template start |
| Template selection | `AdminTemplateSelectionWorkspace` + «استفاده از این قالب» |
| Use Template (pre-R10) | Confirmed template then meta save; sections only on save |
| Page Draft persistence | Catalog `StoreLandingPage` + sections API |
| Section order | `SortOrder` 0..n-1 + `PUT …/sections/reorder` |
| Geometric composition preview | Present (`page-workspace-preview` / `VariantPreviewCanvas`) — **to remove** |
| Section list | Arrows + enable/disable + delete; append-only add |
| Drag/drop | None for sections (HTML5 used elsewhere) |
| Unexpected tracked divergence | None → no RECOVERY_CONFLICT |

No RECOVERY_CONFLICT.
