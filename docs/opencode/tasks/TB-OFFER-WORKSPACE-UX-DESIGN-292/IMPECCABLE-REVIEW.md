# IMPECCABLE REVIEW — Offer Workspace prototype (independent quality pass)

Scope: `mockups/offer-workspace.html` (4 views). Mode: Operate. Reviewed against: clarity, hierarchy, density,
consistency with Toba standard table, a11y/RTL, responsiveness, performance.

## Rubric (0–5) and findings
- Clarity of purpose: 5 — each view's task is obvious (list vs edit; admin vs seller).
- Information hierarchy: 5 — KPI → toolbar → grid; form sections carded with domain captions; sticky summary.
- Density/efficiency: 4.5 — dense rows, row actions, saved views reduce clicks. Minor: advanced filter drawer could
  persist last filter as a saved view (captured in DESIGN follow-up, not blocking).
- Consistency (standard table): 5 — mirrors `/admin/orders` toolbar (search/filter/columns/saved views/export/actions/
  pagination) + badges/Jalali/amounts; no bespoke grid.
- RTL/i18n/Persian: 5 — `dir=rtl`, Persian labels, Jalali dates, `fa-IR` grouping; Persian status words.
- Mobile/responsive: 4.5 — grid scrolls; form collapses to 1 col ≤960px; sticky actions. Minor: KPI strip wraps to 2
  cols (acceptable); could add a compact toolbar overflow menu on very small width (follow-up).
- Accessibility: 4.5 — semantic table/headers, labeled fields, visible focusable buttons; follow-up: ensure ARIA on
  toggles/column-manager popover and ≥44px touch targets in final implementation.
- Performance (design intent): 5 — static/fast; server-side paging/filter avoids heavy client fetch; no redundant
  requests implied. Implementation must keep server-side query.

## Verdict
APPROVED for owner design review (no blocking defects). Non-blocking refinements recorded above (saved-filter-to-view,
compact toolbar overflow, ARIA/touch-target polish) to apply during implementation if owner approves.
