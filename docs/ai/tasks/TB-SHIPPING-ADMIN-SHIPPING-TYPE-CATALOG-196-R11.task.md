PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 8aaf599b1212ea50c1fc75a149892b7317b5b9df
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EXISTING_CHECK_AND_HISTORICAL_OBJECT_NAME_MODEL_ALIGNMENT
Hard-Timebox: 35 minutes HARD incl cleanup/commit/push/result. STOP new implementation/tests at minute28; reserve7. Disposable setup <=8min; each command <=120s.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION / ARCHITECT DECISION
Align the Fulfillment canonical model to TWELVE ALREADY EXISTING database facts: two historical shipping_methods CHECKs and ten historical FK/index names from accepted R9 matrix. Preserve protection and avoid database renames. No new migration or legacy DDL; no default/insert-generation behavior change.
R10's pending index migration is accepted and must remain unchanged. Whole196 still blocked pending defaults/full relational proof.

RECOVERY
Fetch and verify HEAD == origin/main == Expected-Base-SHA; inspect dirty/untracked/stashes without mutation. Mismatch/unsafe divergence => STOP exact state; no reset/stash/rebase/force-push.
Read AGENTS.md/current recovery, R9/R10 ARCHITECT-REVIEW, applicable backend-feature/AMSC/Structure-on-changed-surface instructions, R9 SOURCE-OF-TRUTH-MATRIX and actual owning migrations/model configuration. Reuse existing module ownership/conventions.
Worker analysis proposals are not blanket authorization. This task authorizes ONLY the exact facts below and their focused proof.

AUTHORIZED MODEL FACTS
In own FulfillmentDbContext's existing entity configuration:

Describe two checks from 20261015120000_AddShippingOneLevelCatalog EXACTLY (names and semantic SQL):
ck_shipping_methods_code_canonical:
length(method_code) BETWEEN 1 AND 32 AND method_code = lower(method_code) AND method_code !~ '[^a-z0-9_:]'
ck_shipping_methods_option_code_canonical:
legacy_option_code IS NULL OR (length(btrim(legacy_option_code)) BETWEEN 1 AND 64 AND legacy_option_code = lower(legacy_option_code))
Use existing mapping API patterns. Do not replace SQL with the stricter domain validator, trim more data, add new check or drop existing protection.

Pin these existing physical names with HasDatabaseName/HasConstraintName, preserving EXACT same columns/order/unique/filter/principal/delete action:

ShippingMethodTranslation FK -> fk_shipping_method_translations_method
consolidated_package_members index(consolidated_package_id) -> ix_consolidated_package_members_package_id
consolidated_package_members UNIQUE(shipment_id) WHERE released_at IS NULL -> ix_consolidated_package_members_shipment_active
shipping_groups UNIQUE(fulfillment_id,shipping_group_id) -> ix_shipping_groups_fulfillment_group
shipping_method_translations UNIQUE(shipping_method_id,language_id) -> ix_shipping_method_translations_method_lang
shipping_methods UNIQUE(legacy_service_code,legacy_option_code) WHERE legacy_option_code IS NOT NULL -> ux_shipping_methods_legacy_binding
shipping_service_option_translations UNIQUE(shipping_service_option_id,language_id) -> ix_shipping_service_option_translations_option_lang
shipping_service_options index(shipping_service_id) -> ix_shipping_service_options_service_id
shipping_service_options UNIQUE(shipping_service_id,code) -> ix_shipping_service_options_service_code
shipping_service_translations UNIQUE(shipping_service_id,language_id) -> ix_shipping_service_translations_service_lang
Verify each against authoritative raw-DDL origin and current model before edit. Any unexpected shape difference => STOP, no invented alignment.

EXACT METADATA EXCEPTION
Regenerate current FulfillmentDbContextModelSnapshot and ONLY latest
20261022182208_AddFulfillmentOutboxPendingIndex.Designer.cs BuildTargetModel using unchanged existing canonical factory/tooling plus authorized mapping changes above. Preserve context/class/namespace/migration id.
The corresponding executable R10 .cs Up/Down remains BYTE-IDENTICAL. EVERY older executable migration and EVERY older Designer, including ShippingTypeCatalog, remains byte-identical. No new migration file/id/history row.
This explicitly supersedes R10's metadata immutability ONLY for current snapshot and latest R10 generated target metadata.
Use installed generator APIs or task-owned temporary scaffold as metadata carrier; no package/tool install/upgrade. Delete exact scratch files safely; generated Rename/Alter/Drop SQL must never be applied or retained as executable migration.
Current model evolves: older historical TargetModels need not forever equal current model. Update the R8 parity regression's "latest TargetModel equals current" selection to the ACTUAL latestR10 (or unambiguously ordered actual migration universe), retaining real initializer/differ and zero-operation assertions, normalized semantics and original type-specific identifiers/concurrency assertions. Do not rewrite historical ShippingTypeCatalog metadata to make an obsolete equality pass. R10 current/latest parity test remains strict.
No ignore lists, name stripping, CHECK exclusion, baseline refresh or accept-any-state guard.

DEFAULTS — EXPLICITLY OUTSIDE SCOPE
Do NOT add HasDefaultValue, ValueGeneratedOnAdd/ValueGeneratedNever overrides, drop default, change CLR initializers, insert behavior, domain validation or data.
Five known differences remain visible:
items.quantity_packed=0; items.quantity_processing=0;
shipments.provider_metadata_version=0;
shipments.shipping_method_code=''; shipments.shipping_method_label=''.
They are NOT accepted as permanent asymmetry by this task; no whitelist or full-schema gate relaxation. Runtime insert semantics/default decisions require later bounded authority.

REQUIRED PROOF
Offline:

Before/after source/model semantic comparison shows ONLY two check additions and ten physical-name annotations. No property/type/nullability/default/value-generation/index-shape/delete-action/foreign-reference delta.
Actual initialized IMigrationsModelDiffer: current snapshot vs canonical IDesignTimeModel =0; latestR10 TargetModel vs current=0. Canonical generated normalized relational metadata agrees.
Durable tests assert exact 12 model facts and unchanged shape (including partial filters, unique, FK target/delete action); include negative demonstration that a changed name or missing check is detected rather than filtered.
Hash/diff all executable migrations and every older Designer. No behavior/contract/DI/Host change.

Executed database proof (isolated, not shared):
Fresh task-owned postgres:17-alpine from existing local image; no image/install. Only127.0.0.1:55436, role tb196, DBs fulfillment_tb196 AND fulfillment_tb196_model. Port free beforehand. Parse each target with NpgsqlConnectionStringBuilder exact host/port/role/database before connection; reuse existing ValidateTarget with exact expectedDatabase, reject multi-host/localhost/shared target; assert live current_database/current_user and container binding before DDL. No credentials in output.

Real historical migration chain THROUGH accepted R10 on primary DB; capture normalized own-schema manifest BEFORE. No model-created primary DB.
Project CURRENT canonical model into task-owned MODEL database only via existing GenerateCreateScript mechanism (DDL authorized for this disposable projection ONLY).
Compare full bounded manifest dimensions, including schema/table/column/type/nullability/default, PK/FK/check/index names and definitions, ordered columns/unique/filter/FK actions. Compare same table universe incl type tables; exclude migration-history table by exact table identity only, not broad tokens.
Result must consist EXACTLY of the FIVE known DEFAULT pairs (10 raw lines). Prove no residual CHECK/name/pending-index/other difference. PostgreSQL expression deparsing differences may require explicit structurally equivalent SQL representation; do not broad-normalize identifiers/expressions to hide actual differences. If any unclassified difference remains, BLOCKED exact identity, no extra fix.
Primary manifest BEFORE == AFTER model projection/test activity; all primary migration objects unchanged. No new primary DDL beyond applying its unchanged real migration chain. New mapping adds no executable migration and changes no row.
This is a focused independent residual-diff proof. Do not use the blocked whole-schema collection to run downstream storage/race assertions, and do not modify its substantive/default/name gate to pass with remaining defaults.
If setup unavailable at8min, STOP/BLOCKED and cleanup, no fallback to shared DB.

FOCUSED CHECKS / STOP
Build only Fulfillment.Tests and required references with isolated output if locked; never stop running Host. Run new mapping/residual-diff proof plus affected TypeCatalogModelMetadataParityTests and OutboxPendingIndexMigrationTests OFFLINE facts only (explicit filter; do not accidentally run its DB-owning migration lifecycle concurrently on same target). Include exact factory metadata test if affected, no full module/Host/solution/broad Architecture/storage race run.
ONE bounded harness-only correction/rerun maximum. After first failed correction or any second correction requirement, STOP/PARTIAL preserving failures. R10's two corrections exceeded its authority; do not repeat.
No new package/project, service/wrapper/catalog, cross-module implementation/DbContext/join/FK, Host logic, DTO/API/auth/locale/currency/owner change.
No Shipping/frontend/Storefront/Checkout/Payment/Settlement/Promotion/activation/deployment/manifest/certification/skills/default-OFF flag/PendingAllocation change. TB185 paused.
No shared :3000/:3419/:5088/:5432/DB/Redis/RabbitMQ, cache/bin/obj/.next cleanup, reset/stash/rebase/force-push. Preserve unrelated work and helpers.
No dummy/probe Bridge /api/results; only complete result after commit/push.

DELIVERY
Persist received task verbatim docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11.task.md.
docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11/: RESULT.md, exact mapping/metadata before-after, generated model-only SQL, sanitized primary/model manifests, exact residual10lines, proof of unchanged primary/executable/older artifacts, exact executed commands/counts/failures/UTC/timebox/cleanup.
Append only worker recovery pointer. Clarify no full196 acceptance, no customer-facing change and no deployed schema claim.
Remove only task-owned container/output/scaffolds; verify55436free. Commit scoped files, push fast-forward, fetch verify HEAD==origin/main, post result, STOP_FOR_ARCHITECT. Do not issue/execute next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R11
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R10
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_EXISTING_CHECK_AND_HISTORICAL_OBJECT_NAME_MODEL_ALIGNMENT
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 8aaf599b1212ea50c1fc75a149892b7317b5b9df
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
ExactTwoChecksTenNames-And-UnchangedShapes: ...
Canonical-Snapshot-LatestTarget-ModelParity: ...
HistoricalChain-Versus-Model-OnlyFiveDefaultPairs: ...
PrimaryManifest-And-OlderExecutableMetadata-Parity: ...
DefaultAndInsertGenerationChanges: NONE
ParsedTarget-LiveProvenance-Cleanup: ...
Focused-Commands-And-Test-Counts: ...
FailedRuns-And-CorrectionBudget: ...
Remaining-Defaults-And-FullRelationalAcceptance: PENDING_BLOCKED
TB196-Acceptance: BLOCKED_PENDING_DEFAULTS_AND_RELATIONAL_ACCEPTANCE
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