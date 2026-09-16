# Anti-Pattern Scan — TB-P10-T022-R10

| Anti-pattern | Status |
|--------------|--------|
| Separate geometric composition preview | REJECTED — `page-workspace-preview` / `VariantPreviewCanvas` removed from editor |
| Two independent order models | REJECTED — single `persistOrder` → `reorderAdminLandingSections` / `SortOrder` |
| Client-only drag without persistence | REJECTED — drop calls same reorder API; rollback on failure |
| Divergent arrow persistence | REJECTED — arrows use `persistOrder` |
| Silent Template append | REJECTED — replace composition + confirm dialog |
| Template Apply mutating Catalog | REJECTED — section rows only |
| Deleting Section deleting business data | REJECTED — deletes Page Section config only |
| Disabled Section losing config | REJECTED — `SetEnabled` preserves config/order |
| Hardcoded first-item hacks | REJECTED — index-based insert/reorder |
| Polling/timeouts | REJECTED — none added |
| Duplicate Home/Landing editors | REJECTED — shared `AdminLandingPageComposer` |

Scan date: TB-P10-T022-R10 implementation.
