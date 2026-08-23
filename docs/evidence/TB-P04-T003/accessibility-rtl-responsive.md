# TB-P04-T003 — Accessibility, RTL, responsive

## Keyboard baseline

- Toolbar and pagination are native buttons/inputs (Tab).
- Sort is a header button (Enter/Space).
- Filters and column chooser live in Design System Drawer (focus + Escape from T002).
- Row selection uses labeled checkboxes.
- Reorder: Alt+Arrow on header, plus move buttons in the column drawer.
- Resize: range input with accessible name `{header} width`.

Not claimed: full ARIA `role="grid"` keyboard specification.

## RTL / LTR

- ThemeProvider sets `html.dir`.
- Sticky sides map start/end to `inline-start` / `inline-end`.
- Table uses `text-start`, `start-0`, `inset-inline-*`.
- Showcase toggles direction on the Design System page.

## Responsive

- Desktop: `overflow-x-auto`; sticky key column remains visible.
- `max-width: 767px`: card list for the current page; filters remain in the drawer.
- Do not shrink operational type to Shopeiva compact-admin size; default density is comfortable.

## Visual capture

Browser screenshot tooling was not used in this execution. Capture RTL/LTR desktop, mobile cards, filters, column chooser, saved view, bulk selection, and sticky header on `/design-system` during Architect visual review.
