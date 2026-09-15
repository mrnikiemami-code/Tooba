# Mobile + a11y essentials — TB-P10-T022

## Mobile (system-owned contracts)

- Every selectable Variant retains desktop/tablet/mobile columns, height, itemVisible in `RESPONSIVE_CONTRACTS`.
- Completeness guard asserts mobile fields present.
- ExtraLarge size presets remain bounded by code-owned mobile mapping (LOCK-SF-241 / LOCK-SF-248).
- No Admin mobile/breakpoint controls.

## A11y essentials

- Section headings retain `aria-labelledby` where established on home blocks.
- Banner missing-media uses decorative `aria-hidden` icon + text label.
- Empty states expose readable text (not icon-only).
- Template miniatures marked `aria-hidden`.
- RTL alignment preserved; no LTR-only technical labels in Admin.

## Parent runtime capture

Parent must capture mobile screenshots listed in `screenshots/README.md` on FE :3000.
