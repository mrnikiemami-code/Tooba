PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W3
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W2-R1
Parent-Commit: f90215dd02bf8c475997e132589bc9598a5958dc
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W3 — evacuate UnitOfMeasure Admin slice to Catalog without Host/Infrastructure/Localization.Application coupling

ARCHITECT ACCEPTANCE

W2-R1 is ACCEPTED for the bounded Quantity Settings slice.

Verified at:
f90215dd02bf8c475997e132589bc9598a5958dc

Accepted state:

Host/Admin = 58 production files.
Quantity route is Catalog-owned.
Catalog Application Quantity surface is capability-first/shallow.
no Application *Contracts.cs bundle in W2 surface.
Query/Command authoritative types are unique.
Endpoints→Infrastructure = ZERO.
Catalog→Host = ZERO.
StoreAppearance remains intentionally deferred.
schema/frontend unchanged.

ACTIVE HOST FOLDER

src/backend/Host/Tooba.Host/Admin/

Do not start another Host folder.
Do not start W4.

W3 BOUNDED SLICE

Primary Host production file:

Admin/UnitOfMeasureEndpoints.cs

This file currently mixes:

Admin HTTP routes
direct CatalogDbContext reads
Localization.Application language lookup
transport/request/view records
CQRS writes
error mapping
Host implementation of IUnitOfMeasureLanguageGate

Current Catalog legacy surface also includes:

Tooba.Catalog.Application/UnitOfMeasureWriteContracts.cs
Tooba.Catalog.Application/UnitOfMeasureWriteHandlers.cs
Tooba.Catalog.Infrastructure/UnitOfMeasureDirectory.cs

W3 must fix the destination structure it touches; do not move Host coupling into Catalog.

REQUIRED SKILLS

Read current:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Also read:

AGENTS.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/tmar-current-state.json
W1/W2/W2-R1 evidence
all direct UoM tests/registrations/callers
current Localization.Contracts interfaces
current Catalog Application/Infrastructure UoM types

MANDATORY ANALYZE FIRST

Produce an exact member-level disposition map for UnitOfMeasureEndpoints.cs.

Classify:

five HTTP routes
list/get read behavior
language resolution/fallback
create/update/deactivate writes
transport body/view types
UoM language validation
error codes
Host DI registration
direct DbContext access
direct Localization.Application access

No code move before this map exists.

CRITICAL EXISTING VIOLATIONS TO REMOVE

The current Host UoM surface contains all of these and W3 must not reproduce them:

Host endpoint direct CatalogDbContext.
Host endpoint direct Tooba.Catalog.Infrastructure.Persistence.
Host endpoint direct Tooba.Localization.Application.
Host endpoint raw Results.Json expected-failure mapping.
PlatformHttpException expected-business transport.
InvalidOperationException message-as-code mapping.
Catalog legacy root file UnitOfMeasureWriteContracts.cs mixing port/models/commands.
Catalog legacy root file UnitOfMeasureWriteHandlers.cs bundling handlers.
Catalog Infrastructure throws PlatformHttpException / InvalidOperationException for expected UoM outcomes.
Host-owned HostUnitOfMeasureLanguageGate bridging Catalog to Localization.Application.

TARGET OWNERSHIP

Catalog owns:

UoM use cases
UoM read/write models
UoM persistence
UoM validation/business outcome codes
Admin UoM HTTP endpoints

Localization owns:

language registry/lookup semantics

Host owns only:

neutral platform/composition seams; no UoM business adapter should remain if a lawful Contracts boundary exists.

LOCALIZATION BOUNDARY

Do NOT keep or move a dependency on Tooba.Localization.Application.

Use Tooba.Localization.Contracts only.

Current available cross-module contract includes:

ILanguageLookup
LanguageLookupSnapshot
ILanguageActivationPort where applicable

Choose the smallest correct contract.

Preferred:

Catalog Infrastructure adapter/port implementation consumes ILanguageLookup from Localization.Contracts for:
resolving requested language code/url prefix/default fallback for list view, if that responsibility belongs in the Catalog use-case adapter; and/or
validating LanguageIds for translations.

Do not create a duplicate language registry abstraction if existing Localization.Contracts is sufficient.

If a narrow new Localization.Contracts contract is genuinely required, add only the minimum boundary and keep Localization implementation ownership in Localization.Infrastructure. Do NOT add Localization.Application dependency.

CATALOG APPLICATION STRUCTURE

Capability-first shallow-by-default.

Use a cohesive capability structure, e.g.:

Plain text
Tooba.Catalog.Application/
  Units/
    Commands/
      CreateUnitOfMeasureCommand.cs
      CreateUnitOfMeasureHandler.cs
      UpdateUnitOfMeasureCommand.cs
      UpdateUnitOfMeasureHandler.cs
      DeactivateUnitOfMeasureCommand.cs
      DeactivateUnitOfMeasureHandler.cs
    Queries/
      ListUnitOfMeasuresQuery.cs
      ListUnitOfMeasuresHandler.cs
      GetUnitOfMeasureQuery.cs
      GetUnitOfMeasureHandler.cs
    Models/
      UnitOfMeasureWriteModel.cs
      UnitOfMeasureTranslationWriteModel.cs
      UnitOfMeasureListItem.cs
      UnitOfMeasureDetail.cs
      ...only cohesive internal models
    Ports/
      IUnitOfMeasureDirectory.cs
      IUnitOfMeasureLanguage...cs only if still needed internally
    Validators/
      ...

Equivalent capability name is allowed if current Catalog semantics justify it.

Mandatory:

no UnitOfMeasureWriteContracts.cs
no generic/mixed *Contracts.cs
no root Application UoM files
no one-leaf-folder-per-single-command/query pattern
exact path↔namespace
separate handlers where reasons to change differ

READ PATH

Move GET list and GET detail into proper Catalog Queries/Handlers.

Endpoints must NOT:

access DbContext
access directory directly
access Localization.Contracts directly unless it is a pure transport resolver explicitly justified; preferred: query owns resolution through a Catalog port/adapter
perform business/read-model assembly beyond transport mapping

List behavior to preserve:

order by SortOrder then Code
requested language matches Code or UrlPrefix case-insensitively
fallback to default language, else first language, else Guid.Empty semantics only if current behavior must be preserved
translation fallback to first UoM translation, else Code
IsReferenced from Product UoM references

Get behavior to preserve:

full translations
IsReferenced
missing unit -> same stable error semantics, now canonical Result/error mapping

WRITE PATH

Create/Update/Deactivate must use authoritative MediatR requests returning Result<T> / Result.

No expected business failure via:

PlatformHttpException
InvalidOperationException
exception message parsing

Expected outcomes must use stable Catalog-owned error codes such as existing semantics:

unit.missing
unit.dimension.invalid
unit.code.duplicate
unit.language.unknown

Normalize names only if necessary to fit canonical Catalog error-code ownership; preserve externally observable errorCode semantics unless there is already a canonical locked code. Do not silently change client-visible codes.

VALIDATION

Classify every endpoint-reachable request:

List query
Get query
Create command
Update command
Deactivate command

Each exactly:

VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED

FluentValidation only for untrusted transport shape.
Business rules stay Application/Domain.

Examples likely transport validation:

required/nonblank Code
required/nonblank Dimension
translations shape
valid Guid route IDs only if not already guaranteed by route binding
Do not duplicate DB uniqueness/language existence/business state in validators.

RESULT / ERROR / LOCALIZATION

Catalog Endpoints must use:

ISender
ApiResponseFactory
canonical Catalog error catalog/resources

Extend CatalogErrorCodes / contributor / resx for UoM expected outcomes.

Rules:

one descriptor owner per code
no hard-coded user-facing API errors
no ex.Message classification
no endpoint catch-and-map for expected business failures
unknown exceptions propagate to global boundary

TRANSPORT SHAPES

HTTP request DTOs may live in:
Catalog.Endpoints/Admin/Units/...
if transport-only.

Application result/read models remain Application-owned unless a real foreign module consumer exists.

Do not put internal UoM DTOs into Catalog.Contracts merely because they are DTOs.

HOST LANGUAGE GATE

HostUnitOfMeasureLanguageGate must be removed only after all live semantics are rehomed through Localization.Contracts.

Remove its Program DI registration only after replacement wiring is complete.

After W3 PASS:

Host must own ZERO UoM business/language adapter responsibility from this file.

ENDPOINT TARGET

Move all five routes:

Plain text
GET    /v1/admin/catalog/units/
GET    /v1/admin/catalog/units/{unitId:guid}
POST   /v1/admin/catalog/units/
PUT    /v1/admin/catalog/units/{unitId:guid}
POST   /v1/admin/catalog/units/{unitId:guid}/deactivate

to Tooba.Catalog.Endpoints.

Use ICatalogAdminAuthorizer.

Preserve:

methods
paths
success JSON shapes
status behavior/errorCode semantics
auth behavior

DEPENDENCY RULES

Required final touched surface:

Catalog.Endpoints -> Catalog.Application + Catalog.Contracts + BuildingBlocks
Catalog.Application -> Catalog.Domain + Catalog.Contracts + BuildingBlocks
Catalog.Infrastructure -> Catalog.Application + Catalog.Domain + Catalog.Contracts + Localization.Contracts + neutral BuildingBlocks
no Catalog -> Host
no Catalog -> Localization.Application
no Catalog.Endpoints -> Catalog.Infrastructure
no cross-module DbContext
no cross-module SQL/EF join

HOST INVENTORY

Re-enumerate exact Host/Admin at start and end.

Expected if full file is evacuated safely:

58 -> 57

Do not hard-code PASS to 57 if analysis finds a legitimate retained responsibility; but any retention must be explicitly justified and must not be UoM business ownership.

Do NOT touch StoreAppearance in W3.

DURABLE GUARDS

Add/strengthen guards proving:

Host UnitOfMeasureEndpoints.cs absent after complete evacuation.
Program no longer maps Host UoM route or registers HostUnitOfMeasureLanguageGate.
Catalog Endpoints own all five UoM routes exactly once.
endpoints use ISender + ApiResponseFactory + ICatalogAdminAuthorizer.
no DbContext/Infrastructure/Localization.Application in Endpoints.
no Catalog project reference/source reference to Host.
no Catalog project/source reference to Localization.Application.
UoM Application has capability-first shallow folders.
no UoM *Contracts.cs bundle.
no duplicate authoritative request types.
request validator classification exhaustive.
expected failures use Result/stable codes, not PlatformHttpException/InvalidOperationException message mapping.
Localization cross-module access is Contracts-only.
path↔namespace exact.
Host/Admin file-count delta matches disposition.
W1/W2/W2-R1 state preserved.
StoreAppearance still deferred.
no next Host folder / W4 started.

TESTS

Update/reuse UnitOfMeasureAdminTests to prove behavior parity.

Focused validation:

Catalog.Contracts build
Catalog.Domain build if touched
Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Localization.Contracts/Infrastructure build only if boundary implementation changed
Host build
Host.Tests build
UoM behavior tests
W1/W2/W2-R1/W3 architecture guards

Tests are evidence, not navigation.
No open-ended test/repair loop.

EVIDENCE

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W3/

Required:

analyze.md
disposition-map.md
localization-boundary.md
cqrs-read-write.md
error-result-migration.md
foldering.md
validation.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W3.task.md

SOT

Add hostAdminAmcW3 preserving W1/W2/W2-R1.

Record:

parent commit
activeHostFolder=Admin
file count before/after
UoM endpoint ownership
UoM CQRS state
validator matrix
Localization boundary state
Host language-gate state
result/error state
Catalog→Host
Catalog→Localization.Application
Endpoints→Infrastructure
schema/frontend
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W3_CHECKPOINT

PASS CRITERIA

PASS only if ALL:

all live UoM responsibilities correctly rehomed
Host UoM endpoint file removable with no lost responsibility
five routes Catalog-owned exactly once
all five operations CQRS/ISender-backed
direct Host DbContext access ZERO
Catalog→Host ZERO
Catalog→Localization.Application ZERO
Localization cross-module use Contracts-only
HostUnitOfMeasureLanguageGate removed/replaced lawfully
no PlatformHttpException expected-business flow in touched Catalog UoM surface
no InvalidOperationException message-as-code flow
Result/ApiResponseFactory canonical
Catalog error catalog/resources complete for UoM codes
semantic Contracts correct
capability-first shallow foldering correct
no *Contracts.cs bundle
validator classification exhaustive
path↔namespace exact
route/status/JSON/auth parity preserved
Host/Admin final inventory exact
StoreAppearance still deferred
no schema/migration/frontend change
focused builds/tests/guards PASS
evidence/task/SoT persisted
commit pushed
HEAD == origin/main
working tree clean
W4 not started
next Host folder not started

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W3
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W2-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W2R1-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
UoM-Host-File-State:
UoM-Endpoint-Ownership-State:
UoM-CQRS-State:
UoM-Validator-Coverage-State:
UoM-Application-Structure-State:
UoM-Legacy-Contracts-State:
Localization-Boundary-State:
Host-UoM-Language-Gate-State:
Endpoints-To-Infrastructure-State:
Catalog-To-Host-State:
Catalog-To-LocalizationApplication-State:
Foreign-Module-Dependency-State:
Result-Pipeline-State:
Error-Localization-State:
Message-Classification-State:
PlatformHttpException-State:
Route-Parity-State:
Path-Namespace-State:
Store-Appearance-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
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

After returning:

STOP completely.
Do not start W4.
Do not inspect/start another Host folder.
Wait for Architect review.

END_TOOBA_TASK