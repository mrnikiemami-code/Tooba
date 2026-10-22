PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 563f033177225cf0e00d4042b41bd69f1783469c
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_RELATIONAL_FIXTURE_AND_STORAGE_ATOMICITY_PROOF_COMPLETION
Hard-Timebox: 45 minutes HARD incl cleanup/commit/push/result. STOP new coding/testing at minute35; reserve10 for delivery. Disposable setup<=8min, commands<=120s. One bounded harness correction/rerun only; need second=>PARTIAL/STOP, not another loop.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Finish the currently UNEXECUTED PostgreSQL acceptance of the existing Fulfillment shipping-type catalog using TEST/EVIDENCE changes ONLY. Fix known stale harness identifiers/chronology and strengthen proof BEFORE running it. No production repair may be absorbed. Real behavior/schema failure=>BLOCKED exact reproducer for Architect, not scope expansion.
R12 model/default/schema parity is accepted with ratified decimal HasDefaultValueSql("0")+ValueGeneratedNever. Do not redo/rewrite that production mapping.

RECOVERY / GOVERNANCE
Fetch; verify HEAD==origin/main==Expected-Base-SHA on safe tree, inspect dirty/untracked work. Mismatch/unsafe divergence=>STOP before edits. No reset/stash/rebase/force-push.
Read AGENTS/current recovery, R12 ARCHITECT-REVIEW, applicable backend-feature/AMSC proof guidance and R6/R7/R10-R12 evidence at the changed surface.
All production (including DbContext/factory, shared persistence, ALL migrations/Designers/current snapshot, Domain/Application/Contracts/Endpoints/DI/resources/Host/frontend/dependencies/config/flags) MUST be byte-identical.
Allowed writes ONLY existing Fulfillment.Tests capability fixture/storage/default-parity tests, narrowly needed new test-only helpers/facts under Behavior, received task/evidence and worker recovery pointer. No package/project/runtime wrapper or certification/manifest/skill change.

KNOWN HARNESS DEFECTS — REPAIR IN TESTS UPFRONT

TypeCatalogPostgresSchemaFixture compares current model with migration chain ending BEFORE type/R10; current pending index cannot exist at that old checkpoint.
ReadRawAsync/ReadRootRevisionAsync/CountRawAsync and raw constraint SQL still use quoted PascalCase columns/old FK name. Use actual snake_case and fk_shipping_type_translations_shipping_types_shipping_type_id from the accepted migration. Do NOT change production schema to fit tests.
TwoArrivalGate currently doesn't capture actual original root Revision per context; view Revision is not UPDATE predicate proof.
StatementFailureInterceptor currently counts SELECTs and labels any ReaderExecuted as a completed write. Fix to count actual INSERT/UPDATE/DELETE only.
Old LegacyProjection hides objects by contains("shipping_type")/history prefix. Replace with exact schema/table-owned catalog identity; no broad suppression.

DISPOSABLE TARGET ONLY
Fresh task-owned postgres:17-alpine using locally installed image, no download/install. Publish only127.0.0.1:55436, role tb196, DBs fulfillment_tb196 + fulfillment_tb196_model. Verify port free before start; fail setup8min thenBLOCKED/cleanup.
Parse with NpgsqlConnectionStringBuilder before connection: exact127.0.0.1/55436/tb196/expectedDB; reject multi-host/localhost/shared targets; reuse current strict ValidateTarget. Assert live database/user + container binding before DDL; never log credentials.
No shared5432/tooba_alpha/store-alpha/Host/MigrationRunner or other service. Synthetic fixture data only. Missing configured target must fail closed for required DB proof, no skips/fabricated PASS.

CHRONOLOGY-CORRECT FIXTURE / STRICT GATES
Retain historical additivity AND current-model parity; do not delete one to pass the other.

Resolve initialized snapshot and ACTUAL latest R10 TargetModel versus canonical IDesignTimeModel with real IMigrationsModelDiffer ->0/0. Any mismatch stops dependent DB proof.
Primary DB: actual chain to 20261016120000_AddFulfillmentShippingGroups; capture exact legacy manifest before type migration.
Apply 20261022101415_ShippingTypeCatalog: prove added objects belong exactly to TWO own type tables and their owned PK/FK/CHECK/index/columns, no changed legacy object/default/data. Use exact table identity from pg_catalog, no broad token exclusion.
Apply accepted 20261022182208_AddFulfillmentOutboxPendingIndex: only that one pending index plus history row added; every preexisting object unchanged.
Model DB: canonical GenerateCreateScript projection only. Primary now at CURRENT real chain must match current model EXACTLY across complete manifest (types/defaults/names/CHECK/filter/unique/FK/delete action), excluding only exact history owning table identity; no classified/default/name exceptions. This strict gate must pass BEFORE storage facts execute. Old chronology diagnostic tests may be updated to represent these actual phases, retaining failure on unproved snapshot or ANY current-chain mismatch. No count-only acceptance.
At teardown, real inverse order: Down index to type checkpoint, then Down type to pre-type. Each only if actually applied/history present. Assert exact before manifests restored and no orphan object; no cleanup/no-op labeled Down. If setup fails early, distinguish actual rollback vs cleanup. Do not claim old pre-type schema equals a future model.
Use stored R7/R10 narrow proof by source identity to guide this, but execute new phase/current-chain integration actually required now. All executable history remains unchanged.

STORAGE / SQLSTATE MATRIX — REAL STORE AND PROVIDER
Run existing ShippingTypePostgresStorageTests using corrected fixture after strict gate. Retain:

sort0 -> inactive revision1; fresh-context header/translations/Contracts reader; activate/deactivate/reactivate; selected-language other-text preservation;
identical NONEMPTY patch with advanced clock preserves Revision/CreatedAt/UpdatedAt; stale identical patch returns canonical409 and no mutation;
parameterized raw SQL negatives each independently rolled back: duplicate code, duplicate(type,language), own-parent FK, requiredNULL code/name, blank name, malformed/upper/nonASCII/padded code, sort-1/1000001, revision0, name129/description513; exact actual SQLSTATE AND owned constraint/column identity where provider exposes it; at-limit Unicode128/512 and sort0/1000000 read back in new context.
Fix stale column/FK identity EXPECTATIONS to actual accepted schema, not weaken state/constraint assertions. A test of "foreign schema table absent" is not Contracts-boundary proof; label it accordingly.
Any genuine production invariant failure=>BLOCKED, capture raw SQL/outcome, do NOT edit mapping/migration/store/Domain.

STRICT REVISION RACE
Two independent contexts/stores with distinct proposed complete header+text states. Gate at EACH participant's FIRST SavingChangesAsync after load/staging, BEFORE either write; bounded two-arrival20s, never wait holding a write lock.
Record entry<ShippingType>.OriginalValues[Revision] separately for each ACTUAL context; assert both1 +twoarrivals. Exactly ONE successful result AND ONE canonical shipping_type.revision_conflict (both-success/both-fail invalid).
Winner identified from actual outcomes, not literal text "winner"/"loser". NEW-context root Revision EXACTLY2; full header and every translation match that participant's complete proposal, no hybrid/extra loser translation; CreatedAt preserved. ACTUAL loser context holds zero non-Unchanged entries, later SaveChanges publishes0 and re-reads winner state.
Do not assert immutable earlier view.Revision as original-token evidence.

UNIQUE CREATE RACE
Both existence prechecks finish before first write (SavingChanges two-arrival barrier). Different server-created ids, SAME canonical code, distinct complete translation proposals.
Exactly one success/one canonical duplicate, ONE persisted root with its ENTIRE winning translations, no mixed/orphan rows. Assert winning id from real result; loser has no pending entries and later SaveChanges0 changes nothing. Reject raw provider exception masquerading as canonical duplicate.

MID-SAVE FAILURE / REAL CALLER CANCELLATION
Test-only MaxBatchSize(1); write-only interceptor recognizes actual INSERT/UPDATE/DELETE and records first COMPLETED write plus second write attempt. Reads don't increment/fulfil the proof. Fail SECOND real write after first completed, before implicit commit.
Injected failure: exact injected exception propagates as appropriate (not fake success), NEW context shows identical prior full root/all text/timestamps/revision/no orphan; clean retry succeeds.
Cancellation: caller CancellationTokenSource is ACTUALLY Cancelled at second-write boundary (not merely an uncancelled synthetic OCE); same caller token and cancellation outcome propagate, no business/empty-success conversion. If injecting a chosen OperationCanceledException after cancellation, assert same instance too. Full new-context tree unchanged and valid retry succeeds. Keep proof of first completed write and second boundary in evidence; no SELECT counted as write.

EXACT CONSTRAINT OWNERSHIP
Through REAL ShippingTypeStore catch path with synthetic provider failures: exact ix_shipping_types_code/23505 -> duplicate; exact ix_shipping_type_translations_type_language/23505 -> translation invalid.
Lookalike name containing owned token and exact owned name with wrong state23503 both propagate unchanged DbUpdateException (assert same injected instance where feasible). No message parsing. Actual raw SQL matrix above is the provider half; synthetic test isn't a real unique-race substitute.

SMALL R12 TEST-PROOF HARDENING (NO NEW PRODUCTION/BASELINE)

Assert current Never/BeforeSave=Save/AfterSave=Save for all five default properties OUTSIDE any evidence-path/capture conditional; default absence must not be inferred from GetDefaultValue()==0 alone.
Capture narrowly sanitized ACTUAL INSERT command column->parameter->submitted value binding for affected fields (zero/empty/nondefault), not merely "any parameter contains value"; assert exact bound value and fresh readback. Persist that trace. Reuse the committed R12 baseline case summaries as historical evidence; do not rebuild/recompile old production or pretend old summaries are raw SQL traces.
Reject invalid capture/verify mode; required verify baseline/evidence absence cannot silently disable claimed before/after comparison. A standalone current-invariant assertion must run independent of file output.
Add actual negatives demonstrating changed default and a same-prefix NON-history object are detected by the SAME strict manifest comparison/exact-history identity policy. No production/shared table; task-owned synthetic manifest or disposable test object cleaned exactly. Retain all CHECK/name negatives.
If this optional trace hardening cannot fit after required relational work, report it UNEXECUTED/PARTIAL precisely; do not claim full R12 trace proof. No rerun of already accepted R12 schema projection just for confidence.

FOCUSED EXECUTION / STOP
Build only Fulfillment.Tests and required references (task-owned isolated artifacts if needed, no process restart). Run corrected ShippingTypePostgresStorageTests collection once with live fixture, plus affected metadata/gate/parser/strict-mapping/default-current facts explicitly. Avoid unrelated DB-owning R7/R10/R11/R12 test classes competing on same target. Default trace uses same validated primary serially before teardown or a separate isolated phase; no fixture lifecycle collision.
ONE harness-only correction/rerun max after the first execution. If another correction or production fix needed, stopPARTIAL/BLOCKED with exact case; no retry loop. Intentional baseline negatives are separate from harness errors.
No full Fulfillment/Shipping/Host/solution or broad Architecture suite, no repeated race confidence runs. No test skip/filter exclusion of a REQUIRED case to report green. State exact selected/required set and actually executed count.

DELIVERY
Persist received artifact docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13.task.md.
docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13/: RESULT.md, phased schema/current parity and real Down evidence, exact storage matrix SQLSTATE/owned ids, both originalRevision values/winner+loser proof, unique race, first/second write/caller-token rollback+retry trace, constraint lookalikes, default trace executed/unexecuted, hashes proving ALL production unchanged, commands/failures/counts/UTC/timebox/cleanup.
Append worker recovery pointer; R6 evidence corrections append-only (stale assertions were not executed proof). No decision/manifest/certification promotion.
Remove exact task-owned container/output only;55436free. Commit only tests/docs, push fast-forward, fetchverifyHEAD==origin/main, post complete result, STOP.
PASS means bounded catalog relational evidence completed; Production-Functional-Acceptance remainsPENDING (live registry/UI/Shipping assignment/runtime quote not delivered). Architect alone reviews whole196 disposition after result. TB185paused, default-OFF/PendingAllocation and all shared services intact.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_RELATIONAL_FIXTURE_AND_STORAGE_ATOMICITY_PROOF_COMPLETION
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 563f033177225cf0e00d4042b41bd69f1783469c
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
PhasedHistoricalTypeUp-IndexUp-CurrentParity-And-RealDown: ...
RawStorageMatrix-FreshContext-Lifecycle-NoOp: ...
RevisionRace-TwoOriginalTokens-OneWinner-ActualLoser: ...
UniqueCreateRace-ExactWinningTree-And-CleanLoser: ...
WriteOnlyMidSaveFailure-ActualCallerCancellation-RollbackRetry: ...
ExactConstraint-Lookalike-WrongStateProof: ...
DefaultMetadata-UnconditionalBindingTrace-And-Negatives: ...
ProductionMigrationMetadata-ByteParity: ...
ParsedTarget-LiveProvenance-Cleanup: ...
RequiredVersusExecutedTests-And-Counts: ...
FailedRuns-And-CorrectionBudget: ...
ExactRemainingUnexecutedOrFailingGates: ...
TB196-Acceptance: PENDING_ARCHITECT_REVIEW
Shared-Database-Data-Flag-Changes: NONE
Changed-Files: ...
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