TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162
Parent-Task: TB-SHIPPING-OWNER-OPTION-COVERAGE-161
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-FLAT-METHOD-COMMERCIAL-READINESS
Mode: BOUNDED_ISOLATED_POSTGRES_MIGRATION_UP_DOWN_AND_LEGACY_DATA_PROOF
Expected-Base-SHA: 37f71ccc7591adc5a68e62a901aa33eaf96111c6
Timebox: HARD 45 minutes; one bounded execution, no infinite retries
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Implementation-Task: NONE

Mission

Prove the REAL PostgreSQL migration introduced in TB-161 (20261016211322_OptionScopedCoverageRestriction) applies safely and can be rolled back in a disposable environment, without changing or touching shared services or databases. This task is acceptance/fixture repair only; do not begin the Order storefront authority cutover, add new financial modes, or expand Admin UI.

Mandatory inspection

Read docs/planning/shipping/SHIPPING-ARCHITECT-RECOVERY-2026-10-16.md, docs/architecture/shipping/SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md, TB-160 and TB-161 results, the actual Shipping EF model/migration/designer/snapshot and the Shipping MigrationRunner ownership path. Confirm exact migration chain, SQL, indexes and constraints from source; do not guess table/schema names. Verify git HEAD == origin/main == Expected-Base-SHA before modifications; otherwise STOP with facts. Preserve all unrelated dirty paths and stashes.

Strict environment safety
Use one NEW task-owned disposable PostgreSQL 15+ container/database on unused loopback ports and a unique task prefix; no shared :5432, tooba_alpha, store-alpha tenant data, :3000, :5088, RabbitMQ or Redis writes, stops, migrations, restarts or purge.
Before ANY DDL, execute a fail-closed target guard recording DB host, port, database name, unique scratch container ID, tenant/connection configuration and migration target. Abort on ANY shared identifier or ambiguity. Never store passwords, tokens, connection strings or cookies in committed evidence.
Use the repository's real migration mechanism (MigrationRunner / legitimate EF pathway), not hand-authored approximation of the new migration. Snapshot/check data at each stage. Rollback only against the disposable database. Cleanup exactly task-owned PIDs, containers and files by positive match; no wildcard/broad kill, reset, rebase, autostash or force-push.
Required proofs
Initialize scratch with the immediate previous Shipping migration state using the real migrator. Seed representative pre-TB-161 persisted Shipping coverage grants: service-level option_code=NULL, associated destinations and rates, including more than one sibling canonical option where supported. Record a sanitized baseline row count, identity, relevant properties and absence of the new declaration table.
Apply 20261016211322_OptionScopedCoverageRestriction using real PostgreSQL. Verify migration history, new nullable option column, canonical CHECK constraint, rebuilt unique identity index, shipping.method_coverages table and its mode CHECK/unique semantics against information_schema/pg_catalog; check that each original grant remains service-level with option_code=NULL, no accidental backfill to standard/express, no row loss or money changes.
On the upgraded scratch schema, exercise genuine insert/read/update for two distinct method declarations of the same service: one Unrestricted and one Restricted; method-scoped grants must affect only their owner. Verify disallowed invalid values, duplicate identities and, importantly, Restricted with zero own grants denies, while absent declaration/Unrestricted is unrestricted for strict mode only and still requires an owned active rate. Verify LegacyFallback still uses service-level grants and does not inherit option-scoped grants. Prefer true owner code/lookup integration where feasible, supplemented with DB assertions; distinguish source tests vs HTTP/DB proofs.
Roll back exactly this migration in disposable PostgreSQL via the official migration path. Verify previous schema/index shape and preservation of pre-existing legacy rows. New-format declaration/option-specific data may legitimately be discarded by rollback: capture the impact explicitly; do not assert lossless downgrade of newly-created rows. If downgrade has a legitimate blocking dependency, report precise blocker and PARTIAL, don't suppress it.
Re-apply (Up again) on the same scratch to show reproducible migration from the restored legacy state, preserving the legacy rows; verify no EF drift or collision. Clean up scratch and confirm shared listeners/processes unaffected.
Acceptance & reporting
PASS only with actual isolated PostgreSQL Up -> Down -> Up proof, schema/catalog checks, preserved legacy rows, method isolation and fail-closed evidence. PARTIAL if a required leg is unverified; BLOCKED if environment/safety prohibits a leg. Never report tests or screenshots not actually executed.
Only modify migration/schema production source if a reproduced defect in this migration blocks acceptance; keep fixes owned by Shipping and narrowly tested, explain source diff, run minimal focused Shipping persistence/coverage tests and compile. No speculative schema rewrite. If no defect, production Code-SHA=NONE; commit only task artifact and sanitized evidence.
Save evidence under docs/architecture/evidence/TB-SHIPPING-OPTION-COVERAGE-MIGRATION-162/: RESULT.md, isolation guard, commands/logs (sanitized), schema/index and migration-history before/after, legacy row preservation matrix, ownership/strict-vs-legacy assertion matrix, rollback/reapply and cleanup proof. Record actual pass/fail counts, time, verified Code/Evidence SHA and origin parity.
Do not make a second parallel task. STOP_FOR_ARCHITECT after pushing a fast-forward evidence commit. Next architectural dependency after this task: Order's default-OFF, Shipping-owned StrictOwnedOption customer quote integration, with trusted measurements / MeasurementRequired fail-closed and authenticated isolated purchase-preview proof; not authorized in this task.
END_TOOBA_TASK
