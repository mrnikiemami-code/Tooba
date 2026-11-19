PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R13
Parent-Task: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R12
Channel: tooba-main
Program: TOOBA
Repository: mrnikiemami-code/Tooba
Branch: main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Title: Admin single and selected-row actions; keep no-end beside end label
Mode: OWNER_REQUESTED_ADMIN_OPERATIONS_AND_LOCAL_LAYOUT
Expected-Base-SHA: d6956b5f4363711aeba12ce7281f00fa42f46b62
Issued-Date: 2026-11-20 (Asia/Tehran)
Stop-State: STOP_FOR_ARCHITECT_AND_USER_REVIEW
automaticNextImplementationTask: NONE

## Goal
Owner requires ADMIN deletion/approval/rejection individually AND in groups from the Story list. Current StoryLiveGrid only wires view/delete (seller submit separately), no admin review row or bulk actions. AppDataGrid already supports bulkActions/isAvailable/execute(selected) and pageSelectionOnly; reuse it.
Owner rejects “بدون پایان” at the opposite far corner of the global publication legend. Keep it visually attached to END fields without reintroducing R12 staggered rows.
Read applicable feature instructions; preserve VendorShell, language registry, existing Story backend authorities and R5–R12 recovery/lifecycle/date behavior. One worker, no screenshots/delegation/polling.

## A — complete admin operations using existing grid
- Admin row menu AND detail offer “حذف”، “تأیید”، “رد” as legal for that Story. Show availability/reason for wrong states; do not let admin approve main storefront/already-approved/draft seller content via a forged transition. Server actionCodes/permissions remain authority.
- “تأیید” means existing APPROVE command, not silently publish-now or replace requested dates. Keep existing explicit approve-and-publish/schedule actions where already valid. Wire durable approve envelope to existing /approve endpoint, expectedVersion and occurrence key; do not fake status in FE.
- Admin select one/multiple rows -> existing AppDataGrid bulk toolbar with these three named operations and selected count. Reuse GridBulkAction and stable Story row identity; no alternate grid/selection widget.
- Review confirmation lists selected titles/count and eligibility. For mixed states, clearly show which are eligible and which are skipped with reason, never silently operate on unselected/all-filtered rows. “Select all” means currently selected loaded page per existing pageSelectionOnly; no implicit database-wide action.
- Delete DS confirm warns actual removal. Reject DS dialog requires explicit reason (common reason for chosen rows permitted, explain it). Approval confirms review, preserving each window. Cancel sends zero mutations.
- Use existing single-record endpoints per selected eligible Story, sequential bounded execution, per-row frozen version/key/body. No new bulk backend/schema framework needed. Same action function for row/detail/bulk, no duplicate command implementations.
- Aggregate result: succeeded/failed/unknown/skipped with names/reasons; partial failure not whole-batch success. Failed/unknown retain pending exact envelope; retries send only unresolved items with original bytes/key, never repeat successful delete/approve/reject under new keys. Preserve work if scope changes/stale version; refresh actual server statuses. User can safely continue after one bad row.
- Seller gets no moderation or admin bulk toolbar. Do not grant roles/change auth recovery just to enable controls.

## B — publication layout
In shared StoryPublicationWindowFields:
- Global heading “بازهٔ انتشار” stays above the two columns.
- Place “بدون پایان” immediately beside “پایان نمایش” in the END column header (same visual group), not at global far-left corner.
- Give START/END header rows equal height. Date fields aligned on desktop, clocks aligned; same dimensions/gaps regardless of toggle. End controls remain mounted disabled/dimmed when no-end, values retained.
- RTL desktop start right/end left; mobile start then end, no horizontal overflow. Keep valid labels and existing no-end serialization/required-end validation unchanged.
Apply shared create/edit for admin and seller. No shared DS/global CSS rewrite.

## Verify / deliver inside this task
Focused real mounted ADMIN grid/selection/Dialog interactions: one row approve/reject/delete; multiple selected; mixed eligibility; reject reason and cancel; partial server failure; lost response + exact retry without repeating successful rows; seller isolation. Capture actual endpoint/version/key/body; source scans/copy assertions not sufficient.
Affected backend checks only if new approve route/action binding requires source changes; existing commands/policy must reject unauthorized/stale transitions. No whole repository/AMSC sweep.
Read-only active served admin list shows selected-row bulk toolbar and correct menus. Verify dates at1440/390: checkbox adjacent END label, date tops and clock tops equal within2px, mobile no overflow. No screenshots/live mutations. Use labelled controlled transport for action tests; do not operate on owner records.
Run tsc and affected focused tests. Refresh FE3000; record real dev chunk/mode and Host identity. Existing Html404 production build blocker remains explicit; don't call release ready or expand this task into build repair.
Canonical tooba_alpha@127.0.0.1:5432 SingleStore/store-alpha Host5088 FE3000 skip-bootstrap1. ZERO migrations/DDL/seeds/backfill/reset/restore/live Story-Media mutation/persistent account-grant-config changes/provider/key/money/stock operations. Controlled tests committedRows=0. Preserve user data/Returns/payout.
Persist exact task/receipt; verify base ancestry (docs issuance descendants allowed), normal commit/push/fetch parity; no force/rebase/amend/reset/stash.
Update CURRENT pointers: only R13 active, await Architect then owner test. Return concise A/B before-after and actual interaction/geometry evidence, code/final SHA. PASS only if both requested groups work; genuine blocker = PARTIAL with exact reason, no unfinished PASS. No automatic next task.
STOP_FOR_ARCHITECT_AND_USER_REVIEW

END_TOOBA_TASK
