PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W2
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001
Parent-Commit: 3d15c27c
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: W2 — evacuate bounded Catalog admin settings slice without moving Host coupling into Catalog

ARCHITECT REVIEW OF W1

W1 foundation is accepted as a bounded foundation slice:

Tooba.Catalog.Endpoints exists.
Endpoints references Application/Contracts/BuildingBlocks only.
Host wires Catalog endpoint presentation and mapping.
/Modules/Catalog/ solution grouping exists.
Host/Admin remains 59 production files; no evacuation was falsely claimed.
schema/frontend unchanged.

Pipeline hygiene note:

W1 evidence exists, but no canonical docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001.task.md was found.
Do NOT fabricate a retrospective task definition. W2 itself must be persisted canonically under its exact Task-ID and all future waves must follow BRIDGE-WAKE-V1 artifacts.

ACTIVE HOST FOLDER
src/backend/Host/Tooba.Host/Admin/

Only this Host folder is active. Do NOT start another Host folder.

W2 BOUNDED SLICE

Primary candidates:

Admin/QuantitySettingsEndpoints.cs
Admin/StoreAppearanceSettingsEndpoints.cs
Admin/StoreAppearanceSettingsComposer.cs

Goal:

evacuate only settings responsibilities that are truly Catalog-owned and ready;
do not force all files to move if analysis proves mixed Host/Storefront coupling.

REQUIRED SKILLS

Read current:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Also read AGENTS.md, Host evacuation protocol, complete-reference standard, current SoT, W1 evidence, and all direct callers/registrations/tests for the three W2 candidate files.

MANDATORY ANALYZE FIRST

For each candidate file classify:

HTTP endpoint
auth adapter
Application use case
presentation DTO
Catalog lookup/read model
Catalog command/write model
Storefront projection/cache dependency
Host composition only
foreign module dependency

Produce an exact disposition map before moving.

CRITICAL RULE — DO NOT TRANSFER COUPLING

Host evacuation means responsibility evacuation, not relocation.

StoreAppearanceSettingsComposer currently depends on Host/Storefront StoreAppearanceProjector. That dependency MUST NOT be copied into Catalog.Endpoints or Catalog.Application.

Allowed:

replace it with a proper Catalog-owned Application use case/port and lawful Contracts boundary; or
split the Host composer and move only Catalog-owned responsibility; or
leave unresolved mixed responsibility in Host and return it as NOT_MOVED/DEFERRED with exact blocker.

Forbidden:

Catalog.Endpoints -> Host
Catalog.Application -> Host
Catalog.Endpoints -> Catalog.Infrastructure
copying Host.Storefront dependency into Catalog
foreign Application/Infrastructure/Domain coupling
moving a composer unchanged merely to reduce Host file count

QUANTITY SETTINGS TARGET

For /v1/admin/settings/quantity-rounding:

Catalog-owned endpoint
GET and PUT through MediatR/ISender
no direct endpoint ICatalogLookupGateway
no endpoint-owned business/domain projection
canonical Result/Result<T>
ApiResponseFactory
canonical admin authorizer
preserve route/method/status/success JSON shape

If GET lacks a Query, add the minimum Catalog Query/Handler rather than letting Endpoints call a lookup gateway directly.

STORE APPEARANCE TARGET

For /v1/admin/settings/appearance:

prove true ownership of effective appearance read/save/invalidation behavior;
if Catalog owns it, expose Query/Command via ISender;
eliminate Host composer only after all live behavior is rehomed;
preserve projector invalidation/effective-view behavior without Host dependency;
if this requires broader Storefront redesign, split and defer the unresolved part rather than moving coupling.

SEMANTIC CONTRACTS

stable cross-module DTO/port/event -> Catalog.Contracts
Catalog-internal CQRS requests/results/models/ports -> Catalog.Application
transport-only HTTP shapes may live in Endpoints
no generic/mixed *Contracts.cs
one authoritative CQRS request type per use case

FOLDERING

Capability-first shallow-by-default.
Do NOT create one leaf folder per single Command or Query file.
Do NOT reorganize unrelated Catalog capabilities.

VALIDATION

Every new endpoint-reachable request:

VALIDATOR_REQUIRED or NO_VALIDATOR_REQUIRED
concrete FluentValidation only for transport shape
durable coverage guard

ERROR / LOCALIZATION / RESULT

Before first migrated endpoint PASS:

canonical Catalog error codes/catalog/resources where Catalog owns expected failures
no PlatformHttpException expected-business flow below HTTP boundary
no ex.Message classification
no ad-hoc raw Results.Json for expected failures
ApiResponseFactory
unknown exceptions propagate

AUTHORIZATION

Use W1 ICatalogAdminAuthorizer.
Do not copy static AdminPanelAccess.RequireAuthorizedAsync(...) into Catalog endpoints.
Host remains neutral IAdminPanelAccess implementation owner only.

HOST INVENTORY

At start and end re-enumerate exact Host/Admin.
Record before/after production file count.
Delete only after all live responsibilities are rehomed.
Do NOT claim Host/Admin ZERO in W2.

PROTECTED STATE

Preserve W1 foundation/wiring, routes, JSON success shapes, auth semantics, schema/migrations, frontend, unrelated Host/Admin files, and no next Host folder.

DURABLE GUARDS

Prove:

migrated W2 routes no longer Host-mapped;
no duplicate routes;
Catalog.Endpoints no Host/Infrastructure reference;
migrated endpoints dispatch with ISender;
no direct ICatalogLookupGateway/DbContext in Endpoints;
no Host.Storefront reference from Catalog projects;
canonical Result/ApiResponseFactory expected-failure path;
validator classification exhaustive;
Host/Admin file-count delta matches accepted W2 disposition;
W1 foundation intact;
no next Host folder started.

EVIDENCE

Create docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W2/:

analyze.md
disposition-map.md
quantity-settings.md
store-appearance.md
coupling-audit.md
validation.md
closure.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-AMC-001-W2.task.md

SOT

Update W2 state in docs/architecture/tmar-current-state.json, preserving W1 history:

parent commit
activeHostFolder=Admin
start/end Admin file count
quantity/appearance disposition
retained mixed Host responsibility
Catalog endpoint/CQRS/validator/result state
Host/Storefront coupling state
remaining Admin blockers
nextHostFolderStarted=false
workflowStop=USER_REVIEW_HOST_ADMIN_W2_CHECKPOINT

PASS CRITERIA

PASS only if:

moved responsibilities have correct owners;
no coupling merely transferred from Host to Catalog;
migrated endpoints module-owned and ISender/CQRS-backed;
no Endpoints->Infrastructure;
no Catalog->Host;
no Catalog->foreign Application/Infrastructure/Domain;
semantic Contracts and capability-first shallow foldering satisfied;
canonical Result/error/localization path satisfied;
validator classification exhaustive;
routes/status/JSON/auth preserved;
path↔namespace exact;
exact Host/Admin final inventory recorded;
no unrelated Admin wave started;
no schema/migration/frontend change;
focused builds/tests/guards pass;
task/evidence/SoT persisted;
commit pushed;
HEAD == origin/main;
working tree clean;
next Host folder not started.

If StoreAppearance cannot be safely evacuated without broader redesign, PASS may cover the completed Quantity slice only when StoreAppearance is explicitly NOT_MOVED/DEFERRED; do not falsely claim full W2 closure.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W2
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W1-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Quantity-Settings-State:
Store-Appearance-State:
Catalog-Endpoint-Ownership-State:
Catalog-CQRS-State:
Validator-Coverage-State:
Result-Pipeline-State:
Catalog-Error-Localization-State:
Admin-Authorization-State:
Endpoints-To-Infrastructure-State:
Catalog-To-Host-State:
Host-Storefront-Coupling-State:
Foreign-Module-Dependency-State:
Semantic-Contracts-State:
Capability-Foldering-State:
Route-Parity-State:
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
Do not start W3.
Do not inspect/start another Host folder.
Wait for Architect review.

END_TOOBA_TASK