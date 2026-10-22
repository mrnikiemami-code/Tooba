PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194
Parent-Task: TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 06737dd7428ee96af351af22c62f4389f83fabc3
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_INDEPENDENT_FREIGHT_AND_CALCULATION_DRAFT_SETTINGS
Hard-Timebox: 45 minutes HARD from claim through cleanup, commit, push and complete result. STOP and report PARTIAL if exceeded. Start no implementation or test after minute 38; reserve final 7 minutes for cleanup and reporting.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Persist exactly TWO independent method-root DRAFT selections: freight payment timing (Prepaid / CollectOnDelivery) and calculation mode (Manual / CarrierApi / Free). This completes a bounded authoring capability, not runtime shipping activation. Preserve the accepted owner/region/rate/translation/weekly-schedule foundations. No frontend or Storefront change.

MANDATORY FIRST CHECK
Fetch and verify HEAD == origin/main == Expected-Base-SHA and inspect dirty/untracked work BEFORE edits; mismatch or unsafe divergence => STOP_FOR_ARCHITECT, no recovery reset/rebase/stash/force-push. Read AGENTS.md, current shipping recovery, SHIPPING-ADMIN-RULES-DECISION.md, TB-193-R1 ARCHITECT-REVIEW.md, the applicable repository-owned AMSC Analyze/Migrate/Structure/Certify instructions and backend-feature SKILL.md. Locate the actual skill paths rather than substituting a generic clean-architecture checklist. Read current Authoring code, ShippingFreightPayerMode, request classification/structure guards and docs/architecture/checkout/SHIPPING-GROUP-FEE-ALLOCATION-POLICY-DECISION.md including its later approved component/free-funding decision. Historic tasks are evidence, not current execution authority. Missing governing skill or contradictory authority => precise blocker.

LOCKED PRODUCT SEMANTICS

Freight timing and calculation are INDEPENDENT choices; do not combine them into a single enum. Reuse the existing Shipping.Contracts.Packaging.ShippingFreightPayerMode with its existing numeric/wire conventions. Define one appropriately scoped typed calculation mode with Manual, CarrierApi, Free. Do not create another catalog or reuse installation edition as a calculation mode.
Settings are owned ONCE by the owner-scoped draft root, never per region/rate/translation. Both values require explicit presence on PUT. Absent persisted settings = explicitly UNCONFIGURED, not implicit Manual/Prepaid. Missing/null/unknown values must not deserialize into an accepted enum zero.
All six combinations can be recorded as independent draft choices. Selecting Free does not rewrite freight timing; a dormant CollectOnDelivery choice cannot create a charge while Free is selected. No runtime evaluation or charge is authorized here.
Switching modes preserves every stored manual row, freight/packaging amount, currency binding, group/area, presentation text, canonical method binding and weekly schedule. Incomplete manual configuration remains an incomplete draft; an empty row set is never free or unrestricted shipping.
Freight timing is not product COD, packaging payment timing, PSP allocation or a payment instruction. Preserve the existing packaging contracts and all Order/Payment/Settlement consumers.
CarrierApi is only a requested calculation mode. No inferred carrier identity/integration from serviceCode, credentials, quote, quote expiry, destination coordinates, provider availability or readiness claim. Actual carrier binding/API/lat-long integration remains OPEN.
Approved eventual Free semantics: ALL customer shipping components, including packaging, are zero; actual underlying costs remain auditable and an explicitly identified granting/funding party bears them. This task neither calculates those costs nor implements funding accountability. Do not infer a funder from actor/owner/tenant or fabricate a Party GUID. Persisted manual fees are configuration, not a real expense ledger. Do not zero/delete them or claim mode selection proves financial readiness. Free-above campaigns remain Promotion scope; no threshold field.
Language registry and currency keep their existing database-backed authority. No fa/en language list, default IRR, locale-derived money or second registry. Interface locale never selects content LanguageId.

BOUNDED IMPLEMENTATION
A. Domain/Application
Add cohesive own entities/value model and preflight/apply rules using the existing aggregate pattern. Prefer an optional one-to-one own settings record keyed by draft_id, with two required typed values. Use existing owner scope and root Revision/UpdatedAt. Validate the entire request before mutation. An identical PUT is a truthful no-op: no revision or timestamp advance. Changed settings advance the SAME root revision once; stale expectedRevision is the canonical 409 conflict even for an otherwise identical request. No new explicit BeginTransaction.
One query and one command/handler serve both audiences. Endpoints authorize; Application owns workflow; Domain owns invariants; Infrastructure owns EF persistence. Contracts contain transport facts only. Do not move owner resolution into Domain or access another module implementation/DbContext.

B. API/authorization
Add GET/PUT /v1/admin/shipping-services/authoring-drafts/{draftId:guid}/method-settings and the equivalent /v1/seller/... paths. Require fulfillment.view for GET and fulfillment.manage independently for PUT, through the actual owner authorizer BEFORE ISender dispatch. SingleStore seller must retain canonical capability-unavailable 404 before seller resolution. Filter every access by the complete server-authorized owner plus draft; foreign/unknown draft gives the same safe not-found. Bind no owner, seller, edition, tenant, currency, language or activation authority from transport.
GET must distinguish unconfigured from configured with explicit truthful values; no operational readiness inference. Use existing Result and ApiResponseFactory, body reader, ValidationBehavior, canonical stable errors/resources and resource fallback. Prefer existing applicable errors; add narrowly owned codes only if necessary, each with one descriptor and neutral/fa resources through existing infrastructure. Do not change legacy statuses or shared error machinery.
Enforce required collection/object/value presence and bounds through real validators; update exact route/request/handler/validator classification sets, including input provenance. No count-only guard or permissive exemption.

C. Persistence
Prepare ONE additive migration + designer + snapshot for the own settings table, required fields, own cascade FK, unique root identity and CHECK constraints for the exact allowed enum values. SQL NULL/UNKNOWN must not bypass required fields. No historical migration/designer edit, legacy table mutation, data backfill, cross-module FK or schema startup migrator. Preserve mode settings when existing region/rate/presentation/schedule writers run; draft deletion may use own cascade. Apply migrations only to the disposable test target below. Shared migration/deployment authority remains ungranted.

D. Architecture
Preserve capability-first shallow layout, path/namespace alignment, module-owned DI, Contracts-only crossings, CQRS/ISender and canonical module-call tracing for any actually introduced foreign read (prefer none). No Host business orchestration, new project/package, cross-schema join or second state/telemetry framework. Read actual feature/AMSC skills first. Update intentional module structure facts and classification without broadening roots or weakening guards; no certification promotion or assertion that the historical certified baseline certifies this feature.

REQUIRED EXECUTED PROOF

Actual admin AND seller route delegates, real authorizer/body reader/MediatR validation/handler/directory/result pipeline, with labelled platform-seam fixtures. Prove unconfigured GET, explicit configured PUT/GET, independent six combinations and mode switching with preserved other choice and manual data. Prove missing/null/invalid enum values fail before handler mutation; malformed JSON follows canonical body behavior. Count pre-dispatch denials separately from validation rejection.
Independently prove view-only PUT denial, foreign owner safe 404/no change, Marketplace seller permission success/denial and SingleStore seller unavailability before seller resolution. No test-only production bypass.
Prove same-context invalid input leaves revision/UpdatedAt/tree untouched and no pending edits; true no-op and stale revision behavior. Round-trip existing rates, currency, text, geography and per-day schedule across setting changes and a subsequent existing-capability write.
Real PostgreSQL 17, parsed fail-closed target BEFORE connection: reuse the existing ShippingRelationalTestTarget constants 127.0.0.1:55432 / shipping_tb190r1 / tb190r1, verify free port and fresh task-owned container, live current_database()/current_user BEFORE writes. Do not log credentials. No shared :5432/tooba_alpha/store-alpha use. Apply real module migrations there, read settings from a NEW context; direct parameterized SQL negatives independently rolled back must prove NULL/unknown enum/duplicate PK/invalid own parent rejected with actual SQLSTATE and constraint/column identity. Pin generated forward SQL and historical byte parity.
Deterministic settings-versus-one-existing-capability writer race on the SAME root revision: gate both FIRST SavingChanges AFTER read, assert both ORIGINAL Revision == 1 and two arrivals; bounded wait, no sleep/lock-holding deadlock. Require EXACTLY one success and one canonical conflict, fresh root revision EXACTLY 2, exact winner-dependent settings and other-capability state with no hybrid, and ACTUAL loser context has no pending edits. Reuse the reviewed test pattern, no production hooks. Do not accept a gate before reads or both-success assertions.
Build/test only affected Shipping projects. Run focused new API/domain/storage/race guards; one final whole Shipping suite only while the disposable DB is live and within remaining time. No whole Host.Tests, solution build or broad architecture run; no repeated full suites. If a required fact cannot finish, report precise PARTIAL instead of substituting InMemory or inferring proof. Clean task container/listener and verify port free. Never refresh baselines, skip required facts or weaken unrelated guards.

OWNERSHIP / SAFETY
No frontend/preview/global CSS/navigation, taxonomy, schedule cutoff/holiday/capacity, runtime evaluator, basket amount/count mapping, customer quote/promise, activation/publication, legacy operational shadow, Order/Checkout/Payment/Promotion/Settlement, deployment flag or shared service change. TB-185 stays paused; default-OFF gates and PendingAllocation preserved. Preserve unrelated dirty files/stashes/helpers; no reset/stash/rebase/force-push, package install, cache purge or shared app restart. Use isolated build artifacts when needed; do not overwrite locked running Host output. Only scoped fast-forward commits. Do not POST probe/dummy results to Bridge; only one complete validated result contract.

EVIDENCE / RECOVERY
Persist this received artifact verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194.task.md and sanitized evidence under docs/architecture/evidence/TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194/ (RESULT.md, migration SQL, concrete API/storage/race assertions, isolation/cleanup). Append the bounded mode/freight decision to SHIPPING-ADMIN-RULES-DECISION.md and update the current recovery pointer with actual status, not future acceptance. Distinguish executed real PostgreSQL versus controlled language/auth fixtures and source-only facts. Record exact commands/counts, errors, SHAs, elapsed claim/result UTC and any deviation. PASS requires all bounded proof and hard-deadline compliance; user visual acceptance and production functionality remain PENDING. Stop; no future task scheduling.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-METHOD-MODE-DRAFTS-194
Parent-Task: TB-SHIPPING-ADMIN-WEEKLY-SCHEDULE-DRAFTS-193-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_INDEPENDENT_FREIGHT_AND_CALCULATION_DRAFT_SETTINGS
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 06737dd7428ee96af351af22c62f4389f83fabc3
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Independent-Freight-And-Calculation-Choices: ...
Unconfigured-Presence-And-NoOp-Semantics: ...
Real-Admin-Seller-API-And-Permission-Proof: ...
Owner-Isolation-And-Preflight: ...
Mode-Switch-And-CrossCapability-Preservation: ...
Strict-Root-Revision-Race-And-Actual-Loser-Proof: ...
Real-Postgres-Roundtrip-And-Storage-Negatives: ...
Migration-And-Historical-Byte-Parity: ...
Contracts-Validation-Errors-Tracing-And-Structure: ...
Carrier-Integration-And-Free-Funding-Readiness: NOT_IMPLEMENTED
Customer-Quote-And-Activation: NOT_IMPLEMENTED
Runtime-Merchandise-Basis-And-Measurement-Gate: OPEN_NOT_IMPLEMENTED
Focused-Commands-And-Test-Counts: ...
Changed-Files: ...
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