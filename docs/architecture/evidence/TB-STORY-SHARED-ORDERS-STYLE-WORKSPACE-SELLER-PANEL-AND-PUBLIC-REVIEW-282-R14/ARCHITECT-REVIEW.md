# ARCHITECT REVIEW — TB-282-R14

Date: 2026-11-20 (Asia/Tehran)
Reviewed code: cbba1b3e247426ead2babd4f4dd0fae1755fd166
Reviewed evidence/head: 8c6fe36325f181aba213fb655d735cc48c7384c7
Decision: PARTIAL — accept epoch/scope stop and per-row panel; rejected transport closure required.
Next task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R15

## Accepted
Source freezes batch scope/epoch, checks before/after requests, stops on auth/scope/stale epoch/cancel, marks remaining not_started without envelopes. Existing component epoch effect includes unmount cleanup. The DS result panel retains per-title operation/outcome/reason and defers deleted-detail navigation until deliberate close. R13 publication layout retained.
Worker's 4/4 evidence includes real AppDataGrid selected 2 eligible + 1 skipped, current versions/distinct keys, empty-200 pending + exact recovery retry, a direct batch-helper epoch test, and mounted reject reason/cancel. Epoch test is a helper behavioral fact, not a mounted unmount test.

## Remaining concrete defect
story-executor.ts/sendJsonRequest awaits fetch before finalizeResponse; finalizeResponse's try covers response parsing, not a rejected fetch. story-admin-ops.ts/executeAdminOpBatch awaits runDurableStoryMutation without a catch. StoryManagementScreen.tsx/confirmAdminOp has finally but no catch returning batch results.
Therefore a genuine connection-loss fetch rejection exits the loop before returning items or setting adminOpResult. The already stored attempted command remains, but the required complete result Dialog is skipped; an event-triggered promise can reject unhandled. R14 partial test returns an empty HTTP 200 and does not exercise this path. No claim that a new regression test was independently run here.
R15 is limited to this rejected-transport boundary and a real selected-grid test with same-command recovery. No new feature phase.

## Limits and handoff
Architect independently read production source, commit delta and committed test/evidence; did not rerun worker Windows tests/Host/Next. Worker 4/4 and tsc are reported evidence, not architect-run tests.
Local next-dev trial only; production Html404 prerender blocker remains. No inference that absence of Story next/document establishes its root cause. ZERO live Story/Media mutations in this review. R6 bootstrap effects UNKNOWN preserved.
No full Story/product/user acceptance. Existing action availability still follows server lifecycle/actionCodes.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R15
automaticNextImplementationTask: NONE (R15 explicitly issued by architect for the above defect)
STOP_FOR_ARCHITECT_AND_USER_REVIEW
