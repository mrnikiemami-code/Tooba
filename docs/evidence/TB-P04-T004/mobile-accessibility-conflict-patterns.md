# TB-P04-T004 — Mobile, accessibility, conflict

## Mobile

- Compact header + breadcrumb.
- Section switcher is a full-width select.
- Primary action sticky at the bottom.
- Inspector/activity open in a start-edge Drawer.
- Related Data Grid uses the Grid mobile card renderer from TB-P04-T003.

## Accessibility

- `header` / `nav` / `tablist` / `aside` / `alert` landmarks.
- Overlay Dialog/Drawer reuse T002 focus/Escape behavior.
- Controls keep `min-h-11`.
- Loading uses `aria-busy`.
- Unsaved navigation is a dialog, not a silent route change.

## Conflict

States: `stale-version`, `concurrent-edit`. UI offers reload/review. Command machine maps submit → conflicted. No silent overwrite.

## Visual capture

Screenshot tooling was not used in this execution. Architect visual review of `/design-system` Workspace patterns is still required.
