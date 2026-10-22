PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: e5c155ff910f817ce027023346c4483587cd9dc8
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_MIGRATE_CANONICAL_ERRORS_AND_EDITION_GUARD
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Implement one bounded, real backend foundation repair from W0 inside the existing Shipping module: canonical Shipping semantic-error catalog/resources and authoritative edition/tenant context in the Shipping Admin authorization adapter. Use repository Migrate + backend-feature skills and Structure placement rules. No new Shipping business model, owner scope, API route or database schema in this slice.
The owner authorized sequential clean backend delivery after visual acceptance of TB-186-R2. This is the first implementation increment, not whole-module certification or completion of marketplace seller support.

MANDATORY FIRST CHECK
Fetch origin/main, inspect HEAD/branch/dirty files/stashes and preserve unrelated work. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before edits; changed remote base, divergence or conflicting dirty files => STOP_FOR_ARCHITECT. No reset/rebase/stash/force-push.
W0's pasted result named eea03754 as head; actual current remote is e5c155ff, a descendant with only task-evidence/poster changes. Do not rewind to the pasted SHA. Do not repeat W0.

Read AGENTS.md, docs/ai/AGENT-SKILLS.md, the full tooba-backend-feature, tooba-architecture-migrate and tooba-architecture-structure SKILL.md files, W0 source/validation/model evidence, current relevant TMAR locks/structure standard/reference pattern/SoT/manifests, SHIPPING-ADMIN-RULES-DECISION.md (sections 13–15), and the dated shipping recovery/financial decisions. Analyze/Certify skills define handoff boundaries; do not execute a new Analyze wave or claim Certify here. Do not consult the opt-in findings register.

ARCHITECT REVIEW / RESOLVED SCOPE
W0 is accepted as a bounded source map with these corrections:

Registering localized messages changes visible failure text. Do not describe this as zero observable change. Legacy success wire contracts and failure HTTP statuses are preserved as specified below.
Preview min/max delivery duration is NOT equivalent to PreparationDays + LeadDays. Preparation and transit are separate; no mapping or column repurposing is authorized.
Approved composite freight + packaging + other shipping cost and free-shipping funding decisions already exist in the dated financial appendix/recovery. Do not reopen them as undecided merely by citing TB-177 historical text. Operational/payment implementation and PendingAllocation remain incomplete; no customer charge or financial consumer change here.
Lack of a manifest entry alone does not prove missing module foundation. Reuse Shipping's existing projects/capabilities. No manifest promotion or whole-module certification.
D1–D7 remain deferred for dependent business/schema slices; they do NOT block this explicitly authorized mode/owner-neutral repair. They do NOT authorize guessed owners, taxonomy or migrations.
Record these review corrections in this task's evidence; preserve historical W0 evidence unchanged.

BOUNDARY A — CANONICAL SHIPPING ERRORS

A1. INVENTORY AND COMPATIBILITY BASELINE
Read Shipping.Contracts/Errors/ShippingErrorCodes.cs, its emitters, Result/fault path and current composed catalog. W0 found 44 declared stable codes without descriptors; independently derive the actual code set and check descriptor ownership before registration. Preserve every code spelling and meaning. Do not register Foundation/other-module codes under Shipping or suppress duplicates.
SafeErrorMapper.Map(SemanticError) currently maps unregistered codes to HTTP 400, Business/Warning, preserving the code. For this legacy-compatible slice, register each currently unregistered ShippingErrorCodes code explicitly with HTTP 400, Business classification and Warning severity. This compatibility choice is an explicit Architect decision, not suffix-based status inference. Do NOT silently turn *.not_found into 404 or duplicates into 409, or alter status for any already-registered code. If the actual baseline/lock contradicts this, stop that conflict with exact evidence.
Keep success status/body/Content-Type/Location and validation.failed child-code structure unchanged. Future new contracts may define appropriate statuses in separately scoped work; this task does not redesign legacy HTTP semantics.

A2. ONE CANONICAL OWNER AND RESOURCE PATH
Add Shipping-owned IErrorCatalogContributor and IErrorResourceSet using existing BuildingBlocks presentation/localization infrastructure and module precedent. Candidate cohesive homes:

Tooba.Shipping.Endpoints/Errors/ShippingErrorCatalogContributor.cs
Tooba.Shipping.Endpoints/Errors/ShippingErrorResourceSet.cs OR the existing canonical Resources placement, chosen from actual repository precedent
Tooba.Shipping.Endpoints/Resources/ShippingErrors.resx and locale-specific resources required by configured support/fallback.
Use stable localization keys (prefer existing codes), explicit descriptors, safe fallback and canonical ResourceManager semantics. No new shared-errors project, custom mapper, locale parser, localizer or language table. No reflection/name-suffix classification in production to guess error semantics. Resource-set key ownership must not swallow Foundation or another module's keys.
Discover current supported/configured locales and fallback; neutral + .fa.resx are examples, not a fixed two-language inventory. Provide real translations only where present; prove canonical fallback for an additional configured locale without claiming translation coverage that does not exist. Do not add a DB/config dependency to catalog registration.
Do not turn exception.Message into text. Unknown exceptions remain global unexpected failures. Do not widen ShippingAdminOperation to catch arbitrary faults just to use the new catalog.

A3. LAWFUL COMPOSITION
Discover existing endpoint presentation registration patterns and Host registration path. Register contributor and resource set exactly once with correct lifetimes and real production composition; tests alone are insufficient.
Prefer a Shipping Endpoints-owned presentation extension invoked from the existing Host composition path. Do NOT make Shipping.Infrastructure reference Shipping.Endpoints to register presentation. A minimal registration-only edit to existing Host Program.cs is explicitly authorized; no new Host file/folder or business logic. Do not duplicate existing pipeline/registration or add a schema migrator/hosted service.

BOUNDARY B — AUTHORITATIVE AUTHORIZATION CONTEXT

B1. FIX THE MODULE ADAPTER, NOT THE PLATFORM
Shipping.Endpoints/Admin/ShippingAdminAuthorizer.cs currently hardcodes ToobaEdition.SingleStore and tenant fallback "unknown". Consume existing ICurrentEdition and ICurrentTenant (or the existing canonical resolved-commerce seam if required by current pattern) from scoped DI. No raw configuration/header/query/host parsing, duplicate mode resolver or global mutable state.

SingleStore: pass the actual resolved edition and nonempty stable active TenantId; missing/invalid tenant or unresolved context fails closed before IAuthorizationService.CanAsync.
Marketplace: pass Marketplace and TenantId = null, as documented by AuthorizationCallContext; do not synthesize a tenant or accept a stale SingleStore tenant as valid marketplace context.
null/Unset/unsupported edition or inconsistent context fails closed using the existing Foundation admin-authorization-unavailable semantic path; do not invent a Shipping copy of platform error codes.
Retain the IAdminPanelAccess authentication/panel gate, existing permission identifiers, user subject/resource and Allow/Deny/Unavailable semantics. Deny remains denied; unavailable never allows; cancellation/unexpected exceptions are not swallowed. No seller route, role grant or bypass is introduced.
Do not change platform-wide authorizers or widen certified Host security retained-file sets. This repairs Shipping's call context only; it does NOT establish merchant identity, seller ownership or full installation-mode support.

B2. REAL DI AND TEST FIXTURES
Verify resolution with the production registrations and correct scoped dependencies. Update exact affected test constructors/fixtures to provide explicit trusted edition/tenant contexts; do not supply a default mode inside production for the sake of old tests. Fake Allow authorizers are not proof of the real adapter.
Document that existing context resolution remains the authority and that the owner-scoped business/data gap is still open.

SCOPE / NON-GOALS
Authorized production changes only within Shipping.Endpoints for errors/resources/presentation registration and ShippingAdminAuthorizer; the minimal existing Host Program.cs presentation-registration call; and comment-only corrections of stale "not composed / nothing writes" remarks in ShippingModule/ShippingDbContext if encountered. No behavior edit to Infrastructure/Domain/Application.
Focused tests may be added/updated in Shipping.Tests and exact affected Host.Tests composition/error-catalog fixtures. Existing route files may change only to host the minimal presentation-registration entry if that is the established pattern; no route/request/handler/validator/body-reading behavior change.
Do not extract duplicated body readers or consolidate validation-code classes in this slice. Those cleanups are not prerequisites for this repair and must not expand scope.
No Fulfillment/Geography/Localization/StoreContext/AccessControl production change; no new project dependency except existing lawful references if required and explicitly evidenced; no foreign Application/Infrastructure/Domain/Endpoints implementation reference, shared mutable aggregate, DbContext reach-through, cross-schema SQL/FK/join, Contracts dump or new architecture.
No owner/seller column, region-group container, merchandise-amount tier, taxonomy, calculation/payment/cutoff policy, schedule alteration, data backfill, language-enablement schema, carrier integration, customer quote/charge, frontend/preview change, Storefront, payment, deployment or production flag change.
Default-OFF gates remain byte-unchanged. Preserve PendingAllocation and financial authority. Preserve Host final closure and source-size/architecture guards; no baseline refresh, guard weakening or unrelated repair.

FOCUSED VALIDATION — BOUNDED, NO TEST LOOP

Baseline the current mapper/status/code/envelope and new behavior. Derive the declared ShippingErrorCodes set independently and prove exact set equality with registered Shipping-owned descriptors plus resource coverage/fallback; no fixed "44" count as the only proof.
Construct the real composed ErrorDefinitionCatalog through actual registrations and prove uniqueness, one canonical owner, no descriptor suppression and no duplicate resource registration. Test representative Shipping Result failures through ApiResponseFactory/real localizer: stable code, HTTP 400, canonical error shape and trace context, localized message, unknown locale canonical fallback. Assert required supported locale resources without claiming all languages are translated.
Exercise the real ShippingAdminAuthorizer with controlled platform seams: SingleStore+active tenant, Marketplace+null tenant, null/Unset/unsupported edition, SingleStore missing/empty/inactive tenant, inconsistent marketplace tenant, authorization Allow/Deny/Unavailable, panel rejection and cancellation. Assert actual outgoing AuthorizationCheck edition/tenant/resource/permission; invalid contexts never reach permission authorization. No header-supplied mode/tenant overrides trusted contexts.
Confirm shipped route/request/input-validation universe and success mapping remain unchanged against W0; do not re-audit unrelated modules. Run relevant existing malformed-body/admin-scope/endpoint/composition tests, new error/authorizer tests, catalog uniqueness guard and required touched-path structural/dependency checks. Do not weaken an existing check to accept the repair.
Build affected projects and DI resolution only. Each build/test command has an explicit <=180-second bound, total task hard45m. Group useful focused checks; no repeated cold full build, full solution/all architecture suites, package install, browser/server/DB startup. Unit/composition/in-process HTTP tests must not contact a real database or emit migrations.
One deterministic in-scope fix and affected rerun is allowed after a failure. Persistent failure, scope conflict or time exhaustion => stop and report PARTIAL/BLOCKED with exact evidence. Existing unrelated failures stay reported, never fixed/baselined here.

STRUCTURE / HANDOFF
Reuse existing capability-first shallow placement, exact path-derived namespaces and canonical solution grouping. Do not create one-folder-per-request or decorative projects. Structure-Handoff-State remains REQUIRED for the broader delivery surface. This W1 may report its bounded repair verified, but not READY_TO_STRUCTURE/COMPLETE_REFERENCE_PATTERN for the whole module, nor final Structure/Certify PASS. Do not edit tmar-current-state/manifests to promote Shipping.

EVIDENCE / DELIVERY
Persist received task verbatim in docs/ai/tasks/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1.task.md.
Write concise sanitized evidence in docs/architecture/evidence/TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1/: RESULT.md, explicit code->descriptor/key/status/resource/fallback mapping, authorization test matrix, actual commands/test counts, DI/registration proof, compatibility deltas and exact changed files/SHAs. Include the Architect review corrections above and remaining D1–D7 without reopening decisions already approved.
Commit/push only scoped changes, fetch, verify HEAD-origin parity, report exact task-scoped cleanliness and preserve unrelated dirty/untracked work/stashes. Do not claim whole-tree cleanliness if unrelated work remains. No shell credential/token logging or secrets in evidence. Shared :3000/:3419/:5088/:5432 and existing apps remain untouched; no process/cache cleanup beyond exact task-owned artifacts.
Post one complete Bridge result after validation/commit/push; never probe /api/results with placeholders. Stop for Architect; do not execute or schedule another task. TB-185 performance remains paused.

ACCEPTANCE VERDICT
PASS only for real composed canonical Shipping errors/resources, legacy HTTP compatibility, correct trusted edition/tenant authorization context, negative tests/DI and focused required checks within scope. PARTIAL if required proof/checks are missing; BLOCKED for unsafe recovery or an unresolved in-scope architectural conflict. PASS does not imply owner isolation implemented, full marketplace readiness, business migration approval, integrated Admin acceptance or production release.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W1
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W0
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_AMSC_MIGRATE_CANONICAL_ERRORS_AND_EDITION_GUARD
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Stop-State: STOP_FOR_ARCHITECT
Expected-Base-SHA: e5c155ff910f817ce027023346c4483587cd9dc8
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
automaticNextImplementationTask: NONE
HEAD-Origin-Parity: ...
Architect-Review-Corrections: ...
Error-Code-Descriptor-Set-Equality: ...
Canonical-Resource-And-Fallback-Proof: ...
Real-Production-Composition-And-DI: ...
Legacy-HTTP-Compatibility-And-Visible-Delta: ...
Trusted-Edition-Tenant-Authorization-Matrix: ...
Contracts-And-Persistence-Boundaries: ...
Route-Request-Validation-Parity: ...
Focused-Builds-And-Tests: ...
Structure-Handoff-State: REQUIRED
Remaining-Owner-And-Business-Decisions: ...
Schema-Data-Flag-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK