PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1
Parent-Task: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001
Parent-Commit: 8c0e605c97ec35c116179d95915d38cf736f5468
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: CUSTOMER_FULL_CLOSURE_REPAIR
Title: Repair canonical Result pipeline + Solution Explorer grouping + recovery artifact
Architect-Verification:
The parent task is NOT accepted yet. Architect independently verified two real defects:
1. CustomerProfile.Endpoints still returns raw success results:
   - CustomerProfileEndpoints.GetProfileAsync -> Results.Json(page)
   - CustomerProfileEndpoints.UpdateProfileAsync -> Results.Json(page)
   - CustomerAccountDashboardEndpoints.GetDashboardAsync -> Results.Json(page)
   - CustomerAccountDashboardEndpoints.GetDevContext -> Results.Json(...)
     These violate the canonical API result pattern required by the active skills/reference pattern for HTTP-owned use-cases.
2. CustomerProfile projects are still flat under <Folder Name="/Modules/"> in src/backend/Tooba.slnx; there is no /Modules/CustomerProfile/ Solution Folder grouping.
3. The canonical repository task artifact for the parent task is missing:
   docs/ai/tasks/TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001.task.md
Current parent implementation may otherwise be preserved. This is a bounded repair, NOT a redesign.
Required-Skills:
- .cursor/skills/tooba-architecture-analyze/SKILL.md
- .cursor/skills/tooba-architecture-migrate/SKILL.md
- .cursor/skills/tooba-architecture-certify/SKILL.md
Canonical-References:
- ApiResponseFactory.From(Result<T>) preserves raw successful DTO JSON while centralizing failure mapping.
- Offer Endpoints demonstrate canonical ISender -> Result<T> -> api.From(result) shape.
- TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
- TMAR-HOST-EVACUATION-PROTOCOL.md
- tmar-current-state.json
- tmar-module-structure-manifests.json
SCOPE
Repair only the CustomerProfile/customer-account surface created by the parent task plus minimum required solution/recovery metadata.
Do NOT:
- reopen Host/Customer ownership;
- recreate a Customer module;
- move routes back to Host;
- redesign CustomerProfile/Wishlist/Order/AddressBook/Identity;
- change route paths/methods;
- change success JSON shapes;
- change schema/migrations;
- change frontend;
- weaken guards/tests;
- start next Host folder.
A. CANONICAL RESULT PIPELINE
A1. Re-read all parent-touched CustomerProfile Application/Endpoints requests and handlers.
A2. Convert HTTP-reachable Application requests to canonical Result semantics:
- GetCustomerProfilePageQuery -> IRequest<Result<CustomerProfilePage>>
- UpsertCustomerProfileCommand -> IRequest<Result<CustomerProfilePage>>
- GetCustomerAccountDashboardQuery -> IRequest<Result<CustomerDashboardPage>>
Handlers must return Result<T> using the existing BuildingBlocks Result API. Do not invent another result abstraction.
A3. Endpoint success/failure mapping:
- Replace raw Results.Json(page) for the above three business HTTP routes with:
  var result = await sender.Send(...); return api.From(result);
- Success must remain the same RAW DTO JSON shape because ApiResponseFactory.From<T> already emits Results.Json(result.Value).
- Expected failures must flow through Result/SemanticError + ApiResponseFactory.
- Unexpected exceptions continue to the global exception boundary.
- No endpoint catch-and-map.
- No ex.Message classification.
A4. GET /v1/customer/dev-context:
This is a platform/dev presentation route, not a business CQRS use-case.
Do not force fake CQRS ceremony.
However, do not leave an ad-hoc success/error response path if the canonical response factory can represent it without shape change.
Use the smallest repository-consistent canonical approach:
- production/non-dev NotFound behavior must remain exactly 404;
- dev/testing success shape must remain { actorUserId, label };
- if ApiResponseFactory is used, preserve exact raw JSON success shape.
  If no existing canonical factory method safely represents this platform-only route without changing semantics, document an explicit PLATFORM_DEV_ROUTE_EXCEPTION in evidence and guard it narrowly. Do NOT invent a new API response abstraction just for this route.
A5. Stable error ownership:
- Continue using customer.session.required.
- Do not duplicate/re-register its descriptor.
- Composed catalog duplicates remain ZERO.
B. SOLUTION EXPLORER / PHYSICAL STRUCTURE
B1. Update src/backend/Tooba.slnx so CustomerProfile is grouped exactly under:
/Modules/CustomerProfile/
with:
- Tooba.CustomerProfile.Domain
- Tooba.CustomerProfile.Contracts
- Tooba.CustomerProfile.Application
- Tooba.CustomerProfile.Infrastructure
- Tooba.CustomerProfile.Endpoints
Remove those five CustomerProfile project entries from the flat /Modules/ folder.
Do NOT change project paths, assembly names, namespaces, or physical filesystem locations.
B2. Add a durable focused guard proving:
- /Modules/CustomerProfile/ exists in Tooba.slnx;
- all 5 CustomerProfile projects occur exactly once in that Solution Folder;
- none of those 5 remain as flat direct children of /Modules/;
- project paths remain unchanged.
B3. Do NOT group unrelated modules merely for cosmetic consistency in this task.
Wishlist remains out of this grouping repair unless an exact touched-surface test requires no-op metadata; avoid scope expansion.
C. RECOVERY ARTIFACT REPAIR
C1. Add missing canonical parent task artifact:
docs/ai/tasks/TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001.task.md
It must be the actual task definition that was executed, not a rewritten after-the-fact description.
C2. Persist this repair task at:
docs/ai/tasks/TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1.task.md
C3. Update existing evidence under:
docs/evidence/TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001/
with an R1 repair section/file proving:
- raw Results.Json business paths removed;
- Result<T> pipeline canonicalized;
- success payload parity preserved;
- CustomerProfile Solution Folder grouping added;
- parent task artifact restored.
D. GUARD STRENGTHENING
Strengthen/add focused guards so future regression fails if:
1. CustomerProfile business endpoints call Results.Json directly for CQRS business results;
2. endpoint-reachable CustomerProfile requests no longer return Result / Result<T>;
3. api.From(result) is bypassed for profile/dashboard business routes;
4. CustomerProfile projects lose /Modules/CustomerProfile/ Solution Folder grouping;
5. CustomerProfile project appears twice in Tooba.slnx;
6. Host/Customer production files reappear;
7. foreign Application/Infrastructure/Domain coupling reappears in customer-account surface;
8. duplicate descriptor ownership for customer.session.required reappears.
Do not create brittle guard text that hard-codes irrelevant implementation details.
E. FOCUSED VALIDATION
Build only:
- CustomerProfile.Application
- CustomerProfile.Endpoints
- Host
- Host.Tests / directly relevant test project
Run focused tests only:
- CustomerProfile request/validator coverage guard
- HostCustomerFullClosure guard
- new Result-pipeline guard
- new Solution Folder grouping guard
- customer profile/dashboard behavior parity tests
- ErrorCatalogUniqueCodeGuardTests if directly available in focused host tests
No full solution suite.
No open-ended fix/test loop.
One deterministic local repair per focused failure, then one affected rerun only.
If broader failure appears, STOP and report it.
F. FINAL CERTIFICATION CONDITIONS
PASS only if ALL are true:
- Host/Customer production files = 0
- no Modules/Customer
- profile/dashboard routes remain CustomerProfile.Endpoints-owned
- profile/dashboard CQRS requests use Result<T>
- business endpoint mapping uses ApiResponseFactory.From(result)
- no raw Results.Json(page) remains for profile/dashboard business results
- dev-context route has either canonical factory mapping with exact parity OR an explicit narrow platform-dev exception justified in evidence
- success DTO JSON shape unchanged
- customer.session.required canonical owner unchanged and duplicate count = 0
- /Modules/CustomerProfile/ exists in Tooba.slnx
- all 5 CustomerProfile projects grouped exactly once there
- no flat duplicate CustomerProfile entries under /Modules/
- path/namespace unchanged and exact
- schema/migrations NONE
- frontend unchanged
- both task artifacts exist
- evidence/SoT honest
- next Host folder not started
- commit pushed; HEAD == origin/main
SoT:
Update hostCustomerFullClosure to include:
- repairTask = TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1
- apiResultPattern = CANONICAL_RESULT_T_API_FACTORY
- solutionGrouping = /Modules/CustomerProfile/ EXACT
- taskArtifactState = PRESENT
- workflowStop = USER_REVIEW_HOST_CUSTOMER_FULL_CLOSURE_R1
  Do not rewrite unrelated history.
CANONICAL WORKER RESULT
Return ONLY:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001-R1
Parent-Task: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Host-Customer-Production-File-Count:
Customer-Module-Created:
Profile-Route-Owner:
Dashboard-Route-Owner:
Business-Request-Result-State:
Business-Endpoint-ApiResponseFactory-State:
Raw-Business-ResultsJson-Count:
DevContext-Response-State:
Success-Payload-Parity-State:
Error-Catalog-Duplicate-Count:
CustomerProfile-Solution-Folder-State:
CustomerProfile-Solution-Project-Count:
CustomerProfile-Flat-Duplicate-Count:
Path-Namespace-State:
Schema-Migration-State:
Frontend-State:
Parent-Task-Artifact-State:
Repair-Task-Artifact-State:
Focused-Validation:
Known-PreExisting-Failures:
Remaining-Blockers:
Residual-Debt:
SoT-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Next-Host-Folder-Started: false
STOP
END_TOOBA_WORKER_RESULT
STOP RULE:
After the result, stop completely.
Do not start the next Host folder.
Do not self-issue another task.
END_TOOBA_TASK
