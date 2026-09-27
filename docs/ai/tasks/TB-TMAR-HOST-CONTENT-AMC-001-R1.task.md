PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R1
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001
Parent-Commit: 86a3d7ced96316e0c79c67c52902f6d668d99a00
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_REPAIR
Title: Complete Content destination-module recovery after Host evacuation

REPAIR REASON

The parent task successfully evacuated Host/Content to ZERO, but certification was incomplete because misplaced Host responsibility was rehomed into a non-canonical Content destination.

The following are BLOCKERS and must be repaired before Architect acceptance:

Tooba.Content.Endpoints directly references Tooba.Content.Infrastructure.
ContentPanelComposer directly consumes ContentDbContext and constructs AdminContentGridQueryEngine.
Content HTTP handlers use raw Results.Json(...), ad-hoc status mapping, and PlatformHttpException instead of the canonical Result<T> -> ApiResponseFactory path.
Expected failures are classified through ex.Message.Contains(...).
Content has no complete MediatR 12.5 CQRS ownership for endpoint-reachable use cases.
Endpoint-reachable transport requests have no exhaustive FluentValidation classification.
Content does not have capability-oriented Application/Endpoints/Infrastructure foldering at COMPLETE_REFERENCE_PATTERN quality.
Content error codes/catalog/resources/localization are not on the canonical stable-code pipeline.
Content lacks complete canonical logging / OpenTelemetry / correlation verification for the touched surface.
Content.Infrastructure -> Media.Application is an illegal cross-module dependency and must become Contracts-only.
ContentAdminAccess contains direct authorization decision mechanics and user-facing Persian transport messages instead of a canonical endpoint authorization seam + platform/error presentation.
Parent SoT explicitly records these debts; R1 must close them, not defer them.

REQUIRED SKILLS

Read and follow in order:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Also use the repository standards:

AGENTS.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-architecture-locks.md
docs/architecture/tmar-current-state.json
docs/architecture/tmar-module-structure-manifests.json
canonical COMPLETE_REFERENCE_PATTERN modules such as Offer/Payment/Settlement only as read-only references.

SCOPE

Primary repair surface:

src/backend/Modules/Content/
only the minimum Host composition/security adapter changes required by Content
only the minimum Media.Contracts boundary required to remove Content.Infrastructure -> Media.Application
focused Content/Host tests, guards, evidence, SoT, solution/project metadata

Do NOT:

reopen unrelated Host folders;
touch frontend;
redesign Content business behavior;
change DB schema/migrations;
start independent Media recovery;
create generic shared god layers;
preserve bad coupling through aliases/shims;
broaden into full repository cleanup.

ARCHITECTURE TARGET

Content must leave this repair in COMPLETE_REFERENCE_PATTERN-ready form for the touched HTTP surface.

Required project/layer direction:

Content.Endpoints
→ Content.Application
→ Content.Contracts / Content.Domain
→ Content.Infrastructure only through DI implementation registration, NOT direct endpoint consumption.

No Content.Endpoints -> Content.Infrastructure project reference.

Cross-module:
Content.* -> Media.Contracts only.
No Content reference to Media.Application, Media.Infrastructure, or Media.Domain.

HTTP:
Endpoint -> ISender -> IRequest<Result<T>> / IRequest<Result> -> handler -> application ports -> infrastructure implementation
then:
ApiResponseFactory.From(result) / canonical Created/NoContent equivalent.

AUTHORIZATION:

Endpoint authorization seam must be thin and module-owned in Content.Endpoints.
It may consume neutral BuildingBlocks security/authorization abstractions.
Do not embed localized Persian transport responses in authorization code.
Expected deny/unavailable states must use stable machine codes and canonical error presentation.
No message parsing.

CQRS / MEDIATR

Inventory EVERY endpoint-reachable Content behavior across:

Storefront article/category/author reads
Admin article list/grid/detail/readiness/preview/history
create/update/publish/unpublish/archive/delete
categories
authors
tags
article media
article comments

Each endpoint-reachable behavior must have:

concrete Command or Query
IRequest<Result<T>> or IRequest<Result>
concrete IRequestHandler<,>
endpoint dispatch through ISender
no endpoint direct Directory/DbContext/Infrastructure call
no composer-owned business workflow

If a thin response assembler is genuinely required, keep it Application-owned or endpoint-local and persistence-free; do not preserve the current composer as a bypass around CQRS.

FOLDERING / NAMESPACES

Organize by capability + use case, not root dumps.

Expected pattern examples (adapt names to actual Content semantics):

Application/Commands/<UseCase>/
Application/Queries/<UseCase>/
Application/Validators/Admin/...
Application/Validators/Storefront/...
Application/Ports/...
Contracts/Errors/...
Contracts/Dtos/...
Endpoints/Admin/...
Endpoints/Storefront/...
Endpoints/Errors/...
Endpoints/Resources/...
Infrastructure/Directories/...
Infrastructure/Grid/...
Infrastructure/Adapters/...

Exact path ↔ namespace alignment required.

Do not blindly clone Offer folders; choose folders by Content responsibility.

VALIDATION

Build an exhaustive endpoint-reachable request matrix.

Every request MUST be exactly one of:

VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED

For validator-required:

concrete FluentValidation validator
stable validation code
discovered through canonical AddToobaCqrsFoundation
no manual endpoint validator calls

For no-validator:

explicit durable reason.

Add a guard so a new endpoint-reachable request cannot silently bypass classification.

RESULT / ERROR / LOCALIZATION

Remove:

raw ad-hoc Results.Json success/error mapping where canonical Result applies
PlatformHttpException as Content business/expected-failure transport
ex.Message.Contains(...)
localized Persian error text embedded in endpoint/application flow
anonymous ad-hoc error objects for expected failures

Use:

Result / Result<T>
SemanticError
stable machine codes in Content.Contracts.Errors.ContentErrorCodes (or cohesive subcatalogs)
exactly one canonical descriptor owner per code
Content IErrorCatalogContributor
Content IErrorResourceSet
.resx + .fa.resx
IErrorMessageLocalizer
ApiResponseFactory

Preserve existing HTTP semantics/statuses/error-code meaning where currently observable, but map them through canonical mechanisms.

MESSAGE CLASSIFICATION

ZERO expected-failure classification by exception message.

Search for and remove from touched Content surface:

.Message.Contains(
.Message.StartsWith(
equality/regex/message heuristics for business outcomes
broad InvalidOperationException catches used to infer Content outcome

Unknown/unexpected exceptions propagate to the global exception boundary.

MEDIA BOUNDARY

Current illegal dependency:
Content.Infrastructure -> Media.Application.IMediaDirectory

Repair by reusing or introducing the smallest lawful Media.Contracts port/DTO.

Rules:

natural owner of Media capability remains Media
Content consumes Media.Contracts only
implementation stays Media-owned
Host/module composition may bind the port
no Media DbContext/domain leakage
no new shared generic media abstraction if a narrow Media contract is enough

Do not independently restructure Media beyond the minimum Contracts boundary.

GRID

The Content grid engines/policies may remain Content-owned, but endpoint access must go through Application CQRS.

Required:

DB-native paging preserved
grid normalization ownership remains Content
no Endpoints -> Infrastructure dependency
query handler invokes an Application port implemented by Content.Infrastructure OR another canonical module-local pattern
no in-memory materialization regression
stable validation errors, no PlatformHttpException message presentation from Infrastructure

CONTENT ADMIN ACCESS

Repair the current direct pattern:
HttpRequest + IAdminPanelAccess + IAuthorizationService + ICurrentTenant -> PlatformHttpException + Persian text

Required:

narrow Content endpoint authorization interface/seam by capability/audience
authorization decision remains transport/security boundary, not business domain
stable deny/unavailable codes
no localized message ownership in code
canonical error catalog/resources
no direct authorization boilerplate copied across every endpoint if the repository already has an accepted pattern
preserve permission IDs: view/create/edit/publish

LOGGING

Audit every touched Content handler/integration boundary.

Required:

canonical ILogger<T>
structured templates
no interpolation for sensitive values
no secrets/PII/body dumping
technical logs only where operationally meaningful
do not invent logging noise
use canonical ObservabilityLogScope if scope enrichment is applicable
no parallel logger abstraction

OPENTELEMETRY / TRACING / CORRELATION

Verify and preserve:

ToobaTelemetry
CQRS TracingBehavior<,> via AddToobaCqrsFoundation
canonical correlation middleware/provider
X-Correlation-Id
module call tracing for cross-module Media contract calls where repository pattern requires it
no Content-local ActivitySource/Meter parallel system
no manual correlation ID invention

If no Content-specific logging/tracing code is necessary because canonical pipeline covers it, evidence must explicitly say so.

CONTENT PROJECT FOUNDATION

If Tooba.Content.Contracts does not exist, create it.

Content module final projects should be structurally coherent:

Domain
Contracts
Application
Infrastructure
Endpoints

Update Tooba.slnx grouping exactly under /Modules/Content/.

Do not change assembly names for visual grouping.

DEPENDENCY RULES

At end:

Endpoints -> Application + Contracts + BuildingBlocks presentation/security as needed
Application -> Domain + Contracts + neutral BuildingBlocks
Infrastructure -> Application + Domain + Contracts + external module Contracts only
no foreign Application/Infrastructure/Domain references
no Host reference from Content module projects
no cross-module DbContext/DbSet/raw SQL/join
no Content Endpoints reference to Infrastructure

SEED COMPOSITION

Host/Composition/ContentDevelopmentSeedHost.cs may remain only if it is genuinely Host development/composition orchestration.

Certify:

no Content business rules embedded
no duplicated seed implementation
module-owned seed logic invoked
Development-only behavior preserved
no expansion of Host/Development allowlist

If it directly implements module-specific seed behavior, move that behavior into Content.Infrastructure/Development and keep only thin Host invocation.

SOURCE SIZE / COHESION

Apply current source-size/cohesion guards.
No god files.
No cosmetic file splitting.
No root dumping.
Split large endpoint/composer code by actual use-case/capability.

DURABLE GUARDS

Add/strengthen guards that prove at minimum:

Host/Content remains ZERO.
Content.Endpoints -> Content.Infrastructure project reference = ZERO.
Content project foreign Application/Infrastructure/Domain refs = ZERO.
Content.Infrastructure -> Media.Application = ZERO.
Content→Media boundary = Media.Contracts only.
every Content endpoint-reachable request is classified for validation.
every Content endpoint uses ISender for business use cases.
no endpoint direct DbContext/Directory/Grid-engine construction.
no Results.Json ad-hoc business error objects where canonical factory applies.
no ex.Message classification.
stable Content error codes resolve to exactly one descriptor.
Content resources exist for required localized errors.
path↔namespace exactness.
root allowlists / forbidden folders.
no alias/shim workaround.
Host seed composition remains thin and Development-only.
no schema/migration changes.

FOCUSED VALIDATION

Build only affected:

Content.Contracts
Content.Domain if changed
Content.Application
Content.Infrastructure
Content.Endpoints
Media.Contracts if changed/created
Host
focused test project(s)

Run focused:

Content behavior tests touched by migration
endpoint/CQRS guard
validator coverage guard
structure/path-namespace guard
error catalog/localization uniqueness guard
cross-module dependency guard
Host/Content ZERO guard
grid paging regression
seed composition regression
Result/ApiResponseFactory guard

Tests are evidence, not navigation.
One deterministic local repair per focused failure, then rerun affected validation only.
No open-ended loop.
Do not weaken guards.

SCHEMA / BEHAVIOR / FRONTEND

schema: NONE
migrations: NONE
frontend: UNCHANGED
routes: preserve
success JSON shape: preserve
expected HTTP status semantics: preserve
permissions: preserve
seed semantics: preserve
grid behavior: preserve

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-CONTENT-AMC-001-R1/

Required:

analyze.md
cqrs-request-matrix.md
validation-matrix.md
cross-module-boundaries.md
error-localization.md
observability.md
structure.md
validation.md
closure.md

SOT

Update tmar-current-state.json under:
hostContentAmcR1

Must record:

parent task/commit
Host/Content ZERO preserved
Content projects
endpoint ownership
CQRS/MediatR state
endpoint-reachable request count
validator required/present/no-validator counts
Result/ApiResponseFactory state
message-classification state
error catalog/localization state
logging state
telemetry/correlation state
Content→Media boundary
Endpoints→Infrastructure state
pathNamespace
root allowlists
source cohesion
schemaMigrationState
focused validations
blockers/debt
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_CONTENT_R1_CHECKPOINT

Persist this task:
docs/ai/tasks/TB-TMAR-HOST-CONTENT-AMC-001-R1.task.md

PASS CRITERIA

PASS only if ALL hold:

Host/Content remains ZERO
Content endpoint ownership module-owned
Content.Endpoints -> Content.Infrastructure = ZERO
all endpoint business use cases use MediatR 12.5 ISender
handlers return Result/Result<T>
canonical ApiResponseFactory presentation
exhaustive validator classification + guards
zero expected-failure message parsing
canonical stable errors/catalog/resources/localization
Content admin auth seam is thin and canonical
Content→Media = Contracts-only
no foreign Application/Infrastructure/Domain coupling
no cross-module persistence
logging/telemetry/correlation canonical
capability-oriented foldering and exact namespaces
no god/root dump
seed composition correctly owned
schema/migrations/frontend unchanged
focused builds/tests/guards pass
evidence/SoT/task persisted
commit pushed, HEAD == origin/main
next Host folder NOT started

If any item remains deferred, Status MUST NOT be PASS.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R1
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Host-Content-Zero-State:
Content-Projects-State:
Content-Endpoint-Ownership-State:
Content-CQRS-State:
MediatR-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validator-Present-Count:
No-Validator-Required-Count:
Result-Pipeline-State:
ApiResponseFactory-State:
Message-Classification-State:
Content-Admin-Authorization-State:
Error-Catalog-State:
Localization-State:
Logging-State:
OpenTelemetry-State:
Correlation-State:
Content-To-Media-Boundary-State:
Endpoints-To-Infrastructure-State:
Foreign-Module-Dependency-State:
Cross-Module-Persistence-State:
Grid-State:
Seed-Composition-State:
Path-Namespace-State:
Root-Allowlist-State:
Source-Cohesion-State:
Schema-Migration-State:
Frontend-State:
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

STOP RULE

After result:

STOP completely.
do not inspect/start next Host folder.
do not self-issue another task.
do not poll.

END_TOOBA_TASK