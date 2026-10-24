TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
Channel: tooba-main
Worker-Id: tooba-worker-01
Repository: mrnikiemami-code/Tooba
Branch: main
Parent-Task: TB-SHIPPING-ADMIN-LIVE-EXISTING-DRAFT-READINESS-206
Expected-Base-SHA: a932fa2915fed4827635d0cb8dbb0260baf35945
Mode: BOUNDED_CURRENT_HOST_BUILD_AND_READ_ONLY_STARTUP_CHANGE_PLAN
Hard-Timebox: 20 minutes
Stop-New-Work: minute14; final6 evidence/cleanup/commit/push/fetch/report
Automatic-Next-Task: NONE
Stop-State: STOP_FOR_ARCHITECT

Concrete blocker / objective

206 established a stale running Host, not an empty draft list: shared FE3000 rewrites to Host5088;
Host is healthy but predates Shipping authoring routes. Existing draft/cardinality/auth/schema remain UNKNOWN.
Prepare the CURRENT Host binary and a concrete startup/database change plan in one bounded pass.
Do not ask the user to approve a vague restart; return the exact pending changes and command/environment plan first.
No old UI/proof-suite replay and no feature development.

Read AGENTS + TOP canonical recovery and 206 ARCHITECT-REVIEW.md. Persist this task verbatim.
Verify current main/origin parity and preserve unrelated dirt. Docs-only understood descendants of expected base may be preserved with a recorded explanation; uncertain source divergence stops.

Known source hazard — investigate, do not execute

src/backend/Host/Tooba.Host/Program.cs after builder.Build():
Development+Marketplace calls MarketplaceDevelopmentBootstrap.ApplyAsync.
Development+SingleStore calls DevelopmentSchemaMigrator.ApplyAsync or MigrateSchemaOnlyAsync,
then Content/Support/Wallet seeds and the ShippingOneLevelCatalogDevelopmentSeed gateway.
RunLegacyBootstraps=false DOES NOT mean no migration/no seed.
Hosted Outbox/messaging/module services may also write on start.
Therefore restarting either old OR new Host is NOT authorized in this task.
Do not change ASPNETCORE_ENVIRONMENT to bypass the blocks; that changes authentication/runtime semantics.

Work in order

Reconfirm the exact existing Host5088 process identity/output path and frontend3000 origin read-only.
Preserve both processes and their configuration. Do not trust historical PID24304 without rechecking.
Capture the actual startup environment/edition and canonical database target resolution privately; no credential dumps.

Read source for the startup branches above and the configured module-migrator order, host workers and relevant options.
Make an exact table: startup operation -> current configured gate -> database/data/schema effects -> existing suppression if one really exists.
Do not invent flags, edit Program, remove services, switch edition/environment, weaken guards, or claim disabling one seed disables all.
No migration/seed/bootstrap/Host/MigrationRunner execution, not even for inspection.

Determine applied migration state with READ-ONLY SQL against ONLY the database(s) the existing development target resolves.
Use the existing approved local credentials privately; verify current_database/current_user before reading.
Restrict connection to read-only transaction/default_transaction_read_only, short statement timeout (~5s), no migration context startup or connection/credential printing.
Read exact owned migration-history tables and compare applied ids to checked-in executable migrations for modules the actual startup will migrate.
Existing current system catalog tables may be queried to confirm the pending outbox-index collision risk
(fulfillment.ix_outbox_messages_pending exact table/columns/filter/uniqueness if present).
Never interpret a preexisting same-name index as automatically adopted: R10 migration intentionally fails closed.
Identify pending Shipping/Fulfillment changes and any other startup-wide pending modules. Missing history is UNKNOWN/missing prerequisite, not permission to create it.
Read no business payload; no customer/merchant data dumps. No schema projection, migration generation/apply, DDL/DML, seed/backfill, database reset or container.

Build ONLY src/backend/Host/Tooba.Host/Tooba.Host.csproj and necessary references ONCE, from current sources,
using the repository-supported isolated output/intermediate procedure so locked running outputs/bin/obj/shared files are not overwritten.
Validate the chosen MSBuild artifact routing actually isolates REFERENCES too (do not blindly set common OutputPath and create collisions).
Existing SDK/packages only; no install/upgrade or network package restore. Build timeout <=6min, capture real exit status.
If a safe isolation recipe cannot be established in3min, return BLOCKED_BUILD_ISOLATION; no repeated command experiments.
Do NOT stop Host for a DLL lock or copy over its binaries. Do not run the built executable.
No test suite/solution build/architecture sweep.
Retain the task-owned built output if successful for the next approved refresh, record exact path/source SHA/hashes and launch prerequisites. Do not commit binaries.
If build fails, report FIRST source/environment error with exact diagnostic, no repair wave.

Return a concrete reviewable Host-only refresh plan:

exact current rollback executable/working directory/launch context, captured without secrets;
exact new isolated executable/source SHA, environment/config working directory and port5088;
exact startup migration/seed/background effect set from current config and pending history;
preexisting index collision yes/no/unknown and its impact;
whether an already-existing fully non-mutating startup mode exists (source/config proof), otherwise state NONE;
smallest next action for approval/implementation, distinguishing a runtime refresh from schema/data changes.
If pending changes exist, name their actual migration ids and relevant Up operations; do not merely say "approve all".
No restart, even when pending count0: seeds/workers must still be accounted for.
Source/code and build should be ready for a narrow next refresh step; no further analysis task proposal by default.
Evidence / result

docs/architecture/evidence/TB-SHIPPING-ADMIN-HOST-REFRESH-PREFLIGHT-207/RESULT.md:
verified base/source; host provenance; startup-effects table; read-only verified database aliases and history/pending ids;
exact index shape collision observation; isolated build command/status/time/path/hashes; concrete refresh/rollback plan;
first blocker if any; no invented live/mounted readiness.
Commit only docs/task-owned safe scripts + verbatim task + ONE current recovery worker pointer.
Never commit credentials/config secrets/build output. Preserve old evidence.
Shared frontend/Host processes still unchanged, schema/data/flag unchanged; no temporary sensitive file remains.
Production-Functional-Acceptance=PENDING; Live-Write-Acceptance=PENDING.
Stop for Architect; no automatic next task; do not run historical post-result.js or Bridge probes.
