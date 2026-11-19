PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R16
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R15
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Stop Story grid reload and selection loss during rejection typing
Mode: OWNER_REJECTED_GRID_INTERACTION_REPAIR
Expected-Base-SHA: d7c36bf836f66365772f2f95a24a48e5b23628ae
Issued-Date: 2026-11-20 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Owner failure and confirmed cause
Owner selects a row, opens Reject and types: grid refreshes on each character and selection disappears. This is not accepted delivery. R15 transport repair remains retained; its owner-readiness handoff is superseded by this rejection.
StoryManagementScreen passes inline onDenied/onError/onOk to StoryLiveGrid. These enter queryAdapter dependencies. AppDataGrid.load depends on queryAdapter; its effect force-loads defaultQuery when load changes. Dialog input state therefore triggers another grid read. Other inline action callbacks also rebuild columns/bulk actions; inspect the full causal chain, not just the input.
Screenshot additionally shows an ineligible main-storefront row (“موبایل”). Do not bypass moderation rules or pretend that row is eligible. Preserve truthful skipped reasons; use genuinely Submitted eligible controlled rows to prove rejection.

Read current AGENTS/applicable FE instructions. One worker, no delegation/screenshots/polling. Finish this interaction in one focused delivery; no backend/grid-framework/auth redesign.

## Required repair
Stabilize Story-owned query/column/action inputs as needed so unrelated form/dialog rerenders do not refetch or reconstruct the active grid and do not clear its selected ids. Keep live callback values correct: no stale actor/filter closure, empty-dependency hack, disabled query effects, fake selection restoration or replacement grid.
Opening confirm, typing, backspace, Persian composition and cancelling must preserve selected ids, active filters/sort/page, scroll and focus. Freeze selected eligible/skipped plan for that confirmation. A lawful completed mutation may refresh normally; real filter/page/explicit refresh and actor/scope changes must still work, including clearing old-context selection. Preserve R14/R15 durable envelopes, uncertainty and stop fences.

## Mandatory regression and served proof
Add REAL mounted AppDataGrid interaction regression, run before fix and record observed failure, then final green:
- Select 2 eligible Submitted rows + 1 ineligible. Open Reject; type a Persian reason character-by-character through real input events with a settled render after each character, including backspace/composition where harness supports it. Count actual query transports: zero new reads from opening/typing; selected ids/count unchanged, current text/focus stable, eligibility unchanged.
- Cancel: zero reject requests; selection and query state retained. Reopen, type and confirm; exactly the 2 eligible selected ids, complete reason and frozen current versions/keys. Correct per-row result panel; ineligible never dispatched. Retain exact unknown retry and epoch-stop facts without historical sweep.
- Prove a deliberate filter/page or explicit refresh still fetches; actor/scope change still invalidates selection and prevents old-context dispatch.
- Read-only served browser interaction at FE3000: select/open/type/cancel with query-count and selected-id observations. No live reject/delete/approve POST. If eligible live data unavailable, use clearly labelled controlled HTTP responses on the REAL served UI, not jsdom called browser proof. No live fixtures.

Affected tests + tsc only. Update served FE3000. Host5088 skip-bootstrap=1 unchanged. Html404 production blocker disclosed, no new build-fix phase.
ZERO backend/schema/auth/grant/seed/config/live Story-Media mutations. Preserve VendorShell/language registry, date layout, Returns/payout/user work and R6 effects UNKNOWN.

Persist exact task at docs/ai/tasks/<Task-ID>/TASK.md and docs/architecture/evidence/<Task-ID>/TASK.md + receipt. Verify expected-base ancestry (docs-only issuance descendants allowed); normal commit/push/fetch parity, no force/rebase/amend/reset/stash or keys/binaries staged. Update five CURRENT pointers consistently.
Short result: root, code/final SHA, pre-fix failure, actual query counts/selection/text observations and served/control limits. Do not claim owner acceptance or readiness from helper-only tests. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
