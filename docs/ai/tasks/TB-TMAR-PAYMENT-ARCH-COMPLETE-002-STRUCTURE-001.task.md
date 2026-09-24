PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-PAYMENT-PRECERT-VALIDATION-002
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: PAYMENT_ARCH_COMPLETE_002_STRUCTURE
Title: Structure-certify Payment under ARCH-COMPLETE-002
Backend-Only: YES

Architect verdict

TB-TMAR-PAYMENT-PRECERT-VALIDATION-002 is ARCHITECT-ACCEPTED at:
659d4d06ab7bdce2a2c00b85d16bd308e1c0cf7d

Verified accepted state:

Payment endpoint inventory = exactly 16

15/15 VALIDATOR_REQUIRED validators present

1/1 NO_VALIDATOR_REQUIRED_NO_INPUT present

worker-only ReconcileStalePaymentsCommand remains NO_VALIDATOR_REQUIRED_INTERNAL_WORKER

existing AddValidatorsFromAssembly / AddToobaCqrsFoundation discovery preserved

MediatR = 12.5.0

focused directory decomposition remains accepted

Payment -> Host dependency = ZERO

Host Payment residue = exactly two approved thin security adapters

PaymentContractBridge intact

no schema/migration change

Payment still NOT ARCH-COMPLETE-002 structure-certified

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

One objective only

Close Payment structure certification under ARCH-COMPLETE-002.

This task is STRUCTURE/GUARD/MANIFEST/SoT closure only.

Do NOT:

redesign Payment behavior

add new validators unless a certification guard proves the accepted 15-validator inventory is incomplete

refactor PaymentDirectory again

change Payment contracts/events

change schema/migrations

reopen Host residue work

touch Settlement/Fulfillment/Returns/Notification/Support/Wallet/Promotion

resume Checkout

touch frontend

ARCH-COMPLETE-002 locks

Certification must satisfy exactly:

APPLICATION_CAPABILITY_FOLDERS

ENDPOINTS_CAPABILITY_FOLDERS

INFRASTRUCTURE_CAPABILITY_INTEGRATION_FOLDERS

PATH_NAMESPACE_ALIGNMENT

ROOT_ALLOWLIST

NO_NAMESPACE_ALIAS_WORKAROUND

And preserve:

HOST-MODULE-ENDPOINT-001

ARCH-CQRS-001/002

A. Physical structure audit-in-place

Use the current live Payment folder set as the authority.

Expected top-level production folders:

Application:

Commands

Queries

Errors

Models

Orchestration

Ports

Validators

Endpoints:

Admin

Errors

Storefront

Webhooks

Infrastructure:

Adapters

DependencyInjection

Directories

Events

Messaging

Persistence

Providers

Workers

Do not create ceremonial folders.
Do not move files just to match an old audit snapshot if current path/namespace is already correct.

B. Exact path <-> namespace guard

Strengthen:
src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentArchitectureGuardTests.cs

Replace any loose namespace-prefix/StartsWith acceptance with exact path-derived namespace equality for Payment production .cs files.

Rules:

physical folder path must map exactly to namespace

project-root files must match project root namespace only if explicitly allowed

EF generated migrations/model snapshot may keep their legitimate migrations namespace exemption

no file may pass merely because namespace starts with the expected prefix

Examples:

Application/Validators/Admin/* => Tooba.Payment.Application.Validators.Admin

Application/Validators/Storefront/* => Tooba.Payment.Application.Validators.Storefront

Application/Validators/Webhooks/* => Tooba.Payment.Application.Validators.Webhooks

Infrastructure/Directories/Shared/* => Tooba.Payment.Infrastructure.Directories.Shared

Infrastructure/Workers/* => Tooba.Payment.Infrastructure.Workers

Endpoints/Webhooks/* => Tooba.Payment.Endpoints.Webhooks

If a real mismatch is found:
repair only that exact mismatch.
Do not reorganize unrelated code.

C. Root allowlist + forbidden root files

Add explicit Payment root rules matching the ARCH-COMPLETE-002 manifest.

Tooba.Payment.Application

rootAllowlist: []

forbiddenRootFiles:

PaymentContracts.cs

PaymentHandlers.cs

PaymentRequests.cs

PaymentQueries.cs

PaymentQueryHandlers.cs

StorefrontPaymentOrchestrator.cs

PaymentErrorCodes.cs

Tooba.Payment.Endpoints

rootAllowlist:

PaymentEndpointModule.cs

forbiddenRootFiles:

PaymentStorefrontEndpoints.cs

PaymentAdminEndpoints.cs

PaymentWebhookEndpoints.cs

IPaymentStorefrontAuthorizer.cs

IPaymentAdminAuthorizer.cs

IPaymentAdminGridQueryNormalizer.cs

PaymentErrorCatalogContributor.cs

PaymentErrorResources.cs

PaymentEndpointLocalizer.cs

Tooba.Payment.Infrastructure

rootAllowlist: []

forbiddenRootFiles:

PaymentModule.cs

PaymentDbContext.cs

PaymentDirectory.cs

PaymentAdminDirectory.cs

PaymentReconciliationDirectory.cs

PaymentExpiryDirectory.cs

PaymentEvents.cs

PaymentOutboxRegistration.cs

PaymentReconciliationWorker.cs

PaymentReconciliationOptions.cs

PaymentGatewayRegistry.cs

Guard must fail if a future capability file is flattened back to project root.

D. Alias-workaround guard

Enforce NO_NAMESPACE_ALIAS_WORKAROUND.

Reject:

project/global aliases used to disguise a wrong physical namespace

aliases to foreign Application/Infrastructure/Domain namespaces

type-forwarding/compatibility shims used to preserve old Payment paths

Current:
Tooba.Payment.Application/Orchestration/GlobalUsings.cs
contains:
global using Tooba.Payment.Application.Orchestration;

This exact self-namespace import is not a namespace-alias workaround and may remain only if:

it hides no physical path mismatch

it imports no foreign module layer

evidence explicitly records the exception

the guard is narrow enough that adding another unrelated/foreign global alias fails

Do NOT create new aliases.

E. Validator coverage is now a certification prerequisite, not new work

Use the already-landed complete guard:
PaymentValidatorCoverageGuardTests

Certification must preserve:

endpoint-reachable requests = 16

validator-required = 15

validators present = 15

no-validator-required = 1

worker-only request = ReconcileStalePaymentsCommand

worker-only classification = NO_VALIDATOR_REQUIRED_INTERNAL_WORKER

all endpoint requests = IRequest

endpoints = ISender-based

no direct validator invocation

MediatR = 12.5.0

Do NOT replace this with representative/sample checks.
Do NOT merge business validation into FluentValidation.

F. Cross-module and Host boundaries

Structure guard must continue proving:

Payment.Domain -> no foreign module dependency

Payment.Contracts -> no foreign module Application/Infrastructure/Domain

Payment.Application foreign dependencies are Contracts-only where already approved

Payment.Infrastructure -> no foreign Application/Infrastructure/Domain

Payment.Infrastructure -> no Tooba.Host

Payment.Endpoints -> no Payment.Infrastructure / Host business coupling

no foreign DbContext

Payment -> Host = ZERO

Host Payment residue must remain exactly:

HostPaymentAdminAuthorizer

HostPaymentStorefrontAuthorizer

No Payment reconciliation worker/options/grid policy may return to Host.

G. Directory decomposition lock

Preserve and guard:

PaymentDirectory implements only IPaymentDirectory

PaymentAdminDirectory implements only IPaymentAdminDirectory

PaymentReconciliationDirectory implements only IPaymentReconciliationDirectory

PaymentExpiryDirectory implements only IPaymentExpiryDirectory

no class implements more than one of these four ports

no cast-based directory registration

PaymentDirectory remains <700 physical LOC

no Payment Directories production file >=800 LOC

Shared contains only the approved narrow collaborators unless a separately approved future task changes it

runtime DI proof remains accepted; do not re-run broad DI/integration tests here

Do NOT refactor these classes in this certification task unless a structure guard fails on an actual certification rule.

H. Structure manifest

Update:
docs/architecture/tmar-module-structure-manifests.json

Add Payment:

module = Payment

structureCertified = true

lockVersion = ARCH-COMPLETE-002

Projects:

Tooba.Payment.Application

rootAllowlist: []
forbiddenRootFiles:

PaymentContracts.cs

PaymentHandlers.cs

PaymentRequests.cs

PaymentQueries.cs

PaymentQueryHandlers.cs

StorefrontPaymentOrchestrator.cs

PaymentErrorCodes.cs
forbiddenTopLevelFolders: []

Tooba.Payment.Endpoints

rootAllowlist:

PaymentEndpointModule.cs
forbiddenRootFiles:

PaymentStorefrontEndpoints.cs

PaymentAdminEndpoints.cs

PaymentWebhookEndpoints.cs

IPaymentStorefrontAuthorizer.cs

IPaymentAdminAuthorizer.cs

IPaymentAdminGridQueryNormalizer.cs

PaymentErrorCatalogContributor.cs

PaymentErrorResources.cs

PaymentEndpointLocalizer.cs
forbiddenTopLevelFolders: []

Tooba.Payment.Infrastructure

rootAllowlist: []
forbiddenRootFiles:

PaymentModule.cs

PaymentDbContext.cs

PaymentDirectory.cs

PaymentAdminDirectory.cs

PaymentReconciliationDirectory.cs

PaymentExpiryDirectory.cs

PaymentEvents.cs

PaymentOutboxRegistration.cs

PaymentReconciliationWorker.cs

PaymentReconciliationOptions.cs

PaymentGatewayRegistry.cs
forbiddenTopLevelFolders: []

Remove Payment from:
uncertifiedHttpOwningModules

Do NOT modify certification entries for other modules except the exact expected certified-set assertion.

I. SoT certification closure

Update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs

src/backend/Host/Tooba.Host.Tests/Architecture/TmarCompleteReferenceStructureGateTests.cs

Record Payment:

state = COMPLETE_REFERENCE_PATTERN

httpApplicability = HTTP_OWNING

endpointOwnership = MODULE_ENDPOINTS

cqrs = MEDIATR_12_5

structureCertifiedUnderArchComplete002 = true

validatorCoverage = COMPLETE_15_OF_15_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED

endpointReachableRequests = 16

workerOnlyRequest = ReconcileStalePaymentsCommand_NO_VALIDATOR_REQUIRED_INTERNAL_WORKER

pathNamespace = EXACT

rootAllowlist = ENFORCED

aliasWorkaround = NONE

paymentDirectoryArchitecture = FOCUSED_DIRECTORIES_PAYMENT_RECONCILIATION_ADMIN_EXPIRY

hostResidue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY

paymentToHostDependency = ZERO

Add Payment to:
structureLock.certifiedModules

Expected certified set:

Order

Cart

StoreContext

Offer

Payment

Set this task pending Architect review as:
nextTask = USER_REVIEW_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001
nextTaskGate = USER_REVIEW_REQUIRED_AFTER_PAYMENT_STRUCTURE_CERTIFICATION

Do NOT auto-select the next module.

Preserve:

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

J. Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001/payment-structure-certification.md

Keep evidence concise and factual:

accepted parent commit

exact live folder set

exact path/namespace proof

root allowlists

alias-workaround result

16 / 15 / 1 validator inventory

worker-only exclusion

directory decomposition preservation

Host residue state

Payment -> Host ZERO

manifest change

certified module set

focused validation commands/results

Checkout/frontend preservation

K. FAST-VALIDATION-BUDGET

This certification must remain deterministic and bounded.

Run ONLY these focused validations:

Payment physical/architecture guard tests:
filter to PaymentArchitectureGuardTests

Complete Payment validator coverage guard:
filter to PaymentValidatorCoverageGuardTests

TMAR structure gate only:
filter to TmarCompleteReferenceStructureGateTests

TMAR durable recovery guard only:
filter to TmarDurableGuardTests

Build only the two directly affected test projects, no solution build:

dotnet build src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-restore

dotnet build src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --no-restore

Do NOT run:

full Tooba.Payment.Tests

full Host tests

full TMAR suite

full solution tests

full solution build

Testcontainers

database integration tests

broad regression suites

repeated retries

If a focused test hangs, exceeds the normal focused-test duration, or requires unrelated infrastructure:
return INCOMPLETE with exact blocker and STOP.
Do not retry-loop.

PASS criteria

PASS only if:

Payment physical folders satisfy ARCH-COMPLETE-002

path <-> namespace equality is exact

root allowlists are explicit and enforced

no namespace-alias workaround exists

current self-namespace GlobalUsings exception, if retained, is narrowly justified and guarded

16 endpoint requests remain exhaustively classified

15/15 required validators remain present

1 no-input request remains explicitly NO_VALIDATOR_REQUIRED

worker-only reconcile request remains excluded correctly

MediatR remains 12.5.0

endpoints remain ISender-only

Payment directory split remains intact

Payment -> Host remains ZERO

Host Payment residue remains exactly two approved thin security adapters

no schema/migration change

manifest certifies Payment

Payment removed from uncertifiedHttpOwningModules

structureLock certified set becomes Order, Cart, StoreContext, Offer, Payment

Payment is recorded STRUCTURE_CERTIFIED under ARCH-COMPLETE-002

Checkout remains W5 paused

frontend untouched

all four focused guard groups pass

both affected test projects build

no broad test suite was run

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001
Parent-Task: TB-TMAR-PAYMENT-PRECERT-VALIDATION-002
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Validation-002-State:
Application-Structure-State:
Endpoints-Structure-State:
Infrastructure-Structure-State:
Path-Namespace-State:
Root-Allowlist-State:
Alias-Workaround-State:
GlobalUsings-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
No-Validator-Required-Count:
Validator-Coverage-State:
Worker-Only-Request-State:
MediatR-State:
ISender-State:
Directory-Decomposition-State:
Payment-To-Host-Dependency-State:
Host-Payment-Residue-State:
CrossModule-Boundary-State:
Manifest-State:
Uncertified-List-State:
Structure-Lock-Certified-Modules:
Payment-Structure-Certification-State:
Focused-Payment-Architecture-Guards:
Focused-Validator-Coverage-Guard:
Focused-TMAR-Structure-Gate:
Focused-TMAR-Durable-Guard:
Payment-Test-Project-Build:
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
Do not resume Checkout.
Do not touch frontend.
Do not run broader tests.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK