PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 3f05917bd0e71297bdd1fd973ad8dcd8b6687f93
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_DRAFT_TYPE_ASSIGNMENT_VIA_FULFILLMENT_CONTRACTS
Hard-Timebox: 45 minutes HARD incl cleanup/commit/push/result. STOP new coding/testing at minute35; reserve10. Disposable setup<=8min; commands<=120s. One bounded harness-only correction/rerun; need another=>PARTIAL/STOP with evidence.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Deliver the optional owner-local Shipping draft-root shipping-type selection using the EXISTING Fulfillment.Contracts.IShippingTypeReader. This is the next product slice after ACCEPTED bounded TB196 catalog. Do NOT replay/re-certify the catalog or build a second taxonomy.
Rule authority: SHIPPING-ADMIN-RULES-DECISION.md section27; preserve earlier locks and server scope/language/financial authority.

FIRST GATE / SKILLS
Fetch; verify HEAD==origin/main==Expected-Base-SHA on safe tree before edit, inspect dirty/untracked work. Mismatch/unsafe divergence=>STOP exact state. No reset/stash/rebase/force-push.
Read AGENTS/current recovery, section26/27 and relevant locks, backend-feature plus applicable AMSC/Structure-on-changed-surface instructions, existing Shipping authoring command/query/directory/Domain/DI/endpoints/classification/errors/tests. Reuse canonical capability folders and patterns; no repository-wide audit.
TB196 is accepted for catalog/API/Contracts/schema/storage, not whole-module certification/live merchant/UI/quote. Only this task active.

MODEL / PERSISTENCE
Shipping owns ONE nullable Guid ShippingTypeId on ShippingAuthoringDraft ROOT. Null=explicitly unconfigured; not per group/rate/language/schedule or copied definition text/code. Local domain guard rejects Guid.Empty before mutation; no foreign module in Domain.
One additive Shipping-schema migration adding nullable shipping_type_id + own nonempty-guid CHECK if consistent with mapping/domain. No cross-schema FK/navigation/join, no Fulfillment table/metadata/migration edit, no new index absent a real query need. Existing draft rows stayNULL (no inferred/backfilled type). Every older Shipping migration/Designer unchanged; update own snapshot/new Designer canonically using current module factory conventions.
No startup migrator or shared DB application.
One shared root Revision token, one implicit SaveChanges for assignment+revision/UpdatedAt (no explicit transaction). Atomic preflight/no-op/stale semantics same as existing method settings. All unrelated regions/rates/currency/translations/titles/weekly schedule/settings and other drafts preserved.

CONTRACTS-ONLY READ / APPLICATION OWNERSHIP
Infrastructure adapter consumes ONLY Fulfillment.Contracts.Shipping.IShippingTypeReader, already on the lawful project dependency path. Existing fulfillment catalog owns id/code/active/order/revision/raw LanguageId translations.
Expose the minimal own Application read port/facts if needed, wired scoped once in ShippingModule. No foreign Domain/Application/Infrastructure/Endpoints reference, no DB connection/context resolver copy, no direct HTTP/shared mutable state/cache/catalog mirror.
Use Application handlers for composition of own draft and external facts; endpoints authorize and dispatch only. Directory owns only Shipping persistence. Validate existence/owner/revision BEFORE non-null foreign validation; recheck revision at save through the actual root token. No tracked mutation before any rejection.
Every ACTUAL GetByIdsAsync/ListActiveAsync call gets one canonical mandatory IModuleCallTracer child span (Shipping->Fulfillment), distinct constant operation names; exact cancellation token, same exception propagation, dispose once and parent/correlation restored. No new ActivitySource/logger/correlation/tracer registration, no optional no-op tracer, no ids/codes/names/translations/owner values in telemetry. Exceptions/cancellation never become []/unconfigured success.
Catalog activity is a read-instant fact, NOT a distributed transaction or lock against later definition deactivation.

EXACT PUBLIC BEHAVIOR
Add six routes on existing authorized module composition:
GET /v1/{admin|seller}/shipping-services/authoring-types
GET /v1/{admin|seller}/shipping-services/authoring-drafts/{draftId:guid}/method-type
PUT /v1/{admin|seller}/shipping-services/authoring-drafts/{draftId:guid}/method-type
Both audiences share ONE choices query, ONE current-assignment query and ONE replace command/handler.
Choices: existing whole-owner fulfillment.view permission BEFORE dispatch/I/O, ListActiveAsync only, owner-declared order and raw DB LanguageId texts intact; empty active catalog is valid, unavailability is not empty. No Shipping taxonomy admin/create/update.
GET assignment: full authorized owner+draft filter first; foreign/unknown draft same safe404 and ZERO foreign lookup. Null assignment => unconfigured. Saved id resolved via GetByIdsAsync: return raw id and truthful active/inactive/missing resolution; inactive/deleted reference never cleared or silently replaced. Dependency failure remains error, never "missing" invented from exception.
PUT: fulfillment.manage independently before body read/dispatch; real authorizer SingleStore seller capability404 before seller resolution, Marketplace seller own scope only. Body ONLY ExpectedRevision and explicit ShippingTypeId member, no owner/seller/tenant/edition/definition/code/text.
Presence MUST distinguish omitted vs explicitNULL: omission rejects, explicitNULL clears. Use existing presence-aware/standard JsonRequired transport mechanism, not a new global serializer/framework. Guid.Empty/malformed/nonpositive revision rejects before handler with stable canonical errors; direct invocation cannot silently coerce absent choice toNULL.
Every non-null PUT (including identical stored id) resolves EXACT active definition. Unknown/inactive=>stable rejection and no mutation; reject inconsistent returned ids/facts rather than guessing. No-op for same ACTIVE selection/current revision preserves revisionANDUpdatedAt. Same null/current revision is a true no-op. Stale expectedRevision409 wins before foreign lookup even for identical/clear request; assignment no-op never bypasses stale check.
Read of existing inactive/missing assignment remains truthful; PUT of inactive SAME id still rejects (explicit section27 decision). Clearing needs no foreign read and works while catalog unavailable.
No side effects in Fulfillment; references only. Missing type does NOT mean another owner/draft exists.
Interface culture never chooses content LanguageId or fallback name; pass raw multilingual facts, no fixedfa/en/IRR. Existing context remains language/currency authority; no new registry policy.

CANONICAL API / GUARDS
Endpoints -> real ISender/CQRS/ValidationBehavior -> handlers -> own ports, Result -> ApiResponseFactory; reuse existing authorized body reader and error/resource machinery. New typed errors/descriptors once, neutral+fa resource coverage+configured neutral fallback; no literal user error prose or message parsing.
Validate every new transport-reachable input; choices query exemption only if no caller input, prove provenance with exact universe. Update shipping-request-classification.json and guard comparisons by REAL routes/requests/handlers/validators, not counts alone. Verify literal authoring-types route reachable without collision.
Changes to structure/error/classification expectations may reflect intentional facts, no root widening/skip/baseline refresh or certification promotion. No Host business/composition edit unless unavoidable existing module entry mapper wiring (inside Shipping).
Do not rewrite preexisting routes/DTO semantics. Existing DTOs may receive only minimal additive fact if needed; dedicated view is preferred. Type selection doesn't publish/enable a method.

REQUIRED FOCUSED PROOF — BUILD UPFRONT BEFORE BROAD RUNS

Real module route delegates, real owner authorizer + body reader + MediatR validation + handlers + directory; platform auth and catalog Contracts facts controlled labelled fixtures. Authorized choices/current/PUT/clear/no-op/stale; unauthorized beforedispatch/foreignI/O; view-only writes denied; scoped seller denial; Marketplace seller success; SingleStore seller404zeroresolution; foreignStore/Seller draft404 and zero externalI/O. Body owner spoof cannot override context.
Malformed/missing/null/emptyGuid/revision shape; omitted type DOES NOT clear; explicitNULL clears; invalid last validation leaves SAME context/tracker/root/all capabilities unchanged and later SaveChanges publishes0.
Active list ordering/emptyvalid, raw 1/2/3+language texts/ids preserved under interface-culture switch; inactive/missing read state retained; inactive/unknown assignment rejected; active->inactive betweenrequests reflected honestly, no implicit draft write; exception/cancellation propagation and zero fallback.
Actual adapter DI and real ActivityListener proof of one span per foreign read, parent/trace/correlation/cleanup, errorTYPEonly, token forwarded, no sensitive tags. No span for foreign-owner/stale/nullclear early branches.
Real PostgreSQL own Shipping migration: NEW-context assign/switch/clear/reload, actual nullable column/check and no cross-schemaFK; historical migration byte parity. Existing owner filters and same binding across independent owners unaffected.
Deterministic assignment-write vs an EXISTING rate/settings/region write on SAME root revision: both actual tracked OriginalRevision1 at first SavingChanges beforewrite, two arrivals, exactly one winner/one canonical409, stored revisionEXACTLY2 and full winner-dependent assignment+other-capability tree with no hybrid. ACTUAL loser tracker clean, laterSaveChanges0. Seed both other-capability starting states precisely; no expected predicate inferred from old view.
Assignment only affects root id/revision/time; seed multilingual text/groups/geography/rates/currency/schedule/method settings and verify unchanged after assign/switch/clear. Stale, unknown/inactive and DB failure publish nothing; valid retry succeeds. No-op timestamp test must use advanced controllable clock or observed pre/post values with non-vacuous boundary, no sleeps.

DISPOSABLE DATABASE / EXECUTION
Fresh task-owned postgres:17-alpine from local image, ONLY127.0.0.1:55432, db shipping_tb190r1,role tb190r1; existing parsed ShippingRelationalTestTarget/rejection matrix reused. Verify free port; parse exact host/port/db/role beforeconnection; assert current_database/current_user and hostbinding beforeDDL. No credentials in evidence.
No shared5432/tooba_alpha/store-alpha, no Host/shared apps/Redis/RabbitMQ/package/image download. Setupfailure8min=>STOP/BLOCKED cleanup, no rescue loop.
Apply migrations only to disposable target. No real Fulfillment tables required: catalog facts through real adapter with labelled controlled IShippingTypeReader; do NOT call foreign DB or claim live merchant definition proof.
Build affected Shipping.Tests+dependencies only. Run new focused tests plus affected request/structure/error guards. If all focused pass by minute25, one final whole Shipping.Tests run is authorized while disposable DBlive; otherwise report focused proof and stop ontime. No Fulfillment.Tests/Host.Tests/solution build/architecturewide sweep, confidence loops or unrelated repair.
Any failure outside scope: record actualidentity, no unproved "preexisting" attribution, no guard weakening. OnlyONE harness correction/rerun; another required=>PARTIAL.

EXCLUSIONS / DELIVERY
No frontend/UI/navigation/global CSS, catalog administration, item/offer compatibility, customer grouping, price/rate evaluation, cutoff, carrier credential/call, freefunding, Payment/Settlement/Promotion/promise/activation/featureflag/deployment. No force/reset/stash/rebase/cache/bin/obj/.next purge or shared processstop/restart. TB185paused/defaultOFF/PendingAllocation intact.
Task docs/ai/tasks/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197.task.md verbatim; docs/architecture/evidence/TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197/RESULT.md with owner/route/input/Contracts/tracing/relational/race/preservation proof, exact commands/counts/failures/migrationSQL/hashes and remaining merchant/UI/runtime gates.
Append decision section27 implementation facts if necessary without changing approved semantics; update request manifest/intentional structure declaration truthfully without certification promotion; worker recovery pointer.
Remove exact owned container/output;55432free. Commit scoped changes,pushfastforward,fetchverifyHEAD==origin/main; complete result;STOP_FOR_ARCHITECT. No Bridgeprobe/dummyresult or auto next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-DRAFT-TYPE-ASSIGNMENT-197
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R13-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_DRAFT_TYPE_ASSIGNMENT_VIA_FULFILLMENT_CONTRACTS
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 3f05917bd0e71297bdd1fd973ad8dcd8b6687f93
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
RootOnlyOptionalAssignment-And-NoCatalogCopy: ...
RealAdminSeller-ChoicesCurrentWrite-And-OwnerPermissions: ...
ExplicitPresence-NullClear-ActiveOnly-NoOp-StaleOrdering: ...
InactiveMissingRead-And-DependencyFailureTruth: ...
ContractsDI-RealListener-And-MultilingualRawFacts: ...
SameContextPreflight-And-CrossCapabilityPreservation: ...
RealPostgres-AdditiveMigration-FreshReadback: ...
StrictSharedRevisionRace-ActualWinnerLoserTree: ...
RequestValidationClassification-Errors-Structure: ...
FocusedAndFinalShippingTests-ActuallyRun: ...
FailedRuns-CorrectionBudget-Timebox: ...
Shared-Database-Data-Flag-Changes: NONE
MerchantRegistry-UI-Quote-Activation: NOT_IMPLEMENTED
Changed-Files: ...
Isolation-And-Cleanup: ...
Claim-UTC: ...
Result-UTC: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK