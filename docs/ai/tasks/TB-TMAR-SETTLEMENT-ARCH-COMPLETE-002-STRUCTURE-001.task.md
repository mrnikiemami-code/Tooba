PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SETTLEMENT_ARCH_COMPLETE_002_STRUCTURE
Title: Structure-certify Settlement under ARCH-COMPLETE-002
Backend-Only: YES

Architect verdict

TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 is ARCHITECT-ACCEPTED.

Accepted commits:

validator implementation: 0cc4b52d59f0fa124c9844efbabd0d079df8c16e
focused architecture-folder allowance follow-up: a1d5f5bd52ec12fdbf79b268dc9205ab38eb65b1

Accepted state:

Settlement = COMPLETE_REFERENCE_PATTERN / HTTP_OWNING / MODULE_ENDPOINTS / MEDIATR_12_5
endpoint-reachable requests = exactly 10
validators required = 4
validators present = 4
no-validator-required = 6
4 AUTH_SCOPED_QUERY
2 NO_INPUT
central AddToobaCqrsFoundation/AddValidatorsFromAssembly discovery is proven
no direct validator invocation
Settlement -> Host = ZERO
Host Settlement residue = exactly two approved thin security adapters
path/namespace audit found no mismatch
no alias/type-forwarding debt found
Settlement is still NOT ARCH-COMPLETE-002 certified
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true
One objective only

Close Settlement structure certification under ARCH-COMPLETE-002.

This task is structure/guard/manifest/SoT closure only.

Do NOT:

redesign Settlement behavior
add more validators
move business logic
change payout/grid semantics
change contracts/events
change schema/migrations
reopen Host ownership
touch Payment
touch Fulfillment/Returns/Notification/Support/Wallet/Promotion
resume Checkout
touch frontend
ARCH-COMPLETE-002 locks

Certification must satisfy:

APPLICATION_CAPABILITY_FOLDERS
ENDPOINTS_CAPABILITY_FOLDERS
INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS
PATH_NAMESPACE_ALIGNMENT
ROOT_ALLOWLIST
NO_NAMESPACE_ALIAS_WORKAROUND

Preserve:

HOST-MODULE-ENDPOINT-001
ARCH-CQRS-001/002
A. Live physical structure

Use current Settlement production layout as source of truth.

Expected Application top-level folders:

Commands
Queries
Errors
Models
Ports
Validators

Allowed Application root .cs files:

GlobalUsings.Domain.cs
GlobalUsings.Layout.cs

Expected Endpoints top-level folders:

Admin
Seller

Allowed Endpoints root .cs file:

SettlementEndpointModule.cs

Expected Infrastructure top-level folders:

Adapters
Bridges
DependencyInjection
Directories
Errors
Gateways
Handlers
Messaging
Observability
Persistence
Queries

Allowed Infrastructure root .cs files:

GlobalUsings.Domain.cs
GlobalUsings.Layout.cs

Do not create ceremonial folders.
Do not move files that already satisfy the structure.

B. Exact path <-> namespace guard

Strengthen:
src/backend/Modules/Settlement/Tooba.Settlement.Tests/Architecture/SettlementArchitectureGuardTests.cs

Replace loose StartsWith namespace acceptance with exact path-derived namespace equality.

Apply to Settlement production .cs files across:

Domain
Contracts
Application
Infrastructure
Endpoints

Rules:

physical path must map to exact namespace
project-root non-GlobalUsing files must use exact project-root namespace
legitimate EF Persistence/Migrations + model snapshot exemption only
GlobalUsings files are handled by the dedicated global-using guard
no prefix-only pass

If a real mismatch is found:
repair only that exact mismatch.

C. Explicit root allowlists / forbidden flattened files

Add explicit root guard coverage.

Tooba.Settlement.Application

rootAllowlist:

GlobalUsings.Domain.cs
GlobalUsings.Layout.cs

forbiddenRootFiles include:

SettlementContracts.cs
SettlementCommands.cs
SettlementQueries.cs
SettlementHandlers.cs
SettlementErrorCodes.cs
SettlementAdminModels.cs
RequestSellerPayoutCommand.cs
QueryAdminPayoutGridQuery.cs
Tooba.Settlement.Endpoints

rootAllowlist:

SettlementEndpointModule.cs

forbiddenRootFiles include:

SettlementSellerEndpoints.cs
SettlementAdminEndpoints.cs
ISettlementSellerAuthorizer.cs
ISettlementAdminAuthorizer.cs
Tooba.Settlement.Infrastructure

rootAllowlist:

GlobalUsings.Domain.cs
GlobalUsings.Layout.cs

forbiddenRootFiles include:

SettlementModule.cs
SettlementDbContext.cs
SettlementDirectory.cs
SettlementOutboxRegistration.cs
SettlementEventHandlers.cs
AdminPayoutGridQueryEngine.cs

Guard must fail if future capability files are flattened into root.

D. GlobalUsings / alias-workaround guard

Current approved GlobalUsings are exactly:

Application/GlobalUsings.Domain.cs:

Tooba.Settlement.Domain.Aggregates
Tooba.Settlement.Domain.Entities
Tooba.Settlement.Domain.Events
Tooba.Settlement.Domain.ValueObjects

Application/GlobalUsings.Layout.cs:

Tooba.Settlement.Application.Ports

Infrastructure/GlobalUsings.Domain.cs:

Tooba.Settlement.Domain.Aggregates
Tooba.Settlement.Domain.Entities
Tooba.Settlement.Domain.Events
Tooba.Settlement.Domain.ValueObjects

Infrastructure/GlobalUsings.Layout.cs:

Tooba.Settlement.Application.Ports
Tooba.Settlement.Infrastructure.Directories
Tooba.Settlement.Infrastructure.DependencyInjection
Tooba.Settlement.Infrastructure.Messaging
Tooba.Settlement.Infrastructure.Handlers
Tooba.Settlement.Infrastructure.Observability
Tooba.Settlement.Infrastructure.Bridges
Tooba.Settlement.Infrastructure.Gateways

These are legitimate project-wide imports and may remain.

Guard must reject:

any foreign module Application/Infrastructure/Domain global using
any alias assignment used to hide path debt
TypeForwardedTo
compatibility shim namespace preserving old flattened paths

Do NOT add new GlobalUsings.

E. Validator coverage becomes certification prerequisite

Reuse and preserve:
SettlementValidatorCoverageGuardTests

Certification must preserve exactly:

Endpoint-reachable = 10

VALIDATOR_REQUIRED = 4:

RequestSellerPayoutCommand
ProcessAdminPayoutCommand
RetryAdminPayoutCommand
QueryAdminPayoutGridQuery

NO_VALIDATOR_REQUIRED = 6:

4 AUTH_SCOPED_QUERY
2 NO_INPUT

Required validators present = 4

No validator may exist for the six no-validator-required requests.

Also preserve:

all 10 are IRequest
real handlers exist
endpoints use ISender
no direct validator invocation
MediatR = 12.5.0

Do not create ceremonial validators.

F. Host ownership lock

Strengthen Settlement guard to prove exact Host residue.

Allowed thin Host Settlement security adapters:

HostSettlementAdminAuthorizer.cs
HostSettlementSellerAuthorizer.cs

Allowed non-authority composition/dev/migration references:

Program.cs
Composition/ToobaModuleComposition.cs
Tooba.MigrationRunner/ModuleMigrationRegistry.cs
Development/MarketplaceDevelopmentBootstrap.cs

SettlementDbContext in Host must remain limited to the already accepted migration/dev allowlist.

Guard must prove:

no Host Settlement endpoints
no Host Settlement grid engine
no Host Settlement panel composer
no Settlement business service/runtime owner in Host
Settlement -> Host = ZERO

Do not move/delete the two approved security adapters.

G. Cross-module boundary lock

Preserve and guard:

Settlement.Domain:

no foreign module dependency

Settlement.Application:

Settlement.Domain + approved Contracts-only foreign boundaries
no foreign Application/Infrastructure/Domain
no Host

Settlement.Infrastructure:

approved Contracts-only dependencies such as Order.Contracts / Payment.Contracts / Returns.Contracts / Party.Contracts
no foreign Application/Infrastructure/Domain
no foreign DbContext
no Host

Settlement.Endpoints:

Settlement.Application + BuildingBlocks only
no Settlement.Infrastructure
no Host
no DbContext

No boundary redesign in this task.

H. Structure manifest

Update:
docs/architecture/tmar-module-structure-manifests.json

Add Settlement:

module = Settlement
structureCertified = true
lockVersion = ARCH-COMPLETE-002

Projects:

Tooba.Settlement.Application

rootAllowlist:

GlobalUsings.Domain.cs
GlobalUsings.Layout.cs

rootAllowlistJustification:
Both are genuine project-wide Settlement imports only; no foreign module Application/Infrastructure/Domain coupling and no namespace-workaround behavior.

forbiddenRootFiles:

SettlementContracts.cs
SettlementCommands.cs
SettlementQueries.cs
SettlementHandlers.cs
SettlementErrorCodes.cs
SettlementAdminModels.cs
RequestSellerPayoutCommand.cs
QueryAdminPayoutGridQuery.cs

forbiddenTopLevelFolders: []

Tooba.Settlement.Endpoints

rootAllowlist:

SettlementEndpointModule.cs

forbiddenRootFiles:

SettlementSellerEndpoints.cs
SettlementAdminEndpoints.cs
ISettlementSellerAuthorizer.cs
ISettlementAdminAuthorizer.cs

forbiddenTopLevelFolders: []

Tooba.Settlement.Infrastructure

rootAllowlist:

GlobalUsings.Domain.cs
GlobalUsings.Layout.cs

rootAllowlistJustification:
Both are genuine project-wide Settlement Domain/Application-port/internal integration imports; no foreign module Application/Infrastructure/Domain coupling and no alias workaround.

forbiddenRootFiles:

SettlementModule.cs
SettlementDbContext.cs
SettlementDirectory.cs
SettlementOutboxRegistration.cs
SettlementEventHandlers.cs
AdminPayoutGridQueryEngine.cs

forbiddenTopLevelFolders: []

Remove Settlement from:
uncertifiedHttpOwningModules

Do not alter other module manifest entries except exact certified-set expectations.

I. SoT certification closure

Update:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs
src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs

Record Settlement:

state = COMPLETE_REFERENCE_PATTERN
httpApplicability = HTTP_OWNING
endpointOwnership = MODULE_ENDPOINTS
cqrs = MEDIATR_12_5
structureCertifiedUnderArchComplete002 = true
validatorCoverage = COMPLETE_4_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED
endpointReachableRequests = 10
workerInternalRequests = 0
pathNamespace = EXACT
rootAllowlist = ENFORCED
aliasWorkaround = NONE
hostResidue = TWO_THIN_HOST_SECURITY_ADAPTERS_ONLY
settlementToHostDependency = ZERO
manifestCertified = true

Add Settlement to:
structureLock.certifiedModules

Expected certified set:

Order
Cart
StoreContext
Offer
Payment
Settlement

Set pending Architect review:
nextTask = USER_REVIEW_SETTLEMENT_ARCH_COMPLETE_002_STRUCTURE_001
nextTaskGate = USER_REVIEW_REQUIRED_AFTER_SETTLEMENT_STRUCTURE_CERTIFICATION

Do NOT auto-select another module.

Preserve:

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true
J. Evidence

Create:
docs/evidence/TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001/settlement-structure-certification.md

Record concisely:

accepted parent commit(s)
exact live folder set
exact namespace proof
root allowlists
GlobalUsings justification
validator 10 / 4 / 6 inventory
Host residue state
Settlement -> Host ZERO
cross-module boundary state
manifest change
certified module set
focused validation results
Checkout/frontend preservation
K. FAST-VALIDATION-BUDGET

Run ONLY:

SettlementArchitectureGuardTests
SettlementValidatorCoverageGuardTests
TmarCompleteReferenceStructureGateTests
TmarDurableGuardTests
build Settlement test project:
dotnet build src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj --no-restore
build Host test project:
dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --no-restore

Do NOT run:

full Settlement tests
full Host tests
broad TMAR suites
solution tests
solution build
Testcontainers
DB integration tests
retries

If any focused test hangs:
return INCOMPLETE and STOP.
Do not retry-loop.

PASS criteria

PASS only if:

Settlement satisfies all ARCH-COMPLETE-002 structure locks
exact path/namespace equality is enforced
explicit root allowlists are enforced
GlobalUsings remain only the approved project-wide imports
alias workaround/type forwarding is absent
10 endpoint requests remain exhaustively inventoried
4/4 required validators remain present
6 no-validator-required requests remain validator-free
MediatR remains 12.5.0
endpoints remain ISender-only
Host residue remains exactly two thin security adapters
Settlement -> Host remains ZERO
cross-module boundaries remain Contracts-only where approved
no schema/migration change
manifest certifies Settlement
Settlement removed from uncertifiedHttpOwningModules
certified set becomes Order, Cart, StoreContext, Offer, Payment, Settlement
Checkout remains paused
frontend untouched
all four focused guard groups pass
both test projects build
no broad suite was run
Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Precert-State:
Application-Structure-State:
Endpoints-Structure-State:
Infrastructure-Structure-State:
Path-Namespace-State:
Root-Allowlist-State:
GlobalUsings-State:
Alias-Workaround-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
No-Validator-Required-Count:
Validator-Coverage-State:
MediatR-State:
ISender-State:
Host-Settlement-Residue-State:
Settlement-To-Host-Dependency-State:
CrossModule-Boundary-State:
Manifest-State:
Uncertified-List-State:
Structure-Lock-Certified-Modules:
Settlement-Structure-Certification-State:
Focused-Settlement-Architecture-Guards:
Focused-Validator-Coverage-Guard:
Focused-TMAR-Structure-Gate:
Focused-TMAR-Durable-Guard:
Settlement-Test-Project-Build:
Host-Test-Project-Build:
Checkout-State:
Frontend-Production-Changes:
Recovery-State:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start another module.
Do not run broader tests.
Do not resume Checkout.
Do not touch frontend.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK