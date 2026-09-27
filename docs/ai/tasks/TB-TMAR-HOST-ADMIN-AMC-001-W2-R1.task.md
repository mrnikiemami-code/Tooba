PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W2-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W2
Parent-Commit: 50ee7da5cbbb46822b900842d89503b434cc93d7
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_BY_FOLDER_ADMIN
Title: Repair W2 Catalog Application semantic structure before continuing Admin AMC

ARCHITECT VERDICT ON W2

W2 is NOT accepted yet.

The bounded Quantity migration is directionally correct and the Host coupling was not transferred, but the migrated Catalog Application surface violates the hardened Analyze/Migrate/Certify rules introduced before this wave.

Verified repository defect at commit:
50ee7da5cbbb46822b900842d89503b434cc93d7

Current invalid files:

Tooba.Catalog.Application/Settings/StoreQuantitySettingsContracts.cs
Tooba.Catalog.Application/Settings/StoreQuantitySettingsHandlers.cs
Tooba.Catalog.Application/Settings/SaveStoreQuantitySettingsCommandValidator.cs

StoreQuantitySettingsContracts.cs mixes:

Application port
read model
Query
Command

This is exactly the generic/mixed Application *Contracts.cs pattern the hardened skills forbid.

Also, Settings/ currently acts as a flat mixed bucket instead of the required capability-first shallow responsibility structure.

REQUIRED SKILLS

Read current:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Read also:

docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/tmar-current-state.json
W2 task/evidence
current Catalog Application/Endpoints/Infrastructure files touched by W2

SCOPE

ONLY repair the W2 Quantity Settings migrated surface and its guards/evidence/SoT.

Do NOT:

start W3
touch StoreAppearance beyond preserving its deferred state
touch another Host/Admin capability
touch another Host folder
redesign behavior
change schema/migrations
touch frontend

MANDATORY TARGET STRUCTURE

Use capability-first shallow-by-default structure.

For the Quantity Settings capability, use an explicit cohesive path, for example:

Plain text
Tooba.Catalog.Application/
  Settings/
    Quantity/
      Commands/
        SaveStoreQuantitySettingsCommand.cs
        SaveStoreQuantitySettingsHandler.cs
      Queries/
        GetStoreQuantitySettingsQuery.cs
        GetStoreQuantitySettingsHandler.cs
      Models/
        StoreQuantitySettingsView.cs
      Ports/
        IStoreQuantitySettingsDirectory.cs
      Validators/
        SaveStoreQuantitySettingsCommandValidator.cs
      Mapping/
        StoreQuantitySettingsViews.cs   // only if needed

Equivalent naming is allowed if capability semantics are clearer, but the following are mandatory:

no *Contracts.cs file in Application
no mixed port + model + command + query file
no generic flat Settings/ dump
no one-folder-per-single-request leaf pattern
exact path↔namespace

HANDLER COHESION

Do not keep unrelated Query and Command handlers bundled together if they have different reasons to change.

Preferred:

GetStoreQuantitySettingsHandler.cs
SaveStoreQuantitySettingsHandler.cs

Mapper/helper may be shared only if it is genuinely shared and cohesive.

SEMANTIC CONTRACTS

Keep:

CatalogErrorCodes in Catalog.Contracts.Errors
Application-internal StoreQuantitySettingsView, Query, Command, port remain Application-owned
no new Contracts DTO/port unless a real cross-module consumer exists

Do not move Application-internal types to Catalog.Contracts just to satisfy naming.

GUARD REPAIR

Update W2 guard so it enforces the hardened rule instead of asserting the invalid legacy shape.

Current bad guard behavior includes checking for:
Settings/StoreQuantitySettingsContracts.cs

Replace with durable assertions that prove:

no *Contracts.cs under Catalog.Application for the touched W2 surface
Quantity capability has shallow responsibility folders
Query and Command each have one authoritative request type
validator targets the authoritative Command
no duplicate legacy request/model shape
path↔namespace exact
no one-file-per-request nested leaf folders
Endpoints still ISender/ApiResponseFactory only
Catalog→Host ZERO
Host/Admin file count remains 58
StoreAppearance remains deferred and unmoved

PRESERVE W2 GOOD STATE

Must remain:

Host/Admin 59 → 58
Quantity Host endpoint deleted
Quantity route module-owned in Catalog.Endpoints
GET and PUT through ISender
canonical Result/ApiResponseFactory
Save validator present
Get NO_VALIDATOR_REQUIRED_NO_INPUT
ICatalogAdminAuthorizer
CatalogErrorCodes + contributor + resx
Endpoints→Infrastructure ZERO
Catalog→Host ZERO
StoreAppearance NOT_MOVED/DEFERRED
no Host.Storefront reference in Catalog
schema/frontend unchanged
no next Host folder

SOT / EVIDENCE

Update W2 evidence and SoT honestly:

W2 remains under repair until this task passes
record semantic-structure repair
do not erase W2 history
workflow stop remains USER_REVIEW_HOST_ADMIN_W2_CHECKPOINT

Create:
docs/evidence/TB-TMAR-HOST-ADMIN-AMC-001-W2-R1/

Required:

semantic-structure-repair.md
foldering.md
validation.md
closure.md

VALIDATION

Focused:

Catalog.Application build
Catalog.Infrastructure build
Catalog.Endpoints build
Host build
Host.Tests build if needed
W1 + W2 + W2-R1 architecture guards
Quantity settings behavior tests
path↔namespace guard
no Application *Contracts.cs guard
no duplicate CQRS request shape guard
no single-request leaf-folder guard

PASS CRITERIA

PASS only if:

ZERO Application *Contracts.cs in W2 Quantity surface
no mixed port/model/request bundle
capability-first shallow responsibility structure
Query/Command authoritative types unique
handler files cohesive
validator targets authoritative Save Command
path↔namespace exact
all W2 good architectural boundaries preserved
StoreAppearance remains deferred without coupling transfer
Host/Admin remains 58
schema/frontend unchanged
no W3 started
focused validation PASS
task/evidence/SoT persisted
commit pushed
HEAD == origin/main
working tree clean

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-AMC-001-W2-R1
Parent-Task: TB-TMAR-HOST-ADMIN-AMC-001-W2
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
W2-Behavior-Preservation-State:
Application-Contracts-Bundle-State:
Quantity-Capability-Foldering-State:
Command-Authority-State:
Query-Authority-State:
Handler-Cohesion-State:
Validator-State:
Path-Namespace-State:
Endpoints-To-Infrastructure-State:
Catalog-To-Host-State:
Store-Appearance-State:
Host-Admin-File-Count:
Result-Pipeline-State:
Error-Localization-State:
Schema-Migration-State:
Frontend-State:
Focused-Validation:
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

After returning:

STOP completely.
Do not start W3.
Do not inspect/start another Host folder.
Wait for Architect review.

END_TOOBA_TASK