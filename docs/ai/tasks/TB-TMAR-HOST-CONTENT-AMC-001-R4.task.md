PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R4
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R3
Parent-Commit: c4c44dad8a7e13bc4953914c693448de32be8592
Governance-Commit: d093ad25aa6bd998909c583af0096d3a11094115
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: CONTENT_POSTCERT_SEMANTIC_STRUCTURE_REPAIR
Title: Realign Content Application by capability and repair semantic Contracts ownership

REASON

Content R3 correctly achieved the then-current ARCH-COMPLETE-002 physical certification, but Architect review found two semantic-structure defects that the previous guards did not reject:

Application contains legacy mixed *Contracts.cs bundles under Application/Models that mix internal DTOs/snapshots and command-shaped input records.
Application Commands/Queries are over-foldered: many leaf folders exist only to wrap a single request source file.

The Analyze/Migrate/Certify skills were hardened in governance commit:
d093ad25aa6bd998909c583af0096d3a11094115

The new binding rules are:

semantic Contracts ownership is based on consumer boundary, not filename;
module-boundary contracts belong in Tooba.Content.Contracts;
Application-internal models/ports remain Application-owned;
generic/mixed *Contracts.cs bundles inside Application are not certifiable;
one authoritative CQRS request type per use case;
capability-first, shallow-by-default foldering;
a one-file folder per Command/Query is over-foldering unless real multi-file/complexity justification exists.

REQUIRED SKILLS

Read and follow the CURRENT versions from main, in order:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Also read:

AGENTS.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json
Content R1/R2/R3 tasks/evidence
current Content source tree

Do not rely on the R3 manifest as proof that semantic cohesion is still sufficient under the newly hardened skills.

SCOPE

Primary:

src/backend/Modules/Content/Tooba.Content.Application/
src/backend/Modules/Content/Tooba.Content.Contracts/
direct Content Endpoints/Infrastructure consumers required by namespace/type moves
Content-specific architecture guards/tests
Content manifest/SoT/evidence after realignment
src/backend/Tooba.slnx only if grouping actually needs adjustment

Allowed outside Content:

ONLY direct consumers of a Content type being classified for module-boundary ownership.
If another module consumes a Content Application type, inspect only enough to classify and replace that boundary with Content.Contracts.
Do not recover/restructure that foreign module.

Do NOT:

reopen another Host folder
start another module recovery
change business behavior
change routes/status codes/public JSON semantics
change permissions
change DB schema/migrations
touch frontend
introduce a generic shared contracts project
blindly move every DTO to Content.Contracts
preserve legacy naming merely to minimize diff

MANDATORY ANALYZE — SEMANTIC TYPE INVENTORY

Inventory EVERY production type currently under:

Tooba.Content.Application/Models/
Tooba.Content.Application/Ports/
Tooba.Content.Application/Commands/
Tooba.Content.Application/Queries/
Tooba.Content.Application/Validators/

For every type record:

current file/path
type name
responsibility
direct consumers
whether any consumer is outside Content
whether it is HTTP transport-only, Application-internal, or module-boundary stable
final owner:
CONTENT_CONTRACTS_BOUNDARY
CONTENT_APPLICATION_INTERNAL
CONTENT_ENDPOINT_TRANSPORT
DUPLICATE_LEGACY_SHAPE_REMOVE_OR_REPOINT
final target path/namespace

Do not classify from the suffix Dto, Contract, Command, Request, or Port alone.

CONTRACTS OWNERSHIP RULE

Tooba.Content.Contracts is ONLY for stable module-boundary semantics:

DTOs intentionally consumed by another module;
ports intentionally exposed for another module;
integration events;
stable Content-owned error codes.

Application-internal CQRS requests/results/snapshots/grid rows/directory models/ports remain Application-owned.

If a current Application model is consumed by another module:

move or introduce the narrow stable shape in Content.Contracts;
repoint the foreign consumer to Content.Contracts;
do not expose Content Application;
do not move internal-only members merely because one file currently contains both internal and external types;
split the legacy file by responsibility.

If NO external consumer exists:

keep the type in Application under the correct capability Models/Ports/Requests;
rename/split away the misleading *Contracts.cs legacy filename.

APPLICATION COHESION — LEGACY CONTRACT BUNDLES

Audit at minimum the current legacy files:

Models/ContentContracts.cs
Models/ContentArticleCommentContracts.cs
Models/ContentArticleMediaContracts.cs
Models/ContentAuthorContracts.cs
Models/ContentCategoryContracts.cs
Models/ContentPublicTaxonomyContracts.cs
Models/ContentTagContracts.cs

Required final state:

no generic/mixed *Contracts.cs bundle remains in Content.Application;
DTO/snapshot/model types are grouped under their owning capability;
command-shaped input records are not kept in Models if the authoritative MediatR request already exists;
no duplicate semantic request shape survives merely for compatibility;
no compatibility alias/shim/type-forwarding workaround.

AUTHORITATIVE CQRS REQUEST RULE

For every endpoint-reachable Content use case:

exactly one authoritative Application MediatR Command/Query type;
handler targets that request;
validator targets that request where applicable;
endpoint maps transport body into that request;
internal directories/services receive cohesive parameters/models as needed but must not recreate a second application command with the same business meaning.

Specifically audit current command-shaped records found inside legacy Models bundles such as:

CreateArticleCommand / UpdateArticleCommand legacy model shapes
Content Category create/update/SEO/media/move/reorder input records
Content Author create/update input records
equivalent Tag/Comment/Media legacy command shapes

Do not delete a shape until all call sites are safely repointed and behavior is preserved.

PROFESSIONAL FOLDERING — CAPABILITY FIRST

Final Content Application should be capability-first and shallow by default.

Use actual Content capabilities discovered from the code, expected to include concepts such as:

Articles
Categories
Authors
Tags
Media
Comments
shared Composition only if genuinely cross-capability

Preferred shape:

Plain text
Tooba.Content.Application/
  Articles/
    Commands/
    Queries/
    Models/
    Ports/
    Validators/
  Categories/
    Commands/
    Queries/
    Models/
    Ports/
    Validators/
  Authors/
    Commands/
    Queries/
    Models/
    Ports/
    Validators/
  Tags/
    Commands/
    Queries/
    Models/
    Ports/
    Validators/
  Media/
    Commands/
    Queries/
    Models/
    Ports/
    Validators/
  Comments/
    Commands/
    Queries/
    Models/
    Ports/
    Validators/
  Composition/

Adapt capability names to repository semantics. Do NOT create empty decorative folders.

ONE-FOLDER-PER-REQUEST RULE

The current pattern like:

Plain text
Commands/
  CreateArticle/
    CreateArticleCommand.cs
  UpdateArticle/
    UpdateArticleCommand.cs

must NOT remain when the leaf directory exists only to contain one request source file.

Flatten such cases into the owning capability:

Plain text
Articles/
  Commands/
    CreateArticleCommand.cs
    UpdateArticleCommand.cs
    PublishArticleCommand.cs

Same for Queries.

A deeper per-use-case folder is allowed ONLY when that use case genuinely owns multiple cohesive production files with distinct responsibilities or real complexity makes isolation materially clearer.

Do not flatten a legitimate complex multi-file use case merely to satisfy aesthetics.

VALIDATORS

Move/repoint validators consistently with capability-first ownership.

Requirements:

coverage remains exhaustive;
current expected matrix remains 17 REQUIRED / 17 PRESENT / 34 NO_VALIDATOR unless direct inventory proves a legitimate count change;
canonical AddToobaCqrsFoundation discovery continues to work;
no manual validator invocation;
stable validation codes preserved.

PORTS

Classify every port:

module-internal Application→Infrastructure port: keep under owning capability Application/<Capability>/Ports or a shared Application/Ports only if genuinely cross-capability;
cross-module exposed port: move to Content.Contracts/<Capability>/Ports.

No port may be moved to Contracts merely for visual symmetry.

MODELS

Split by capability and meaning.

Examples:

Article snapshots/history/public read models -> Application/Articles/Models/ unless proven cross-module boundary.
Category workspace/tree internal models -> Application/Categories/Models/.
Author workspace/grid/picker internal models -> Application/Authors/Models/.
Media workspace/gallery internal models -> Application/Media/Models/.
Comments/tag models likewise.

Do not use generic ContentContracts.cs/*Contracts.cs catch-all files in Application.

CONTENT.CONTRACTS

After the consumer audit:

keep existing Errors/ContentErrorCodes.cs;
add only proven boundary DTOs/Ports/Events;
organize them by capability;
exact path↔namespace;
no dumping internal CQRS models into Contracts.

If there are no additional cross-module Content contracts today, it is valid for Content.Contracts to remain mostly Errors-only.

DEPENDENCY RULES

Must remain:

Endpoints -> Application + Contracts + neutral BuildingBlocks only
no Endpoints -> Infrastructure
Application -> Domain + Contracts + neutral BuildingBlocks
Infrastructure -> Application + Domain + Contracts + external Contracts only
no foreign Application/Infrastructure/Domain
no Host reference from Content projects
no cross-module persistence/join

R1/R2/R3 PROTECTED STATE

Preserve:

Host/Content = ZERO
Content→Media = Media.Contracts only
Content→Localization = Localization.Contracts only
typed ContractOperationException(Code) expected fault flow
message classification = ZERO
PlatformHttpException expected-business transport = ZERO in Content App/Infra
Result/ApiResponseFactory canonical
ApiResponseFactory.Created
error catalog/resources/localization
OpenTelemetry/correlation foundation
schema/migrations NONE
frontend UNCHANGED
five Content projects
/Modules/Content/ solution grouping

PATH / NAMESPACE

Every moved production file must have exact namespace matching physical path.

No aliases or forwarding to preserve old namespaces.

Update all direct consumers atomically.

STRUCTURE MANIFEST / CERTIFICATION

R4 changes the certified physical structure, so R3 certification must be revalidated, not assumed.

Update tmar-module-structure-manifests.json after repair:

Content exactly once
structureCertified: true only after all new semantic/foldering gates pass
exact root allowlists
forbidden legacy Application *Contracts.cs names
forbidden old one-file Commands/Queries directory structure using a durable guard appropriate to actual final tree
all five project entries remain correct

Update tmar-current-state.json with hostContentAmcR4 and keep Content in structureLock.certifiedModules only if R4 certification passes.

DURABLE GUARDS

Add/strengthen guards proving at minimum:

No Tooba.Content.Application/Models/*Contracts.cs.
No generic/mixed Application *Contracts.cs bundle anywhere.
Every Content Application model/port is classified by semantic ownership.
No external module project references Tooba.Content.Application.
Any cross-module Content consumption resolves through Tooba.Content.Contracts.
No duplicate command/query-shaped Application model exists beside the authoritative MediatR request.
No unjustified leaf folder under Content Commands/Queries contains only one .cs request file.
Capability-first folders exist and contain their owned requests/models/ports/validators.
No empty decorative capability folders.
Exact path↔namespace across Content.
CQRS request count/validator matrix remains exhaustive.
Endpoints→Infrastructure remains ZERO.
Content→Media/Localization remain Contracts-only.
Host/Content remains ZERO.
manifest root allowlists match disk.
Content remains ARCH-COMPLETE-002 structureCertified=true only when all these guards pass.

Guard must be semantic enough to prevent recreating:

Application/Models/ContentContracts.cs;
one-directory-per-single-request patterns.

Do not create a brittle guard based only on today's exact list of commands if a structural invariant can be checked instead.

FOCUSED VALIDATION

Build affected:

Content.Contracts
Content.Domain if namespace consumers change
Content.Application
Content.Infrastructure
Content.Endpoints
Host
Host.Tests / focused Content tests

Run focused:

new semantic Contracts ownership guard
folder granularity guard
duplicate CQRS request-shape guard
path↔namespace
manifest/root allowlists
R1/R2/R3 Content guards updated as necessary WITHOUT weakening architecture intent
validator coverage
CQRS/ISender
cross-module dependency
result/error/localization guards
relevant Content behavior tests

Tests are evidence, not navigation.
One deterministic local repair per focused failure; rerun only affected validation.
No open-ended cleanup loop.

BEHAVIOR PRESERVATION

Must preserve:

routes
HTTP methods
success JSON shape
expected statuses/error codes
permissions
CQRS semantics
grid behavior
seed behavior
public Content contract semantics where already consumed
schema/migrations
frontend

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-CONTENT-AMC-001-R4/

Required:

type-ownership-inventory.md
legacy-contract-bundles.md
capability-foldering.md
cqrs-authority.md
cross-module-consumers.md
structure-certification.md
validation.md
closure.md

SOT

Add/update:
hostContentAmcR4

Record:

parent task/commit
governance commit
semanticContractsState
applicationLegacyContractsState
duplicateCqrsShapeState
capabilityFolderingState
singleFileRequestFolderState
contentContractsBoundaryState
pathNamespace
validatorCoverage
R1R2R3Preservation
structureCertified
manifest state
focused validation
blockers/debt
schema/frontend
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_CONTENT_R4_CHECKPOINT

PASS CRITERIA

PASS only if ALL hold:

every Content Application Model/Port/Request is semantically classified
true module-boundary Content contracts live in Content.Contracts
internal models/ports stay Application-owned
ZERO misleading generic/mixed Application *Contracts.cs bundles
ZERO duplicate CQRS command/query semantic shapes
capability-first foldering is in place
ZERO unjustified one-file-per-Command/Query leaf folders
per-use-case folders exist only where multi-file cohesion/complexity justifies them
validators remain exhaustive
path↔namespace exact
no external module references Content.Application
Contracts-only foreign boundaries
Endpoints→Infrastructure ZERO
Host/Content ZERO
R1/R2 typed-result/error guarantees preserved
manifest updated honestly
ARCH-COMPLETE-002 structure certification re-passes under the new skill rules
no schema/migration/frontend change
focused builds/tests/guards PASS
evidence/SoT/task persisted
commit pushed
HEAD == origin/main
working tree clean
next Host folder NOT started

A known semantic Contracts/folder-granularity violation is a certification blocker and cannot be residual debt.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R4
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R3
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Governance-Skill-State:
R3-Preservation-State:
Host-Content-Zero-State:
Semantic-Contracts-State:
Application-Legacy-Contracts-State:
Content-Contracts-Boundary-State:
Cross-Module-Content-Consumer-State:
Duplicate-CQRS-Shape-State:
Capability-Foldering-State:
Single-File-Request-Folder-State:
Content-Application-Structure-State:
Validator-Coverage-State:
CQRS-MediatR-State:
Endpoints-To-Infrastructure-State:
Foreign-Module-Dependency-State:
Message-Classification-State:
PlatformHttpException-State:
Result-Error-State:
Path-Namespace-State:
Structure-Manifest-State:
ARCH-COMPLETE-002-State:
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

After returning the canonical result:

STOP completely.
Do not inspect/start the next Host folder.
Do not self-issue another task.
Wait for Architect review.

END_TOOBA_TASK