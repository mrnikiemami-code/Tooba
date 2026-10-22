PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 0399dd5933165f6ca473eb2d2a6751e2ac41ec1c
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_REGISTRY_LIFECYCLE_MODULE_TRACING_AND_REAL_DI_EVIDENCE
Hard-Timebox: 35 minutes HARD including commit/push/result. Stop new test work at minute 28; reserve7minutes for cleanup/report. STOP/PARTIAL on expiry; no finishing missing facts afterward.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
TEST/EVIDENCE ONLY for the remaining nonrelational type-catalog gates: authoritative Localization adapter-backed language lifecycle, real ModuleCallTracer listener proof and actual FulfillmentModule DI registration. R2 Domain and R3+R4 API proof are accepted; do not repeat/rewrite them. PostgreSQL/snapshot/constraint/race proof remains pending. No new feature or product code repair in this task.

FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work BEFORE edits; mismatch/unsafe tree => STOP without edits. No reset/stash/rebase/force-push. Read AGENTS.md, recovery, TB-196/R1/R4 Architect reviews, repository backend-feature skill, actual Shipping type handlers/store/language adapter and canonical telemetry implementation/tests. Reuse existing lawful foundation/fixture patterns, not an unrelated module's implementation. Base includes726ae303 transport document; do not use onlya638266b.
Scope tests to the type catalog; do not re-audit Fulfillment or repair legacy tracing. No skill/manifest/architecture promotion.

A. REAL PRODUCTION REGISTRATION, CONTROLLED DATA
Invoke the actual FulfillmentModule.AddServices registration entry and actual endpoint-presentation/foundation/CQRS registrations needed for this capability. Do NOT reproduce AddScoped<ShippingTypeStore>/IShippingTypeStore/IShippingTypeReader/IShippingTypeLanguageFacts manually as R3 did and call that production DI proof.
Replace ONLY test data boundaries: configure own EF InMemory context safely (remove conflicting Npgsql option-config registrations where needed), controlled ILanguageLookup, IClock/id and required platform snapshots. Do not replace the module's type store/reader/adapter/gate registrations. Register no new production framework, migrator or hosted-service execution.
Resolve the actual capability services in a scope: concrete ShippingTypeStore, IShippingTypeStore and IShippingTypeReader are SAME scoped instance; a second scope gets a different store. IShippingTypeLanguageFacts resolves to LocalizationShippingTypeLanguageFacts; IModuleCallTracer resolves to the canonical foundation implementation; real handlers/validators resolve. No duplicate descriptor/resource owner introduced.
Do not validate/resolve every unrelated Fulfillment worker or start a Host merely to test these registrations. Existing module migrator registration must NEVER be executed; no provider connection, schema discovery or migration call. Record which data-boundary descriptors were overridden so production capability wiring remains independently assessed.

B. REAL ADAPTER -> HANDLERS -> STORE LANGUAGE MATRIX
Use a MUTABLE controlled ILanguageLookup returning LanguageLookupSnapshot with genuine fixture ids + IsActive/IsDefault/SortOrder metadata. Feed it through the ACTUAL registered LocalizationShippingTypeLanguageFacts, real CQRS handlers/validation and own InMemory store. A hand-built ShippingTypeLanguageFacts object bypassing the adapter is insufficient. Exercise actual commands/queries via ISender; no HTTP harness rewrite needed.
Execute:

Exactly ONE active NON-fa language/default: create inactive type with that name, read actual DefaultLanguageId/HasDefaultLanguageName/raw text. No fa fallback.
TWO active mixed RTL/LTR languages: create/patch one language then another; first language's stored name/description stay byte-identical. Explicit null description clears ONLY the selected pair; [] preserves all text. Raw LanguageIds remain stable independently of interface culture.
THREE-PLUS known languages, with a NON-first active default and one inactive row: create using real declared default, not list position; edit explicit active languages; retain inactive stored text.
SAME stored type: write a language while active, deactivate it in the registry, GET retains its exact raw text, an attempted edit of that inactive id fails canonically without revision/timestamp/text change, reactivate it and GET reveals the exact previous text. No delete/auto-translation.
SAME type: change registry default to an active language without stored name; GET HasDefaultLanguageName=false and the actual new default id. Activation attempt fails until that default's name is explicitly written, then activation succeeds. Type active state alone makes no shipping method quote-ready.
Invalid registry cases through adapter/handler: absent IsActive or SortOrder, empty/duplicate id, no active language/default, multiple defaults -> exact current owned canonical code, no guessed order/default, no persisted mutation. Use compact tables and actual error constants.
For culture invariance use a restored CurrentCulture/CurrentUICulture scope (fa-IR/en-US or another configured fixture); do not add locale-to-language logic. Preserve successful no-op/revision semantics; reuse existing R4 proof rather than re-running its whole file.
Record controlled Contracts fixtures != live merchant Localization database proof. No UI registry acceptance is implied.

C. REAL ACTIVITYLISTENER PROOF
Use canonical production ModuleCallTracer over canonical ToobaTelemetry ActivitySource with a REAL ActivityListener and ambient parent/correlation scope. Serialize/filter collection to this capability's module.Fulfillment.Localization.ListLanguages spans; prevent interference with unrelated tests.
For an actual executed ILanguageLookup.ListAsync assert EXACTLY ONE child span, source Fulfillment, target Localization, operation ListLanguages, canonical span name/tags, correct ParentSpanId/shared TraceId and ambient correlation. Assert stopped/disposed once and Activity.Current restored to parent afterward. Across multiple reads span count must equal the fixture's actual lookup count, not a guessed handler count.
Exercise successful lookup, returned invalid registry facts (foreign lookup still completed successfully; subsequent business validation is not a dependency exception), throwing lookup and caller cancellation. Assert the EXACT same thrown exception/cancellation instance/token is preserved where the canonical API permits identity comparison; no catch-to-business-success or empty list. Error instrumentation must use exception TYPE only, canonical status and no swallowed cancellation.
Verify exported tags/events contain NO sentinel exception message, language names/ids, type code/id, tenant, currency, token or user data. No no-listener shortcut, mock-only tracer, optional constructor/no-op fallback, new ActivitySource/correlation framework, or production instrumentation patch.
Real listener failures are evidence of a blocker, not authority to edit production. Do not alter legacy neighboring adapters to make tests green.

BOUNDED VERIFICATION
Only add/modify directly relevant Fulfillment TEST files/fixtures and task evidence; no production csproj/C#/DI/Contracts/Endpoint/resource/Domain/schema/EF mapping/migration/designer/snapshot or existing locks/guards. Own test-project references may only be minimally adjusted for already-existing own-module assemblies if genuinely absent; no package install.
Use focused test classes/filters for this registry/listener/DI slice; build only Fulfillment.Tests and references with task-owned isolated output if needed. No full module/solution/Host build or test, no broad Architecture filter, no repeated confidence runs. Commands <=120s. One bounded harness-only correction/rerun allowed; a genuine production failure => preserve assertion, report PARTIAL and STOP. Do not relax expected behavior or absorb a repair wave.
No container/DB/schema/seed/server/browser/network HTTP listener/probe/migration operation. Never resolve Npgsql connection settings as a substitute for overriding the test data boundary. No shared app/port/bin/obj/.next/cache mutation; no stopping a locked Host.
No exact-constraint lookalike test or PostgreSQL schema/race work in this slice; these remain later gates. No Shipping assignment/frontend/Storefront/financial/grouping/quote/promise/activation/deployment/flag changes. TB-185 paused; PendingAllocation/default-OFF preserved. Preserve unrelated dirty/untracked/stashes, no reset/stash/rebase/force-push, no Bridge dummy/probe payload.

EVIDENCE / STOP
Persist received task verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5.task.md and sanitized docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5/RESULT.md: exact production registrations invoked vs data-boundary overrides, matrix inputs/assertions, listener events/tags stripped of sensitive values, exact commands/counts, limitations and UTC claim/result. Do not call source presence executed runtime proof. Update recovery status pointer, not Architect ACCEPT.
PASS only for this bounded DI/language/listener evidence within35min; PARTIAL for missing required fact/deadline or real failing behavior. Whole TB196 remains PARTIAL_PENDING_CONSTRAINT_AND_RELATIONAL_SNAPSHOT_PROOF (only if this slice fully passes), otherwise list remaining nonrelational gates too. No live registry/browser or production acceptance claim.
Scoped commit/push/fetch HEAD==origin/main, cleanup exact task-owned outputs and stop. No future task file or automatic continuation.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R5
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R4
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_REGISTRY_LIFECYCLE_MODULE_TRACING_AND_REAL_DI_EVIDENCE
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 0399dd5933165f6ca473eb2d2a6751e2ac41ec1c
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Actual-Module-DI-Invoked-And-TestOverrides: ...
Scoped-Store-Alias-And-Adapter-Tracer-Resolution: ...
OneTwoThreePlus-Registry-Through-RealAdapter: ...
DeactivateReactivate-DefaultChange-And-RawText: ...
Invalid-Registry-NoGuess-And-NoMutation: ...
RealListener-Parent-Correlation-Count-And-Disposal: ...
Exception-Cancellation-Identity-And-NoSensitiveTelemetry: ...
Focused-Commands-And-Test-Counts: ...
Exact-Failing-Behavior-If-Any: ...
Changed-Files: ...
Production-Schema-Contract-Changes: NONE
Constraint-Lookalike-And-Postgres-Snapshot-Race-Proof: PENDING_NOT_RUN
TB196-Acceptance: PARTIAL_PENDING_REMAINING_PROOF
Shared-Database-Data-Flag-Changes: NONE
Isolation-And-Cleanup: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Hard-Deadline-Compliance: ...
Out-Of-Scope-Commands: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK