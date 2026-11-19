PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R15
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R14
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Retain bulk results when fetch rejects on connection loss
Mode: R14_TRANSPORT_REJECTION_CLOSURE_ONLY
Expected-Base-SHA: 8c6fe36325f181aba213fb655d735cc48c7384c7
Issued-Date: 2026-11-20 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## One concrete gap
R14 epoch/scope stop, untouched not_started rows and per-row result Dialog accepted at source level. Empty/unverified 200 coverage accepted. No redesign or additional feature phase.
Real connection loss remains uncovered: sendJsonRequest awaits fetch outside finalizeResponse's try; executeAdminOpBatch awaits runDurableStoryMutation without catch; confirmAdminOp has only finally. A rejected fetch therefore bypasses batch results and result Dialog, despite an already persisted intent. R14's partial test returns empty 200, not rejected transport.

Read applicable AGENTS/FE instructions and R14 ARCHITECT-REVIEW.md. One worker; no delegation/screenshots/polling.

## Repair and prove
Handle actual rejected transport at the appropriate existing Story boundary, without swallowing unrelated programming errors as success. Preserve the attempted row's exact persisted version/key/body and show a readable uncertain result; never label it successful or definitely unapplied. Return/show complete per-row results even after this connection loss. Keep prior successes, skipped reasons and untouched states truthful. Existing epoch/scope/auth/cancel fences still stop later rows; never create an envelope for an untouched row.

Extend the REAL mounted AppDataGrid bulk test: select two eligible rows plus one skipped; first succeeds, second fetch rejects with TypeError/network failure. Assert named first success + second unknown + skipped reason visible in result Dialog, no unhandled rejection, and only second remains pending. Close the result Dialog through its actual close control before using recovery. Retry through visible recovery UI; same second route/version/key/body bytes, first never repeated, matching pending clears on bound success. Exercise actual rejection, not empty/non-JSON HTTP response. Run this fact before the fix and record its observed failure, then final green.
Retain/check R14 epoch stop fact; include rejection while epoch changes so later rows remain not_started with zero later dispatch. Only affected mounts + tsc; no historical sweep.

ZERO backend/schema/auth/grant/config/bootstrap/business-data changes. No live Story/Media writes. Keep Host5088 skip-bootstrap=1 / FE3000 next-dev, VendorShell, date layout, existing i18n authority and R13 single/bulk actions. No new build-repair phase: Html404 production blocker remains disclosed.

Persist this exact TASK.md at docs/ai/tasks/<Task-ID>/TASK.md and docs/architecture/evidence/<Task-ID>/TASK.md with receipt. Verify expected-base ancestry; docs-only issuance descendants allowed. Normal commit/push/fetch parity; no force/rebase/amend/reset/stash or staging keys/binaries. Update five CURRENT pointers consistently. Short result: code/final SHAs, precise rejected-fetch evidence, honest limits. Worker PASS != Architect ACCEPT != owner acceptance.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
