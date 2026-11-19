PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R14
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R13
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Finish bulk per-row results and stop on scope change
Mode: R13_BULK_CLOSURE_ONLY
Expected-Base-SHA: eae4b3196823a874b04b5987a87e5f4504584f89
Issued-Date: 2026-11-20 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Two concrete repairs, no new phase
R13 layout B and single action wiring retained. Source: executeAdminOpBatch iterates all entries with captured scope, no batch epoch check; confirmAdminOp drops result.items and closes Dialog after a count-only toast. R13 mixed/partial mounts use separate detail confirms, not actual selected multi-row UI.
Read applicable FE feature rules. Only finish existing grouped-operation obligations; no backend/auth/grid/Story redesign. One worker; no delegation/screenshots/polling.

1. Freeze batch actor/scope/epoch before first dispatch. Before EVERY subsequent request, revalidate current gate epoch/active scope; stop on stale_epoch/scope/auth cancellation/unmount. If scope changes while first response is unknown/in-flight, do not dispatch later rows under captured old actor. Retain exact attempted envelope; untouched remaining rows explicitly “اجرا نشد”. Do not persist untouched rows as if sent or invent their outcome. Existing server authorization remains authoritative.
2. Keep/show actual result.items in a DS result panel/dialog: each title, operation, succeeded/failed/unknown/skipped/not-started and readable reason. Count-only toast is insufficient. Transport loss/unverified2xx means outcome uncertain, not definite no-effect. Keep exact unresolved command recoverable; retries use same version/key/bytes, never repeat succeeded rows. Close result UI deliberately, and don't navigate away before user can inspect results. Preserve all eligible/skipped information.
Use current module/executor; no generic batch framework or new storage authority.

## Focused verification
- Actual admin AppDataGrid: select two submitted rows plus one ineligible row, invoke bulk action and confirm; verify only eligible selected ids dispatched, distinct per-row keys/current versions, skipped reason and per-title results visible. No no-op helper-only substitution.
- One succeeds, second loses response; exact retry only second, same bytes/key, first never repeated; actual failed/refused row has named reason.
- Pause first response, change epoch/actor or unmount; resolve it, assert no second network request and remaining rows not-started. Retained attempted uncertainty stays scoped.
- Reject selected rows requires reason; cancel zero requests. Reuse retained single/delete/layout facts, no R1–R13 sweep.
Only affected tests + tsc. Keep FE3000 next-dev current; production Html404 release blocker unchanged. Read-only live list toolbar/result layout where feasible; no live approve/reject/delete or screenshots.

ZERO backend/schema/business rows/account/grant/config changes/seeds/provider/money/stock/key operations. Canonical Host5088 skip-bootstrap1 / FE3000; preserve R5–R13 behavior/VendorShell/languages/Returns/payout/user work.
Persist exact task+receipt; verify base ancestry (docs-only issuance descendants allowed), commit/push/fetch parity; no force/rebase/amend/reset/stash. Update CURRENT: R13 B accepted, bulk closure R14 only. PASS requires both repairs and real selected-grid test; genuine blocker PARTIAL. Short result with code/final SHA and exact evidence. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
