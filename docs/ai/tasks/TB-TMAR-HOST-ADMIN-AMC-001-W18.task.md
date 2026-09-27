PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W18
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W17
Parent-Commit: a4642fb8592062ff854ce30408ae498792ed32b4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W18 — establish canonical ProductWorkspace composition module skeleton and boundary locks; no route moves

ARCHITECT ACCEPTANCE

W17 is ARCHITECT-ACCEPTED.

Verified W17 decision:

remaining Host ProductWorkspace routes = 19
full ProductWorkspace post-write aggregate responses = 15
blind Catalog ownership of aggregate composition = REJECTED
PageComposition suitability = NO
selected target = split ownership:
Catalog = Catalog write authority + Catalog-only routes
new ProductWorkspace module = Admin aggregate composition + aggregate HTTP
Host = temporary residue only
direct CatalogDbContext reach-through must be eliminated
ProductWorkspace composition must use module Contracts only
Party.Application dependency must become Party.Contracts
StoreAppearance remains deferred

Verified parent commit:
a4642fb8592062ff854ce30408ae498792ed32b4

W18 OBJECTIVE

Create the canonical Tooba.ProductWorkspace module skeleton so W19 can begin migrating aggregate reads without inventing architecture during the move.

NO EXISTING PRODUCTWORKSPACE HTTP ROUTE MOVES IN W18.

NO RESPONSE CONTRACT CHANGE.

NO EXISTING HOST PRODUCTWORKSPACE PRODUCTION FILE DELETION.

NO NEW BUSINESS BEHAVIOR.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W19.

REQUIRED READ

Before changes inspect:

AGENTS.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/tmar-current-state.json
W17 evidence completely
current certified module project/reference conventions
current solution / .slnx grouping conventions
BuildingBlocks Result/MediatR/FluentValidation registration conventions
current Host module registration conventions
Party.Contracts exact lookup port
Catalog.Contracts current product/category/variant/media/quantity/read contracts
Offer.Contracts / Pricing.Contracts / Inventory.Contracts / Tax.Contracts gateway contracts

MANDATORY ARCHITECTURE LOCK

Create exactly this production module family unless repository conventions require a mechanically equivalent project naming form:

Plain text
src/backend/Modules/ProductWorkspace/
  Tooba.ProductWorkspace.Contracts/
  Tooba.ProductWorkspace.Domain/
  Tooba.ProductWorkspace.Application/
  Tooba.ProductWorkspace.Infrastructure/
  Tooba.ProductWorkspace.Endpoints/

Do NOT create fake business concepts merely to populate folders.

The Domain project MAY contain no domain types yet if none are semantically justified.
An empty-but-valid Domain assembly is preferable to invented domain behavior.

W18 does NOT claim ARCH-COMPLETE-002 certification.

PROJECT REFERENCE RULES

Contracts:

stable ProductWorkspace boundary semantics only
may reference neutral BuildingBlocks only if actually required
must not reference foreign Application/Infrastructure/Domain

Domain:

own domain only
no foreign module references

Application:

references ProductWorkspace.Contracts + ProductWorkspace.Domain + neutral BuildingBlocks
may reference foreign *.Contracts ONLY when an actual stable boundary is needed
MUST NOT reference:
Catalog.Application / Infrastructure / Domain
Offer.Application / Infrastructure / Domain
Pricing.Application / Infrastructure / Domain
Inventory.Application / Infrastructure / Domain
Tax.Application / Infrastructure / Domain
Party.Application / Infrastructure / Domain
Host

Infrastructure:

references ProductWorkspace.Application / Contracts / Domain
may reference foreign Contracts only
no foreign DbContext
no foreign Application / Infrastructure / Domain
no Host

Endpoints:

references ProductWorkspace.Application / Contracts + presentation/neutral BuildingBlocks
no ProductWorkspace.Infrastructure reference
no foreign Infrastructure
no Host

MODULE COMPOSITION ENTRIES

Create canonical root composition entries only:

ProductWorkspaceModule.cs in Infrastructure
ProductWorkspaceEndpointModule.cs in Endpoints

Root .cs files must be allowlisted deliberately.

W18 may wire DI only if it is behavior-neutral.
W18 MUST NOT map any existing /v1/admin/products route.

If an empty endpoint-map extension is created, use a name that does NOT conflict with current Host:

preferred MapProductWorkspaceModuleEndpoints(...)

Do NOT use the existing Host extension name MapProductWorkspaceEndpoints.

HOST INTEGRATION

Preferred W18 behavior:

add project references / solution grouping needed for builds
do NOT call the ProductWorkspace endpoint map from Host yet
do NOT transfer route ownership
do NOT change current ProductWorkspace DI unless required to compile a neutral module registration

If Infrastructure registration is wired into Host, it must register only module-local no-op/composition services and must not alter current runtime behavior.

No duplicate route mapping.

APPLICATION SKELETON

Create capability-first structure for the future aggregate read surface without duplicating Host DTO authority.

Preferred:

Plain text
Tooba.ProductWorkspace.Application/
  Composition/
    Models/
    Ports/
    Queries/
    Validators/

But W18 MUST NOT create duplicate ProductWorkspaceView / AdminProductListItem DTOs merely as placeholders.

Create only:

assembly/module markers if repo conventions require them
real boundary interfaces required for the skeleton
future capability directories if current structure rules/guards need them

Empty .gitkeep folders are allowed only when they support the immediately-next W19 structure and are documented.
Do not create speculative command classes.

CONTRACTS SKELETON

Do NOT copy Host request/response records into Contracts in W18.

Contracts may contain:

module marker
stable error-code container only if already justified
stable cross-module interface declarations only if they are immediately required for W19 and can be defined without guessing

No mixed ProductWorkspaceContracts.cs bundle.

CATALOG READ BOUNDARY — DESIGN LOCK, NOT FULL MIGRATION

W19 will require ProductWorkspace composition to read Catalog data without CatalogDbContext or Catalog.Application.

W18 must document and, only if safe/precise, establish the minimal stable Catalog.Contracts read boundary.

Before adding any new Catalog.Contracts type:

inventory existing Catalog.Contracts gateways.
reuse existing contracts where possible.
add only missing stable boundary semantics.
do NOT create one giant "workspace snapshot" if that merely mirrors Catalog persistence.
do NOT expose EF entities/domain entities.
do NOT add write commands to Catalog.Contracts in W18.

Preferred future read needs to cover W19:

product identity/list basics
variant identifiers/status/catalog seam
product attributes + variant-axis values
media references
category assignments / leaf names / paths
brand identity/display
localized product fields
quantity policy + unit options
publish readiness
history shell

W18 may stop at a documented exact boundary map if implementing those contracts safely exceeds the timebox.
PASS does NOT require all W19 Catalog read gateways to be implemented.

PARTY BOUNDARY

Verify canonical Party.Contracts lookup equivalent to current Party.Application.IPartyLookupGateway.

Record exact target type/path for W19.

Do NOT change Host ProductWorkspaceComposer to Party.Contracts yet unless this can be done as a purely mechanical behavior-preserving boundary correction with zero route/runtime effect and fits safely.

Default W18: document only; W19 performs live adoption.

AUTHORIZATION BOUNDARY

ProductWorkspace.Endpoints will need Admin authorization without depending on Host or Catalog.Endpoints.

Create a ProductWorkspace-owned endpoint authorization abstraction if current module conventions require it, e.g.:

IProductWorkspaceAdminAuthorizer

Location:

Endpoints shared Admin area, OR stable Contracts only if cross-project implementation requires it.

Do not put security business policy in Application.

No route consumes it yet in W18.

WORKSPACE SCOPE BOUNDARY

Current:
X-Tooba-Workspace-Scope=view

W18 must decide canonical future owner:

ProductWorkspace.Endpoints transport policy

Create only the smallest reusable parser/policy if doing so creates no behavior change and no duplicate runtime ownership.
Otherwise document exact W19 destination.

Do not move/remove Host ReadPermissions in W18.

ERROR BOUNDARY

Do not duplicate Catalog-owned errors.

Future ProductWorkspace composition errors should be owned by ProductWorkspace only when they describe composition/workspace transport semantics, not Catalog business failures.

W18 may establish:

ProductWorkspace error code container
endpoint error contributor/resources
ONLY if real W19 errors are already known.

Avoid placeholder error catalogs.

SOLUTION / MANIFEST / STRUCTURE

Add all new projects to the canonical solution / .slnx grouping exactly once.
ProductWorkspace appears exactly once as one module grouping.
Add/update module structure manifest as preCert / NOT_CERTIFIED if the repository uses such state.
MUST NOT set structureCertified=true.
MUST NOT add ProductWorkspace to structureLock.certifiedModules.
exact path↔namespace for every created .cs.
no root mixed files beyond composition entries/explicit markers.

DEPENDENCY GUARDS

Create durable architecture guards proving:

ProductWorkspace module family exists exactly once.
Contracts/Application/Domain/Infrastructure/Endpoints project references obey allowed direction.
ProductWorkspace.Application -> foreign Application = ZERO.
ProductWorkspace.Application -> foreign Infrastructure = ZERO.
ProductWorkspace.Infrastructure -> foreign Application/Infrastructure/Domain = ZERO.
ProductWorkspace.Endpoints -> ProductWorkspace.Infrastructure = ZERO.
ProductWorkspace.* -> Host = ZERO.
ProductWorkspace has no foreign DbContext reference.
ProductWorkspace does not map /v1/admin/products routes yet.
Host still owns exactly the W17 19 remaining ProductWorkspace routes.
Host/Admin count remains 52.
W1–W16-R1 accepted surfaces remain.
StoreAppearance unchanged/deferred.
frontend/schema unchanged.
W19 not started.

NO BEHAVIOR CHANGE PROOF

W18 must prove:

same 19 Host ProductWorkspace routes remain exactly where they were
no duplicate endpoint registration
ProductWorkspace module endpoints count = 0
Host ProductWorkspaceComposer/Models behavior unchanged
no persistence/schema changes
no API contract changes

EXPECTED PRODUCTION CHANGE

Allowed:

new ProductWorkspace module project files / composition entries
project/solution references
architecture guards
optional neutral DI registration
narrowly justified boundary abstractions with zero consumers yet

Forbidden:

changing existing route behavior
moving existing DTO authority
moving ProductWorkspaceComposer logic
changing Catalog business logic
changing foreign module business logic
creating persistence schema/migrations
frontend

TIMEBOX

Target 10–12 minutes.
Hard max ~15 minutes.

If safe module skeleton + guards cannot be completed within the limit:
return INCOMPLETE and STOP.
Do not silently broaden or start W19.

FOCUSED VALIDATION

Build:

ProductWorkspace.Contracts
ProductWorkspace.Domain
ProductWorkspace.Application
ProductWorkspace.Infrastructure
ProductWorkspace.Endpoints
Host only if Host project references changed
Host.Tests guards only

Do not run solution-wide build.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W18/

Required:

analyze.md
project-structure.md
dependency-matrix.md
catalog-read-boundary-map.md
party-boundary.md
authorization-scope-boundary.md
behavior-neutrality.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W18.task.md

RECOVERY SOT

Add hostAdminAmcW18.

Record:

task / parent / parentCommit
activeHostFolder=Admin
W17Decision=PRESERVED
productWorkspaceModuleState=SKELETON_ESTABLISHED
structureCertified=false
moduleProjects
endpointRouteCount=0
HostRemainingProductWorkspaceRoutes=19
HostAdminFileCount=52
dependencyBoundaryState
catalogReadBoundaryState
partyBoundaryTarget
authorizationBoundaryState
workspaceScopeBoundaryState
foreignDbContextState=ZERO_IN_NEW_MODULE
HostDependencyState=ZERO_IN_NEW_MODULE
StoreAppearance=DEFERRED
schema/frontend unchanged
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W18_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

W17 architecture decision preserved.
ProductWorkspace 5-project production skeleton exists.
no fake domain/business behavior invented.
no existing ProductWorkspace Host route moved.
ProductWorkspace endpoint route count=0.
Host remaining route count=19.
Host/Admin count=52.
no duplicate route registration.
project reference boundaries lawful.
ProductWorkspace -> Host ZERO.
ProductWorkspace Application -> foreign Application/Infrastructure ZERO.
ProductWorkspace Infrastructure -> foreign Application/Infrastructure/Domain ZERO.
ProductWorkspace Endpoints -> Infrastructure ZERO.
new module foreign DbContext ZERO.
exact path/namespace.
not falsely structure-certified.
solution grouping correct.
focused builds/guards PASS.
StoreAppearance untouched.
no schema/frontend changes.
task/evidence/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W19 not started.

GIT / USER WORK SAFETY

Never:

git reset
git clean
unsafe checkout --
unsafe restore
unsafe rebase
blind stash manipulation
broad git add .

Preserve user work.
On collision return RECOVERY_CONFLICT.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W18
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W17
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W17-Decision-Preservation-State:
ProductWorkspace-Module-State:
ProductWorkspace-Contracts-State:
ProductWorkspace-Domain-State:
ProductWorkspace-Application-State:
ProductWorkspace-Infrastructure-State:
ProductWorkspace-Endpoints-State:
ProductWorkspace-Structure-Certification-State:
ProductWorkspace-Endpoint-Route-Count:
Host-Remaining-ProductWorkspace-Route-Count:
Host-Admin-File-Count:
Solution-Grouping-State:
Path-Namespace-State:
Application-Foreign-Application-State:
Application-Foreign-Infrastructure-State:
Infrastructure-Foreign-Application-State:
Infrastructure-Foreign-Infrastructure-State:
Infrastructure-Foreign-Domain-State:
Endpoints-To-Infrastructure-State:
ProductWorkspace-To-Host-State:
Foreign-DbContext-State:
Catalog-Read-Boundary-State:
Party-Boundary-State:
Authorization-Boundary-State:
Workspace-Scope-Boundary-State:
Behavior-Neutrality-State:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
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

STOP RULE

After result:

STOP completely.
Do not start W19.
Do not move any ProductWorkspace route.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK