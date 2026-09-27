PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R3
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R2
Parent-Commit: 9f6579b4333005fda0f9994ea7c02295a9326ed4
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_REPAIR
Title: Final Content ARCH-COMPLETE-002 structure certification and manifest promotion

ARCHITECT REVIEW OF R2

R2 is ACCEPTED for its bounded repair scope.

Verified on commit:
9f6579b4333005fda0f9994ea7c02295a9326ed4

Verified:

Host/Content remains ZERO.
Content.Endpoints -> Content.Infrastructure = ZERO.
Content -> Media = Contracts-only.
Content expected-failure message classification repaired to typed ContractOperationException(Code).
PlatformHttpException removed from Content Application/Infrastructure expected business flow.
Media readiness boundary now uses typed stable codes.
ApiResponseFactory.Created used for create paths.
R2 guards/evidence/SoT exist.

One certification gap remains:
Content is NOT present in docs/architecture/tmar-module-structure-manifests.json, therefore it is not yet ARCH-COMPLETE-002 structure-certified.

This R3 is the final bounded Content closure task. Do not start another Host folder.

REQUIRED SKILLS

Read and follow in order:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Also read:

AGENTS.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json
R1/R2 task + evidence
existing certified module manifest entries only as read-only reference.

SCOPE

Primary:

src/backend/Modules/Content/
Content-related structure guards/tests only
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json
R3 task/evidence
solution grouping only if Content grouping is actually non-canonical

Do NOT:

reopen R1/R2 architecture unless certification finds a concrete violation
start next Host folder
perform unrelated module cleanup
alter Media/Localization beyond direct Content certification evidence
touch frontend
change schema/migrations
redesign business behavior

GOAL

Promote Content to:

COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 structureCertified=true

only after actual structural verification.

CERTIFICATION REQUIREMENTS

Physical tree

enumerate all non-generated production files in:
Tooba.Content.Contracts
Tooba.Content.Domain
Tooba.Content.Application
Tooba.Content.Infrastructure
Tooba.Content.Endpoints
verify no root dumping
verify capability/responsibility-oriented folders
verify no duplicate/stale physical copies
verify no namespace-alias workaround

Path ↔ namespace

exact physical path-derived namespace for every production .cs file in Content
no mismatch hidden by project/global aliases

Root allowlists
Create explicit root allowlists for every Content project in the manifest.
Root files must be only genuine composition/global project-wide files.

Do not blindly accept current root files.
For every root .cs file:

classify responsibility
if capability-specific, move it to the correct folder
if genuinely project-wide/composition, retain with explicit justification

Endpoints structure

capability/audience endpoint files under Admin/, Storefront/, etc.
root should contain only composition entry plus explicitly allowed shared folders (Errors/, Resources/)
no endpoint capability file at root
no duplicate route mapping

Application structure

Commands/Queries/Models/Ports/Validators grouped by capability/use case
no capability-specific .cs at project root
no legacy composer/directory-contract dump
51 endpoint-reachable requests remain covered
validation matrix remains 17 required / 17 present / 34 no-validator unless repository reality proves a legitimate correction

Infrastructure structure

capability/integration driven
Persistence under Persistence
foreign adapters under coherent adapter/integration folders
grid under coherent Content-owned location
composition entry only at root if canonical
no fragmented duplicate top-level integration folders
no foreign Application/Infrastructure/Domain dependencies

Contracts / Domain structure

Contracts DTOs/Ports/Errors organized coherently
Domain aggregates/entities/value objects/rules grouped coherently
no generic root dumping
exact namespaces

Solution Explorer grouping

all five Content projects grouped under one /Modules/Content/ solution folder
do not change assembly names/project paths merely for visual grouping

Existing R1/R2 guarantees must remain true

Host/Content ZERO
Endpoints→Infrastructure ZERO
Content→Media Contracts-only
Content→Localization Contracts-only
CQRS/MediatR/ISender intact
message classification ZERO
PlatformHttpException expected-business transport ZERO in App/Infra
Result/ApiResponseFactory canonical
error catalog/resources canonical
unknown exceptions propagate
no cross-module persistence

MANIFEST

Add Content to:
docs/architecture/tmar-module-structure-manifests.json

Required:

"module": "Content"
"structureCertified": true
"lockVersion": "ARCH-COMPLETE-002"
all five Content projects represented
exact root allowlists
explicit forbiddenRootFiles based on migrated legacy/root-debt names
forbiddenTopLevelFolders where appropriate
concise justifications for any non-empty root allowlist

Do NOT mark structureCertified: true unless all checks actually pass.

STRUCTURE LOCK / SOT

Update:
docs/architecture/tmar-current-state.json

Required:

add Content to the canonical structureLock.certifiedModules collection if that is the current SoT mechanism
add/update hostContentAmcR3
record:
parent task/commit
ARCH-COMPLETE-002
structureCertified: true
pathNamespace state
root allowlists state
solution grouping state
endpoint/CQRS/validation state
dependency boundary state
Host/Content ZERO
schema/frontend unchanged
focused validation
blockers/debt
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_CONTENT_R3_FINAL_CHECKPOINT

If the repository uses another canonical field for certified modules, follow repository reality; do not invent duplicate SoT.

DURABLE GUARDS

Add or strengthen guards proving:

Content exists in module structure manifest exactly once.
structureCertified == true.
lockVersion == ARCH-COMPLETE-002.
all five Content projects have manifest entries.
actual root .cs files exactly match manifest root allowlists.
forbidden root files absent.
forbidden top-level folders absent.
exact path↔namespace alignment for all Content production .cs.
Content project files physically exist and project references resolve.
all five projects are under /Modules/Content/ in Tooba.slnx.
Host/Content remains ZERO.
Endpoints→Infrastructure remains ZERO.
foreign Application/Infrastructure/Domain dependency remains ZERO.
CQRS/ISender + validator matrix remains intact.
message classification and PlatformHttpException regressions remain ZERO.
no schema/migration changes.

Do not weaken existing guards.

FOCUSED VALIDATION

Run focused:

Content project builds
Host build if composition/solution metadata touched
Content structural guard
path↔namespace guard
root allowlist guard
solution grouping guard
existing R1 + R2 Content guards
validator coverage guard
CQRS/result/dependency guards

Tests are evidence, not navigation.
One deterministic local repair per focused failure.
No open-ended refactor loop.

BEHAVIOR PRESERVATION

Must preserve:

routes
permissions
success DTO shapes
expected error codes/status semantics
seed behavior
grid behavior
schema/migrations = NONE
frontend = UNCHANGED

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-CONTENT-AMC-001-R3/

Required:

physical-tree.md
root-allowlists.md
path-namespace.md
solution-grouping.md
certification.md
validation.md
closure.md

PASS CRITERIA

PASS only if ALL hold:

R2 remains architecturally intact
Content physical structure is capability/responsibility coherent
exact path↔namespace for all production files
root allowlists exactly match reality
forbidden root files/folders absent
all five Content projects grouped under /Modules/Content/
manifest contains Content exactly once
structureCertified: true
lockVersion: ARCH-COMPLETE-002
Content recorded in canonical structure lock SoT
Host/Content ZERO
no Endpoints→Infrastructure
Contracts-only cross-module boundaries
CQRS/MediatR/ISender intact
validator coverage intact
canonical Result/error/localization/observability intact
focused builds/tests/guards PASS
schema/migrations/frontend unchanged
no remaining certification blocker
residual debt does NOT contain an ARCH-COMPLETE-002 prerequisite
commit pushed
HEAD == origin/main
working tree clean
next Host folder NOT started

If any required structural repair remains, Status MUST NOT be PASS.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CONTENT-AMC-001-R3
Parent-Task: TB-TMAR-HOST-CONTENT-AMC-001-R2
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
R2-Preservation-State:
Host-Content-Zero-State:
Content-Structure-State:
ARCH-COMPLETE-002-State:
Structure-Manifest-State:
Structure-Lock-SoT-State:
Content-Projects-State:
Physical-Tree-State:
Root-Allowlist-State:
Path-Namespace-State:
Solution-Grouping-State:
Endpoint-Ownership-State:
CQRS-MediatR-State:
Validator-Coverage-State:
Result-Error-State:
Message-Classification-State:
PlatformHttpException-State:
Cross-Module-Boundary-State:
Cross-Module-Persistence-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
Known-PreExisting-Failures:
Remaining-Blockers:
Residual-Debt:
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

After returning this result:

STOP completely.
Do not inspect/start another Host folder.
Do not self-issue another task.
Wait for Architect review.

END_TOOBA_TASK