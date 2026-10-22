PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190
Parent-Task: TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Expected-Base-SHA: 676b7efcb9d4f402e662d5f955e82a33b252c835
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_ISOLATED_METHOD_DRAFT_AND_REGION_GROUP_PERSISTENCE
Hard-Timebox: 45 minutes HARD; STOP and report PARTIAL if exceeded.
Stop-State: STOP_FOR_ARCHITECT
Automatic-Next-Task: NONE

MISSION
Implement the first persisted part of the new Shipping authoring model: an owner-isolated method DRAFT referencing an existing canonical Fulfillment method binding, containing multiple region groups and multiple destination selections per group. Geography is stored once per group, never once per future rate/packaging/schedule row. Save/read/replace a whole draft region tree coherently with optimistic concurrency.
This is usable draft CRUD and persistence, not another analysis-only wave and not live pricing/activation. Rates/metadata/modes/schedules will extend these stable draft/group identities in later bounded tasks; do not fabricate them now.

MANDATORY FIRST CHECK
Fetch origin/main, inspect HEAD/branch/dirty files/stashes and preserve unrelated work. A safe strictly behind main may fast-forward only to the exact expected base. Require HEAD == origin/main == Expected-Base-SHA before edits. Different remote base/divergence/overlapping dirty work => STOP_FOR_ARCHITECT / RECOVERY_CONFLICT. No reset/rebase/stash/force-push.
Read AGENTS.md, registry and full tooba-backend-feature skill; apply relevant Migrate/Structure rules to new domain/persistence placement without starting a new full AMSC cycle. Read current Shipping rules/recovery, TB-188/189 sources/tests/classification, actual ShippingDbContext/migrations/design-time factory, current catalog/Geography Contracts and canonical Result/error/DI/tracing/currency foundations.
No findings-register scan or unrelated certified-module audit.

ARCHITECT ACCEPTANCE / D3 DESIGN
TB-189's owner-authority foundation is accepted from source review, not proof of persisted-data isolation. Reuse its actual IShippingAuthoringOwnerAuthorizer and its Store/Marketplace/Seller semantics. Require fulfillment.view on reads and fulfillment.manage on writes, independently, on EVERY request; no browser-returned scope as credential.
The draft is a SHIPPING-OWNED authoring aggregate referencing Fulfillment's immutable ServiceCode + optional OptionCode identity. This does not create a second carrier/method catalog, transfer catalog authority or permit seller edits to shared Fulfillment translations. Method binding stays immutable on an existing draft; catalog creation/owner-specific presentation metadata are outside this slice.
D3 decision: add new Shipping-owned draft/group/selected-area persistence, with stable draft and group IDs and an explicit owner key. One owner may configure its own draft for a binding independently of another owner. Geography selection belongs to the GROUP and is not copied to future rate rows, packaging or schedule.
Legacy policy/catalog/coverage/packaging/schedule rows are NOT migrated, copied, renamed, rekeyed or assigned owners here. Legacy runtime and default-OFF gates stay identical. The new model is explicitly Draft with no publish/activate/quote/customer consumer; an empty draft/group is incomplete configuration, never unrestricted coverage or free shipping.

BOUNDED IMPLEMENTATION

1. COHESIVE OWNER-KEYED DOMAIN AND PERSISTENCE
Add the smallest cohesive Shipping domain aggregate and own persistence mapping for:
- draft identity, explicit owner key, canonical service/option binding, revision/concurrency token and existing-style audit timestamps;
- stable region-group identity and intentional order;
- selected destination identities under each group (country / administrative area / city / service area, stopping at any valid level).
Use typed existing destination values where appropriate, no translated labels as identity. Do not depend on Geography Domain or Application. Domain must not reference Shipping.Application; Infrastructure maps the server-authorized TB-189 scope to the domain's persisted owner representation. Do not move authorization into the Domain or copy/derive identity from transport.
Choose exact owner-key columns/invariants consistent with TB-189 (Store uses real TenantId and no seller; Marketplace neither; Seller actual SellerPartyId and no tenant). No invented Party/platform GUID, hostname-as-tenant or client owner ID. Ensure nullable owner/binding keys remain unique correctly under PostgreSQL semantics, including service-level OptionCode null.
Use ShippingDbContext/schema, own mappings and own internal FKs only. Prefer explicit draft/group/area relational structure; do not persist an untyped client JSON blob or put foreign mutable aggregates into the document.
All lookup/list/update paths must filter by the FULL authorized owner scope, including Store TenantId even if connection routing already isolates stores. Loading by draftId alone then trusting caller scope is forbidden. Same binding may exist in separate owners; one owner's update never touches another.
Keep stable group IDs on read/update and prevent a group ID belonging to another draft/owner being reattached. No cascade across aggregate/owner boundaries.

2. TRUSTED METHOD AND GEOGRAPHY FACTS
Reuse the current canonical method-binding validation through Shipping's existing IShippingMethodOptionValidator and Fulfillment Contracts. Preserve service-level/legacy/canonical-only support, exact identity semantics and case normalization rules proved from source; never parse a display code to manufacture service/option. Do not seed legacy rows or flip the one-level gate.
Validate every destination against the actual Geography Contracts. Parent/child exclusion must use authoritative ancestry, not GUID equality, labels, name prefixes or client-provided ancestor arrays.
Geography administrative areas can have ParentAdministrativeAreaId recursively. Prove true ancestry through IGeographyLookup snapshots; Geography.GetByIdsAsync alone validates a submitted identity but is not proof of a complete arbitrary-depth ancestor chain. Use the owner-provided city->area and service-area->city relationship where necessary. Reject missing/inconsistent/cyclic ancestry rather than guess.
Build a bounded per-operation trusted selection/path representation for the pure domain overlap rule. Cache/batch repeated reference reads only inside the operation, not a new global cache or foreign-schema query. Distinguish node kind as well as ID, including fixtures that reuse the same GUID across different node kinds.
Reject duplicate or ancestor/descendant selections across ALL groups of the SAME draft, as well as within one group. Examples:
- Country Iran selected => all descendant additions conflict.
- Province Tehran selected => its city/area conflicts.
- Golpayegan selected => adding its province or Iran conflicts; independent Qom is allowed.
- Parent admin area vs nested admin child conflicts at arbitrary actual depth.
Selections in another method/draft/owner are independent. Do not silently remove/replace previous selections.
Canonical spans are required at every executed new foreign module read, with correct source/target/operation/parent/correlation and no destination/owner/user IDs or names in tags. Reuse existing traced seams; avoid double-wrapping a call already covered. No foreign implementation dependency.

3. AUTHORIZED HTTP + CQRS CRUD
Expose scoped routes under explicit non-colliding authoring-drafts paths for admin and seller audiences:
- POST collection: create complete initial draft region tree, returning actual new ID/revision and canonical Created Location for that audience;
- GET collection and GET draftId: list/read only the current authorized owner's drafts;
- PUT draftId: replace the submitted draft's region tree atomically using required expectedRevision, leaving immutable method binding/owner unchanged.
Use the same authoritative application use cases for admin/seller variants, preserving the legitimate many-routes/one-request classification introduced at 189.
Use TB-189 authorization BEFORE dispatch, with operation-specific View/Manage. SingleStore seller route stays unavailable; no admin arbitrary-seller-selection bypass. Do not bind owner/mode/tenant/SellerPartyId from request body/query. A forged resource/group ID never grants ownership.
All untrusted IDs, binding strings, collections, optional fields and expectedRevision require explicit FluentValidation shape coverage with stable codes. Use technically bounded payload sizes from existing module conventions and document them, not an invented commercial area limit.
Missing/foreign draft IDs on read/update return the SAME safe not-found outcome (404), without disclosing owner; stale expectedRevision returns explicit 409. Duplicate own binding/group/area conflicts must be explicit canonical errors with localized resources, not raw DB exception text or first-wins behavior. Preserve existing 44 legacy 400 errors and 188/189 statuses.
No endpoint business aggregate/persistence work, no Results.Json, no custom dispatcher/local mapper. One real IRequest/handler per actual use case; scoped store/directory ports stay internal Application.
Do not expose a shipping method as operationally active because its draft saved. Response and evidence must state Draft/incomplete scope honestly. No name/type/calculation/rate/currency/schedule placeholder values and no claim that this is the final full editor contract.

4. COHERENT SAVE / CONCURRENCY / RETRIES
Validate method/geography/owner and the full proposed replacement BEFORE changing tracked persistent state. One logical tree save must have one module-owned atomic persistence boundary; do not send N independent browser writes.
Use a monotonic revision with a REAL relational concurrency predicate, not just compare in-memory then blindly overwrite. Include revision update and group/area changes in the same SaveChanges transaction; EF's own atomic SaveChanges can provide the transaction without a new hand-rolled cross-module transaction.
A repeated identical PUT with the current revision must not duplicate groups/areas. A stale retry conflicts and leaves state unchanged. Uniqueness races must not silently merge data or expose provider detail; do not catch unrelated DbUpdateException as a duplicate.
Prove an invalid selection in the final group leaves all previous stored groups/revision unchanged; failed replacement must not partially clear or reparent data. In-memory provider cannot prove relational transaction/concurrency/unique behavior: label its limits and inspect actual generated predicates/constraints separately.

5. ADDITIVE MIGRATION — PREPARE ONLY
An additive Shipping-owned migration, designer and updated snapshot are explicitly authorized for these NEW tables/constraints only. Preserve every existing migration byte-for-byte and existing legacy tables/indexes/constraints. No destructive data movement/backfill, no ALTER weakening of existing unique keys, no cross-module FK.
Generate/review SQL without applying it to any shared database. Do not start the real Host, run MigrationRunner apply, alter shared :5432/tooba_alpha, change deployment flags or register a startup schema migrator.
Use an existing repository relational test harness/provider only if it is already available and safely task-owned. No new package/provider, image download, database/container startup or shared database writes in this task. Produce EF relational-model/SQL/concurrency-token proof plus real context save/clear/new-context read tests using the existing test provider; distinguish prepared SQL/model/in-memory execution from unperformed PostgreSQL runtime.
PASS for this bounded prepared backend slice does not close the real-PostgreSQL migration/transaction/constraint acceptance gate. If migration generation/review cannot be completed safely, return PARTIAL with exact missing proof rather than handwave "database ready".

6. DURABLE DECISION / GUARDS
Append a concise dated D3 draft/owner/group decision and prepared-only migration status to SHIPPING-ADMIN-RULES-DECISION.md. Document how later rate rows will reference stable group IDs without repeating geography; do not implement them now.
Update actual route/request/input classification and guard sets for the authorized CRUD use cases, including both audience paths and complete transport provenance. Preserve rejection of unclassified new routes/requests.
Extend current physical/persistence/error/DI guards only for intentional new facts. Existing operational-quote compatibility forbids historical operational-quote transaction/migration changes; this task authorizes a new independent draft authoring aggregate, NOT a quote-side money transaction. If a legacy blanket guard now conflates unrelated authoring SaveChanges with operational observation, preserve its approved operational invariant with precise tests; do not skip/weaken quote/Order observation protections.
Correct stale ShippingDbContext remarks (still "NOT WIRED") comment-only while preserving CLI-only migration application. No broad source-size/architecture baseline refresh or new certification claim. Do not update unrelated module states.

FOCUSED ACCEPTANCE / PROOF
Run domain overlap/identity tests, real authorized in-process CRUD -> real MediatR -> real store/DbContext round-trip with controlled canonical source fixtures, new-context readback and correct revision:
- Store A vs Store B; Marketplace vs Seller A vs Seller B; same method binding in independent owners;
- read/write forged other-owner draft/group IDs fail without data change; View cannot Manage; SingleStore seller denial;
- multiple groups, multiple disjoint areas, every stop level, deep parent/child across groups, shared GUID different node kinds, invalid/cyclic/missing authority;
- stable group identity/order after reload/replacement, no duplicate on retry, expectedRevision conflict;
- late invalid group leaves prior persisted snapshot/revision intact;
- relational model owner-shape/unique/FK/concurrency mappings and generated SQL checked; no real relational execution claim from InMemory;
- unchanged legacy rows/rates/coverage/packaging/schedules and flags, no live quote consumer or activation; no fabricated zero/no-coverage interpretation.
Run new focused CRUD/domain/store/validator/DI/tracing/route tests, existing 188/189/W1/W3-R1 regressions and exact persistence/structure/classification/error/compatibility guards as affected. One final Shipping suite after targeted checks pass is allowed.
Build affected projects only. No full solution, full Order/Host or broad architecture suite. Each command <=180 seconds, task hard45m; unique task-owned isolated artifacts when needed. One deterministic local repair + affected rerun only; persistent failure/unproved authority/wider necessary schema change => stop PARTIAL/BLOCKED.
Do not allocate most of the time to broad test repetition; implement and prove this bounded persisted increment first.

AUTHORIZED SCOPE / NON-GOALS
Shipping own Domain/Application/Infrastructure/Endpoints/Tests for draft/group/area CRUD only, existing ShippingDbContext/new additive migration/snapshot, minimal own DI/error/resources/guards/classification, decision/task/evidence. Existing owner's authorization and foreign Contracts are consumed, not redesigned.
No foreign module production edit, Host business logic, foreign DbContext/SQL/FK/join/shared transaction, public-contract dump, new package/project, old data backfill, rule publish/activation, full tariff engine/amount condition/packaging/payment/carrier/schedule/taxonomy/translation metadata/financial implementation, frontend/Storefront/Checkout/payment or deployment change.
Binding references are authoritative Fulfillment identities; this slice neither creates catalog methods nor lets sellers mutate shared names. Labels/translation integration follows separately. All default-OFF gates, PendingAllocation and TB-185 pause preserved.

EVIDENCE / DELIVERY
Receipt: docs/ai/tasks/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190.task.md verbatim.
Evidence: docs/architecture/evidence/TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190/RESULT.md plus concise aggregate/table/owner map, migration SQL review, schema-before/after scope, source-backed ancestry proof, atomicity/concurrency/retry matrix, actual HTTP/store tests, exact commands/counts/SHAs, prepared-vs-executed database limitations and remaining editor/runtime gates.
PASS only for actual owner-filtered draft CRUD/store roundtrip, trusted multi-region grouping with cross-group ancestry exclusion, atomic API design/concurrency mapping, prepared additive migration and focused required checks, with honest relational runtime gate. PARTIAL for missing required proof/timebox, BLOCKED for source/authority/isolation/scope conflict.
Product Admin completion, real Postgres migration/transaction proof, full editor save, live quote use and activation remain pending. Do not call in-memory tests production acceptance.
Commit/push scoped files, fetch verify exact parity and scoped cleanliness, preserve unrelated work/stashes. One complete Bridge result, no dummy/probe result. Stop for Architect; no automatic next task.

RESULT FORMAT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-SHIPPING-ADMIN-OWNER-REGION-DRAFTS-190
Parent-Task: TB-SHIPPING-ADMIN-AUTHORIZED-OWNER-SCOPE-189
Channel: tooba-main
Worker-Id: tooba-worker-01
Agent-Type: cursor
Repository: mrnikiemami-code/Tooba
Branch: main
Program: TOOBA-SHIPPING-ADMIN-BACKEND-DELIVERY
Mode: BOUNDED_OWNER_ISOLATED_METHOD_DRAFT_AND_REGION_GROUP_PERSISTENCE
Bridge-Task-Row: ...
Status: PASS|PARTIAL|BLOCKED
Expected-Base-SHA: 676b7efcb9d4f402e662d5f955e82a33b252c835
Base-SHA: ...
Code-SHA: ...
Evidence-SHA: ...
HEAD-Origin-Parity: ...
Owner-Keyed-Aggregate-And-Canonical-Binding: ...
Actual-Authorized-CRUD-And-NewContext-Readback: ...
Cross-Owner-And-Operation-Permission-Isolation: ...
Trusted-Geography-And-Deep-CrossGroup-Overlap: ...
Stable-Group-Identity-And-No-Duplicated-Geography: ...
Atomic-Replace-Revision-And-Retry-Proof: ...
Relational-Model-SQL-Versus-InMemory-Limits: ...
Additive-Migration-And-Legacy-Preservation: ...
Contracts-DI-Tracing-And-Input-Classification: ...
Focused-Commands-And-Test-Counts: ...
Real-Postgres-Acceptance: PENDING
Full-Editor-Rate-And-Runtime-Activation: NOT_IMPLEMENTED
Shared-Database-Data-Flag-Changes: NONE
Changed-Files: ...
Isolation-And-Cleanup: ...
Elapsed-Minutes: ...
Production-Functional-Acceptance: PENDING
Stop-State: STOP_FOR_ARCHITECT
automaticNextImplementationTask: NONE
Automatic-Next-Task: NONE
END_TOOBA_WORKER_RESULT
END_TOOBA_TASK
