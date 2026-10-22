PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: ae969bfa3783a4d99df172f419408e9a0a451ccc
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_FULFILLMENT_OPERATIONAL_TYPE_CATALOG_ADMIN_AND_CONTRACTS
Hard-Timebox: 45 minutes HARD including cleanup, scoped commits, push and complete result. Stop new implementation/tests at minute 38; reserve final 7 minutes. PARTIAL on expiry; no detached work or test loop.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Deliver ONE usable Fulfillment-owned operational shipping-type catalog: authorized Admin create/list/detail/update (including active state and language-keyed text), persisted lifecycle/revision, and a narrow read-only Contracts seam. No Shipping assignment yet. The previous suggested catalog-plus-assignment slice is NOT authorized: it crossed two new persistence authorities and omitted catalog management. This task creates no UI, operational pricing or customer-grouping behavior.

MANDATORY FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA, inspect dirty/untracked work before edits; mismatch or unsafe divergence => STOP_FOR_ARCHITECT without edits. No reset/rebase/stash/force-push.
Read AGENTS.md, current shipping recovery, TB-195 ARCHITECT-REVIEW.md, SHIPPING-ADMIN-RULES-DECISION section 26, and the appended operational-classification clarification in SHIPPING-ADMIN-FLAT-METHOD-CONTRACT.md. Read repository .cursor/skills/tooba-backend-feature/SKILL.md plus applicable AMSC Migrate/Structure instructions; use TB-195 analysis as corrected by Architect. Read Structure BEFORE target layout and check only changed capability. This is an additive authorized feature/migration, NOT permission to recertify modules. Missing governing skill or unreconcilable lock => precise blocker.
Inspect actual Fulfillment catalog/domain/persistence/DI/errors/routes/validators/auth seams and Localization Contracts metadata before coding. Locate and reuse existing canonical policies rather than cloning Shipping implementation or a whole sibling module.

LOCKED OWNERSHIP / AUTHORITY

Fulfillment owns type identity/display/lifecycle in the resolved commerce database: SingleStore = resolved active store DB, Marketplace = installation DB shared for authorized owner selection. No SaaS-global registry, owner-local definition copy or caller-selected tenant/database.
Admin of that installation manages definitions. A seller permission on its own draft is NOT taxonomy administration. Do not add seller catalog-write routes or admin-picks-any-seller.
Shipping will later own a nullable owner-local draft assignment through Contracts; DO NOT change Shipping production/contracts/tests/schema or its binding index now. No item/offer binding or method classification inference.
A shared type carries NO rates, coverage, packaging, schedule, currency, payment, capacity, membership or promise. Flat methods and B/C policy isolation remain intact. Do not restore a provider/child editor or former global "Types of Service" tab.
Four preview labels are examples, never a seed or closed enum. No built-in/sample rows, seed endpoint, inferred ordinary default or code/weight-based classification.

BOUNDED DATA / LIFECYCLE

Add own ShippingType entity/translation capability under existing Fulfillment projects. Reuse current canonical ID/time conventions. Stable server-created nonempty type id; immutable canonical code (ASCII lowercase letter followed by lowercase letters/digits/underscore, 1..64 chars; reject malformed rather than silently renaming); nonnegative bounded sort order (0..1000000), explicit IsActive, Revision concurrency token and UTC timestamps.
Persist own fulfillment.shipping_types and shipping_type_translations, unique code and unique (type_id, language_id), required nonblank name (<=128), optional description (<=512), own FK cascade only. No FK to Localization or Shipping, no cross-schema join/navigation. PostgreSQL UUID fields are typed; do not invent regex "canonical uuid" validation. Reject empty ids where applicable.
POST creates INACTIVE, revision 1. Caller cannot set id, active state, revision, owner/edition/tenant. It must include explicit sort order and non-null translation collection with the current active DEFAULT language name; additional ACTIVE language entries optional. Do not require translations for every configured language or hardcode fa/en.
PUT requires expectedRevision>0, explicit nullable-on-wire isActive and sortOrder, and non-null translations; omitted/null flags must not deserialize into false/zero. Translations patch only the explicitly listed LanguageIds (not whole-dictionary replacement); [] preserves all text. A named entry needs a real nonblank name; explicit null/empty description clears that selected description only. Duplicate/empty/unknown/inactive ids and null entries fail before mutation. Stored inactive-language text remains readable and byte-preserved; reactivation reveals it, no auto-translation.
Languages/default/order/direction come from authoritative ILanguageLookup metadata already extended by TB-188, not HTTP locale or a copied registry. The lookup itself includes inactive rows. Validate active/default metadata explicitly and fail canonically if unusable. Interface locale does not target content language or derive a default.
Activating/reactivating a type requires an actual stored/proposed name for the current active default language. Deactivating is non-destructive; no DELETE API. Do not claim deactivation changes existing Shipping drafts or snapshots (there is no assignment here). GET returns raw translations, explicit active state and truthful default-name completeness, not a guessed translated name.
Full preflight (including language facts) before tracked mutation; header/translations/revision one implicit SaveChanges. Identical PUT preserves revision AND UpdatedAt. Check stale expectedRevision before no-op: canonical conflict even for identical payload. Concurrent edit must use a real DB concurrency predicate, not an in-memory precheck only. Unique code races map by SQLSTATE/owned constraint identity, never provider message text. Unexpected exceptions/cancellation propagate; clear rejected pending edits. No new explicit business transaction or cross-module write.

API / SECURITY / CANONICAL ARCHITECTURE
Use FOUR additive routes under the ACTUAL current Fulfillment Admin prefix discovered from composition, preferably /shipping-types: GET collection, GET /{typeId:guid}, POST collection, PUT /{typeId:guid}. Record exact final routes. Collection is a bounded catalog (validate paging/filter if introduced; do not accept unclassified caller input). GET includes inactive definitions for administration; Contracts ListActive is separate.
Require verified trusted Admin panel/installation access AND fulfillment.view on reads / fulfillment.manage on writes BEFORE ISender/body business dispatch. IMPORTANT: existing IFulfillmentAdminAuthorizer has only RequireAuthorizedAsync(context, token), no permission parameter. It must NOT be claimed to enforce these distinct permissions. Reuse canonical platform admin/effective-authorization seams/constants for this new capability through a module-owned Endpoints gate, without changing unrelated legacy routes or inventing permission grants. No caller owner/edition/tenant/header override. Unresolved edition/inactive or inconsistent tenant fails closed. Seller-scoped grants cannot authorize taxonomy administration. If the platform cannot lawfully provide installation Admin permission semantics, report BLOCKED rather than copying an unsafe hardcoded edition or treating panel membership as manage.
Module Endpoints -> ISender -> real CQRS/FluentValidation -> Application -> own Domain/ports -> Infrastructure. Every transported route/body/query input has real validation with provenance; body errors follow canonical reader/result machinery. Created status/body/Location use actual ApiResponseFactory canonical API, not Results.Json(...,201), raw ProblemDetails, ad-hoc mapper or exception-message parsing. Preserve all old routes/shapes/statuses.
Stable new errors have one natural Fulfillment descriptor owner and resources through existing locale infrastructure and neutral fallback. Do not take over shipping.* or register duplicate Foundation errors. Existing .fa resources are not a two-language production limit. No new telemetry/localization framework.
Every actually executed foreign Localization/platform call requiring module tracing uses canonical IModuleCallTracer with exact source/target/operation, ambient parent/correlation, one child span per executed call, exception TYPE only and cancellation propagation. No language ids/text, type ids/code, tenant/secrets/exception messages in tags.
Expose a small read-only Fulfillment.Contracts type reader for active-list and requested-id snapshots (id/code/isActive/order/raw LanguageId translations), with own registered implementation, no EF types. By-id reads may return INACTIVE facts for future honest historical display; active selection must use explicit active fact. Legitimately empty catalog is distinct from unavailable/unmigrated schema or failed dependency. Do not copy a legacy gate's empty-success fallback or tie it to the one-level method/operational/pricing flags. No new feature flag necessary: catalog management has no customer activation effect. Do not catch DB absence into [] or swallow cancellation.
Use existing projects, shallow capability layout and exact namespaces. No foreign Application/Infrastructure/Domain reference, shared DbContext, cross-module transaction or new package/project. Any indispensable Host change is limited to an existing composition-root registration line; no Host business file/folder. Update only directly affected classification/structure facts, no baseline widening or recertification promotion.

MIGRATION / ISOLATION
ONE additive Fulfillment migration + designer + snapshot for these own tables/checks/indexes/FK. No historical migration/designer edits, no legacy catalog/policy changes, no backfill/seed, no startup schema migrator. Required fields/ASCII code/nonblank text/sort/revision constraints should match domain; SQL NULL/UNKNOWN cannot bypass NOT NULL requirements. Historical migrations byte-identical.
Execute real migrations ONLY in a FRESH task-owned postgres:17-alpine container on unique loopback 127.0.0.1:55436, database fulfillment_tb196, role tb196. Verify port free before start. Parse connection string with NpgsqlConnectionStringBuilder before connection and reject foreign/multi-host/shared port/db/role (allowed text in ApplicationName is not target proof). Verify live current_database()/current_user before any write. No shared :5432/tooba_alpha/store-alpha/postgres use, no credential logging. Adapt existing test isolation pattern within Fulfillment tests only; no dependency on Shipping.Tests or production DB guards. Remove exact task-owned container and verify port free.

REQUIRED PROOF (NOT COUNTS ALONE)
A. Real route delegates + registered module authorizer + body reader + MediatR validation/handlers + actual directory/DbContext + ApiResponseFactory; labelled controlled platform/Localization Contracts fixtures. Prove inactive creation, GET list/detail, update/activate/deactivate/reactivate, canonical Location, code immutable, view-only write denial and seller-only grant denial before dispatch, unresolved/mismatched installation fail-closed. Separate dispatch count from handler count. API harness may use InMemory but cannot substitute for relational proof.
B. Dynamic languages: one non-fa, two mixed RTL/LTR, three-plus with non-first default; selected-language patch leaves others byte-identical, inactive stored text survives deactivation/reactivation, no-op preserves revision AND UpdatedAt, changed interface culture leaves LanguageIds/raw text unchanged. Fixture registry is NOT proof of a live merchant language DB; state this limit.
C. Null/missing fields and bool/order presence, bad code, Guid.Empty, duplicate languages, inactive/unknown languages, blank/overlength text, invalid sort and stale revision: canonical rejection before mutation. Bad LAST translation must leave SAME aggregate/tracker/timestamp/revision unchanged and later SaveChanges publishes nothing. Dependency failures/cancellation are not empty catalog success.
D. Real PostgreSQL fresh-context roundtrip and raw parameterized SQL negatives in independent rolled-back transactions: exact SQLSTATE + owned constraint/column for duplicate code, duplicate translation pair, invalid own parent, required NULL, blank text, invalid code/sort/revision. Valid Unicode and at-limit values persist. Generated forward SQL pinned.
E. Deterministic two-reader same-type revision race: synchronize FIRST SavingChanges after both reads/staging, assert both ORIGINAL revision1 and two arrivals, bounded wait without lock-holding deadlock; exactly one success/one canonical conflict, fresh revision2 and exact winning header/translations without hybrid, actual loser tracker has no pending edits. Reuse test-only pattern, no production hooks. Unique-code concurrent create must produce one record and one canonical duplicate outcome, not leak a Postgres exception.
F. Reader/DI and real-listener proof for introduced cross-module calls (parent/correlation/one span per call/disposal/PII absence). Focused build/tests only for Fulfillment and affected dependencies; existing Shipping code remains byte-identical. Run affected guards, then at most ONE whole Fulfillment suite with disposable DB live IF it fits. Do not rerun full suites for process confidence. No Host.Tests, solution build, unrelated architecture suite, package installs or browser setup. Report pre-existing failures honestly; do not claim all-green after filtering them out. No timebox extension for unrelated guards.

EVIDENCE / STOP
Persist received task verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196.task.md; sanitized docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196/RESULT.md, forward SQL, actual API/language/storage/race/tracing assertions and cleanup. Update recovery pointer with Worker status, not Architect ACCEPT. Record exact commands, counts, SHAs, UTC claim/result, limitations and deadline. No fabricated source/DB/browser proof. PASS requires coherent usable catalog API/reader and all required bounded proof; PARTIAL if time/evidence incomplete, BLOCKED if lawful ownership/security/isolation cannot be preserved.
No frontend/UI/tab, Shipping assignment, item/offer compatibility, taxonomy-based method restriction/grouping, cutoff, basket measurements/amount basis, price/quote/promise, carrier/funder/Payment/Settlement/Promotion, activation/deployment or default-OFF flag changes. TB-185 paused and PendingAllocation unchanged. Preserve unrelated dirty/untracked/stashes; scoped fast-forward commits only. Never POST probe/dummy payloads to Bridge. No future task or automatic continuation; STOP_FOR_ARCHITECT.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-ANALYSIS-195
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_FULFILLMENT_OPERATIONAL_TYPE_CATALOG_ADMIN_AND_CONTRACTS
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: ae969bfa3783a4d99df172f419408e9a0a451ccc
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Canonical-Type-Identity-And-NonInherited-Metadata: ...
Actual-Admin-Routes-And-Independent-Permissions: ...
Trusted-Edition-Tenant-And-Seller-Denials: ...
Database-Languages-SelectedPatch-And-DefaultReadiness: ...
NoOp-Timestamp-Revision-And-SameContext-Preflight: ...
ReadOnly-Contracts-DI-And-Availability-Semantics: ...
Real-Postgres-Storage-And-UniqueCode-Race: ...
Strict-Revision-Race-And-Actual-Loser-Proof: ...
Canonical-Results-Errors-Validation-Tracing-And-Structure: ...
Migration-And-Historical-Byte-Parity: ...
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
Shipping-Assignment-And-UI: NOT_IMPLEMENTED
Customer-Grouping-Quote-And-Activation: NOT_IMPLEMENTED
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