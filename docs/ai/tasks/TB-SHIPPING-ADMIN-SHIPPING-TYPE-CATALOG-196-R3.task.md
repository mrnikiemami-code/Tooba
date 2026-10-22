PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 46a136dc5e365e29a380f76d16e0ab4f6892b332
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_REAL_DELEGATE_AUTH_INPUT_EVIDENCE_ONLY
Hard-Timebox: 35 minutes HARD including commit/push/result. Stop new test-writing/execution at minute 28; final 7 minutes reporting/cleanup. On expiry STOP/PARTIAL; do not finish missing facts after the deadline.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Close ONLY the missing actual API/auth/body/validation proof and exact four-route classification for the existing type catalog. TEST/EVIDENCE ONLY. R2 Domain repairs are accepted; do not modify them. Do not collect the entire remaining TB-196 backlog into this task. Full multilingual lifecycle, ActivityListener and PostgreSQL/snapshot proof stay pending.

FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work before edits. Mismatch/unsafe divergence => STOP without edits; no reset/stash/rebase/force-push. Read AGENTS.md, current recovery, TB-196/R1/R2 Architect reviews and current capability source. Follow repository backend-feature skill for meaningful focused tests and preserve architecture locks. No implementation, migration or recertification authorization.
Read current ShippingTypeAdminEndpoints, ShippingTypeBodyReader, real module DI registration, four CQRS requests/handlers/validators, real store/language adapter, existing canonical in-process route test pattern and error catalog. Reuse a test harness pattern, not another module's implementation. No folder/namespace/guard-driven rework.

TEST HARNESS REQUIREMENTS
Compose the actual Fulfillment endpoint mapping and real registered ShippingTypeAdminAuthorizer, body reader, MediatR/ValidationBehavior, handlers, ShippingTypeStore over FulfillmentDbContext, ApiResponseFactory and canonical exception presentation/error catalog. Invoke actual RequestDelegate with route values and real JSON streams; no Kestrel/listener/network.
EF InMemory is allowed and must be labelled as such. Platform auth/edition/tenant/clock/id and ILanguageLookup are controlled fixtures. Use the actual LocalizationShippingTypeLanguageFacts adapter with existing real canonical tracer registration; do not fake the module authorizer, language-facts resolution, handler, sender success or result factory. Supply one active NON-fa language with real IsActive/SortOrder/default fixture metadata; this is not a live Localization DB or full dynamic-registry matrix.
Measure ISender dispatch separately from arrival past validation/handler execution; count language lookup calls. On unauthorized malformed body, additionally use a read-counting stream to prove the body was NOT read. Reuse production composition for the capability registrations; do not copy a fake composition path that never tests the registered gate/adapter.

BOUNDED EXECUTED FACTS

Authorized lifecycle through real delegates:
POST valid code + sortOrder=0 + default-language name -> inactive, revision1, canonical Created status/body/Location referencing real id. GET list/detail return it; PUT activates, deactivates and reactivates through actual handler/store; GET sees changes. Repeating identical nonempty text must preserve revision and UpdatedAt; stale identical PUT gives canonical revision conflict. Exactly the selected code stays immutable; attempted code change rejects without data loss.
Permission/installation matrix before dispatch:
view grant GET succeeds; view-only POST/PUT denied without handler/language read;
Admin panel rejection (including labelled seller-only actor) prevents authorization/business dispatch;
panel accepted but requested capability denied also prevents dispatch (never claim seller denial solely because no seller seam is called);
missing/unknown edition, SingleStore missing/inactive tenant, Marketplace leftover tenant -> canonical authorization-unavailable, zero outgoing authorization checks after context rejection, zero dispatch;
valid Marketplace/no-tenant and valid SingleStore/active tenant forward the correct trusted authorization call context;
request query/header/body owner/edition/tenant text does not replace the controlled trusted context.
Use compact table/theory cases; do not add new permission semantics or grants.
Authorize BEFORE body:
Unauthorized malformed POST/PUT denied with stream.ReadCalls=0, SentCount=0, HandlerReached=0 and no registry read. Authorized malformed JSON/empty/null/array/wrong scalar kinds follow shipping_type.body.malformed via real factory; nothing persists, handler not reached. Do not merely assert a source-code ordering.
Lossless validation matrix:
Missing/null required sortOrder, PUT isActive/expectedRevision/translations, null translation collection, valid FIRST+null LAST entry, duplicate/empty LanguageId, blank/overlength name and invalid sort/code: exact canonical rejection, unchanged state. Validation rejection may dispatch but must not reach handler; body-shape rejection must not dispatch. GET Guid.Empty is rejected by real query validator. No null element is filtered into a successful partial save.
Actual route -> request -> handler -> validator set equality:
Enumerate only the shipping-types capability routes from production endpoint data/source and compare BOTH directions against the four known route/request dispatches. Require the two write validators and detail validator resolve via actual DI/pipeline; justify parameterless ListShippingTypesQuery as NO_VALIDATOR_REQUIRED with no route/query/body input. Prove a synthetic extra capability route/request or a dropped classification is detected; count4 alone is insufficient. Use an existing classification mechanism if present; otherwise one capability-scoped test-owned classification/guard, not a production framework or global manifest edit.
List's removed languageId binding stays removed; differing query/culture must not target content text or alter raw LanguageIds. No new query feature.

SCOPE / FAILURE RULE
No production C# or production project/DI/Contracts/resource/schema/EF mapping/migration/designer/snapshot changes. You may add directly necessary own-module TEST-project references to the module's own Endpoints/Infrastructure only if missing; no new external package or cross-module implementation reference.
If a test exposes a genuine production defect, preserve failing evidence and report PARTIAL/BLOCKED with exact member/code/outcome. Do NOT adjust expected semantics, fake a successful dependency, soften a guard, skip a fact, alter production, or begin a repair wave. If a harness assumption is wrong, fix the harness once using authoritative source and rerun only its affected filter.
Build ONLY Fulfillment.Tests and required project references with task-owned isolated artifacts if necessary. Run only the new API/classification tests and directly reused tests if necessary for a harness change. No full Shipping/Fulfillment/Host/solution suite, no broad Architecture filter, no Host build; a running Host DLL lock is a reason to isolate output, never stop the app or expand scope. Commands bounded <=120s; no detached work.
No actual DB/container/migration/seed/server/browser/network HTTP probe/package/cache operation. No full language lifecycle matrix, listener collection, constraint-lookalike test or PostgreSQL/race work in this slice. Those remain separately scheduled gates after review, not waived requirements.
No Shipping assignment/frontend/Storefront/financial/pricing/quote/promise/grouping/activation/deployment/flags. TB-185 paused, PendingAllocation/default-OFF unchanged. Preserve unrelated dirty/untracked/stashes; no reset/stash/rebase/force-push, no shared output/cache cleanup or shared listener changes. No Bridge probe/dummy result.

EVIDENCE / STOP
Persist received artifact verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3.task.md. Commit test files and sanitized docs/architecture/evidence/TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3/RESULT.md with precise facts: actual delegate/DI composition, auth matrices and counters, input-to-code matrix, classification set equality/negative detection, exact command counts, fixture limits, SHAs and UTC timing. No source-only proof labelled executed.
Update recovery pointer to Worker status only. PASS is bounded API/classification evidence, NOT acceptance of TB-196 or API-backed live merchant DB/browser proof. Keep full registry lifecycle, ActivityListener, exact-constraint negative and relational/snapshot/races PENDING. PARTIAL for missing facts/time expiry or genuine failing behavior; stop, no next task.
Scoped commit/push, fetch verify HEAD==origin/main; clean exact task-owned temporary output and report elapsed claim/result accurately (do not carry forward inconsistent historic timestamps).

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R3
Parent-Task: TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R2
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_TYPE_CATALOG_REAL_DELEGATE_AUTH_INPUT_EVIDENCE_ONLY
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 46a136dc5e365e29a380f76d16e0ab4f6892b332
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Actual-Delegate-DI-Pipeline-And-Fixture-Limits: ...
CreateReadUpdate-Lifecycle-And-NoOp: ...
Independent-Permissions-And-Trusted-Installation: ...
Unauthorized-Malformed-Body-ZeroReads: ...
Lossless-Input-Rejection-And-PreHandler-Counters: ...
FourRoute-RequestHandlerValidator-SetEquality-And-Negative: ...
Focused-Commands-And-Test-Counts: ...
Exact-Failing-Behavior-If-Any: ...
Changed-Files: ...
Production-Schema-Contract-Changes: NONE
Registry-Lifecycle-Listener-ConstraintNegative-Proof: PENDING_NOT_RUN
Postgres-Snapshot-And-Relational-Races: PENDING_NOT_RUN
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