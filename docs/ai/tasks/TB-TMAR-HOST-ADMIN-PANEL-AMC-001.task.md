PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001
Parent-Task: TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Panel AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_PANEL_ANALYZE
Title: Analyze Host/Admin/Panel ownership and produce a bounded microservice-ready migration plan
Estimated-Time-Minutes: 15
Hard-Timebox-Minutes: 20

BASELINE

Repository branch:
main

Repository HEAD observed by Architect:
cc2bdc1d4c134330fb814580cbfca026c0c0f808

Last accepted implementation:
913ce3ca6c7e89a04f6ed9ebb74d1bc8a8660179

Last accepted task:
TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1

Current accepted Host checkpoint:
Jobs

Jobs state:
HOST_ZERO / DEAD_INFRA_REMOVED

NEXT-FOLDER RULE

This task opens ONLY:

src/backend/Host/Tooba.Host/Admin/Panel/

Do NOT start:

Host/Admin/Access
Host/Admin/Development
Host/Admin/Grid
any other Host folder

Outside the active folder, follow ONLY direct references/call sites/dependencies required to understand ownership and produce a safe migration plan.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

This task is ANALYSIS-ONLY.

Do NOT execute migration.
Do NOT move/delete production code.
Do NOT change runtime behavior.
Do NOT certify.
Do NOT start the next Host folder.

The later workflow, after Architect approval, will be:

ANALYZE -> MIGRATE -> CERTIFY

using:

tooba-architecture-analyze
tooba-architecture-migrate
tooba-architecture-certify

ACTIVE FOLDER — CURRENT ARCHITECT INVENTORY

At task start, re-enumerate the folder from disk. Architect currently observes exactly:

src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelComposer.cs
src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelEndpoints.cs
src/backend/Host/Tooba.Host/Admin/Panel/AdminPanelModels.cs

Do not trust this remembered list blindly.
Re-enumerate at start and again before returning the Result.

KNOWN CURRENT SURFACE — DO NOT TREAT AS PREDECIDED DISPOSITION

Architect currently observes:

AdminPanelComposer.cs

cross-module dashboard composition
Catalog.Contracts
Offer.Contracts
Order.Contracts
Party.Contracts
BuildingBlocks.Grid
dashboard summary composition
seller list composition
seller grid delegation

AdminPanelEndpoints.cs

Host-owned routes:
GET /v1/admin/dashboard
GET /v1/admin/sellers
POST /v1/admin/sellers/query
GET /v1/admin/dev-context
AdminPanelAccess.RequireAuthorizedAsync(...)
raw Results.Json(...)
catch (PlatformHttpException ...)
local ToError(...)
direct Host Development dependency for dev-context
direct Host Grid helper dependency for seller grid

AdminPanelModels.cs

AdminDashboardSummary remains Host-owned today
historical comments show multiple prior Admin DTO evacuations

These observations are input to analysis only.
Do NOT assume KEEP or MOVE from the wording above.

PRIMARY DECISION

Determine the TRUE ownership and final disposition of every production responsibility in Host/Admin/Panel.

The analysis must answer whether each item is:

legitimate thin Host cross-module presentation composition;
business/application responsibility that belongs to an existing module;
endpoint ownership that must leave Host;
generic platform/admin security seam;
development-only seam that belongs elsewhere;
mixed responsibility requiring split;
or blocked by an architecture decision.

Do NOT invent a new Admin/BFF module merely to empty Host.

If cross-module aggregation legitimately has no natural business-module owner, assess whether a thin Host composition seam is architecturally acceptable under current TMAR locks.
Ownership exception MUST NOT become a quality exception.

MANDATORY ANALYSIS

COMPLETE ACTIVE-FOLDER READ

Read all production files in Host/Admin/Panel completely.

For every file inspect:

callers
registrations
routes
request/response models
dependencies
direct consumers
tests/guards
error behavior
authorization path
localization/user-facing strings
logging/telemetry if any
RESPONSIBILITY MAP

Classify every significant responsibility using the Analyze skill categories, including:

HTTP_ENDPOINT
PRESENTATION_COMPOSITION
HOST_COMPOSITION_ROOT
CROSS_MODULE_ORCHESTRATION
APPLICATION_USE_CASE
CONTRACT
AUTHORIZATION_ADAPTER
DEVELOPMENT_SEED / DEVELOPMENT_ONLY_RUNTIME
other exact applicable categories

Mark MUST_SPLIT where one file mixes ownership domains.

OWNERSHIP / MICROSERVICE READINESS

For every responsibility identify:

canonical owner now;
target owner if migration is required;
exact reason;
microservice blocker state.

Hard boundary:
cross-module communication must be Contracts-only.

Explicitly prove presence/absence of:

foreign .Application
foreign .Infrastructure
foreign .Domain
foreign DbContext / DbSet
cross-module EF/SQL join
shared mutable entities
Host business authority
endpoint-to-infrastructure reach-through

Do not report LEGAL_CONTRACTS_ONLY if any forbidden dependency exists.

ENDPOINT OWNERSHIP

Audit all four routes presently mapped by AdminPanelEndpoints.

For each route determine:

final owner;
whether Host ownership is legal;
whether an existing module Endpoint surface should own it;
whether route behavior can move without public-contract change;
whether any route is genuinely cross-module composition and therefore needs an explicitly justified Host seam.

No duplicate route ownership.

CQRS / MEDIATR READINESS

For every HTTP-reachable business behavior determine the required canonical shape:

Endpoint -> ISender -> IRequest/Handler -> Result<T> -> ApiResponseFactory

MediatR standard:
12.5

Flag every bypass.

Do NOT create implementation in this task.

API RESULT / ERROR AUDIT

Explicitly analyze:

Results.Json(...)
local ToError(...)
catch (PlatformHttpException ...)
status-code preservation
error-code preservation
current success DTO shape
canonical Result/SemanticError + ApiResponseFactory path
global IExceptionPresentationService path for unexpected exceptions

No ex.Message / exception.Message classification may survive a future migrated/certified surface.

Do not redesign the public response contract during architecture migration.

LOCALIZATION / HARD-CODED USER-FACING TEXT

This is a hard TMAR requirement.

Search the active surface and directly affected path for:

hard-coded Persian user-facing strings
hard-coded English user-facing strings
ad-hoc error titles/messages
duplicate localized text
machine codes without canonical catalog ownership

In particular classify the current development response title:
"Not Found"

Distinguish comments/XML documentation from runtime user-facing text.

Future certified production behavior must use the canonical error/localization mechanism:

stable machine error codes
canonical ErrorDescriptor owner
IErrorMessageLocalizer / resources
no hard-coded FA/EN user-facing API messages
ADMIN ACCESS / SECURITY SEAM

AdminPanelEndpoints currently uses:
AdminPanelAccess.RequireAuthorizedAsync(...)

Determine whether:

this remains a legitimate Host-level security adapter;
endpoints should consume a narrower typed security seam;
or any policy/business authority is misplaced.

Do NOT migrate Host/Admin/Access in this task.
It is a separate unopened Host recovery unit.

DEV-CONTEXT

Analyze GET /v1/admin/dev-context separately.

Determine:

whether it belongs in Admin/Panel at all;
whether it is a legitimate development-only Host/platform route;
whether it should move to an already-approved development location;
whether moving it would violate a locked Host/Development allowlist;
whether a new Host folder would be required.

No sink-folder regression.
No growth of a closed/locked Host allowlist without explicit Architect authorization.

ADMIN GRID HELPER DEPENDENCY

Analyze the direct use of:

Tooba.Host.Admin.Grid.AdminGridQueryEndpoint

Do NOT open Host/Admin/Grid as a recovery unit.
Inspect only the minimum direct dependency required to understand the Panel boundary.

Determine whether Panel migration can be planned without pushing debt into Admin/Grid.

COMPOSER / DASHBOARD AGGREGATION

For:

GetDashboardAsync
ListSellersAsync
QuerySellersGridAsync

determine whether the aggregation is:

legal presentation composition;
business policy;
application orchestration;
or mixed.

For each external port classify:

owning module
contract legality
sync coupling implication for later microservice extraction
whether contract lookup is acceptable
whether event/projection/read-model would be required for true service separation

Prefer the smallest microservice-ready boundary.
Do not introduce speculative infrastructure if Contracts-only synchronous composition is already valid.

MODEL OWNERSHIP

Classify:
AdminDashboardSummary

Do not keep it in Host merely because the current endpoint is in Host.
Do not push it into Contracts merely because it is a DTO.

Use semantic consumer ownership.

PATH / NAMESPACE / COHESION

Audit the active folder for:

exact path↔namespace
duplicate/stale types
root dumps
file cohesion
god-file/mixed responsibility
alias/shim
TypeForwardedTo
unnecessary compatibility layer
CLOSED-FOLDER INTEGRITY

For every proposed destination outside Host/Admin/Panel, classify:

OPEN_FOR_CURRENT_TASK
LOCKED_BY_ACCEPTED_DISPOSITION
NEW_LOCATION

Do not propose:

another closed Host folder as sink;
resurrection of a HOST_ZERO folder;
silent growth of an accepted exact allowlist.

If the only clean target would violate a locked destination, return NEEDS_ARCHITECT_DECISION for that responsibility.

BEHAVIOR PRESERVATION MAP

Record the exact behavior that a later migration MUST preserve:

route paths
HTTP methods
success DTO shapes
status codes
stable error codes
authorization semantics
dev-only availability semantics
seller grid behavior
dashboard counting semantics
cancellation propagation

No product/business redesign.

EXPECTED ANALYSIS OUTPUT

Produce one decisive migration plan optimized for fast execution, NOT multiple speculative alternatives.

Plan must be sliced so each future Cursor implementation task is approximately 15–20 minutes maximum.

Prefer the fewest safe waves.

For every proposed migration wave state:

exact source files/responsibilities
exact destination
contracts reused/created
endpoint/CQRS changes
error/localization changes
guard changes
focused validation only
expected Host/Admin/Panel file count after the wave
certification preconditions

If one bounded migration wave cannot safely fit the timebox, split it before recommending it.

NO TEST LOOP

Analysis is repository inspection, not test-driven navigation.

Do NOT run solution-wide tests.

Do NOT enter fix -> test -> fix loops.

Because production code must not change in Analyze mode:

builds are not mandatory unless one very small focused build is necessary to verify an architectural fact;
do not run broad tests merely to produce green evidence.

RECOVERY / SOT

Do NOT advance lastAcceptedImplementation.
Do NOT mark Admin/Panel migrated or certified.

Persist:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-PANEL-AMC-001.task.md

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001/analyze.md
docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001/ownership-map.md
docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001/route-map.md
docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001/migration-plan.md

The evidence must clearly state:
ANALYSIS_ONLY_NOT_MIGRATED_NOT_CERTIFIED

Do not rewrite unrelated Recovery history.

GIT

Work from latest origin/main.
Do not assume the Architect-observed HEAD is still current; fetch first.

No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.

This task may commit/push ONLY:

the exact task file
analysis evidence
minimal Recovery metadata needed to record analysis-in-progress/completed WITHOUT advancing accepted implementation

Do not commit production code changes.

If unrelated tracked user work prevents a safe docs-only commit:
STOP and report it; do not overwrite it.

SUCCESS CRITERIA

PASS only if:

active folder was enumerated at start and end
every production file/responsibility in Host/Admin/Panel has an explicit disposition
all four routes have explicit final ownership analysis
cross-module boundary inventory is complete
no cross-module DB/join issue is left unclassified
CQRS/MediatR readiness is explicit
raw Results.Json / PlatformHttpException / local ToError state is explicit
hard-coded runtime FA/EN user-facing text audit is explicit
Admin Access seam is classified without reopening Admin/Access
dev-context is classified without sink-folder regression
Admin/Grid dependency is classified without reopening Admin/Grid
AdminDashboardSummary semantic ownership is decided
exact behavior-preservation map exists
one decisive, minimal migration plan exists
every future implementation wave is <=20 minutes estimated
no production code changed
no schema change
no frontend change
no next Host folder started
state remains NOT_MIGRATED / NOT_CERTIFIED
workflow stops for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001
Parent-Task: TB-TMAR-HOST-JOBS-DEAD-REGISTRY-CLEANUP-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Mode-State:
Active-Host-Folder:
Initial-Production-File-Count:
Final-Production-File-Count:
Folder-Enumeration-State:
Ownership-State:
File-Disposition-State:
Route-Count:
Route-Ownership-State:
Dashboard-Ownership-State:
Seller-List-Ownership-State:
Seller-Grid-Ownership-State:
Dev-Context-Ownership-State:
Admin-Access-Seam-State:
Admin-Grid-Dependency-State:
Cross-Module-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Join-State:
CQRS-MediatR-Readiness-State:
Api-Result-Mapping-State:
PlatformHttpException-State:
Hardcoded-User-Facing-Text-State:
Localization-Catalog-State:
AdminDashboardSummary-Ownership-State:
Path-Namespace-State:
Cohesion-State:
Sink-Folder-Regression-Risk-State:
Closed-Folder-Integrity-State:
Behavior-Preservation-Map-State:
Migration-Wave-Count:
Migration-Wave-Timebox-State:
Recommended-Next-Task:
Production-Code-Change-State:
Schema-Change-State:
Frontend-State:
Recovery-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not migrate.
Do not certify.
Do not start Host/Admin/Access.
Do not start Host/Admin/Development.
Do not start Host/Admin/Grid.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK