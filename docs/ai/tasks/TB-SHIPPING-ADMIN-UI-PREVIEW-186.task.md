PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-UI-PREVIEW-186
Parent-Task: TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: a889b0190322f13ee9b983a614011f24e132dd90
Program: TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE
Mode: BOUNDED_MULTILINGUAL_ADMIN_VISUAL_PREVIEW_ONLY
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Build the FIRST interactive visual preview of the redesigned Shipping Admin inside the existing Tooba frontend. The user rejected the present list and tall four-tab modal and explicitly requested appearance FIRST, to inspect before any real behavior/data migration work. Deliver an actual browser-openable preview, not another plan, screenshots-only deliverable, standalone external HTML, or business/backend implementation. Use the project's established design system and multilingual architecture. This task is one bounded visual slice; technical PASS is NOT the user's visual acceptance.

The parent task identifies the previous pipeline checkpoint only. TB-185 Storefront performance remains PAUSED. Do not resume it.

MANDATORY FIRST CHECK
Fetch origin and verify branch main and HEAD == origin/main == Expected-Base-SHA before edits. Inspect dirty files; preserve unrelated work. On mismatch, do not merge/rebase/reset/stash; STOP_FOR_ARCHITECT with the exact observed SHA and blocker. Claim only this task through the current Bridge-Wake lifecycle; no parallel or historical task.

Read:

AGENTS.md and docs/ai/TOOBA-PIPELINE-PROTOCOL.md.
.cursor/skills/tooba-frontend-feature/SKILL.md and docs/architecture/FRONTEND-ARCHITECTURE-STANDARD.md.
docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md, especially sections 5–6 and 10–11.
docs/architecture/shipping/SHIPPING-STOREFRONT-UX-REFERENCE.md and its original reference image.
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md (latest appendices override historical directions).
Inspect the five rejected current-UI screenshots in:
docs/architecture/shipping/evidence/SHIPPING-ADMIN-UI-BASELINE-2026-10-21/
list.png, general.png, rates.png, schedule.png, packaging.png.
They are owner-provided BEFORE references, not the desired design.
Trace the existing shipping route, AppDataGrid, forms/tokens, multilingual content editor and locale/direction helpers before coding. Reuse these patterns; do not invent a second design system.

AUTHORIZED SURFACE / PREVIEW BOUNDARY

Add a dedicated authenticated Admin preview at /admin/shipping-services/design-preview (route composition plus task-scoped feature/components/state/fixtures following repository conventions). Preserve the existing /admin/shipping-services behavior, all existing production API clients, old dialogs and their write gates. No live navigation replacement before user approval.
Preview fixture content is explicitly SAMPLE and temporary. A short localized banner says this is a design preview and changes are not persisted. In-memory state only; no business POST/PUT/PATCH/DELETE, no carrier calls, no database/Host/migration/config/feature-flag changes, no persistent browser storage and no fake server-success notices.
Use typed UI view models scoped to the preview, not proposed production DTOs or a new quote/rule engine. Unsupported future fields may be edited temporarily only here. No production claim of seller authorization, geographic server validation, actual pricing, real tax/FX, capacity or reservable delivery windows.
Retain existing authentication/layout. A preview owner switch is only a clearly sample visual choice, never a real tenant/seller authority change. Do not bypass auth or rewrite the Admin shell.

BOUNDED VISUAL IMPLEMENTATION
A. LIST
Use the existing AppDataGrid wrapper with compact, readable rows. Show shipping type, method name, calculation mode, freight payment timing, status and View/Edit actions. Codes/GUIDs must not dominate the list. Add a clear Add Method action. Show owner context (marketplace or sample seller) once. Keep search/actions aligned, avoid the rejected list's excessive empty spacing and disconnected actions. View must be read-only; Edit/Add opens the preview workspace.

B. FULL-PAGE EDITOR, NOT THE TALL MODAL
Provide back-to-list navigation, clear method title and an accessible action bar. Use two primary tabs: “اطلاعات روش” and “مناطق و هزینه‌ها”, localized equivalents for other interface languages. Preserve drafts when changing tabs.
General tab:

Shipping type ABOVE method name (ordinary/heavy/extra-heavy/grocery are sample operational types).
Localized method name and description, compact content-locale tabs with translation status.
Freight payment choice: prepaid / freight collect. This is NOT payment for the goods on delivery.
Calculation choice: manual / carrier API / free.
A compact shared operating-schedule editor/summary, with expandable details rather than repeated paragraphs.
Manual mode shows the manual configuration. Automatic shows a carrier selector, understandable integration status and a concise address-map-coordinates requirement; no map SDK/key/API implementation in this task. Free explains zero customer shipping/packaging and displays sample funding responsibility; do not silently delete previously entered manual drafts.
Missing production authority belongs in this preview's clear boundary note, not repeated architecture warnings inside every form.

C. ONE GEOGRAPHIC GROUP, MANY RATE ROWS
Regions tab uses collapsible region-group cards. Each has one title/region summary, Edit Regions action, a rates table and Add Rate Row action.

Demonstrate one group containing multiple areas; display localized complete geographic paths/chips.
Cascading picker: country -> administrative area -> city -> service area. The user may ADD at any level and stop there. ADDING a parent selects all descendants; merely choosing a dropdown parent to browse does not itself add it.
Parent/child and duplicate conflicts are disabled with clear explanation across region groups of the SAME sample owner and method. No automatic removal/replacement. Independent methods/owners may use the same areas. Use a small explicit ancestry fixture; never infer ancestry from labels or string-split technical codes.
Selecting Iran then ADD blocks descendants; adding a city first blocks adding its ancestors but allows disjoint siblings. Existing selected items remain editable/removable.
Under the SAME region group allow multiple sample condition/cost rows. Row editor has optional weight, count and merchandise-amount ranges, freight charge, packaging charge and delivery duration. No second geography picker in the row, packaging or schedule.
Within one row conditions are AND; rows are alternatives, not summed charges. Explain this with one short helper, not an essay. Show units and whether range endpoints are inclusive/exclusive. Use non-overlapping fixture examples; do not implement a financial evaluation engine.
Offer Add/Edit/Remove for temporary rows with cancel/unsaved-state handling. Shared schedule or temporary row override may be shown compactly without entering geography again.
Preview Save/Apply updates only the local draft, with “applied to preview only” feedback. Reset restores the fixture. No misleading “saved to server” action.
Shipping-free promotions remain a separate concept; do not restore a campaign threshold field here. Merchandise amount eligibility ranges remain present.
Currency is displayed from sample store context, not an editable free-text IRR field. Currency and interface language remain independent; no FX conversion.

MULTILINGUAL / RTL-LTR — REQUIRED NOW
Use the existing i18n mechanism. Real store locales are dynamic; preview may use an explicitly labeled locale fixture if no read-only configuration seam is readily available. Do not hard-code fa/en/ru as production store settings.
Distinguish interface language from the content language being translated. For the preview, include Persian RTL and English LTR, plus any configured locales already supported by the reused path. Names/descriptions and optional region-group titles have separate per-locale values; incomplete translation status is truthful. Editing English must not overwrite Persian. Switching either locale must preserve regions, costs, conditions and the draft.
Use stable sample IDs for types/places and localized labels, existing geography naming conventions, locale-aware number/money/unit formatting and existing date/time conventions. No raw GUID label, backend binding paragraph or unmarked cross-language fallback. Translation, accessible names, errors, buttons, empty states and preview notices must be covered.
This is not a global translation sweep. Limit dictionary changes to preview keys and the established namespace; do not alter shared currency/domain policy.

VISUAL QUALITY / INTERACTION CHECKS
Reuse Tooba tokens, typography, controls and Admin layout; Persian-first, with correct English mirroring. Compact spacing, consistent label alignment and clear primary/secondary actions. Avoid nested tabs and nested scrolling inside a tall modal. At approximately 1440px, 1024px and 390px widths keep actions usable and no page-wide accidental horizontal overflow; a bounded rates-table scroll is acceptable.
Ensure list -> Edit -> both tabs -> region/row editor -> back works. Owner/mode/locale switching must not accidentally fire live requests or discard drafts. Keep focus, keyboard operation, field labels, validation placement and View-mode non-writability usable. No dead buttons presented as working features.

VALIDATION / NO LOOPS
Use existing dependencies and browser harness; do not install packages or download browsers.
Run focused preview interaction checks: language independence/RTL-LTR; country/descendant conflict and disjoint sibling; add two rows under one region selection; read-only View; no business network writes.
Run src/frontend npm run typecheck once when feasible, with a 180-second bound. If timeout/pre-existing failure, record it and stop that check; do not rerun unchanged or start full suites. Fix only errors introduced in this scope. No baseline refresh, coverage expansion or repository-wide audit.
Render the actual preview route in a real browser and inspect its DOM/layout at the three widths; inspect English LTR as well as Persian RTL. At most one bounded harness recovery; if unavailable, report PARTIAL with exact reason. Do not spend the task on screenshots or repeatedly restart servers.
Screenshots are OPTIONAL internal evidence only, not the user deliverable or an acceptance prerequisite. The user needs an actual preview URL and concise steps, not screenshot galleries.
Use the existing running frontend if safe without restart/config/cache mutation. If a separate Next process is necessary, use one free loopback port and isolated task-owned output directory; no new Host/DB, no shared .next cleanup, no touching shared ports :3000/:5088/:5432. No repeated cold builds or fixture/DB setup.
For a task-owned preview listener, report ownership/PID/port and provide the repo-native start command. It may remain only for this requested review under the normal project lifecycle; stop other task-owned temporary processes. Never stop/restart unrelated apps.

OWNERSHIP / NON-GOALS
Only task-scoped frontend preview, necessary scoped translation keys, task receipt and evidence. No production backend, schema, contracts, migrations, actual shipping write paths, Checkout, Payment, Settlement, carrier account/credentials, Storefront, global CSS/theme or shared design-system redesign. Do not read/update the deferred findings register. Do not address unrelated cart/currency bugs or TB-185 performance.
The user explicitly authorized this visual preview change despite historical four-tab descriptions; this is not authority to remove their live behavior or weaken architecture guards. If a real lock conflict remains, identify it and STOP.
Preserve all unrelated dirty files/stashes, running shared services and data. No reset/stash/rebase/force-push. No secrets or customer information in fixtures/evidence.

ACCEPTANCE VERDICT
PASS only for a working, browser-inspected multilingual interactive visual preview with the specified two-tab full-page editor, one multi-region selection with multiple rate rows, temporary state only, preserved live behavior and no live writes. Provide the exact preview URL and simple user navigation. State USER_VISUAL_ACCEPTANCE=PENDING and PRODUCTION_FUNCTIONAL_IMPLEMENTATION=NOT_STARTED.
PARTIAL if required visual slice or real-browser proof is missing. BLOCKED if base/auth/isolation/architecture prevents safe implementation. Do not claim backend readiness, Admin completion, Storefront acceptance or deployment. STOP_FOR_ARCHITECT after this single result; no next Task.

EVIDENCE
Persist the received file verbatim at docs/ai/tasks/TB-SHIPPING-ADMIN-UI-PREVIEW-186.task.md.
Write concise sanitized evidence to docs/architecture/evidence/TB-SHIPPING-ADMIN-UI-PREVIEW-186/RESULT.md:
actual base/code/evidence SHAs, changed paths, architecture ownership, reused components and i18n/locale source, exact URL/start command, browser/viewport checks, interaction checks, business-network-write check, commands actually run, pre-existing failures, elapsed minutes and remaining user-review gate.
Commit scoped changes, push origin main normally, fetch and verify HEAD == origin/main. Do not include unrelated work. A missing test/browser check stays UNVERIFIED, never fabricated PASS. Do not author a future task or automatically continue.

RESULT FORMAT
Use the following field names and markers; replace ellipses with actual values or NONE. No placeholders in the transmitted result.
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-UI-PREVIEW-186
Parent-Task: TB-STOREFRONT-PERF-BROWSER-ACCEPTANCE-185
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-UI-ACCEPTANCE
Mode: BOUNDED_MULTILINGUAL_ADMIN_VISUAL_PREVIEW_ONLY
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: a889b0190322f13ee9b983a614011f24e132dd90
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
automaticNextImplementationTask: NONE
HEAD-Origin-Parity: ...
Preview-URL-And-Start-Command: ...
List-And-Two-Tab-Workspace: ...
One-Geography-Multiple-Rate-Rows: ...
Multilingual-Content-And-Interface-Independence: ...
RTL-LTR-And-Responsive-Checks: ...
Real-Browser-Proof: ...
Business-Network-Writes: ZERO|...
Live-Shipping-Behavior: PRESERVED|...
Focused-Tests-And-Typecheck: ...
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
User-Acceptance-Still-Open: YES
Production-Functional-Implementation: NOT_STARTED
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
