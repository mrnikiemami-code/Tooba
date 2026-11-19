# ARCHITECT REVIEW — TB-282-R13 — 2026-11-20

Decision: PARTIAL — layout B accepted; single routing accepted within controlled evidence; bulk closure R14 only.
Reviewed code41bbd1e97e526641ded1260a741ec6d1e8129d38 / evidence main eae4b3196823a874b04b5987a87e5f4504584f89.
Independent GitHub source/diff/receipts review; no Windows/live test rerun by Architect.

Accepted: existing AppDataGrid bulkActions + pageSelectionOnly, admin-only row approve/reject and shared confirm Dialog, reason gate, per-row reviewed command builder; approve route distinct from publish. No-end beside end label, equal-height headers, worker CDP0px/no overflow1440/390. Backend untouched.
Actual detail approve/reject/cancel transport facts and direct two-row executor test present. Seller detail lacks moderation. Backend still owns legal actionCodes.

Blocking bulk defects:
- executeAdminOpBatch loops using captured args.scope and actor with no batch-start epoch/revalidation between entries. Per-command executor captures current epoch independently; after an earlier request returns stale_epoch, loop continues and later old-actor entries may dispatch. Stop remaining batch on scope/epoch invalidation.
- confirmAdminOp uses summary counts only, drops result.items and closes plan. Names/reasons/individual outcomes are not presented despite task requirement. Ineligible rows are visible before confirm; failed/unknown per-row results disappear into counts/generic pending cards.
- Unverified-success/transport uncertainty returned by executor as failed may be counted simply failed rather than unknown; preserve honest outcome semantics.

Evidence limits:
8 facts include classify helper, layout, detail approve/reject, mixed classify plus single-detail confirm, two separate detail confirms for partial/retry, seller-detail isolation, direct executeAdminOpBatch. No actual multi-row selected-grid confirm/mixed-result path. Live list selection proves toolbar appears, not command execution. One focused real selected-grid test needed, no broad proof phase.
Origin Admin skip-copy still calls it main storefront even for admin-on-behalf-of-shop; noted wording limit, no new ownership policy.
Production Html404 blocker retained; current next-dev acceptable for local review only, not release readiness.
No full admin-bulk/user acceptance yet. Only R14 existing bulk closure; preserve B and all single-operation progress.
activeTask: TB-STORY-SHARED-ORDERS-STYLE-WORKSPACE-SELLER-PANEL-AND-PUBLIC-REVIEW-282-R14 (ISSUED_NOT_CLAIMED)
automaticNextImplementationTask: NONE
STOP_FOR_ARCHITECT_AND_USER_REVIEW
