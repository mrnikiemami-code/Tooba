# TASK — TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1

Issued from Bridge row `e5cf0ca8-4ead-4dd3-800f-56cdd7c7afcc` at `2026-11-08T15:47:24.1388334Z`
(claimed `2026-11-08T15:47:45Z`). Reproduced verbatim from the delivered task envelope.

```
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254-R1
Parent-Task: TB-SHIPPING-ORDER-OPERATIONS-PROFESSIONAL-UI-254
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: Tooba Shipping — finish the professional UI and actual authorized handoff
Mode: VERIFY_REAL_ORDER_COMPOSITION_FIX_SMALL_READABILITY_GAPS_AND_CLOSE_NEW_STRUCTURE_REGRESSION
Expected-Base-SHA: 5a11a4b8f98d00952bdd42fa1ab6da60c1b547f7
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE

1. Bounded Architect review

254 is PARTIAL for delivery, not a user-accepted professional UI.
Code confirms the main fixes: normal cards no longer contain reuse/quantity inputs, a dedicated workflow dialog exists, and seller asides filter group-covered parcels.
Retain those improvements and 253 technical recovery. Do NOT redesign again or reopen backend.
Outstanding:

the explicitly required authorized canonical browser DOM/layout was NOT checked; shell200/chunk symbol counts are not its substitute;
long essential item titles still use truncate in GroupItems and dialog rows;
the NEW dialog was added to an already-failing flat directory, worsening the guard from 11→12. This extra violation is task-introduced, not wholly pre-existing.
Close these exact items and give a real handoff.
2. UI guidance and narrow implementation

Read the existing frontend feature skill/standard and current design-system Dialog/Button conventions.
Use an actually available applicable design skill if present; report its exact source in evidence or honestly say none was available. No skill installation/creation detour.
Keep the approved single operational shipping composition:
normal page = method/day/hour/fee/items/parcels + contextual action;
editable quantities/reuse choices = only the purposeful workflow dialog;
seller packing section = packing only, plus genuinely orphan parcels if needed;
financial/timeline/global shell unchanged.
Marketplace-only grouping/proven-origin rule and Store/Seller separation unchanged.

Readability:

Wrap essential product titles naturally in group cards and workflow dialog instead of truncate/ellipsis; allow full identification at 390 width without page overflow.
Inputs/labels/button hit targets should be usable (44px) with normal-size glyphs; no oversized radio widgets or fixed overlay over content.
Ensure every visible parcel action appears once and targets the exact parcel; group-covered parcels must not reappear as actionable seller-aside cards.
Check shared-package members in a controlled mounted fixture: one package action owner, no duplicate locked-child actions.
Preserve tracking/cancel/dispatch/deliver handlers and existing standalone create dialog.

Structure:
Place ONLY this task's new workflow-dialog unit in the existing appropriate owning feature structure with its public import surface, following FRONTEND-ARCHITECTURE-STANDARD and actual boundary guards. No whole admin-folder migration.
Update necessary imports/tests. Do not add an allowlist/baseline exemption.
Run affected structure/boundary guard(s) once and report no new violation vs the 254 base parent. Existing unrelated red entries stay disclosed; do not sweep them.

3. Mandatory actual authorized rendered canonical check

Use the original:
http://127.0.0.1:3000/admin/orders/01a1b92d-e97e-7000-999a-143350d0edaf
FE3000→Host5088 SingleStore/store-alpha/tooba_alpha.

Use a TASK-OWNED browser/profile with an existing sanctioned admin access seam. Do not touch/close/reload arbitrary user tabs or bypass auth. Previous 253-R3 documented successful sessionless task-profile rendering; reuse the lawful approach, not an unauthorized cookie/session injection.
A loadDenied/401/shell-only page is NOT acceptance. If actual authorized render cannot be obtained, report PARTIAL with exact blocker instead of PASS.

On the rendered order at 1440/1024/390 (standing restriction NO screenshots):

record method/day/hour, real item titles, group and parcel counts;
default shipping section has ZERO reuse checkboxes/quantity inputs; packing row selection elsewhere remains lawful;
each real parcel has one visible identifier/status/tracking/next-action location;
no duplicated group-covered seller-aside parcel card;
no page overflow or covering toolbar; essential title wraps legibly;
focus/keyboard operates contextual entry and dialog; close returns focus;
no user-data mutations.
Inspect operator comprehension, not just data-testid or min-height counts.
Use controlled mounted Marketplace fixture for dialog cases not reachable on this Store-owned order. No fake Marketplace conversion or replacement order as the final handoff.

Dialog regression:
close/reopen after unknown response keeps exact occurrence/envelope; no auto-clear/remint; one request on double submit.
Retained operations remain accessible even if fresh-create readiness is false.
On partial progress, recorded line titles/quantities must remain understandable in retained summary; do not hide the recorded request by current zero-remaining rows.
No backend/idempotency redesign.

4. Focused checks and deployment

Run affected mounted UI tests once + typecheck + production build. No full Order/Shipping/PostgreSQL suite for a frontend composition change.
Rebuild/restart only FE3000 from D:\Users\User\source\repos\SarvNewVer after final edits. Host5088 untouched.
Preserve current migration, DataProtection and local CapturedPreview ON / AvailableDateCount=7; checked-in OFF.
No new schema, business SQL, reset/reseed, merchant/cart/order/pack/create/dispatch/cancel/payment mutation.
No live Tipax/account/network, new final port/database/edition/runtime, or global CSS/storefront changes.
Verify final served BUILD_ID matches read-only rendered UI checks; not merely source symbols.

5. Recovery and result

Persist exact TASK.md + RESULT/RECEIPTS in this ID's task/evidence dirs.
Append 254 Architect correction: source improvements accepted, authorized real-page acceptance absent and new flat-directory violation introduced. Do not describe all guard failures as pre-existing.
Update all three TOPs:
AGENTS.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md
Keep technical vs user visual acceptance distinct, prior recovery history and Tipax deferred status.
Commit/push normally, verify local==origin/main; no amend/force/reset/stash/rebase.
Return complete worker envelope with exact Base/Code/Evidence SHA, guidance actually used, focused proof, real authorized DOM receipts, final runtime/build identity and review URL.
Measured elapsed, no invented speed promise. Missing authorized UI proof → PARTIAL, not full delivery.
STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
END_TOOBA_TASK
```
