# Architect review — TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Date: 2026-10-22 (Asia/Tehran)
Reviewed head: dc69068352dad28a9f8f440e107c764d707b0370
Implementation/evidence checkpoint: e0495691b2e2ed4175bdb0823cc0a8ce1ab0c6ee
Disposition: BOUNDED_FIXES_REVIEWED / GUARD_AND_PROOF_REPAIR_REQUIRED. No full Architect ACCEPT yet.

## Decision on the reported blocker
Choose option 1: preserve the existing Shipping-owned two-stage replacement transaction and immediate uniqueness constraints. TB-190-R1 explicitly authorized that transaction. Do NOT create a deferred-constraint migration just to satisfy the historical scanner.
OperationalQuoteCompatibilityTests.The_operational_slice_adds_no_migration_and_no_money_movement scans the entire Shipping module for transaction text. That broad assumption predates persisted authoring. Authorize only a bounded guard correction proving the one reviewed authoring replacement transaction while continuing to reject operational/money/schema-startup authority and any unexpected transaction. No blanket file/folder exclusion, skip, baseline refresh, or Assert.True substitute.
Next/only task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2, parent TB-190-R1. This is continuation of the repair, not rate/editor implementation.

## Reviewed implementation
- Required RegionGroups is no longer conditional on its own presence; null Request guards and missing/null HTTP regressions exist.
- Full preflight of retained identities and node uniqueness precedes mutation.
- Replacement uses one ShippingDbContext transaction, staged positive orders then final zero-based orders, one commit; revision remains a concurrency token and selected-area rows can move without losing identity.
- Original migrations/snapshot are unchanged and no new schema is required.
- Worker reports real PostgreSQL 17 tests 4/4 and module suite 538 pass / 1 fail / 539. Architect read code/evidence, did not run .NET/PostgreSQL here; counts are worker-reported.
- Production XML on ShippingAuthoringDraftDirectory still incorrectly says a single SaveChanges/no manual transaction; a comment-only correction is authorized.

## Additional evidence gaps discovered independently
1. The race test uses Task.WhenAll but has no barrier proving both contexts loaded the same revision before either persisted. It assumes context A wins (asserts A's exact result and checks context B), so scheduling can create a false failure or merely test an early stale check. Add a bounded deterministic two-reader barrier before writes, assert exactly one winner without assuming its index, and check the actual loser tracker plus winner-specific persisted snapshot. Never wait at a point requiring both transactions to hold conflicting row locks.
2. NewContextAsync validates the raw connection string with substring Contains assertions. That does not prove actual Host/Port/Database: a non-loopback connection with the allowed strings in ApplicationName can pass. Parse with the existing NpgsqlConnectionStringBuilder; require exact loopback host, task-only port/database/user and reject multihost/shared targets BEFORE opening/migrating. Container ownership is also a pre-write operational guard. Test adversarial strings offline, never by connecting.
3. The test XML says missing configuration skips facts, but implementation throws. Preserve fail-closed behavior, correct the explanation and document that full suite execution includes PostgreSQL provisioning; do not manufacture skipped/passed facts or run the suite against a removed database.
4. The reported rollback test injects an ordinary exception, not caller cancellation. Add targeted cancellation after first stage and prove unchanged persisted tree/revision and original cancellation propagation. If a production flaw is discovered, report PARTIAL with exact code impact instead of broadening a guard-only/proof slice.

These findings do not assert a shared database was touched or that reported PostgreSQL runs were fabricated. They identify missing guarantees in future repeatable proof.

## Gates retained
No frontend, rate rows, modes, taxonomy, schedule, activation, financial or Storefront changes. No new language/owner authority. Contracts-only, ISender/Result/validation, locale/error semantics, Host boundaries, default-OFF flags, PendingAllocation and TB-185 pause remain locked. Full product/editor/production acceptance stays pending. R1 result and original task remain immutable history; append corrections only.
