PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001
Parent-Task: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: PAYMENT_HOST_RESIDUE_REPAIR
Title: Move Payment reconciliation and grid policy out of Host
Backend-Only: YES

Architect verdict on audit

TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001 is ARCHITECT-ACCEPTED at:

dcb8b416b19d8f9db90e8162754723e4cfbbfb14

Verified closed decisions:

HostPaymentAdminAuthorizer = KEEP thin security adapter

HostPaymentStorefrontAuthorizer = KEEP thin security adapter

HostPaymentAdminGridQueryNormalizer = MOVE_TO_PAYMENT_MODULE

PaymentReconciliationHostOptions = MOVE_TO_PAYMENT_MODULE

PaymentReconciliationHostedService = MOVE_TO_PAYMENT_MODULE

Payment structure certification remains separate

16 HTTP requests + 1 worker request audited

15 HTTP validators missing, deferred to structure task

PaymentDirectory / bridge naming / error debt deferred to structure task

zero Payment/Host production code changed by audit

One objective only

Remove Payment-specific runtime ownership from Host:

Payment reconciliation worker + options become Payment-owned.

Admin Payment grid whitelist/normalization becomes Payment-owned.

Host retains only the two explicitly approved thin security adapters.

Do NOT structure-certify Payment here.
Do NOT add the 15 validators here.
Do NOT refactor PaymentDirectory here.
Do NOT rename PaymentHostContractBridge here.
Do NOT repair error-message debt here.
Do NOT touch Settlement.
Do NOT resume Checkout.
Do NOT touch frontend.

A. Reconciliation ownership

Delete from Host:

src/backend/Host/Tooba.Host/PaymentReconciliationHostedService.cs

src/backend/Host/Tooba.Host/PaymentReconciliationHostOptions.cs

Create Payment-owned worker capability:

src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Workers/PaymentReconciliationWorker.cs

src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Workers/PaymentReconciliationOptions.cs

Namespaces must exactly match paths:

Tooba.Payment.Infrastructure.Workers

PaymentReconciliationOptions

Own canonical section:

Tooba:PaymentReconciliation

Expose:

Enabled

PollIntervalSeconds

PendingAgeMinutes

BatchSize

Preserve current defaults unless existing canonical configuration says otherwise:

Enabled = true

PollIntervalSeconds = 60

PendingAgeMinutes = 5

BatchSize = 20

Add:
public const string SectionName = "Tooba:PaymentReconciliation";

Configuration validation / normalization

Payment owns its Payment-specific scheduling policy.

Required fail-safe shape:

PollIntervalSeconds > 0

PendingAgeMinutes >= 1

BatchSize > 0

Do not silently introduce unrelated limits.

If current system convention is IValidateOptions<T>, use it.
Otherwise normalize only at one Payment-owned boundary and test it explicitly.

Do NOT leave Math.Max(...) business-policy clamping spread through the worker if it can be centralized.

B. PaymentReconciliationWorker

Move the existing behavior into Payment.Infrastructure.

Preserve:

BackgroundService lifecycle

per-target iteration

generic worker target source

per-target commerce context assignment

scoped ISender

ReconcileStalePaymentsCommand

worker registry success/failure reporting

PaymentGatewayInstrumentation reconciliation telemetry

cancellation behavior

current logging intent

But Host must no longer own:

Payment poll cadence

PendingAgeMinutes

BatchSize

command construction

reconciliation telemetry

Generic platform seams

The Payment worker may depend ONLY on existing generic platform abstractions, never Host concrete types.

Use the generic interfaces already audited, such as:

IOutboxPollTargetSource

IWorkerCommerceContextFactory

IBackgroundWorkerRegistry

If the exact current interface name differs, use the existing generic interface from BuildingBlocks/Persistence; do not create a Host reference and do not create a Payment-specific copy.

Payment.Infrastructure must have ZERO reference to Tooba.Host.

Commerce context

Do not use WorkerCommerceContextFactory concrete Host type.

Use the existing generic worker commerce-context seam discovered in the audit/repository.

If no generic seam can supply the exact context needed, STOP with INCOMPLETE instead of introducing a Payment -> Host dependency.

C. PaymentModule owns registration

Edit:

src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs

PaymentModule must own:

PaymentReconciliationOptions configuration binding

options validation if used

AddHostedService<PaymentReconciliationWorker>()

Remove duplicate registration lines already visible in PaymentModule while touching the file:

duplicated IPaymentGatewayCatalogPort -> PaymentGatewayCatalogAdapter

duplicated IPaymentWebhookSignatureVerifier -> PaymentWebhookSignatureVerifierAdapter

This is safe registration cleanup only; no behavior change.

Also clean the malformed adjacent using line if still present:
using Tooba.Payment.Contracts.Returns;using ...

No unrelated DI rewrite.

D. Host Program cleanup

Edit:
src/backend/Host/Tooba.Host/Program.cs

Remove Payment-specific Host registration/configuration for:

PaymentReconciliationHostOptions

PaymentReconciliationHostedService

HostPaymentAdminGridQueryNormalizer

Keep generic platform worker services in Host if they are platform-wide:

BackgroundWorkerRegistry

generic target source

generic worker context implementation

Keep:

HostPaymentAdminAuthorizer

HostPaymentStorefrontAuthorizer

Do NOT move those two.

Host should no longer mention:

PaymentReconciliationHostOptions

PaymentReconciliationHostedService

HostPaymentAdminGridQueryNormalizer

E. Payment-owned Admin grid normalization

Delete:

src/backend/Host/Tooba.Host/Admin/HostPaymentAdminGridQueryNormalizer.cs

Keep the existing Payment-owned interface:

src/backend/Modules/Payment/Tooba.Payment.Endpoints/Admin/IPaymentAdminGridQueryNormalizer.cs

Update its XML documentation so it no longer says Host implements it.

Create:

src/backend/Modules/Payment/Tooba.Payment.Endpoints/Admin/PaymentAdminGridQueryNormalizer.cs

Namespace:
Tooba.Payment.Endpoints.Admin

Implement IPaymentAdminGridQueryNormalizer.

The Payment module owns the whitelist and normalization behavior.

Preserve the exact existing Payment field policy:

reference : Text, searchable

customer : Text, searchable

amount : Number

status : Enum

supply : Enum

reservation : Enum

provider : Text, searchable

created : Date

completed : Date

Preserve:

default sort = created

default direction = existing canonical behavior (desc if current Host policy produces it)

tie-break = reference

existing paging/search/filter/operator normalization behavior

Important layering rule

Do NOT reference:

Tooba.Host.Grid

AdminListGridPolicies

AdminReceiptListItem

Host generic grid implementation classes

Use only:

Tooba.BuildingBlocks.Grid

Payment-owned DTO/request shapes

If BuildingBlocks lacks one convenience required by the old Host policy, implement the smallest Payment-owned normalizer logic in this class. Do not move Host grid engine code wholesale.

F. Endpoint registration

Edit:

src/backend/Modules/Payment/Tooba.Payment.Endpoints/PaymentEndpointModule.cs

Register the default module-owned normalizer in:

AddPaymentEndpointPresentation(...)

Preferred:
services.AddSingleton<IPaymentAdminGridQueryNormalizer, PaymentAdminGridQueryNormalizer>();

Use Scoped only if implementation is not stateless.

Host must no longer register this port.

G. Remove Host Payment grid whitelist

Edit:

src/backend/Host/Tooba.Host/Grid/AdminListGridPolicies.cs

Delete ONLY the Payments policy block.

Do not alter unrelated:

Orders

Sellers

Content

Reviews

Stories

If AdminReceiptListItem exists only for this Payment grid policy and is now dead, remove it only if the compiler/reference search proves zero remaining production uses.
Otherwise leave it and report it for a later owning-module cleanup.

H. Approved Host adapters

Keep unchanged unless compile-only import cleanup is required:

Host/Admin/HostPaymentAdminAuthorizer.cs

Host/Storefront/HostPaymentStorefrontAuthorizer.cs

Strengthen guard to prove these two contain no:

Payment business service

gateway choice

state transition

reconciliation rule

DB access

response composition

grid field whitelist

I. Tests / guards

Add focused tests for:

Worker/options

disabled worker exits without reconciliation polling

enabled worker dispatches ReconcileStalePaymentsCommand

command uses Payment-owned PendingAgeMinutes and BatchSize

worker uses generic interfaces, not Host concrete types

cancellation path remains clean

options invalid values fail/normalize at one Payment-owned boundary

PaymentModule registers worker/options

Grid

module-owned normalizer accepts canonical Payment fields

rejects unknown field

preserves paging normalization

preserves default sort created

preserves filter operators by field kind

Host AdminListGridPolicies has no Payments policy

Host normalizer file is gone

Payment endpoint presentation registers module normalizer

Host residue

Host has no Payment reconciliation implementation/options

Host has no Payment grid whitelist

only Payment-named retained Host files are the two approved thin security adapters, plus generic references in Program/composition as necessary

Payment.Infrastructure has zero Host reference

Strengthen:
src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentArchitectureGuardTests.cs

Do NOT add structure certification assertions yet.

J. Recovery / high-level state

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

durable guard expectations

Stamp audit acceptance:

lastAcceptedTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001

lastAcceptedCommit = dcb8b416b19d8f9db90e8162754723e4cfbbfb14

Record Payment Host state:

paymentHostResidue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY

reconciliationOwnership = PAYMENT_INFRASTRUCTURE_WORKER_AND_OPTIONS

adminGridPolicyOwnership = PAYMENT_ENDPOINTS

hostPaymentSecurityAdapters = HostPaymentAdminAuthorizer,HostPaymentStorefrontAuthorizer

structureCertification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001

Set:
nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001

Do NOT certify Payment yet.
Do NOT advance to Settlement yet.

Preserve:

Cart/Order/StoreContext/Offer certifications

Checkout W5 paused

frontendFrozen = true

K. Evidence

Create:

docs/evidence/TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001/payment-host-residue-repair.md

Record:

audit acceptance

5 residue decisions

worker/options before/after

generic platform seams used

Program cleanup

module registration

grid whitelist before/after

retained security adapters proof

no Payment -> Host dependency

exact validation

recovery state

Validation

Run only:

focused Payment worker/options tests

focused Payment grid normalizer tests

PaymentArchitectureGuardTests

relevant Payment endpoint tests

relevant Host registration/folder guards

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

one final:
dotnet build src/backend/Tooba.slnx

No broad unrelated suites.

PASS criteria

PASS only if:

Payment reconciliation worker lives in Payment.Infrastructure

Payment reconciliation options live in Payment

PaymentModule owns their registration

Host has no Payment reconciliation worker/options

Host admin Payment grid normalizer is gone

Payment owns exact grid whitelist/normalization

AdminListGridPolicies has no Payments policy

only two approved Payment Host security adapters remain

both retained adapters are business-free

Payment has zero Host dependency

runtime behavior is preserved

Payment remains NOT structure-certified

recovery points directly to structure task

Checkout/frontend unchanged

focused tests pass

full build passes

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001
Parent-Task: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-AUDIT-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Audit-Acceptance-State:
Payment-Reconciliation-Worker-State:
Payment-Reconciliation-Options-State:
Generic-Worker-Seams-State:
PaymentModule-Registration-State:
Host-Program-Payment-Runtime-State:
Host-Payment-Grid-Normalizer-State:
Payment-Owned-Grid-Normalizer-State:
AdminListGridPolicies-Payment-State:
HostPaymentAdminAuthorizer-State:
HostPaymentStorefrontAuthorizer-State:
Payment-To-Host-Dependency-State:
Behavior-Parity:
Architecture-Guards:
Focused-Validation:
Full-Build:
Payment-Structure-Certification-State:
Cart-Certification-State:
Order-Certification-State:
StoreContext-Certification-State:
Offer-Certification-State:
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
Do not start structure certification automatically.
Do not start Settlement.
Do not resume Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
