PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0e53f4603b299b2f5e61f6d4604ac9957a0c109f
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_HISTORICAL_SCHEMA_RECONCILIATION_ANALYSIS_ONLY
Hard-Timebox: 20 minutes HARD including evidence/commit/push/result. Stop new investigation at minute15; reserve5 for delivery. Report PARTIAL at expiry; no extension or investigation loop.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Produce a source-backed, object-by-object reconciliation proposal for the Fulfillment historical migrated schema versus the UNCHANGED canonical current model after accepted R8. Analysis and documentation ONLY. Do not implement the proposal, run a database, scaffold metadata or repair tests. R8 is complete; do not replay it.

RECOVERY / AUTHORITY
Fetch; verify HEAD == origin/main == Expected-Base-SHA and inspect tracked/untracked state before edits. Any mismatch or unsafe divergence: STOP with exact state, no speculative repair. No reset/stash/rebase/force-push.
Read AGENTS.md, current Shipping recovery, applicable backend-feature/AMSC Analyze instructions and migration guidance, R6/R7/R8 Architect reviews, R6 raw schema manifests and the relevant source provenance. Read only relevant decision/lock sections; no broad repository audit or findings-register work.
R8 proves snapshot and latest TargetModel match canonical IDesignTimeModel (reported 0/0 differences); it does NOT establish historical database parity. R7 isolated latest Up/Down remains accepted for its narrower scope. Neither needs reexecution.

SCOPE / INPUTS
Use existing R6 historical/model manifests, migration source up to 20261016120000_AddFulfillmentShippingGroups, unchanged latest ShippingTypeCatalog Up/Down, current FulfillmentDbContext/configuration and R8 canonical metadata.
Prior evidence reports: missing historical ix_outbox_messages_pending, migration-only ck_shipping_methods_code_canonical and ck_shipping_methods_option_code_canonical, five defaults and ten object-name differences. Treat these as leads, NOT a required count or new ground truth. Re-derive the actual bounded discrepancy set from the saved manifests and exact source; classify missing/ambiguous facts honestly.
No DB/container/network probe or schema projection; Git read/fetch/push for the authorized documentation workflow is permitted.

REQUIRED DELIVERABLES

SOURCE-OF-TRUTH-MATRIX.md
For EVERY reported discrepancy, record exact schema/table/object; historical fact from raw manifest + owning migration/path; canonical model fact from configuration/metadata/path; exact expression/type/default/ordered columns/filter/action where relevant; behavioral significance; confidence/missing proof.
Separate actual object absence/presence, defaults, naming-only equivalence and metadata mismatch. Do not re-label the repaired 191 naming operations as historical database differences.
For claimed naming equivalence compare full shape (columns/order/uniqueness/filter, FK target/delete action etc.), not just similar names. If existing evidence lacks a dimension, mark UNKNOWN_PENDING_EXECUTION, not equal.

RECONCILIATION-PROPOSAL.md
Recommend ONE smallest dependency-ordered approach with reasons and explicit decisions needing Architect authorization:

Pending-outbox index: trace existing shared Tooba persistence/outbox mapping and existing module migration conventions. Identify whether this is a shared infrastructure invariant and who owns creating it; no parallel outbox policy or broad module migration program.
Shipping-method CHECKs: trace actual canonical validation/normalization and historical CHECK semantics. Recommend preservation/alignment only with explicit compatibility rationale; do not silently discard database protection or assume current model always wins.
Defaults: explain insert behavior/server-side contract and current omission/CLR-value behavior; do not assume harmless because today's tests set a value.
Names: identify whether renaming is necessary at all for runtime/error catch/index identity/future migration generation. Do not manufacture full equality by excluding all names or accepting a blanket baseline.
State any future runtime mapping addition, additive migration or metadata regeneration by exact scope; ALL remain PROPOSED, not performed. Old applied migrations/designers are immutable. No amendment of older executable history. No seed/backfill or destructive operation.
If one authorized future repair cannot safely resolve all findings, split planning into small dependency-ordered slices, not executable task envelopes. Recommend the FIRST slice only for next authorization.
Record safe Up/Down/data-preservation considerations and future disposable-Postgres proof: actual historical chain -> proposed change -> exact intended object delta, preserved unrelated schema/data, real Down where lawful, model parity, raw storage constraints and strict concurrency/rollback proof. Do not invent PASS for unrun assertions.
RESULT.md
Exact reviewed baseline, discrepancy identities/counts derived vs unresolved, recommended first slice, decisions/blockers, changed files, checks actually run (source analysis only), elapsed/UTC claim/result, cleanup and stop.
Append only a worker pointer to current recovery. Persist this received task verbatim under docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9.task.md.
Allowed writes: that task receipt, docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9/**, and recovery WORKER pointer only. No approved decision rewrite; proposals must be clearly unapproved. No executable helpers unless indispensable to deterministic offline manifest comparison; keep them task-owned, no new packages or external services.

HARD EXCLUSIONS
NO src/** changes (including tests, runtime mapping, factory, snapshot/designer, migrations, contracts, endpoint/auth/DI/resources). No manifest/certification/skill/Host/frontend changes.
No build/test suite, dotnet ef, schema generator, PostgreSQL/container/server/browser/HTTP probe, MigrationRunner, package/image installation, cache/bin/obj/.next cleanup or shared process/port operation.
No Shipping assignment/type UI/customer grouping/quote/payment/carrier/funding/cutoff/activation/deployment. TB185 paused, default-OFF/PendingAllocation and legacy financial authority unchanged.
No guard weakening, skipped tests, model/manifests filters, false snapshot=historical equality or applying proposed DDL. Existing untracked/stashed/dirty work preserved. Never send dummy/probe Bridge /api/results payload.
If source evidence cannot establish a safe decision within this bound, report the precise missing fact and STOP; do not create a live database to fill it.

DELIVERY
Commit only authorized documentation; push fast-forward; fetch verify HEAD==origin/main. Do not run next task or generate its downloadable envelope. STOP_FOR_ARCHITECT.
Worker PASS means completed bounded analysis only; whole TB196 remains BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE and production acceptance PENDING.
One active task: after this result the Architect independently reviews before authorizing any next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R9
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R8
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_HISTORICAL_SCHEMA_RECONCILIATION_ANALYSIS_ONLY
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 0e53f4603b299b2f5e61f6d4604ac9957a0c109f
Base-SHA: ...
Code-SHA: UNCHANGED
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Derived-Discrepancy-Set-And-Provenance: ...
Outbox-Checks-Defaults-Naming-Decisions: PROPOSED_NOT_IMPLEMENTED ...
First-Repair-Slice-Recommendation: ...
Unresolved-Facts-And-Architect-Decisions: ...
Checks-Actually-Run: SOURCE_ANALYSIS_ONLY
Production-Metadata-Schema-Data-Flag-Changes: NONE
HistoricalSchema-And-FullRelationalAcceptance: PENDING_BLOCKED
TB196-Acceptance: BLOCKED_PENDING_HISTORICAL_SCHEMA_AND_RELATIONAL_ACCEPTANCE
Changed-Files: ...
Isolation-And-Cleanup: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: NONE
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK