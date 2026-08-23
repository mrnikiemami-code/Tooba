# TB-P04-T004 — Workspace pattern catalog

| Pattern | Foundation | Notes |
| --- | --- | --- |
| WorkspaceShell | Done | Header, status, commands, sections, panels |
| Action hierarchy | Done | primary/secondary/destructive/overflow + permission/busy |
| Section navigation | Done | tabs desktop, select mobile, serializable section id |
| Summary + main + inspector | Done | inspector becomes Drawer on narrow |
| View/edit/dirty | Done | dirty `Set` + unsaved dialog |
| Conflict | Done | alert + reload seam |
| Permission/read-only | Done | `resolveWorkspaceAction` + shell flag |
| Activity / audit | Done | separate lists |
| Embedded Data Grid | Done | showcase related section |
| Master-detail return | Done | serialize `listQuery` + `selectedId` |
| Command state | Done | idle→confirming/submitting→succeeded/failed/conflicted |
| Destructive confirm | Done | showcase confirm panel |
| Empty/error/loading | Done | EmptyState / ErrorState / Spinner |
| Mobile | Done | `forceNarrow` + matchMedia |
| i18n seam | Done | fa/en catalogs; ErrorState `retryLabel` RESOLVED |
