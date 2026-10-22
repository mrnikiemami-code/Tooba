PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2
Parent-Task: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AUTHORIZED_DRAFT_TRANSACTION_GUARD_AND_PROOF_HARDENING
Bridge-Task-Row: 80a440d4-0324-4805-aa2f-9c5f46fb2b8b
Status: PASS
Expected-Base-SHA: 260904c8ca5ae51c7a06af6103c1e2d215050d8f
Base-SHA: 260904c8ca5ae51c7a06af6103c1e2d215050d8f
Code-SHA: __CODE_SHA__
Evidence-SHA: __EVIDENCE_SHA__
HEAD-Origin-Parity: HEAD == origin/main == __CODE_SHA__
Historic-Authorization-And-Guard-Invariant: OPTION 1 implemented — the authorized single Shipping-owned staged replacement transaction and the immediate uniqueness constraints are preserved; no deferred-constraint migration was created. The obsolete "no BeginTransaction text anywhere in Shipping" assertion is replaced (not deleted) by ShippingTransactionBoundary: it proves exactly one reviewed transaction start site at ShippingAuthoringDraftDirectory::ReplaceRegionGroupsAsync (one begin, >=2 staged saves, one commit, rollback path) while still forbidding every startup-schema authority, every operational money/capacity write, and every unexpected transaction. The allowed file is NOT blanket-excluded: its content and enclosing member are checked, so other members of the same file fail. Static-limitation stated: the scanner is not a data-flow or runtime-enforcement proof; the executed PostgreSQL facts are the behavioural half.
Unexpected-Transaction-Negative-Proof: PASS — executed negatives: transaction in another Shipping file -> UNEXPECTED_SHIPPING_TRANSACTION naming that file/member; transaction in another member of the allowed file -> same rule (no filename-only exemption); a second begin inside the reviewed member -> AUTHORING_TRANSACTION_SHAPE; stripped boundary -> AUTHORING_TRANSACTION_SHAPE.
Allowed-File-Content-And-Startup-Migration-Negative-Proof: PASS — MigrateAsync/Database.MigrateAsync injected inside the allowed file -> SHIPPING_STARTUP_SCHEMA_AUTHORITY; production Shipping has zero startup migration authority; operational money/capacity markers -> SHIPPING_OPERATIONAL_WRITE_AUTHORITY; the operational-migration non-existence, approved-consumer set, Contracts-only observation pair, registration-only Order edge, default-OFF gate, capacity enum and legacy read-seam assertions are retained with equal or stronger coverage.
Parsed-PreWrite-Isolation-And-Negatives: PASS — ShippingRelationalTestTarget parses with NpgsqlConnectionStringBuilder BEFORE any connection and compares to test-owned constants 127.0.0.1 / 55432 / shipping_tb190r1 / tb190r1. Offline negatives (no network): absent config, foreign host carrying allowed text in ApplicationName, non-loopback host, multi-host list, port 5432, tooba_alpha/store-alpha/postgres databases, postgres/tooba_app roles — all rejected. Live pre-write verification asserts current_database()/current_user; container binding verified as 5432/tcp=>127.0.0.1:55432 postgres:17-alpine with task-only database and role. XML claiming missing config "skips" facts corrected; implementation throws (fail closed, nothing skipped).
Deterministic-Original-Revision-Race-And-Actual-Loser-Proof: PASS — TwoReaderBarrierInterceptor arrives at the FIRST SavingChanges of both participants (after each read and prepared revision 1, before either write; a bare BEGIN holds no row lock, no lock-holding wait) and records the concurrency token ORIGINAL value: both assert 1, which is the exact UPDATE predicate. Gate is bounded (20s) so a failed participant cannot hang the suite. Either participant may win; winner/loser derived from actual results; stored tree equals the WINNER's tree at revision 2; no staged order residue; the ACTUAL losing context (loser.Index) has zero non-Unchanged tracker entries and re-reads the winner's tree.
Second-Stage-Cancellation-And-Rollback: PASS — caller token cancelled at the start of the SECOND SaveChanges (after the staged write, before commit): OperationCanceledException propagates (not converted to a fake business success), fresh-context readback keeps revision 1, the identical previous group ids/orders/area ownership, no staged residue, and a valid retry then succeeds. The injected second-save failure rollback test is retained.
Relational-Reorder-Shift-Insert-NodeMove: PASS — real PostgreSQL: swap A@0,B@1,C@2 -> C@0,B@1,A@2, delete-first shift 1->0/2->1, insert-before retained groups, and node move between groups preserving the SAME selected-area row id; final orders exactly 0..n-1, no duplicate draft node, no staged residue.
Focused-Commands-And-Test-Counts: build 0 errors; OperationalQuoteCompatibilityTests 19/19; ShippingAuthoringDraftRelationalTests + ShippingRelationalTargetIsolationTests 17/17.
Final-Shipping-Suite: 558 passed / 0 failed / 0 skipped / 558 — executed while the isolated database was still running (before cleanup), with no exclusions or skips hiding required proof.
Production-Executable-Schema-Flag-Changes: NONE (one comment-only XML correction in ShippingAuthoringDraftDirectory; no production C#, contract, endpoint, handler, validator, DI, schema, migration, snapshot, flag or authority change; no production behaviour repair was needed and no new runtime flaw was found).
Changed-Files: src/backend/Modules/Shipping/Tooba.Shipping.Infrastructure/Authoring/ShippingAuthoringDraftDirectory.cs (XML comment only); src/backend/Modules/Shipping/Tooba.Shipping.Tests/Compatibility/ShippingTransactionBoundary.cs (new detector); src/backend/Modules/Shipping/Tooba.Shipping.Tests/Compatibility/OperationalQuoteCompatibilityTests.cs (guard refactor + 6 negative facts); src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingRelationalTestTarget.cs (new parsed isolation target + 12 offline negatives); src/backend/Modules/Shipping/Tooba.Shipping.Tests/Authoring/ShippingAuthoringDraftRelationalTests.cs (parsed isolation, live target check, deterministic barrier race, second-stage cancellation); docs/architecture/shipping/SHIPPING-ADMIN-RULES-DECISION.md (appended section 20, authorization/proof correction only); docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-22.md (status pointer only); docs/architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2/{RESULT.md,worker-result.md,post-result.js,task-receipt.json}; docs/ai/tasks/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190-R2.task.md.
Isolation-And-Cleanup: fresh task-owned container tooba-shipping-tb190r2 (postgres:17-alpine) on 127.0.0.1:55432, database shipping_tb190r1, role tb190r1; port verified free before start and zero listeners after docker rm -f; shared :5432/tooba_alpha/store-alpha, shared apps and shared ports untouched; no credentials logged; setup inside the 8-minute bound, every command under 180 seconds.
Elapsed-Minutes: 44
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
