PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189
Parent-Task: TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 5b74601fe6db10bde1575eb680b8104f2ac553a5
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_SERVER_AUTHORIZED_SHIPPING_RULE_OWNER_SCOPE
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Implement the authoritative owner-scope resolution and authorization foundation needed before owner-scoped Shipping rule persistence. Deliver actual authorized read endpoints identifying the CURRENT permitted rule owner, with a reusable module-owned authorization seam for later operations. Distinguish installation context from settings ownership and seller identity.
This is one real backend increment. It does NOT create owner-scoped rate storage, make existing global rules seller-editable or claim complete marketplace seller authoring. No schema/data migration or frontend integration.

MANDATORY FIRST CHECK
Fetch origin/main and inspect HEAD/branch/dirty files/stashes. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before edits. Changed remote base/divergence/conflicting dirty files => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. Preserve unrelated work/running apps; no reset/rebase/stash/force-push.
Read AGENTS.md, registry, full tooba-backend-feature skill and relevant AMSC/Structure/Contracts rules. This is feature development on the accepted baseline, not another module-wide AMSC cycle or recertification.
Read SHIPPING-ADMIN-RULES-DECISION.md, current 188 context source/query/endpoint and classification, W1 trusted edition/tenant authorizer, IPlatformAccessSeams and its actual canonical implementations/consumers, permission identifiers, current error resources/DI/guards and W3-R1 tracing pattern. Read only direct required dependencies; do not scan the findings register or reopen unrelated certified modules.

ARCHITECT REVIEW OF 188
188 is accepted from source review for the additive database-backed configuration read; real merchant DB/browser execution remains NOT PROVEN. Existing legacy metadata snapshot semantics and canonical traced Localization read are preserved.
Two corrections:

The ShippingAuthoringContextView.Edition XML currently says Marketplace means seller-scoped authoring is ready. That sentence is false (missing negation). Correct it to explicitly say edition alone does NOT establish seller-scoped authoring readiness.
Worker ran dotnet build src/backend/Tooba.slnx despite the task's affected-project-only rule. Record this scope deviation; do not repeat broad builds/tests or use more commands as a substitute for bounded proof.

ARCHITECT D1 SCOPE DECISION
Rule OWNER is the authority over Shipping settings; it is not necessarily a merchant Party identity:

SingleStore => Store owner scope, keyed internally by the stable resolved active TenantId. The store itself owns its settings. Do not fabricate a merchant/seller Party GUID merely because no canonical merchant seam exists.
Marketplace platform => Marketplace owner scope in the currently resolved installation/database. TenantId and SellerPartyId are absent; do not invent a platform GUID or use a client deployment identifier as authority.
Marketplace seller => Seller owner scope with the actual SellerPartyId returned by the canonical ISellerPanelAccess seam for the authenticated actor, plus the actual required Shipping permission for that SAME owner. Browser-supplied seller header is only a candidate consumed by that canonical seam, never an authority by itself.
In SingleStore, marketplace/seller-specific authoring scopes are unavailable at the server, regardless of supplied headers/UUIDs. Store scope is not a renamed seller.
A future persisted rule must carry its server-authorized scope and every read/write must authorize the operation afresh. The response here is not a credential, owner grant or reusable write token.
Legacy global catalog/policy rows remain unchanged and UNASSIGNED to the new owner-scoped authoring model until a separately authorized migration decides their attribution. Do not automatically assign/copy them to any seller or change quote behavior.
This decision addresses authority modelling, not D3 storage/backfill. Localization registry remains the effective store/installation registry from 188, not per-seller language settings.

BOUNDED IMPLEMENTATION

TYPED INTERNAL OWNER SCOPE
Introduce one cohesive Application-internal immutable owner-scope model (Store / Marketplace / Seller) with mutually valid fields. Factories/invariants prohibit inconsistent combinations: Store requires the actual stable tenant id and no seller; Marketplace has neither tenant nor seller; Seller requires nonempty SellerPartyId in Marketplace and no SingleStore tenant.
Keep owner identity separate from ActorUserId, mode and display labels. Do not create a new enum/project in shared BuildingBlocks, public god-contract or Shipping.Contracts dump merely to share a local query model.
Do not expose connection references, internal stable tenant keys or security details in the HTTP response. A compact view may return owner kind and SellerPartyId only when the caller is authorized for that seller; no fake Party/platform identifier.
This model is data about a resolved owner, not authorization by type name: authority comes from actual server gates on every request.

AUTHORIZATION SEAM — NO UI-TRUSTED OWNER
Add a Shipping Endpoints-owned authorization adapter using existing platform seams:

Admin read: preserve real IShippingAdminAuthorizer + fulfillment.view. With trusted resolved SingleStore => Store scope; Marketplace => Marketplace scope.
Seller read: FIRST require resolved Marketplace and consistent tenant/commerce context, then call ISellerPanelAccess.RequireAuthorizedAsync on the actual request. That seam alone is not proof of a Shipping permission: use the canonical IPlatformEffectiveAccessReader (or the already-established equivalent owner-bound permission policy, proved from source) for that SAME actor, PlatformAccessOwnerKind.Seller and returned SellerPartyId.
Require the existing requested fulfillment.view permission at GlobalWithinOwner scope. DeniedByCeiling and narrower category/product/brand/warehouse grants must NOT become whole-owner permission. Reuse exact canonical effective-grant semantics after inspecting current consumers; do not invent manage-implies-view, a role bypass, or an "admin can pick any seller" exception.
A reusable authorization method may accept an existing permissionId so later writes can separately request fulfillment.manage; this task exposes only read endpoints. Unit-test that view permission does not authorize manage. Do not add/grant/rename platform permissions.
Never trust a sellerId/ownerKind/mode/tenant in a body/query or manually parse a seller header in Shipping. The existing seller seam owns candidate resolution/actor->party authorization. Reject unknown, unauthorized/empty actor/seller, denied ceiling or missing whole-owner permission before ISender dispatch.
Keep unresolved/inconsistent context fail-closed using canonical existing platform errors. SingleStore seller endpoint must fail with explicit Shipping capability-unavailable (404) before seller resolution or any business query, not synthesize Marketplace. A missing seller permission is a stable Shipping owner-scope-denied (403), with canonical descriptor/resources/fallback and no owner existence leakage. Reuse a genuinely equivalent existing code if proved; do not duplicate Foundation code ownership.
Propagate real platform authorization unavailable/cancellation/unknown faults according to their canonical meaning; no catch-and-allow, synthetic success or exception-message classification.

Reuse W1's trusted installation-context checks rather than a parallel mode authority. A minimum mechanical extraction of the existing private edition/active-tenant validation into one Shipping-owned internal authorization helper is allowed if needed to share it, with identical W1 behavior and all old tests retained. Do not change platform-wide authorizers or Host security retained sets.

Any actual Shipping -> effective-access module read must preserve canonical call tracing using existing IModuleCallTracer at the appropriate established boundary. Determine the existing seam's ownership/decoration from source; no second tracer pipeline or globally relabelled AccessControl service. If a new Shipping-owned boundary span is required, use a constant target/operation and no actor/seller IDs in tags. Do not introduce the previous W3 omission again.

TWO REAL READ ENDPOINTS / ONE AUTHORITATIVE USE CASE
Add:
GET /v1/admin/shipping-services/authoring-owner
GET /v1/seller/shipping-services/authoring-owner
Each uses the real corresponding Shipping owner authorizer, then ISender with the SERVER-derived authorized scope -> one authoritative read query/handler -> Result -> ApiResponseFactory.From with a typed compact owner view.
Both routes implement the same current-owner read; share the use case instead of duplicate command/query shapes. Request scope is constructed on the server, not bound from transport. The handler does not access HttpContext or foreign modules.
No request body or owner/mode/seller/tenant query binding. Do not add an endpoint that lists arbitrary sellers or ownership of foreign rules. Source authorization failures occur before dispatch and must not call a fake Allow authorizer in production.
Route matching/collision with existing serviceId/serviceCode literals must be proved. Existing 188 authoring-context API remains unchanged except the incorrect XML clarification; a seller-specific language/context API is NOT part of this task.
These endpoints report authorized scope only. They must not list, save or activate any pre-existing ownerless rate/canonical method, imply ownership of global Fulfillment catalog rows, or announce canManageSellerRules=true.

CLASSIFICATION / ERROR / STRUCTURE GUARDS
Derive the updated actual route/request universe. One shared query may legitimately be reachable from two routes: exact distinct request classification must not assume one route equals one unique request. Represent both route/input provenance paths without duplicate or missing request semantics. Do not make the guard tolerate arbitrary unclassified routes.
NO_VALIDATOR_REQUIRED is permitted only if all request fields are demonstrably derived by executed server authorization, with no malformable caller shape forwarded unchecked. Prove seller candidate input was handled by the canonical platform seam before constructing the request. If any caller-controlled field is added, classify/validate it properly; optionality is not trust.
Add only the explicit new 403/404 descriptors required above; preserve every old code and 188's 503 plus the original 44 legacy 400 mappings, resource equality, composed-catalog uniqueness and configured fallback.
Keep capability-first shallow exact paths/namespaces and actual DI/lifetimes. Update only guard facts changed by these two intentional routes/capability files; do not weaken dependency/root/route/source-size protections or re-certify the module.
Append the dated D1 scope decision, legacy non-attribution and read-only limitation to SHIPPING-ADMIN-RULES-DECISION.md. Preserve history and other decisions.

ACCEPTANCE / FOCUSED TESTS
Execute meaningful unit + in-process HTTP/DI tests:

SingleStore active tenant => Store, correct internal stable tenant, no seller; Marketplace/null tenant => Marketplace; unresolved/Unset/unsupported/inconsistent context fails, same W1 gates preserved.
SingleStore seller route => 404 capability unavailable before seller seam/query dispatch; no request override can enable it.
Marketplace seller route => canonical authenticated actor+SellerPartyId, actual whole-owner fulfillment.view grant => Seller scope. View-only cannot authorize manage in the reusable seam.
Wrong/empty seller/actor, actor-party denial, required grant absent, denied ceiling, narrower grant only, foreign-owner grant/candidate mismatch, authorization unavailable and cancellation all fail safely; no default owner or list of foreign rules. Verify actual outgoing permission-reader owner parameters.
Request query/header/body owner/mode/tenant overrides cannot replace resolved scope. The existing canonical seller-candidate header is accepted ONLY through real seam authorization, never by copying it into the response.
Actual endpoint composition / routing, typed response, canonical 403/404 errors, ISender called exactly once after authorization and zero times on denial. Fakes must be labelled; test production adapter, not only a permissive replacement.
Two scopes/store fixtures do not alias/leak; response cannot be reused as authority for a later operation. No static state/cache. Required platform calls produce canonical spans if applicable, tested with a real listener, bounded tags and exact arguments/exception propagation.
Existing 188 dynamic-language/context regressions and W1 error/auth + W3-R1 traces + W2-R1 compatibility stay green. New classification guard proves both routes map to exactly one classified use case; rejects a new unclassified route/request.

Run only affected Shipping projects/tests and exact relevant platform-semantic fixtures if needed. One final Shipping module suite allowed after targeted checks. No full solution/Order/architecture suite; no redundant repeat. Each command <=180 seconds and total hard45m; use unique task-owned isolated build output if necessary.
No DB/server/browser/container/package/migration/seed startup or shared process restart/cache purge. This feature is testable without a schema. Source-bound platform contracts may be faked in tests, but do not claim a running user's identity store was exercised.
One deterministic in-scope repair + affected rerun only. Unproven owner-permission semantics, wider required platform changes or persistent failure => stop PARTIAL/BLOCKED with exact handoff; do not fabricate a safe owner.

AUTHORIZED WRITES / NON-GOALS
Shipping Application internal owner model/query/view; Shipping Endpoints owner-authorization adapter/routes, minimal mechanical shared W1 context helper if required; own DI/registration and tests; necessary own error codes/resources; classification/structure guard facts; incorrect 188 XML; decision/task/evidence.
No new public cross-module contract unless an actual consumer requires it (none planned); no direct AccessControl/Party implementation dependency, foreign DbContext/schema query, Host production file, new BuildingBlocks authority, custom identity resolver, persistent owner profile or column, schema/migration/backfill, rule write, rate/group/taxonomy/mode/schedule/cutoff/carrier/promotion/financial engine or language behavior change. No frontend/Storefront/Checkout/payment, config/flag activation or deployment.
Do not assign existing global rows an owner or expose them through the seller route. Preserve default-OFF gates, PendingAllocation and TB-185 pause. D3 persistence, legacy mapping execution and integrated UI remain future separate tasks; D1 authority is not proof of stored-data isolation.

EVIDENCE / DELIVERY
Receipt: docs/ai/tasks/TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189.task.md verbatim.
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189/RESULT.md, concise owner/permission/provenance matrix, real route/DI/span assertions, commands/counts, actual metadata/error changes, exact source/Code/Evidence SHAs, source-vs-executed authority limitations, isolation and elapsed.
PASS only for actual authorized current-owner reads, correct installation-specific behavior and operation-specific seller permission proof, no forged scope, lawful current boundaries, canonical errors/tracing and required focused checks. PARTIAL if mandatory proof/timebox incomplete; BLOCKED if authority/source/scope unsafe. No claim of persisted owner isolation, seller-authored rules, real merchant-runtime acceptance or recertification.
Commit/push scoped files, fetch verify exact HEAD-origin parity, report scoped cleanliness and preserve unrelated work/stashes. No probe/dummy result payload; one complete Bridge result. Stop for Architect; no automatic next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189
Parent-Task: TB-SHIPPING-ADMIN-CONFIGURATION-CONTEXT-188
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_SERVER_AUTHORIZED_SHIPPING_RULE_OWNER_SCOPE
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 5b74601fe6db10bde1575eb680b8104f2ac553a5
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
D1-Owner-Scope-Authority-And-Legacy-NonAttribution: ...
SingleStore-And-Marketplace-Admin-Proof: ...
Marketplace-Seller-And-Operation-Permission-Proof: ...
Forged-Foreign-Narrow-And-Ceiling-Denial-Matrix: ...
Real-Authorized-Routes-And-No-PreDispatch-Leak: ...
Shared-UseCase-And-Input-Provenance-Classification: ...
DI-Contracts-Boundaries-And-Canonical-Tracing: ...
Error-Resources-And-Existing-188-Parity: ...
Focused-Commands-And-Test-Counts: ...
188-Review-Corrections-And-Build-Scope: ...
Stored-Owner-Isolation-And-Rule-Writes: NOT_IMPLEMENTED
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