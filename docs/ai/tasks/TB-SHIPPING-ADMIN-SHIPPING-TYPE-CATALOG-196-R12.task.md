PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 49f594933f1468e75c011f737745f32a26382540
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_HISTORICAL_DEFAULT_MODEL_ALIGNMENT_WITH_EXPLICIT_INSERT_PARITY
Hard-Timebox: 35 minutes HARD including cleanup/commit/push/result. Stop NEW implementation/tests at minute28; reserve7. Disposable setup<=8min. Commands<=120s.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION / EXACT ARCHITECT DECISION
Close the FIVE remaining historical-server-default/model discrepancies WITHOUT changing current EF explicit-write behavior, runtime business rules or any database schema. Model follows the existing migrated DB; no new fallback/business defaults.
For ONLY these existing Fulfillment properties, use correctly typed HasDefaultValue PLUS explicit ValueGeneratedNever after it:
FulfillmentItem.QuantityPacked decimal ->0m
FulfillmentItem.QuantityProcessing decimal ->0m
Shipment.ProviderMetadataVersion int ->0
Shipment.ShippingMethodCode string ->string.Empty
Shipment.ShippingMethodLabel string ->string.Empty.
Preserve current type/precision/requiredness/accessors/CLR initializers/save behavior. Do not let HasDefaultValue convention switch insert generation to OnAdd or omit zero/empty values.
This supersedes earlier default-mapping prohibition ONLY for these five annotations and the strict proof below. If EF cannot keep explicit-write semantics with these annotations, STOP/BLOCKED, no alternative silent behavior change.

FIRST GATE / REUSE
Fetch; verify HEAD==origin/main==Expected-Base-SHA; inspect tracked/untracked work. Mismatch/divergence=>STOP precise state before edits. No reset/stash/rebase/force-push.
Read AGENTS/current recovery, R11 ARCHITECT-REVIEW, applicable backend-feature/AMSC/Structure changed-surface/migration instructions, current FulfillmentDbContext and authoritative old migration default expressions.
R11 independently saved primary218/model218 with exactly5defaultpairs (10rawlines); CHECK/name/index facts now aligned. Preserve all accepted fixes. Authorize only this task; no next-task envelope or replay.

ALLOWED PRODUCTION SURFACE
Only FIVE property mappings inside existing FulfillmentDbContext; current FulfillmentDbContextModelSnapshot; and latest
20261022182208_AddFulfillmentOutboxPendingIndex.Designer.cs BuildTargetModel.
Regenerate metadata canonically with existing factory/tools. Preserve new default metadata AND explicit non-generation semantics; prove rather than assume generator handles the override.
Keep latest executable .cs Up/Down BYTE-IDENTICAL and EVERY older executable migration/Designer BYTE-IDENTICAL (including ShippingTypeCatalog). No new migration file/id/history row. No factory/shared persistence/business/API/Contracts/Endpoints/DI/Host/frontend/project/package change.
Task-owned scaffold is permitted only as a metadata carrier; delete exact scratch files safely; do NOT retain/apply generated AlterColumn/default DDL. Never modify existing migration history to fit the model. No tool install/upgrade.

REQUIRED INSERT PARITY — REAL EF / REAL PROVIDER
Before changing the mapping, capture concrete baseline ValueGenerated + BeforeSaveBehavior + AfterSaveBehavior for the five properties and actual EF INSERT command columns/parameters on the disposable historical target. Use a small test-only DbCommandInterceptor, not a hand-written SQL replacement for EF.
Exercise both zero/empty AND nondefault values for ALL FIVE fields:

decimal quantities0 and lawful positive fractional values within numeric(18,6);
metadata version0 and positive nonzero;
method code/label empty string and explicit nonempty strings.
After authorized mapping change, execute same cases through actual FulfillmentDbContext SaveChanges:
ValueGenerated remains Never and save behavior matches baseline;
EVERY INSERT still explicitly supplies each affected column and its actual value, including zero/empty, never DEFAULT or omitted;
NEW-context readback preserves each submitted value exactly; no rounding/string fallback;
explicit NULL for required strings is not silently replaced by server empty-string defaults (canonical EF/provider requiredness may reject, but must not succeed with substituted text).
Keep command evidence narrowly sanitized: only task-owned SQL/affected values, no connection/password/customer payload.
Use existing domain factories/test builders when lawful. If a persistence-only fixture needs constructing a mapped object or assigning EF CurrentValues to reach an empty/NULL boundary normalized by a domain factory, keep it test-only and explicitly label this ORM-contract proof, not accepted domain input or a production bypass. No public setter/new runtime seam/InternalsVisibleTo change.

Durable tests must detect both a changed server default and accidental ValueGeneratedOnAdd/omitted-column behavior. An intentional pre-fix default-metadata failure may establish the regression; distinguish it from a harness correction, never weaken expectations to green.
Historical raw omit-column writers already get these defaults from the DB. Verify on disposable target that omit-column SQL still yields the same five values and explicit NULL remains rejected; this does not authorize inventing a new application fallback.
No application normalization, API validation, actor/language/currency or fulfillment state transition change.

STRICT SCHEMA / METADATA PROOF

Initialized real IMigrationsModelDiffer: current snapshot vs canonical IDesignTimeModel=0; ACTUAL latestR10 TargetModel vs current=0. Retain normalized semantics and original shipping-type identifier/concurrency assertions. No ignore list / operation mutation.
Fresh task-owned primary DB: apply REAL unchanged historical chain THROUGH R10. Capture complete own-schema normalized pg_catalog manifest BEFORE. No model-created primary.
Separate task-owned model DB: project CURRENT canonical model using GenerateCreateScript. DDL is allowed only for this disposable projection, not primary reconciliation.
Compare complete manifests with Assert.Equal / both-direction exact set equality: schema/table/column/type/nullability/default, PK/FK/index/check identity+definition, ordered columns/uniqueness/filter/FK action. Exclude migration-history objects by EXACT (schema,table) identity IN THE CATALOG QUERY (including index owning table), not string.Contains/prefix matching.
Expected difference is ZERO, including default expressions. Do NOT remove matched default pairs, strip defaults/names/checks, broad-normalize expressions or introduce a whitelist. Negative tests must show changed default or a same-prefix non-history table is detected, not excluded.
Tighten HistoricalNameAlignmentResidualDiffTests to the new zero-difference contract (rename fact/class if useful within same capability). Replace its prior residual classifier with strict whole equality and exact history-object query. Record older five-pair proof as history; this is an intentional stricter lifecycle assertion, not baseline weakening.
Primary schema BEFORE==AFTER; synthetic EF/raw rows are only task test data, no schema/data migration. Hash all existing executable migrations and older designers.
Do NOT modify/run the old TypeCatalogPostgresSchemaFixture/full-schema collection in this slice: its pre-R10 chronology needs separate integration authorization; independent full-chain parity here does not prove the old collection passed. No storage/type unique/revision-race battery now.

DISPOSABLE AUTHORITY / BOUNDS
Fresh task-owned postgres:17-alpine from locally present image ONLY. No download/install.
Publish only127.0.0.1:55436; role tb196; DBs fulfillment_tb196 and fulfillment_tb196_model; task credentials never logged.
Port free beforehand; parse each with NpgsqlConnectionStringBuilder before any connection; exact127.0.0.1/55436/tb196/expectedDB, reject localhost/multi-host/shared targets; reuse fail-closed ValidateTarget with exact expectedDatabase. Assert current_database/current_user and container binding before DDL.
No shared5432/tooba_alpha/store-alpha/Host/MigrationRunner/Redis/RabbitMQ or any other DB. Setup fails8min=>BLOCKED + cleanup. Primary historical-chain migration and task-owned synthetic inserts permitted, no reconciliation DDL or production rollout.
Separate model DB must start empty. Capture schema/data proof around activities with fresh contexts.

FOCUSED CHECKS / STOP
Build only Fulfillment.Tests and required references; isolated output if locked, never stop a shared process. Run new default-insert facts, strict schema-parity fact and affected metadata/CHECK-name/factory offline facts only. Explicit filters: no competing DB-owning R7/R10 lifecycle tests on same target.
No full Fulfillment/Shipping/Host/solution/broad Architecture run, no package/cache operations, no repeated confidence run.
ONE harness-only correction/rerun maximum; need a second=>STOP/PARTIAL with exact failures. Maintain35min stop28; do not extend to finish a bigger feature.
No unrelated cleanup, guard weaken, certification/manifest promotion, extra schema fix, foreign DbContext/join/FK/dependency or duplicated rule.
No Shipping/UI/Storefront/Checkout/Payment/Settlement/Promotion/carrier/funder/cutoff/quote/activation/deployment/flags change. TB185 paused; default-OFF/PendingAllocation untouched.
Preserve dirty/untracked/stashes/shared bin/obj/.next/cache/apps/ports. No resets/rebase/stash/force-push. Never dummy/probe POST Bridge /api/results.

DELIVERY / TRUTHFUL GATES
Persist task verbatim docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12.task.md.
docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12/RESULT.md plus before/after metadata+captured INSERT SQL/parameter proof, value/NULL/omit results, canonical regenerated diff, primary/model manifests+ZEROdiff, hashes, executed commands/counts/failures, UTC/timebox/cleanup.
Append worker recovery pointer only. Whole196 remains BLOCKED_PENDING_FULL_RELATIONAL_ACCEPTANCE even if default/full-chain parity passes; old fixture integration/storage/races/atomicity/constraint-lookalikes are separate unexecuted gates. No whole-module certification/product/merchant runtime acceptance.
Remove only task-owned container/output/scaffolds; verify55436free. Commit scoped changes, push fast-forward, fetch HEAD==origin/main, post complete result then STOP_FOR_ARCHITECT. No auto next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R12
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_HISTORICAL_DEFAULT_MODEL_ALIGNMENT_WITH_EXPLICIT_INSERT_PARITY
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 49f594933f1468e75c011f737745f32a26382540
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
ExactFiveDefaults-And-ValueGeneratedSaveBehaviorParity: ...
BeforeAfter-RealEfInsertColumnsParameters-And-NewContextReadback: ...
ZeroEmpty-Nondefault-ExplicitNull-And-RawOmitProof: ...
Canonical-Snapshot-LatestTarget-ModelParity: ...
HistoricalChain-Versus-CurrentModel-ZeroDifference: ...
ExactHistoryIdentity-And-NegativeDetection: ...
PrimarySchema-And-OlderExecutableMetadata-Parity: ...
ParsedTarget-LiveProvenance-Cleanup: ...
Focused-Commands-And-Test-Counts: ...
FailedRuns-And-CorrectionBudget: ...
FullRelationalFixture-Storage-Race-Acceptance: PENDING_NOT_RUN
TB196-Acceptance: BLOCKED_PENDING_FULL_RELATIONAL_ACCEPTANCE
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