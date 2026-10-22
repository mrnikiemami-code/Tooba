PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 21d3900503475ce26052c33b97bb420cd8ec765c
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DB_BACKED_SHIPPING_AUTHORING_CONTEXT_READ
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Deliver the first product-facing backend read for the accepted Shipping Admin: a real authorized authoring-context API supplying effective installation edition, configured default currency, active content languages and their default/direction metadata from existing authorities. Remove the need for any future production UI to treat fa/en preview fixtures as store configuration.
This is one additive backend feature, not another AMSC recovery cycle, frontend connection or owner/region/schema implementation.

MANDATORY FIRST CHECK
Fetch origin/main and inspect HEAD/branch/dirty files/stashes. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before edits; changed remote base/divergence/conflicting dirty files => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push. Preserve unrelated work/running apps. Execute only this received task.
Read AGENTS.md, registry, full tooba-backend-feature skill and relevant module Contracts/architecture/guards/error/DI paths. Apply existing Structure/AMSC rules to the changed capability; do not restart module-wide Analyze/Migrate/Structure/Certify or reopen unrelated certified modules.
Read SHIPPING-ADMIN-RULES-DECISION.md sections 13–16, dated recovery, W0 language/context findings, W3-R1 correction/evidence, current Shipping classification JSON/guard, canonical API/localization/tracing contracts and actual Localization lookup/directory/mappings.

ARCHITECT ACCEPTANCE / D6 DECISION
The W3-R1 tracing repair is accepted for the existing baseline from source review: all three actual foreign reads have canonical module-call spans; the original W3 exception rationale is superseded. This does not complete new product requirements.
For this feature, configured content languages mean ACTIVE rows of the existing Localization registry in the resolved commerce database:

SingleStore: registry in that tenant/store's existing resolved database;
Marketplace: registry of the marketplace installation, shared by seller authoring for now.
The owner did not request per-seller language settings. Do not invent a new store/seller-language table, copied registry or second fallback policy. Per-seller enablement, if later requested, is separate work.
Localization owns language identity/activation/default/culture/direction/names/order. Shipping consumes public Contracts and performs only its authoring read projection/readiness checks. Interface locale and content language remain independent; currency does not derive from locale.
Edition comes from existing resolved platform context; default currency from StoreContext. No Shipping-owned mode flag, owner selector implementation or seller identity inference. Returning Marketplace is not a claim that marketplace seller-scoped writes are ready.

BOUNDED IMPLEMENTATION

EXTEND THE EXISTING LOCALIZATION LOOKUP ADDITIVELY
Current Localization.Contracts/Ports/LanguageLookupContracts.cs exposes LanguageLookupSnapshot(LanguageId, Code, Culture, UrlPrefix, IsDefault) and ILanguageLookup.ListAsync, documented as KNOWN languages. The bridge currently loses IsActive, Direction, DisplayName, NativeName and SortOrder although the authoritative LanguageSnapshot already contains them.
Add the minimal missing metadata to that same snapshot with non-positional properties, preserving the existing five-argument constructor, deconstruction, method signature and known-language set/order semantics. Map every added value from the authoritative Localization snapshot in LanguageLookupBridge. IsActive must come from the actual row, never default-true inference. Nullable absent metadata from old manually constructed snapshots must not be treated as a genuine configured direction/name.
Do not change ListAsync to return only active rows: existing callers may need inactive known-language identity. Shipping selects active rows using the owner-provided flag; this is a read projection, not a new language activation policy.
Inspect direct consumers for compatibility. No foreign Application/Domain type in the public snapshot, no exposed DbContext, no new resolver/cache/seeding behavior. Existing Localization HTTP routes and domain write invariants remain unchanged. Do not introduce a new lookup port parallel to ILanguageLookup.

SHIPPING AUTHORING-CONTEXT QUERY / API
Add one authorized GET /v1/admin/shipping-services/authoring-context (literal route, prove no collision with serviceCode/serviceId routes). Endpoint belongs to Shipping.Endpoints and uses the real ShippingAdminAuthorizer with existing fulfillment.view, ISender -> one authoritative query/handler -> Result -> ApiResponseFactory.From.
Return a typed view containing:

effective edition SingleStore or Marketplace, derived server-side;
authoritative default currency from ICurrentStoreCommerceContext;
ordered ACTIVE content languages with real LanguageId, code, culture, url prefix, direction, display/native names and default flag;
one explicit defaultContentLanguageId referencing that active list.
Do not expose connection references, raw tenant/control-plane secrets or fabricate merchant/seller/owner IDs. Do not return canManageSellerRules/canSelectOwner=true merely because edition is Marketplace; owner authorization/data support is a later feature.
The GET accepts no owner/mode/tenant/language/currency override or request body. Browser headers/query cannot change the resolved authority. The interface locale may localize errors through the existing resolver but must not replace the content-language set or default.
No UI edit in this task. The new API is consumed/proved through its real HTTP/handler composition in tests and documented for the later frontend task, not presented as already connected.

LAWFUL SOURCE ADAPTER / FAIL-CLOSED READINESS
Use a Shipping Application-internal port/model and a Shipping Infrastructure adapter consuming ONLY Localization.Contracts, StoreContext.Contracts and existing BuildingBlocks commerce/edition seams. Add minimal legal project references only where actually required. Do not read Localization.Application/Infrastructure/Domain or its schema, join databases, call the local HTTP API from inside Shipping, or make HTTP endpoint code aggregate business facts.
Reuse canonical existing currency/context rules. Reflect store configuration without inferring IRR from country/fa. Require trustworthy consistent edition/tenant/commerce context; W1 authorizer retains its own gate. Never create a source/default mode to satisfy DI.
Active list must have usable owner-provided metadata and exactly one active declared default. Zero active languages, missing/multiple active defaults, missing effective currency or inconsistent/unresolved context means authoring context unavailable. This is a consumer readiness guard: it does not choose a new Localization default, activate rows, rewrite settings, or duplicate Localization write policies.
Add one stable Shipping-owned authoring-configuration-unavailable semantic error (or reuse an exact existing canonical code if source proves the same meaning), explicit 503 descriptor using the repository's defined availability classification/severity, localized resources and safe canonical fallback. This task explicitly authorizes this new read failure status; the original 44 legacy Shipping codes KEEP their HTTP 400 / Business / Warning. Adjust any old all-descriptors-are-400 test only to distinguish the preserved legacy set from the new explicitly classified code; do not weaken code-set/resource/unique-owner proof.
Unexpected dependency exceptions/cancellation propagate through existing canonical boundaries; no catch-all turning infrastructure failure into a fake fa/en success, empty-success configuration or zero currency. No raw exception text to client.
Every actual Shipping -> ILanguageLookup.ListAsync call must use the canonical IModuleCallTracer with stable source/target/operation, correct parent/correlation/disposal and no language list/IDs/user data in tags. Do not repeat the W3 mistake or globally decorate another module's service. Existing Geography/Fulfillment spans stay unchanged.

STRUCTURE, CLASSIFICATION AND CURRENT-GUARD UPDATES
Use existing module capability conventions and shallow exact namespaces. A cohesive Authoring capability with Queries/Models/Ports and matching Infrastructure adapter is allowed; no one-folder-per-query or generic Contracts dump. API-only internal read model belongs to Application, not Shipping.Contracts merely for being a DTO.
Update actual route/request/input classification for this one additive GET. It has no caller-controlled use-case inputs; a NO_VALIDATOR_REQUIRED classification is allowed ONLY with explicit provenance proof of the actual endpoint -> request construction and no hidden optional query binding. Authorization is not a substitute for input validation if any input is introduced.
Extend guard manifests/expected capability membership/counts only for the actual authorized feature, retaining detection of unclassified new requests, root/namespace/solution protections, descriptor equality and W2-R1 observation boundaries. The existing root composition guard must recognize an intentional new capability mapper if one is needed; do not keep a false fixed-five count or drop the exact mapper/route universe checks.
Inspect route constraints and literal precedence in an actual in-process endpoint routing test, not only a string assertion.
Current AMSC certifications remain historical baseline evidence; do not claim whole-module recertification for a feature build or alter unrelated states. Keep updated structure/guard facts truthful. No baseline widening or new exception to illegal coupling.

RECORD THE NEW SEMANTICS
Append a short dated section to SHIPPING-ADMIN-RULES-DECISION.md stating this D6 scope decision, source authority and the new API's semantics. Document the exact response, source ports, new error and remaining UI/owner limitations under this task evidence. Do not overwrite previous decisions or call DB-backed behavior runtime-proven from fake fixtures alone.

FOCUSED ACCEPTANCE / TESTS

Localization mapping: original known-language behavior and positional constructor/deconstruction preserved, actual active/inactive/default/direction/name/order metadata projected unchanged.
Query/source tests: ONE active non-fa language; TWO active languages; THREE or more active languages including RTL+LTR and an inactive language which is excluded; default is not first; default and order are owner-provided, never chosen from locale.
Invalid readiness: no active language, inactive-only default, no/multiple active default, unusable required metadata, missing currency, unresolved/inconsistent context => explicit unavailable Result, no fixture fallback or write. A thrown dependency/caller cancellation is preserved.
Context isolation: two independently resolved store/database source fixtures with different language sets/currencies do not leak into each other; SingleStore/Marketplace values come from trusted context and cannot be overridden by request inputs. No new cache.
Real in-process GET through module endpoint composition with ordinary auth/middleware fixtures: status/body/canonical error/permission/ISender dispatch, unauthorized denied, literal-route collision check, correct typed dynamic response. No authentication bypass in production. Label test fakes honestly.
Real module DI with existing foundation/tracer and Contracts test doubles; one listener-backed Shipping -> Localization child-span proof with correct parent and no leaked identifiers. Existing R1 trace tests remain green.
Actual database provenance is SOURCE-backed by Localization LanguageDirectory/ListAsync -> LocalizationDbContext in the resolved commerce DB; tests use controlled fakes/in-memory fixtures unless an already safe test-owned provider exists. Do not claim a real running user's DB/browser was exercised. No shared DB/server or new provider/package for demonstration.
Run affected Localization tests in the existing test home (discover it, do not create a project) plus new Shipping context/route/DI, classification/structure/error/auth/tracing/compatibility checks. One final Shipping suite after targeted checks pass is allowed; no full solution/Order or broad architecture reruns. Build affected projects only, use task-owned isolated artifacts if outputs locked.
Each command <=180 seconds; hard45m total. One deterministic in-scope repair + affected rerun only; missing authority/scope expansion/persistent failure => PARTIAL/BLOCKED, no test loop.

AUTHORIZED SCOPE / NON-GOALS
Localization: existing Contracts lookup snapshot additive metadata + existing bridge mapping and directly affected tests only. No LanguageDirectory/domain/HTTP/schema/seed/bootstrap change.
Shipping: one context endpoint/query/internal models/port/source adapter, own DI, necessary legal Contracts references, one error/catalog/resources and directly affected guards/tests/classification/decision evidence. Host production changes not expected because existing Shipping composition is already mapped; any new Host business authority is forbidden.
No frontend/preview, owner-scope persistence/seller grants, taxonomy/region groups/rate/mode/schedule/cutoff/financial engine, carrier API, promotion logic, schema/migration/snapshot/data/config/flag change, package, new project, foreign implementation access, cross-schema join or deployment.
No startup service/schema migrator change. Preserve all default-OFF gates, PendingAllocation, existing public language API, existing Shipping routes and financial consumers. TB-185 stays paused.

EVIDENCE / DELIVERY
Receipt: docs/ai/tasks/TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188.task.md verbatim.
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188/RESULT.md with actual source map/Contracts edges, response/error samples sanitized, language/mode/currency matrix, route collision/DI/real listener proof, commands/counts, source-vs-executed DB distinction, changed files/SHAs and remaining product gates.
PASS only for the genuinely wired authorized context read, dynamic metadata with 1/2/3+ proof, fail-closed handling, no forged mode/locale/currency, legal traced module boundary and focused passing checks. PARTIAL for missing mandatory proof/timebox; BLOCKED for unsafe source recovery or unresolved in-scope authority.
No integrated Admin/customer acceptance follows PASS; D1–D5/D7 and UI connection remain pending, and D6 is only implemented to the scope above.
Commit/push only scoped files, fetch verify exact parity, report scoped cleanliness, preserve unrelated dirty/untracked/stashed work. Post one complete Bridge result; never probe /api/results with placeholders. Stop for Architect; no automatic next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188
Parent-Task: TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_DB_BACKED_SHIPPING_AUTHORING_CONTEXT_READ
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 21d3900503475ce26052c33b97bb420cd8ec765c
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Authoritative-Edition-Currency-And-Language-Sources: ...
Additive-Language-Lookup-Compatibility: ...
One-Two-ThreePlus-And-Inactive-Language-Proof: ...
Default-Direction-Ordering-And-Locale-Independence: ...
Invalid-Configuration-And-Dependency-Failure-Proof: ...
Real-Authorized-GET-Route-And-Collision-Proof: ...
Trusted-Context-And-Isolation: ...
Contracts-Boundaries-DI-And-Module-Call-Tracing: ...
Request-Classification-And-Structure-Guards: ...
Legacy-HTTP-And-Behavior-Parity: ...
Focused-Builds-And-Test-Counts: ...
DB-Provenance-Source-Versus-Executed: ...
Remaining-Owner-UI-And-Product-Gates: ...
Schema-Data-Flag-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK