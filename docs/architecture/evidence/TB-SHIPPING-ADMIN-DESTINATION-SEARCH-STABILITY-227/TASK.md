PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Task-ID: TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227
Parent-Task: TB-SHIPPING-ADMIN-MOBILE-WEEK-VISUAL-226
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Mode: BOUNDED_DESTINATION_INTERNAL_SEARCH_AND_LAYOUT_STABILITY
Expected-Base-SHA: ISSUE_COMMIT_CONTAINING_THIS_TASK
HARD25 minutes / implementation stop15 / final10 proof+recovery+commit. One correction maximum.
User authorizes implementation. Do not enqueue another task or execute historical post-result scripts.

FIRST
Read TOP AGENTS.md, canonical recovery and relevant repository frontend/design-system instructions.
225-R2 money and 226 mobile week are accepted within their recorded limits; do not reopen them.
Fetch main, fast-forward safely; preserve unrelated changes. No reset/stash/rebase/force-push.

DELIVER ONE VISIBLE FIX
1. Reproduce destination-modal shake/jump when selecting country, province and city. Record actual cause,
   not a guess: measure dialog rect, page and internal scroll, focus and popup placement before/during/after
   each branch load. Inspect conditional loading/path/empty-note rows, dialog centering and scrollbar/focus.
   Current source conditionally inserts these rows: this is a suspect, not yet a proved sole cause.
2. Reuse the existing design-system searchable combobox/popover implementation after inspecting its API.
   All FOUR fixed fields remain visible: country -> province/admin -> city -> service area.
   Search MUST be INSIDE the opened dropdown popup, never a separate external search input.
   Use existing styling/components; no new general UI framework, package or parallel design system.
   Search only actual loaded DB options for the current verified branch. No hardcoded places or global
   eager preload. Explain incomplete-branch pagination honestly if existing API is paginated; never claim
   a complete search over unloaded results.
3. Stabilize dialog geometry, focus and scroll during branch loading and choice changes. Keep stable field
   identities, no remount on option count, no scrolling page to selected controls. Reserve a compact status
   region or use existing stable layout where appropriate; do not hide the defect with animation/delay or
   giant fixed empty space. Dropdown must fit viewport and scroll internally on 360/390 mobile.
4. Preserve current-generation coordinator and all authority: descendants empty/disabled until parent chosen;
   parent change clears ALL descendants immediately; superseded reads never win. Active DB identities only.
   Deepest verified node is ONE destination, breadcrumb one chip. Add keeps modal open and parents available.
   Duplicate/overlap only on explicit Add through existing MessageBox; staging a country is not an error.
   No money/rate/range/week/publication/backend/geography-data changes.
5. Accessible labels, combobox state, keyboard navigation, RTL/LTR and focus return. Escape closes an open
   dropdown first, without accidentally closing the entire destination dialog. Empty/loading/error states
   stay honest and visually stable; no fake fourth-stage data (Tehran currently has no service areas).

FOCUSED PROOF
One typecheck after final source; affected narrow behavioral tests only (no full 188/backend/solution replay).
Use a NEW task-owned Chrome profile with a verified free CDP port and task-created target ONLY.
Never enumerate/select/reload user tabs or Electron webviews: 225-R1 lost unsaved local edits; no recovery
was possible and this incident must remain documented.
Real API traffic, no interception. Exercise country/province/city selection, reset of descendants, search
inside popup, keyboard selection and no-result state. Verify modal rect + scroll + focus stay stable through
loading; report numeric observations and any remaining movement honestly. Desktop and 390 native viewport
screenshots with popup open; inspect 360 fit too. Real service-area options unavailable => state this limit,
use focused fixtures for that branch without claiming live coverage.
No Apply/PUT/publication/catalog/seed/migration/restart required. Discard only task-local edits normally.
Do not touch user draft revision or selected pointer. Preserve default-OFF/TB185 paused/PendingAllocation.

RECOVERY AND FINISH
Persist TASK.md + RESULT.md and concise actual proof under:
docs/architecture/evidence/TB-SHIPPING-ADMIN-DESTINATION-SEARCH-STABILITY-227/
Update TOP of ALL THREE: AGENTS.md; docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md;
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md.
Record accepted 225-R2/226, actual 227 outcome, live NOT-connected Storefront boundary, and NEXT PRIORITY:
concrete Storefront integration PER SHIPPING GROUP using the EXISTING storefront cards/controls/DOM/CSS,
not a new design, audit or calculator. Use 219 selected immutable revision + 218 composer + real trusted
fulfillment owner/type/membership authority and existing projection/selection/commit; never browser owner
or price, never seller=fulfillment inference. Unknown physical facts remain null.
Compact order history decision remains mandatory: shared immutable capture, compact per-order-group
reference and final money/receiving facts; no full authoring JSON per order, no historical repricing.
This task does NOT implement that Storefront connection. Architect reviews 227 then issues the concrete
next integration slice without asking the user to repeat authorization.
Stage exact paths only, scoped commit/push/fetch parity. Record actual elapsed and deviations.
STOP_FOR_ARCHITECT. Automatic-Next-Task NONE. Do not claim user Visual ACCEPT.
