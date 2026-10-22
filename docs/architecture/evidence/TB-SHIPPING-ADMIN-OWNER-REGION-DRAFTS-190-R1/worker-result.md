PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DRAFT_VALIDATION_PREFLIGHT_AND_RELATIONAL_REPLACE_REPAIR
Bridge-Task-Row: ddd0e203-fc9d-4217-97c1-7482215ab4d4
Status: BLOCKED
Expected-Base-SHA: 4b5da2b018b64aa14a227c0942d3f6ad14c1b323
Base-SHA: 4b5da2b018b64aa14a227c0942d3f6ad14c1b323
Code-SHA: e0495691b2e2ed4175bdb0823cc0a8ce1ab0c6ee
Evidence-SHA: e0495691b2e2ed4175bdb0823cc0a8ce1ab0c6ee
HEAD-Origin-Parity: HEAD == origin/main == e0495691b2e2ed4175bdb0823cc0a8ce1ab0c6ee
Required-Collection-HTTP-Validation: PASS — required NotNull is now unconditional (parent-Request guard only) on both write validators; [] remains a valid incomplete draft; POST omitted, POST explicit null, PUT omitted and PUT explicit null all return 400 validation.failed with shipping.validation.region_groups_required before any handler dispatch (counter) and persist nothing; handlers additionally refuse an explicit null collection instead of normalising it to empty.
Same-Aggregate-And-Tracker-Preflight: PASS — PreflightRegionGroups proves shape, retained-ID membership, repeated/unknown/foreign IDs, node validity and draft-wide node uniqueness before any Apply/Order/Revision/timestamp change; regression with a valid first retained group and a bad LAST identity leaves the same aggregate instance, identical group/area snapshot, revision, UpdatedAt, all tracker entries Unchanged, and a later SaveChanges in the same context publishes nothing; a valid replacement afterwards succeeds with the retained group id.
Postgres-Reorder-Shift-Insert-NodeMove: PASS — real PostgreSQL 17: pure swap C@0,B@1,A@2, delete-first shift, insert-before retained groups, and node move between groups preserving the SAME selected-area row id; final orders exactly 0..n-1, no staged order residue, no duplicate draft node.
Real-Revision-Race-And-Atomic-Rollback: PASS — two independent Npgsql contexts at revision 1: exactly one success, loser returns shipping.authoring.draft_revision_conflict with no loser group/area rows and an empty tracker; a test-owned interceptor failing the SECOND SaveChanges after the staged write rolls the whole replacement back (previous tree, revision 1 and area ownership intact on new-context readback, then a valid retry succeeds).
Duplicate-Binding-And-Owner-Isolation: PRESERVED — the owner-scoped unique binding index and whole-owner filters were not modified; every read/write still filters by the full owner scope.
Persistence-Strategy-And-Transaction-Boundary: One Shipping-owned transaction, both stages, single commit, rollback on every failure/cancellation; temporary non-conflicting order range (StagedOrderBase) then final zero-based order; node moves are UPDATEs of the same row. No second business authority, no cross-module transaction, no provider-message classification. Non-relational (test) path keeps a single save with final orders.
Corrective-Migration-And-Historical-Byte-Parity: NONE required — the immediate unique index is preserved and satisfied by the staged strategy; no TB-190 or older migration/designer/snapshot file was touched.
Real-Postgres-Acceptance: PASS
Architecture-Authorization-And-Locale-Parity: PRESERVED — no frontend, Storefront, Host, Contracts signature/route/permission, catalog, rate, taxonomy, schedule, currency or language change; no new error code or resource; no D2/D4/D5 work; TB-185 pause, default-OFF flags, PendingAllocation and legacy Shipping error statuses untouched.
Focused-Commands-And-Test-Counts: build 0 errors; ShippingAuthoringDraftRepairTests 8/8; ShippingAuthoringDraftEndpointTests 4/4; ShippingAuthoringDraftRelationalTests (PostgreSQL) 4/4; whole Tooba.Shipping.Tests 538 passed / 1 failed / 539.
Changed-Files: src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Validation/ShippingAuthoringDraftValidators.cs; src/backend/Modules/Shipping/Tooba.Shipping.Application/Authoring/Commands/ShippingAuthoringDraftCommands.cs; src/backend/Modules/Shipping/Tooba.Shipping.Domain/Authoring/ShippingAuthoringDraft.cs; src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDraftDirectory.cs; src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringDraftRepairTests.cs (new); src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringDraftRelationalTests.cs (new); src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringDraftEndpointTests.cs (dispatch counter + required-collection HTTP case); docs/architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1/{RESULT.md,task-receipt.json,post-result.js}; docs/ai/tasks/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1.task.md.
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: task-owned container tooba-shipping-tb190r1 (postgres:17-alpine) on 127.0.0.1:55432, database shipping_tb190r1, task credentials; fixture fails closed on a missing/shared connection string; shared :5432/tooba_alpha and every other shared port untouched; container removed and port 55432 free.
Elapsed-Minutes: 45
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE

BLOCKER (single, precise):
The only failing test is the pre-existing OperationalQuoteCompatibilityTests.The_operational_slice_adds_no_migration_and_no_money_movement. It scans the whole Shipping module for the literal BeginTransaction/DbTransaction and asserts the offender set is empty. The task explicitly authorises one Shipping-owned transaction as a narrow exception to TB-190's single-save preference, and that authorised code is the scanner's only offender. Softening or refreshing that guard is forbidden by this task, so it was left untouched and no baseline was refreshed. Architect must choose: (1) authorise a bounded update of that legacy scanner to permit the single module-owned replacement transaction, or (2) authorise an additive corrective migration replacing the immediate unique index with an equivalent DEFERRABLE INITIALLY DEFERRED unique constraint so the single-SaveChanges preference can be restored with no explicit transaction. All three R190 findings are otherwise repaired and proven (see RESULT.md).
END_TOOBA_WORKER_RESULT
