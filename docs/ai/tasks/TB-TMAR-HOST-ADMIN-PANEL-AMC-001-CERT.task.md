PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-CERT
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Panel AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_PANEL_CERTIFY
Title: Independently certify Host Admin Panel + Admin Development endpoint closure and Party sellers ownership
Estimated-Time-Minutes: 15
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

W4:
ACCEPTED FOR CERTIFICATION ENTRY

Architect independently verified:

dev-context moved to Host/Admin/Development
Panel dev-context residue = ZERO
GET /v1/admin/dev-context mapped once
admin.dev.unavailable = canonical 404 descriptor
hard-coded "Not Found" removed from dev-context path
actorLabel localized prose removed; AdminEmail machine identity used
Admin/Development exact file count = 2
Host/Admin recursive file count = 19
Panel exact file count = 3
dashboard W3 preserved
sellers GET/POST remain Party-owned
accepted implementation commit = 39ea2e66d188cab7719ac99dd624c22778d7fb18

IMPORTANT CERTIFICATION SCOPE

This certification is NOT a blanket certification of all Host/Admin.

IN SCOPE:

Host/Admin/Panel
Host/Admin/Development ONLY as required by W4 ownership
Party sellers GET/POST ownership introduced by W1/W2 and W2-R1 structure
exact route ownership interactions needed to certify those surfaces
Program mappings for these surfaces
directly affected Foundation error descriptor/resource ownership
exact Host/Admin structural membership only to prove no accidental file drift

OUT OF SCOPE AS RECOVERY UNITS:

Host/Admin/Access
Host/Admin/Access/Authorizers
Host/Admin/Grid

Do NOT certify those out-of-scope folders merely because recursive structural guards enumerate them.
Do NOT repair them.
Do NOT claim entire Host/Admin is fully certified.

Any blocker discovered in Access/Grid that directly invalidates the in-scope surface MUST be reported as:
EXTERNAL_CERTIFICATION_BLOCKER
with exact file/symbol/evidence.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-certify/SKILL.md

CERTIFY means independently re-verify.
Do NOT trust W1/W2/W3/W4 Result claims by themselves.

Do NOT migrate unless one tiny certification-only evidence/guard correction is required.
If production repair is materially required:
STOP INCOMPLETE and name the exact repair task needed.

CERTIFICATION TARGET

A. Host/Admin/Panel retained end state:

Panel exact files:

AdminPanelComposer.cs
AdminPanelEndpoints.cs
AdminPanelModels.cs

Only HTTP route owned by Panel:
GET /v1/admin/dashboard

Disposition:
KEEP_AS_THIN_HOST_CROSS_MODULE_PRESENTATION_COMPOSITION

Dashboard requirements:

IAdminPanelAccess
Result<AdminDashboardSummary>
ApiResponseFactory
no Results.Json
no local ToError
no local PlatformHttpException catch
no hard-coded runtime user-facing FA/EN
no MediatR/ISender/IRequestHandler
HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION explicitly accepted
Composer Contracts-only
no persistence
no business write/policy
no foreign App/Infra/Domain

B. Host/Admin/Development W4 end state:

Exact files:

AdminDevActorBootstrap.cs
AdminDevContextEndpoints.cs

Route:
GET /v1/admin/dev-context

Requirements:

exactly one route mapping
owner = Admin/Development
unavailable = admin.dev.unavailable
HTTP 404
ApiResponseFactory + SemanticError/Result
no hard-coded runtime "Not Found"
no localized actor-label prose
actorLabel = machine identity semantics
password not exposed in response
HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION explicitly accepted
no foreign App/Infra/Domain
no DbContext/join

C. Party sellers closure:

GET /v1/admin/sellers
POST /v1/admin/sellers/query

Requirements:

both Party.Endpoints-owned only
Host Panel seller route residue = ZERO
ISender
IRequest<Result<...>>
handlers
ApiResponseFactory
GET validator classification = NO_VALIDATOR_REQUIRED
POST validator classification = VALIDATOR_REQUIRED_ENVELOPE_ONLY
validator physically under Admin/Sellers/Validators
stable machine validation code
no ex.Message classification/presentation
grid.* error code semantics preserved
IAdminSellersGridPort remains Party.Contracts boundary
no Party -> Host
no foreign App/Infra/Domain
no cross-module DbContext/join

MANDATORY CERTIFICATION AUDITS

PHYSICAL TREE / EXACT MEMBERSHIP

Re-enumerate from disk:

Host/Admin/Panel
Host/Admin/Development
Party.Application/Admin/Sellers
Party.Endpoints/Admin/Sellers

Verify exact path↔namespace.

Verify no:

duplicate old files
alias
shim
TypeForwardedTo
stale physical copy
compatibility wrapper

Host/Admin recursive file count must remain exactly 19.

Do NOT weaken exact allowlists.

ROUTE OWNERSHIP MATRIX

Build exact matrix for:

GET /v1/admin/dashboard
GET /v1/admin/dev-context
GET /v1/admin/sellers
POST /v1/admin/sellers/query

For each prove:

exact owner
exact mapping count = 1
no Host duplicate
no module duplicate
method unchanged
CQRS / PRESENTATION

Party sellers:
must satisfy normal module CQRS standard:
Endpoint -> ISender -> IRequest/Handler -> Result -> ApiResponseFactory

Host retained dashboard/dev route:
must NOT invent fake Host CQRS.
Verify documented exceptions:

HOST_PRESENTATION_COMPOSITION_CQRS_EXCEPTION
HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION

These exceptions do NOT waive:

Result
ApiResponseFactory
stable codes
localization
boundary hygiene
VALIDATORS

Party sellers requests:

enumerate endpoint-reachable requests
ListAdminSellersQuery = NO_VALIDATOR_REQUIRED
QueryAdminSellersGridQuery = VALIDATOR_REQUIRED
QueryAdminSellersGridQueryValidator physically under Validators
validation code owner unique
no meaningless duplicate validators
ERROR CATALOG / LOCALIZATION

Verify:

admin.dev.unavailable descriptor exactly once
classification NotFound
HTTP 404
LocalizationKey exact
corresponding resource/localizer path resolves through canonical mechanism
no duplicate descriptor
no alternate local mapper
no hard-coded runtime title in dev-context path
no hard-coded runtime FA/EN in dashboard path
no ex.Message as contract

If SafeTitleFallback contains English in the central catalog, classify it according to existing canonical platform fallback policy; do NOT silently call inline endpoint hard-coding acceptable.

HARD-CODED USER-FACING TEXT

Scan all IN-SCOPE touched production paths for runtime Persian/English presentation prose.

Important distinction:

XML/comments are not runtime presentation
machine identifiers/codes are not presentation prose
development credential constants are sensitive but are not presentation text; credential security is NOT to be redesigned in this task
actorLabel must no longer be localized prose

Any runtime presentation prose in-scope blocks certification.

CONTRACTS-ONLY / MICROSERVICE READINESS

For Panel composer and Party sellers verify:

foreign Application = ZERO
foreign Infrastructure = ZERO
foreign Domain = ZERO
module -> Host = ZERO
foreign DbContext = ZERO
cross-module SQL/EF join = ZERO
no shared mutable entity transport

Dashboard sync aggregation through module Contracts is permitted as edge composition.
Record extraction posture:
MICROSERVICE_READY_EDGE_COMPOSITION

Party seller path must remain module-extractable through Contracts-only calls.

PERSISTENCE / TRANSACTIONS

In-scope Host code:

DbContext = ZERO
IQueryable = ZERO
SaveChanges = ZERO
transaction ownership = ZERO

Party sellers application/endpoints:
same, except module Infrastructure may own its own legal persistence implementation if encountered; cross-module persistence remains ZERO.

AUTHORIZATION

Dashboard:
IAdminPanelAccess at edge.

Party sellers:
IAdminPanelAccess at edge.

Dev-context:
Development-only availability semantics; no admin business authorization should be invented unless existing route contract required it.

Do NOT reopen Admin/Access.
Only certify seam consumption from in-scope surfaces.

OBSERVABILITY / SENSITIVE LOGGING

In-scope code must introduce ZERO:

custom ActivitySource
custom Meter
manual traceparent
custom correlation
sensitive log payload

Explicitly verify no password/token/cookie/auth header logging.

COHESION / FILE RESPONSIBILITY

Panel:

Composer = dashboard composition
Endpoints = dashboard HTTP
Models = dashboard response model

Development:

Bootstrap = dev actor bootstrap/snapshot
Endpoints = dev-context HTTP/response

Party:
capability-first shallow structure.

No god file / mixed unrelated responsibility.

GUARD QUALITY

Inspect existing guards critically.

IMPORTANT:
There is historical guard/comment wording that may say all Host/Admin is "CERTIFIED".
Do NOT rely on that wording.

For this task:

guard assertions may be reused where factually relevant
stale/misleading comments may be corrected as docs/test metadata only
do NOT broaden certification status to Access/Grid

Create/strengthen a focused durable guard for THIS certification scope if needed.

The guard must prove:

exact in-scope files
route uniqueness
ownership
result/API mapping
Party CQRS
validator placement
localization code uniqueness
no forbidden dependencies
hard-coded runtime presentation text zero in-scope
BEHAVIOR PARITY

Prove no public behavior regression across W1-W4:

Dashboard:
same route/method/DTO/count semantics

Dev-context:
same route/method/success fields
404 unavailable preserved
actorLabel semantic normalization explicitly documented as approved hygiene change

Sellers:
same route/method/request/response shapes
same grid behavior
same stable error codes/status

Schema:
NONE

Frontend:
UNCHANGED

CERTIFICATION VERDICT

PASS only if every in-scope material requirement is independently verified.

Allowed final certification label:

HOST_ADMIN_PANEL_AMC_CERTIFIED

with explicit sub-states:

PANEL_KEEP_CERTIFIED
ADMIN_DEVELOPMENT_DEV_CONTEXT_CERTIFIED
PARTY_SELLERS_OWNERSHIP_CERTIFIED

Do NOT use:
HOST_ADMIN_FULLY_CERTIFIED

Do NOT certify:
Access
Grid

FOCUSED VALIDATION ONLY

Build:

Host
Party.Application
Party.Endpoints
Party.Infrastructure only if required by directly affected certification fact

Run focused:

AdminPanelComposition
relevant HostAdmin Canon guards
focused new certification guard
Party sellers focused guards/tests
TmarDurableGuard only for Recovery truth

No solution-wide tests.

NO OPEN-ENDED TEST LOOP.

Certification is inspection-first.

If one evidence/guard assertion has one deterministic non-production issue:
one bounded correction + one rerun.

If production code needs material repair:
STOP INCOMPLETE.
Do not self-migrate.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-CERT/

Required:

certification-summary.md
physical-tree.md
route-matrix.md
cqrs-validator-matrix.md
boundary-microservice.md
localization-errors.md
hardcoded-runtime-text.md
authorization.md
observability-sensitive.md
behavior-parity.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-CERT.task.md

RECOVERY / SOT — MANDATORY

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-PANEL-AMC-001-CERT
lastAcceptedCommit MUST remain latest implementation commit unless certification itself changes production code (it should not)
latest implementation lineage = W4 / 39ea2e66d188cab7719ac99dd624c22778d7fb18
certificationState = HOST_ADMIN_PANEL_AMC_CERTIFIED
panelState = PANEL_KEEP_CERTIFIED
adminDevelopmentDevContextState = CERTIFIED
partySellersOwnershipState = CERTIFIED
accessState = NOT_CERTIFIED_BY_THIS_TASK
gridState = NOT_CERTIFIED_BY_THIS_TASK
currentHostCheckpoint = Admin/Panel
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_PANEL_AMC_001_CERT
staleCurrentPointerState = ZERO
nextHostFolderStarted = false

If certification produces only docs/tests/guards:
record certification/docs commit separately.
Do NOT overwrite lastAccepted implementation SHA with docs-only SHA.

GIT

Work from latest origin/main.

No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.

Commit/push main only on PASS.

SUCCESS CRITERIA

PASS only if:

Panel exact 3 files
Admin/Development exact 2 files
Host/Admin recursive exact 19 preserved
exact path↔namespace
dashboard unique owner Host Panel
dev-context unique owner Admin/Development
sellers GET/POST unique owner Party
Party CQRS canonical
validator matrix exact
dashboard/dev Host exceptions explicitly documented and bounded
Result/ApiResponseFactory used on all in-scope Host HTTP presentation
hard-coded runtime user-facing FA/EN in in-scope paths = ZERO
admin.dev.unavailable descriptor unique
localization path canonical
ex.Message classification/presentation = ZERO
foreign App/Infra/Domain = ZERO in forbidden boundaries
module -> Host = ZERO
cross-module persistence/join = ZERO
sensitive logging = ZERO
behavior parity verified
schema NONE
frontend UNCHANGED
focused validations PASS
no production repair required
certification label exactly HOST_ADMIN_PANEL_AMC_CERTIFIED
Access/Grid explicitly NOT certified by this task
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-CERT
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Certification-State:
Panel-Certification-State:
Admin-Development-DevContext-Certification-State:
Party-Sellers-Ownership-Certification-State:
Access-Certification-State:
Grid-Certification-State:
Panel-File-Count:
Admin-Development-File-Count:
Host-Admin-Recursive-File-Count:
Path-Namespace-State:
Route-Matrix-State:
Dashboard-Route-Owner-State:
Dev-Context-Route-Owner-State:
Get-Sellers-Route-Owner-State:
Post-Sellers-Query-Route-Owner-State:
Duplicate-Route-State:
Party-CQRS-State:
Validator-Matrix-State:
ApiResponseFactory-State:
Host-CQRS-Exception-State:
Admin-Dev-CQRS-Exception-State:
Hardcoded-Runtime-User-Facing-Text-State:
Admin-Dev-Unavailable-Descriptor-State:
Localization-State:
Exception-Message-Classification-State:
Contracts-Only-State:
Module-To-Host-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Cross-Module-Persistence-State:
Observability-State:
Sensitive-Logging-State:
Cohesion-State:
Microservice-Readiness-State:
Behavior-Parity-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Guard-State:
Production-Repair-Required-State:
Recovery-State:
Last-Accepted-Implementation-Commit-State:
Certification-Commit-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start Admin/Access.
Do not start Admin/Grid.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK