PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ONE-LEVEL-TABS-084
Parent-Task: TB-SHIPPING-ONE-LEVEL-TABS-083
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TOOBA-SHIPPING-ONE-LEVEL-TABS
Mode: BOUNDED_ADMIN_FIVE_TAB_SHELL_AND_ONE_LEVEL_LIST
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 2b80990f8016ab1beb8c946429fcfad13bd5437b
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE
Timebox: 30 minutes HARD

ARCHITECT RULING
083 ACCEPTED for isolated reversible migration, legacy-code compatibility and targeted repairs. Build the merchant-facing one-level, five-tab Shipping admin surface without changing checkout authority, payment, shipping rate authority or persistent customer data. The new Fulfillment one-level schema EXISTS IN SOURCE ONLY and is NOT migrated/backfilled in Development. Tooba:ShippingOneLevelCatalog remains OFF by default. Do not fake or silently activate it. The old catalog remains operational read authority until explicit backfill and cutover authorization. This task implements the UI shell/tab 1 behind a safe OFF-by-default feature gate; do not take over the current merchant screen while the gate is OFF. A direct method list MUST preserve code identities from 081/083; do not present provider->option as a second configurable level. Avoid an empty new UI replacing the working legacy UI.

PREFLIGHT — STOP ON CONFLICT

Claim exactly one Pending task from tooba-main through BRIDGE-WAKE; preserve the received task artifact as UNSTAGED. Read AGENTS.md, docs/ai/TOOBA-PIPELINE-PROTOCOL.md, docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-081.md, -082.md, -083.md, and the exact changed files in 082/083.
Fetch origin/main and confirm Expected-Base-SHA or demonstrate only non-overlapping result/evidence bookkeeping drift. Preserve ALL pre-existing dirty/untracked paths, especially src/frontend/app/admin/*; never reset/stash/rebase/force-push, never overwrite user edits. On overlap, STOP RECOVERY_CONFLICT.
Inspect actual src/frontend/app/admin/shipping-services-screen.tsx, existing route(s), design-system tab primitives and tests before editing. Identify route/feature separation and the existing /admin/shipping-services functionality. Report current source-of-truth and gates.

MISSION A — FIVE-TAB SHELL + TAB 1 (IMPLEMENTATION)

Prepare a genuine RTL, responsive, keyboard-accessible five-tab admin experience at the existing /admin/shipping-services route, with the EXACT labels: «سرویس‌ها», «تعرفه و پوشش», «بسته‌بندی و کرایه», «برنامه کاری و ظرفیت», «بررسی هزینه». Include selected-method context; tab state/deep links via ?tab=...&method=... (safe sanitization of inputs); correct aria/tablist/tab/tabpanel semantics, arrow/Home/End keyboard navigation, visible focus, sensible no-selection state, loading/error/denied states. Reuse the current design system where sound; don't create a parallel design system.
Tab «سرویس‌ها» displays independent method rows (examples: post:standard, post:express, tipax:standard, tipax:express, store_courier, in_person, snapp_courier), with independent Persian display labels where data actually exists, stable method code, active/inactive state, selected method. Do not render the nested «انواع سرویس (سطح ۲)» editor or option-count column IN THE NEW UI. provider_kind/internal provider binding is never a selectable second level. Inactive stays inactive; post:express and snapp_courier must NOT become bookable. Don't fabricate a label, status or new row from a provider-level fallback policy.
All four other tabs are honest, integrated shell destinations for existing capabilities, NOT new invented CRUD. At this task's boundary they may show a clear scoped 'to be wired in 085–088' state if proper functional integration cannot be safely finished in 30 minutes. Do not show a fake Save button. Keep the currently working legacy actions and dialogs fully available when the new gate is OFF. Tab-specific capability implementation remains scheduled for 085–088.
Gate behavior must be observable and fail closed. With Tooba:ShippingOneLevelCatalog OFF/absent, existing Shipping admin route MUST render legacy screen with all current actions working. Do NOT enable the gate globally or add an enabled appsettings section. If frontend cannot read the server-side gate safely, implement only a disabled-by-default explicit frontend-compatible read seam; do not infer activation from existence of schema, method data or client query string. Never expose a query-param switch that bypasses the OFF gate. If no trustworthy gate read can be implemented in time, deliver the new components dormant/unmounted and report the precise missing binding; do NOT claim live UI rollout.
IMPORTANT: new catalog reader is read-only and no live Development migration/backfill was authorized. If method data are unavailable, show an honest unavailable/loading/retry state in gated UI, never silently fall back to the legacy option tree and claim the new catalog is authoritative. No invented method identity or fake development fixtures. Source edits must remain focused on frontend and an absolutely necessary read-only existing API seam only if independently justified and within scope; otherwise STOP for architect.

MISSION B — PROOF / SAFETY
6. Focused tests for keyboard tab navigation, URL selection, no-selection/error/denied, OFF-gate unchanged legacy route, one-level code identity, inactive visibility, provider metadata hidden and no second-level editor. Use existing frontend test tooling; smallest relevant test/build, no broad suite loops.
7. Verify no change to Tooba:ShippingRateAuthority, Tooba:ShippingOperationalShadow or the legacy StorefrontShippingService / StorefrontCheckoutService; do not modify historical persisted codes or paid snapshots. No DB writes, migrations, backfill, Host restarts, server reconfiguration, customer price/availability changes, COD settlement, capacity reservation or delivery promises.
8. If a real browser can be safely used without restarting services or touching user data, capture an OFF-gate legacy-preservation screenshot/check. Otherwise clearly distinguish code-level proof from live browser proof. No fabricated PASS.

ALLOWED CHANGE SURFACE

src/frontend/app/admin/shipping-services-screen.tsx ONLY if no pre-existing overlapping dirty change; minimal gate-preserving composition.
src/frontend/app/admin/shipping-services/** (feature-local new shell, tab/list components, focused tests)
existing immediate frontend route/config/test files ONLY if essential to wire a safe disabled-by-default feature gate and plainly enumerated in the result. Do not modify unrelated admin screens.
docs/planning/shipping/SHIPPING-ONE-LEVEL-UI-084.md
docs/architecture/evidence/TB-SHIPPING-ONE-LEVEL-TABS-084/**
docs/ai/tasks/TB-SHIPPING-ONE-LEVEL-TABS-084.task.md receipt only UNSTAGED.
Any backend endpoint/contract/schema change is OUT OF SCOPE unless separately architect-authorized; report missing support as PARTIAL instead of guessing.

ACCEPTANCE / STOP
PASS only if a real five-tab shell and independent-method tab-1 component are implemented with RTL/accessibility/URL state, no second-level configuration in new UI, OFF gate preserves legacy route, targeted tests actually pass, and a truthful evidence record is committed and pushed. If the safe gate/read seam is unavailable, PARTIAL with exact blocker. If there is source overlap or unsafe environment, RECOVERY_CONFLICT or BLOCKED. Stop no later than 30 minutes and report real elapsed. No automatic 085.

REQUIRED RESULT CONTRACT
Return exactly one complete canonical BRIDGE-WAKE-V1 worker result with: Task-ID, Parent-Task, Channel, Worker-Id, Agent-Type, Program, Mode, Bridge-Task-Row, Status, Stop-State, Expected-Base-SHA, Base-SHA, Code-SHA, Evidence-SHA, automaticNextImplementationTask: NONE; followed by exact changed paths, gate behavior, evidence, actually run tests, missing wiring, live limitations, safety, elapsed time; then END_TOOBA_WORKER_RESULT and END_TOOBA_TASK.
END_TOOBA_TASK