PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W13
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W12
Parent-Commit: ac5a141a8cb6f27b8761038e7b68b23f145431a4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W13 — evacuate Product Workspace Media Admin slice to Catalog with focused ProductMedia capability

ARCHITECT ACCEPTANCE

W12 is ARCHITECT-ACCEPTED.

Verified parent:
ac5a141a8cb6f27b8761038e7b68b23f145431a4

Accepted W12 state:

final CatalogAttribute category-change routes moved to Catalog.
CatalogAttributeEndpoints.cs deleted.
Host Program MapCatalogAttributeEndpoints() removed.
Host/Admin count 53 -> 52.
CategoryChanges CQRS/Result/focused directory present.
canonical assignment-level code preserved.
transaction/history/safety-unpublish/variant safety preserved.
StoreAppearance still deferred.
W13 not started.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W14.

WHY THIS W13

The next large Catalog-owned Host surface is Product Workspace.
Do NOT big-bang ProductWorkspaceEndpoints.cs / ProductWorkspaceComposer.cs.

W13 is a member-level AMC slice limited to the coherent Product Media capability.

PRIMARY HOST FILES

Admin/ProductWorkspaceEndpoints.cs
Admin/ProductWorkspaceComposer.cs
Admin/ProductWorkspaceModels.cs

These files remain after W13 for other Workspace slices.

EXACT W13 ROUTES

Base:
/v1/admin/products/{productId:guid}

GET /{productId:guid}/media
GET /{productId:guid}/media/readiness
POST /{productId:guid}/media
POST /{productId:guid}/media/placeholder
PUT /{productId:guid}/media/order
PUT /{productId:guid}/media/{assetId:guid}/primary
PATCH /{productId:guid}/media/{assetId:guid}
DELETE /{productId:guid}/media/{assetId:guid}

Only these eight routes belong to W13.

RETAIN FOR LATER WAVES

Do not migrate:

workspace list/get/grid/history
create/core/title/quantity policy
category assignments
brand
lifecycle/publish/readiness/delete
SEO
workspace variant create/patch
StoreAppearance
Seller panel
other Host Admin capabilities

EXPECTED HOST FILE COUNT

ProductWorkspace files remain.

Expected Host/Admin recursive production *.cs:
52 -> 52

Member metric:

ProductWorkspace media Host route count before = 8
after = 0

REQUIRED READ

Read current:

AGENTS.md
.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W1–W12 evidence
complete ProductWorkspaceEndpoints.cs
relevant sections of ProductWorkspaceComposer.cs
media-related models in ProductWorkspaceModels.cs
current Catalog media methods in ICatalogDirectory / CatalogDirectory
Catalog Domain media rules/entities
existing Catalog Application/Product media/readiness code if any
current Catalog error catalog/resources
all media tests/callers

MANDATORY ANALYZE FIRST

Create exact disposition map before code changes.

Classify:

endpoint route registrations for all 8 media routes
ListMediaAsync
GetMediaReadinessAsync
AttachMediaAsync
AttachPlaceholderMediaAsync
ReorderMediaAsync
SetPrimaryMediaAsync
PatchMediaAsync
DetachMediaAsync
Composer:
ListMediaAsync
GetMediaReadinessAsync
AttachMediaAsync
AttachPlaceholderMediaAsync
ReorderMediaAsync
SetPrimaryMediaAsync
PatchMediaAltAsync
DetachMediaAsync
MapMediaViews
EnsureProductExistsAsync usage
EnsureCatalogEdit usage
models:
AdminProductMediaAttachRequest
AdminProductMediaPlaceholderRequest
AdminProductMediaOrderRequest
AdminProductMediaPatchRequest
ProductMediaView
ProductMediaReadinessView
CatalogDirectory methods:
GetProductMediaEditorStateAsync
GetProductMediaReadinessAsync
AttachMediaReferenceAsync
AttachGeneratedPlaceholderMediaAsync
ReorderProductMediaAsync
SetProductPrimaryMediaAsync
PatchProductMediaAltAsync
DetachProductMediaAsync
exact ProductHistory/media side-effects, if any
primary-image uniqueness behavior
media order exact-set behavior
media detach semantics
placeholder semantics
product existence semantics
permissions/header semantics
actor binding need
Program/module registration implications

No code move before disposition exists.

TARGET OWNERSHIP

Catalog owns:

Admin product media editor/readiness.
attach existing media reference.
attach generated placeholder.
reorder.
set primary.
patch alt text.
detach reference.
all eight HTTP routes.
focused media persistence/use-case seam.
stable media errors.

Host owns:

ZERO responsibility for these eight routes after W13.
ProductWorkspace remains only for later non-media slices.

APPLICATION STRUCTURE

Preferred:

Tooba.Catalog.Application/ProductMedia/

Commands/
AttachProductMediaCommand
AttachPlaceholderProductMediaCommand
ReorderProductMediaCommand
SetPrimaryProductMediaCommand
PatchProductMediaCommand
DetachProductMediaCommand
Queries/
GetProductMediaQuery
GetProductMediaReadinessQuery
Models/
Ports/IProductMediaDirectory.cs
Validators/

An equally shallow cohesive name such as Products/Media is acceptable if current Catalog conventions make it cleaner.

Mandatory:

no generic/mixed *Contracts.cs bundle.
no one-folder-per-single-request nesting.
exact path <-> namespace.
unique authoritative request types.
no Host models referenced by module code.

FOCUSED PERSISTENCE SEAM

Preferred:

IProductMediaDirectory
ProductMediaDirectory

Extract only media-specific persistence and mapping.

Do not leave new MediatR handlers calling broad ICatalogDirectory if a focused seam is feasible.

Legacy ICatalogDirectory media wrappers may remain only where proven live callers/tests still need them.
If retained:

one-way thin wrappers into focused media directory.
document exact callers.
no duplicate business authority.

ENDPOINT FLOW

All eight:
HTTP
-> ICatalogAdminAuthorizer
-> CatalogActorRequestBinding only if media history/current actor semantics actually require it
-> ISender
-> CQRS
-> focused media port
-> Result
-> ApiResponseFactory

Do not add actor binding mechanically if media operations do not use actor context.
Analyze current semantics first.

Endpoints must NOT inject:

ProductWorkspaceComposer
ICatalogDirectory
IProductMediaDirectory
CatalogDbContext

WORKSPACE PERMISSION HEADER

Current Host uses:
ReadPermissions(request)
based on X-Tooba-Workspace-Scope.

For media:

reads currently do not call EnsureCatalogEdit.
writes currently call EnsureCatalogEdit(permissions).

Audit actual behavior of X-Tooba-Workspace-Scope=view.

Preserve effective authorization semantics.

Do NOT copy ProductWorkspacePermissions Host model into Catalog blindly.

Preferred:

Admin auth remains canonical via ICatalogAdminAuthorizer.
if the Workspace scope header is a real live transport policy, create the smallest module-owned endpoint policy/binding for this capability.
if analysis proves it is development/legacy-only or redundant after canonical authorization, do not silently remove it; document evidence before removal.
no Catalog dependency on Host.

RESULT / ERROR SEMANTICS

Current Host/Composer uses PlatformHttpException and expected InvalidOperationException mappings.

W13 must move expected media failures to typed Result.

Use:

Result / Result<T>
CatalogErrorCodes
CatalogErrorCatalogContributor
CatalogErrors.resx
CatalogErrors.fa.resx
ApiResponseFactory

At minimum classify:

product missing
permission denied / workspace media write forbidden if scope remains meaningful
asset id missing
attach rejected / duplicate if distinguishable
placeholder rejected
no media / media collection empty
reorder invalid/mismatched exact set
media reference missing
primary target missing
patch target missing
detach target missing

Preserve existing stable workspace media codes where externally meaningful:

workspace.product.missing
workspace.permission.denied
workspace.media.asset.missing
workspace.media.attach.rejected
workspace.media.placeholder.rejected
workspace.media.empty
workspace.media.order.invalid
workspace.media.order.rejected
workspace.media.missing

If normalizing under catalog.*, do so only with evidence and explicit compatibility plan.
Default preference for W13: preserve these existing externally visible codes to minimize contract drift.

No:

PlatformHttpException expected flow on moved surface.
InvalidOperationException catch-to-code.
ex.Message.Contains(...).
localized message classification.

MEDIA BUSINESS SEMANTICS

Preserve:

media list ordering: primary first, then DisplayOrder.
MediaAssetId, IsPrimary, DisplayOrder, AltText shape.
readiness shape:
HasPrimaryImage
MediaCount
IsReady
MessageFa
attach existing media asset semantics.
placeholder generation semantics.
POST attach returns HTTP 201.
POST placeholder returns HTTP 201.
reorder requires exact existing media set.
set-primary uniqueness behavior.
alt-text patch semantics.
detach means unassign reference only; shared media asset is not deleted.
product existence behavior.
tenant/mutation guard.
no schema change.

PRIMARY INVARIANT

Audit and preserve current invariant:

when media exists, exactly one primary where current Catalog behavior enforces that.
reorder must not accidentally change primary selection unless existing behavior does.
detach current primary must preserve/repair primary behavior exactly as current Catalog implementation does.

VALIDATION MATRIX

Classify all 8 endpoint-reachable requests.

Expected baseline to verify:

GetProductMediaQuery: NO_VALIDATOR_REQUIRED
GetProductMediaReadinessQuery: NO_VALIDATOR_REQUIRED
AttachProductMediaCommand: VALIDATOR_REQUIRED (MediaAssetId != Guid.Empty)
AttachPlaceholderProductMediaCommand: likely NO_VALIDATOR_REQUIRED if body optional and AltText unrestricted
ReorderProductMediaCommand: VALIDATOR_REQUIRED (collection non-null; Guid.Empty transport shape if applicable)
SetPrimaryProductMediaCommand: NO_VALIDATOR_REQUIRED (route ids only)
PatchProductMediaCommand: likely NO_VALIDATOR_REQUIRED unless transport contradiction exists
DetachProductMediaCommand: NO_VALIDATOR_REQUIRED

Do not duplicate persisted exact-set, product existence, or media membership rules in FluentValidation.

BEHAVIOR PARITY

Preserve exact routes/methods/statuses:

GET media → 200 JSON list
GET readiness → 200 JSON
POST media → 201 JSON list
POST placeholder → 201 JSON list
PUT order → 200 JSON list
PUT primary → 200 JSON list
PATCH media → 200 JSON list
DELETE media → 200 JSON list

Preserve body field names/shapes:

MediaAssetId
AltText
OrderedMediaAssetIds

HOST PARTIAL RETENTION

After W13:
ProductWorkspaceEndpoints.cs remains.

It MUST NOT contain:

eight media route mappings
eight migrated media endpoint methods
media-only Host transport records if no other live consumer

ProductWorkspaceComposer.cs remains, but it MUST NOT retain public media methods solely for migrated routes if zero other callers.

ProductWorkspaceModels.cs:

remove media-only request records when no remaining Host consumer.
ProductMediaView may remain if ProductWorkspaceView.Media still uses it.
ProductMediaReadinessView may remain if another remaining workspace model/method still uses it; otherwise remove.
do not force deletion when genuinely used by remaining Workspace composition.

Do not keep dead code for old guards.

HOST INVENTORY

Expected:

Host/Admin *.cs: 52 -> 52
Workspace Media route count: 8 -> 0
ProductWorkspace files remain partial.

DURABLE GUARDS

Prove:

eight media route mappings absent from Host ProductWorkspaceEndpoints.
all eight routes Catalog.Endpoints-owned exactly once.
all eight use ICatalogAdminAuthorizer + ISender.
endpoint direct Composer/directory/DbContext injection = ZERO.
focused ProductMedia Application capability exists.
focused media port/directory exists or equally narrow lawful design evidenced.
Result + ApiResponseFactory canonical.
no PlatformHttpException on migrated media surface.
no expected InvalidOperationException/message parsing.
workspace-scope permission behavior preserved or evidence-backed replacement.
media response shapes/status parity preserved.
primary invariant preserved.
reorder exact-set semantics preserved.
detach remains unassign-only.
Catalog -> Host = ZERO.
Endpoints -> Infrastructure = ZERO.
validator matrix exhaustive.
exact path <-> namespace.
Host/Admin count remains 52.
W1–W12 preserved.
StoreAppearance deferred.
schema/frontend unchanged.
W14/next Host folder not started.

TESTS

Focused behavior tests:

list media ordering.
product missing.
readiness empty/no-primary/ready.
attach existing valid.
attach empty asset id validation.
attach duplicate/rejected behavior.
placeholder create.
reorder exact set success.
reorder mismatch failure.
set primary.
primary uniqueness.
patch alt.
detach non-primary.
detach primary behavior.
missing media target.
X-Tooba-Workspace-Scope=view write behavior parity.
route ownership/auth.
canonical status/errorCode.

Focused validation:

Catalog.Contracts
Catalog.Domain if touched
Catalog.Application
Catalog.Infrastructure
Catalog.Endpoints
Host
Host.Tests
media focused tests
W7–W13 architecture guards

DB tests may be skipped only for unavailable Docker/Testcontainers and must be recorded distinctly; non-DB guards/builds must pass.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W13/

Required:

analyze.md
disposition-map.md
product-media-capability.md
cqrs.md
workspace-scope-policy.md
result-errors.md
validation.md
behavior-parity.md
primary-invariant.md
partial-host-retention.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W13.task.md

RECOVERY SOT

Add hostAdminAmcW13 preserving W1–W12.

Record:

task/parent/parentCommit
activeHostFolder=Admin
Host Admin count before/after
ProductWorkspace endpoint file state=RETAINED_PARTIAL
media Host route count before/after
endpoint ownership
CQRS
focused media port/directory
validator matrix
Result/error state
workspace-scope policy state
primary invariant
route/behavior parity
Catalog->Host
Endpoints->Infrastructure
StoreAppearance deferred
schema/frontend unchanged
remaining ProductWorkspace slices
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W13_CHECKPOINT

PASS CRITERIA

PASS only if:

all eight media routes Host 8 -> 0.
all eight Catalog-owned exactly once.
MediatR/Result/ApiResponseFactory canonical.
Admin authorization preserved.
workspace view-scope write restriction preserved or evidence-backed lawful equivalent.
focused ProductMedia capability and persistence seam exist.
expected errors typed; no message parsing.
response/status/body parity preserved.
primary/reorder/detach semantics preserved.
no dead Host media route code retained.
ProductWorkspace non-media surfaces unchanged.
Host/Admin count 52.
W1–W12 preserved.
StoreAppearance deferred.
no schema/frontend change.
focused builds/tests/guards PASS.
evidence/task/SoT persisted.
commit pushed.
HEAD == origin/main.
working tree clean.
W14 not started.
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
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W13
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W12
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W12-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
ProductWorkspace-Endpoint-File-State:
ProductWorkspace-Composer-State:
ProductWorkspace-Models-State:
Media-Host-Route-Count-Before:
Media-Host-Route-Count-After:
Media-Endpoint-Ownership-State:
Media-Route-Count:
Media-CQRS-State:
Media-Application-Structure-State:
Media-Persistence-Port-State:
Media-Validator-Coverage-State:
Media-Result-State:
Media-Error-Localization-State:
Media-Message-Classification-State:
Media-PlatformHttpException-State:
Media-InvalidOperationExpectedFlow-State:
Workspace-Scope-Policy-State:
Primary-Invariant-State:
Reorder-Semantics-State:
Detach-Semantics-State:
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
Do not start W14.
Do not migrate SEO/core/grid/lifecycle/variant-workspace slices.
Do not start another Host folder.
Wait for Architect review.

END_TOOBA_TASK