PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W15
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W14-R1
Parent-Commit: 7f35f60c430e782b75ac65e32d9471fbf90985a9
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W15 — evacuate Product Workspace History read route to Catalog with focused ProductHistory read capability

ARCHITECT ACCEPTANCE

W14-R1 is ARCHITECT-ACCEPTED.

Verified parent:
7f35f60c430e782b75ac65e32d9471fbf90985a9

Accepted W14-R1 state:

W14 SEO migration preserved.
CatalogCategorySlugNormalizer owns one canonical normalization core.
TryNormalizeSlug / TrySlugifyFromName added.
ProductSeoDirectory has zero expected InvalidOperationException catch/control flow.
no message classification.
Host/Admin remains 52.
StoreAppearance deferred.
W15 not started.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W16.

W15 BOUNDED SLICE

Primary Host files:

Admin/ProductWorkspaceEndpoints.cs
Admin/ProductWorkspaceComposer.cs
Admin/ProductWorkspaceModels.cs

W15 owns ONLY:

GET /v1/admin/products/{productId:guid}/history

Current query inputs:

route productId
optional query section
optional query skip
optional query take

Everything else in ProductWorkspace remains for later waves.

EXPECTED HOST STATE

ProductWorkspace files remain partial.

Expected Host/Admin recursive production *.cs:
52 -> 52

History Host route count:
1 -> 0

WHY THIS SLICE

This route is Catalog-only and does not depend on Offer/Pricing/Inventory/Party/Tax composition.
It is a safe bounded extraction before touching the cross-module Workspace aggregate/grid.

REQUIRED READ

Read:

AGENTS.md
.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W13/W14/W14-R1 evidence
current ProductWorkspaceEndpoints.cs
current ProductWorkspaceComposer.cs history members
current ProductWorkspaceModels history records
ICatalogDirectory.ListProductHistoryAsync
CatalogDirectory.ListProductHistoryAsync
ProductHistoryRules
ProductHistoryPage / ProductHistoryEntryDto contracts
history tests/callers
current Catalog error catalog/resources
CatalogWorkspaceScope

MANDATORY ANALYZE FIRST

Before edits create exact disposition map for:

Endpoint:

route registration
GetHistoryAsync
ReadPermissions / X-Tooba-Workspace-Scope effect
AdminPanelAccess.RequireAuthorizedAsync
ToError

Composer:

GetHistoryPageAsync
BuildHistoryShellListsAsync
ToHistoryItemView
GetAsync aggregate Activity/Audit history shell usage

Models:

ProductHistoryPageView
ProductHistoryItemView
ProductHistoryItem
ProductWorkspaceView.Activity
ProductWorkspaceView.Audit

Catalog:

ICatalogDirectory.ListProductHistoryAsync
CatalogDirectory.ListProductHistoryAsync
ProductHistoryPage
ProductHistoryEntryDto
ToHistoryDto
ProductHistoryRules.SectionLabelFa
ActorSystem fallback
ordering
section filter
skip/take normalization

No code move before disposition exists.

TARGET OWNERSHIP

Catalog owns:

Admin history page read HTTP.
history paging/filter read use-case.
stable product-missing error.
focused history read persistence seam.

Host owns:

ZERO HTTP responsibility for history route after W15.
aggregate GetAsync may still keep Activity/Audit shell composition for now.
ProductHistoryItem aggregate DTOs may remain if live in ProductWorkspaceView.

APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/
ProductHistory/
Queries/
GetProductHistoryQuery.cs
GetProductHistoryHandler.cs
Models/
Ports/
IProductHistoryReader.cs
Validators/

Equivalent cohesive shallow structure accepted.

Important:

this W15 is READ-side only.
do NOT centralize all history writes.
do NOT move QueueProductHistory from other capabilities.
do NOT create a cross-capability history write service.
no mixed Contracts bundle.
exact path↔namespace.

FOCUSED READ SEAM

Preferred:

IProductHistoryReader
ProductHistoryReader

It should own:

product existence check for read.
section filtering.
ordering.
skip/take normalization.
mapping to read model / existing history DTO as lawful.

Legacy ICatalogDirectory.ListProductHistoryAsync may remain for proven live callers such as Workspace aggregate/history shell/tests, but if retained:

delegate one-way to focused reader.
no duplicate query implementation.
document exact live callers.

Do not touch AppendProductHistoryAsync or per-capability history write behavior in W15.

CQRS / ENDPOINT

Flow:

HTTP
-> ICatalogAdminAuthorizer
-> ISender
-> GetProductHistoryQuery
-> IProductHistoryReader
-> Result<ProductHistoryPageView-or-equivalent>
-> ApiResponseFactory

No actor binding required for this read unless analysis proves otherwise.

Endpoint must not inject:

ProductWorkspaceComposer
ICatalogDirectory
IProductHistoryReader
CatalogDbContext

WORKSPACE SCOPE

Current Host:
X-Tooba-Workspace-Scope=view
=> CanView=true
=> history GET allowed.

Preserve:

GET history allowed under view scope.
no write-scope requirement.
canonical Admin authorization still required.

Do not add CatalogWorkspaceScope.AllowsCatalogEdit to this GET.

RESULT / ERROR

Current external behavior:

missing product -> workspace.product.missing 404
permission denied only if CanView false; current ReadPermissions always CanView=true for normal/view modes, Admin auth remains outer security.

W15:

use typed Result.
preserve workspace.product.missing.
no PlatformHttpException expected flow.
no InvalidOperationException expected flow.
no message parsing.

If legacy ICatalogDirectory wrapper must continue throwing for old callers, keep that compatibility OUTSIDE new HTTP/read seam and document it.

PAGING / FILTER PARITY

Current behavior must remain exactly:

skip = Math.Max(0, skip) where endpoint default is 0.
take = Math.Clamp(take <= 0 ? 50 : take, 1, 100) where endpoint default is 50.
section blank/null => no section filter.
section nonblank => Trim then exact section equality.
total count after section filter.
ordering:
OccurredAt descending
HistoryId descending
Skip/Take after ordering.
response includes normalized skip/take.

ENDPOINT INPUT DEFAULTS

Current Host endpoint:

skip null -> 0
take null -> 50

Preserve these exact defaults.

VALIDATION MATRIX

Classify GetProductHistoryQuery.

Expected:
NO_VALIDATOR_REQUIRED

Reason:

route Guid constrained.
section optional.
skip/take are normalized/clamped by use-case/read seam, not rejected.

Do not add business validation for section values unless current behavior already rejects them (it does not).

RESPONSE SHAPE PARITY

Preserve ProductHistoryPageView JSON:

Items
TotalCount
Skip
Take

Each item:

HistoryId
EventType
Section
SectionLabelFa
SummaryFa
BeforeSummary
AfterSummary
ActorDisplayName
OccurredAt

Preserve actor display fallback from Catalog history mapping:

blank actor display -> ProductHistoryRules.ActorSystemFa

HOST PARTIAL RETENTION

After W15:

ProductWorkspaceEndpoints.cs:

remove history route registration.
remove GetHistoryAsync method.
retain all other routes.

ProductWorkspaceComposer.cs:

remove public GetHistoryPageAsync if no remaining caller.
keep BuildHistoryShellListsAsync if still used by aggregate GetAsync.
keep ToHistoryItemView only if still used; otherwise remove.
do not keep dead code for old guards.

ProductWorkspaceModels.cs:

remove ProductHistoryPageView and ProductHistoryItemView if history route was sole consumer.
KEEP ProductHistoryItem if ProductWorkspaceView.Activity/Audit still uses it.
do not remove aggregate Activity/Audit fields in W15.

HOST INVENTORY

Expected:

Host/Admin count 52 -> 52
history Host route 1 -> 0
ProductWorkspace files retained partial

DURABLE GUARDS

Prove:

Host history route absent.
Catalog owns exact history route once.
route uses ICatalogAdminAuthorizer + ISender.
no workspace edit-scope requirement on GET.
endpoint no Composer/directory/DbContext.
focused ProductHistory read capability exists.
focused read seam exists.
Result + ApiResponseFactory canonical.
no PlatformHttpException on moved history surface.
no expected InvalidOperationException/message parsing on moved read seam.
workspace.product.missing preserved.
skip/take normalization preserved.
section trim/exact filter preserved.
ordering preserved.
ActorSystem fallback preserved.
aggregate Activity/Audit shell remains functional and untouched behaviorally.
Catalog->Host ZERO.
Endpoints->Infrastructure ZERO.
validator matrix exact.
path↔namespace exact.
Host/Admin count 52.
W1–W14-R1 preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W16/next Host folder not started.

TESTS

Focused tests:

product missing -> workspace.product.missing 404.
empty history.
section null/blank no filter.
section trimmed exact filter.
skip negative -> 0.
take null/default -> 50.
take <= 0 -> 50.
take >100 -> 100.
stable ordering OccurredAt DESC, HistoryId DESC.
total count before pagination.
ActorSystem fallback.
JSON shape parity.
view scope GET allowed.
route ownership exactly once.
aggregate Activity/Audit code still compiles and existing tests preserved.

Focused builds:

Catalog.Contracts
Catalog.Application
Catalog.Infrastructure
Catalog.Endpoints
Host
Host.Tests
W13–W15 guards + focused history tests

DB tests may skip only due unavailable Docker/Testcontainers and must be recorded.
Non-DB builds/guards must pass.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W15/

Required:

analyze.md
disposition-map.md
product-history-read-capability.md
cqrs.md
result-errors.md
paging-filter-parity.md
validation.md
behavior-parity.md
partial-host-retention.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W15.task.md

RECOVERY SOT

Add hostAdminAmcW15, preserving previous states.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin count before/after
ProductWorkspace retained partial
history Host route count before/after
endpoint ownership
CQRS
focused reader
validator matrix
Result/error state
paging/filter/order parity
aggregate history shell preservation
Catalog->Host
Endpoints->Infrastructure
StoreAppearance deferred
schema/frontend unchanged
remaining ProductWorkspace slices
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W15_CHECKPOINT

PASS CRITERIA

PASS only if:

Host history route 1 -> 0.
Catalog owns route exactly once.
canonical CQRS/Result/ApiResponseFactory.
Admin auth preserved.
view scope read behavior preserved.
focused ProductHistory read capability/seam.
no expected exception/message classification in moved surface.
exact paging/filter/order/actor fallback behavior preserved.
aggregate Activity/Audit remains intact.
no dead Host history route/composer/route-only models.
Host/Admin count remains 52.
W1–W14-R1 preserved.
StoreAppearance deferred.
no schema/frontend change.
focused builds/tests/guards pass.
task/evidence/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W16 not started.
next Host folder not started.

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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W15
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W14-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W14R1-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
ProductWorkspace-Endpoint-File-State:
ProductWorkspace-Composer-State:
ProductWorkspace-Models-State:
History-Host-Route-Count-Before:
History-Host-Route-Count-After:
History-Endpoint-Ownership-State:
History-Route-Count:
History-CQRS-State:
History-Application-Structure-State:
History-Read-Port-State:
History-Validator-Coverage-State:
History-Result-State:
History-Error-Localization-State:
History-Message-Classification-State:
History-PlatformHttpException-State:
History-InvalidOperationExpectedFlow-State:
Workspace-View-Scope-State:
Paging-Normalization-State:
Section-Filter-State:
Ordering-State:
Actor-Fallback-State:
Aggregate-Activity-Audit-State:
Admin-Authorization-State:
Endpoints-To-Infrastructure-State:
Catalog-To-Host-State:
Route-Parity-State:
Behavior-Parity-State:
Path-Namespace-State:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
Remaining-ProductWorkspace-Slices:
Remaining-Admin-Blockers:
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

STOP RULE

After result:

STOP completely.
Do not start W16.
Do not migrate publish-readiness or other Workspace slices.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK