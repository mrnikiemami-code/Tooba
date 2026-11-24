# IMPECCABLE REVIEW (R1) — Offer workspace redesign

Scope: `mockups/offer-workspace-r1.html` (4 views + picker + mobile 360). Mode: Operate. Independent pass.

## Blocking check vs architect's mandatory items
- Table = order-grid pattern, no KPI cards, correct columns (seller removed in seller table): PASS.
- Pro pickers (search/filter/server-paging, 1M/10k scale, product image/brand/variant, attribute chips, no auto-select,
  locked after create): PASS.
- Multi-currency price rows (currency/market/channel/validity/status/type, add/edit-amount/retire, currency locked once
  saved, IRR no decimals): PASS.
- Discounts section (all fields + preview, active not economically editable, Promotion-owned, add flagged): PASS.
- Multi-warehouse inventory + total + add-position flagged: PASS.
- Quantity/UoM (unit read-only, 0–6 decimals/step/rounding, min/max/clear, unit shown): PASS.
- Shipping (responsibility incl unset, published/allowed service, read-only scope/schedule, editions, count not final):
  PASS.
- Returns (3 modes, days bounds, override): PASS.
- Lifecycle (statuses incl archived no-reactivate, independent stock, history, per-status actions, unsaved guard,
  per-section error): PASS.
- No technical domain names shown; not the long form; single action bar: PASS.

## Rubric
Hierarchy 5 · Density/efficiency 5 · Consistency with order grid 5 · RTL/Persian/Jalali 5 · Mobile 4.5 ·
A11y 4.5 · Performance intent 5.

## Non-blocking follow-ups (apply at implementation)
- ARIA roles on segmented controls/pickers; ≥44px touch targets; virtualized picker list for 1M rows; persisted advanced
  filter as saved view; i18n copy lock pass.
Verdict: APPROVED for owner design review (no blocking defects). Feature-flagged items (add-warehouse, add-discount)
are honestly marked not-ready pending contract completion.
