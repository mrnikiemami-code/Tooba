# Architect review — TB-275 — 2026-11-16

Decision: PARTIAL / NOT ACCEPTED as a connected operator workflow.
Reviewed source: abc0bad78e1b9a5c98cde8887c62d7dbc6078f28, delivered with docs-only posting head a780ee38ac56711ee0d3b6fc66cec0d9c8a46091.
This is an independent source review through GitHub; no Architect local runtime, browser or test execution is claimed. Worker build/runtime/test receipts remain worker evidence.

## Work retained
The Returns-owned work description, classified queue/count envelope, workspace, explicit receipt quantities/conditions, legacy partial-receipt continuation, deterministic retry identity and mobile layout are retained. No rollback, reimplementation of the module or reopening of accepted 272-R1/273-R2/274-R1/shipping work is authorized.

## Source-confirmed connected defects
1. admin-return-workspace.tsx:152-174,205-206 unconditionally calls finish after dispatch; finish removes the stored intent BEFORE examining answered.state. A 4xx/409/503/unknown represented as AdminResult therefore clears unresolved intent. null (missing checkout/default branch at721-764) is treated as success. The manual “نادیده گرفتن” at292-300 also deletes a possibly-sent intent without authoritative reconciliation.
2. return-review-intent.ts storeReviewIntent only setItem's a reserialized object; no verified read-back precedes send. clearReviewIntent compares occurrenceKey only, not action + exact body. readReviewIntent accepts any nonempty action and substitutes {} for absent body. The reviewed transport target/checkout is not frozen; dispatch uses the current snapshot checkout.
3. Workspace epoch protects initial GET only (116-126). run/finish/load/finally (110-114,152-212) have no captured-epoch fence. A late mutation/refetch answer can change the newer case UI and pending lock; a retained unresolved intent can be ignored and overwritten. Mounted lifecycle proof is absent.
4. ReturnDirectory.RetryRefundAsync:394-406 gates on current RefundFailed and resolves CURRENT un-retried failure BEFORE the deterministic child read at412. Consequently an exact previous retry cannot replay after Pending/Completed, and when its child Failed the original failure is excluded at471-473. The alleged duplicate-success test actually expects RefundRetryInvalidState. Missing ExpectedFailedAttemptId is auto-selected by ReadReviewedFailedAttempt (463 refuses Guid.Empty but accepts null;481 compares only when supplied), contrary to explicit reviewed identity. A repeated legacy request can select a new failed leaf instead of retaining its intent.
5. The frontend 21-test file uses node:test, helper tests and source-regex assertions, not a mounted workspace. It does not prove the issued double-click, storage failure/read-back, exact unknown retry, remount, retarget/late-answer or action route behavior. Rename no proof; exercise the actual component with repository mount runner.

## Next authorized work
Only TB-RETURNS-CONNECTED-WORKFLOW-REPAIR-AND-USER-REVIEW-276: close these connected defects, prove the actual workspace and owning retry path, then create ONE lawful ordinary Requested return case through the existing owning API for the user's review. The user explicitly authorized that case alongside advancing work and wants to test at task end. This overrides earlier blanket no-live-case instructions for this one bounded case only.

Case creation may occupy its requested return capacity; it does not authorize approval/rejection, a fabricated receipt/inspection, refund/recovery/retry execution, payment/Wallet/Inventory/provider effects, source-order or shipping history changes. Resolve eligibility from actual owning reads. No artificial Paid/Delivered facts, bypass of windows/quantity rules, seed/direct SQL, date rewrite or synthetic money/stock history. Leave the case visible for the user. If no lawful candidate exists, finish repairs and report the exact eligibility blocker; do not manufacture acceptance.

No new migration. No screenshot/delegation/loop/broad suite. One worker/task, one result, STOP_FOR_ARCHITECT, automaticNextImplementationTask: NONE.
Worker PASS is not Architect ACCEPT; user visual/functional, provider, money and full Returns acceptance remain unclaimed. Preserve original 275 reports unchanged.
