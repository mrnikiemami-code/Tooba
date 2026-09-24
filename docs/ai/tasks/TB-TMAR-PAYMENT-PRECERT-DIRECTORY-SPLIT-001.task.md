PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001
Parent-Task: TB-TMAR-PAYMENT-PRECERT-HYGIENE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: PAYMENT_PRECERT_DIRECTORY_DECOMPOSITION
Title: Split PaymentDirectory god-file before ARCH-COMPLETE-002 certification
Backend-Only: YES

Architect verdict

TB-TMAR-PAYMENT-PRECERT-HYGIENE-001 is ARCHITECT-ACCEPTED at:
22849a17e31aa3357277c3979ac351a3f3f9f8e5

Verified:

dead obsolete Payment ports removed

PaymentContractBridge rename complete, no shim

Payment/Wallet expected faults typed; no Payment Message classification

localized Payment Application fallback removed

Payment -> Host dependency remains ZERO

Host Payment residue remains only HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer

Payment still NOT ARCH-COMPLETE-002 certified

Checkout W5 paused; frontend frozen

Why this task exists

Known remaining extraction-readiness defect:
Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs

Current:

971 physical LOC

implements FOUR ports:
IPaymentDirectory,
IPaymentReconciliationDirectory,
IPaymentAdminDirectory,
IPaymentExpiryDirectory

Do not certify Payment while this multi-responsibility god-file remains.

One objective

Behavior-preserving decomposition only.

Do NOT:

add the 15 validators

structure-certify Payment

change MediatR/endpoints/contracts/events

change schema/migrations

redesign Payment behavior

touch Settlement

resume Checkout

touch frontend

Target structure

Under:
src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/

Final classes:

PaymentDirectory

implements ONLY IPaymentDirectory

PaymentReconciliationDirectory

implements ONLY IPaymentReconciliationDirectory

PaymentAdminDirectory

implements ONLY IPaymentAdminDirectory

PaymentExpiryDirectory

implements ONLY IPaymentExpiryDirectory

No façade implementing multiple ports.
No compatibility wrapper.
No type forwarding.

A. PaymentDirectory

Keep only IPaymentDirectory operations:

InitiateAsync

VerifyAsync

GetAsync

GetLatestForCheckoutAsync

HasSucceededPaymentForCheckoutAsync

RegisterProofAssetAsync

SubmitManualEvidenceAsync

RetryManualAfterRejectionAsync

Keep only helpers required by these methods.

Remove from this class:

stale reconciliation scan

admin reads/mutations

cancel/refund admin flow

unpaid expiry/reopen flow

Target:
PaymentDirectory.cs < 700 physical LOC
Prefer <650 naturally; do not compress formatting to game LOC.

B. PaymentReconciliationDirectory

Create:
PaymentReconciliationDirectory.cs

Namespace:
Tooba.Payment.Infrastructure.Directories

Own only:
ReconcileStalePendingAsync

Use:

PaymentDbContext for stale-pending/attempt reads

IPaymentDirectory for canonical VerifyAsync

Preserve exact cutoff/order/batch/attempt/processed-count semantics.
Do not duplicate verification logic.

C. PaymentAdminDirectory

Create:
PaymentAdminDirectory.cs

Namespace:
Tooba.Payment.Infrastructure.Directories

Move:

GetOperationalAsync

GetLatestOperationalForCheckoutAsync

ReconcileAsync

ConfirmDepositAsync

RejectDepositAsync

RestoreDepositAsync

UnconfirmDepositAsync

CloseOrStartRefundForOrderCancelAsync

RestoreAfterOrderCancelRestoreAsync

Move admin-only operational snapshot mapping here.

For ReconcileAsync use IPaymentDirectory.VerifyAsync.
Do not duplicate Verify behavior.

Preserve:

manual state rules

allocation/attempt loading

ContractOperationException codes

typed refund behavior

RefundPending on payment.refund.gateway.unconfigured

IClock/IIdGenerator behavior

no Message classification

D. PaymentExpiryDirectory

Create:
PaymentExpiryDirectory.cs

Namespace:
Tooba.Payment.Infrastructure.Directories

Move:

ExpireDueUnpaidAsync

ReopenExpiredForRetryAsync

Preserve:

transaction boundaries

FOR UPDATE SKIP LOCKED

status filters

timeout ordering

batch semantics

returned CheckoutIds

actor access

gateway initiation

attempt creation

timeout assignment

clock/id behavior

No DB semantic change.

E. Shared implementation

Do NOT create Common/Helpers/Utils/Manager dumping grounds.

If both core and expiry need the exact same actor-access or timeout-assignment implementation, at most create narrowly named internal collaborators under:

Directories/Shared/

Allowed:

PaymentActorAccess.cs

PaymentUnpaidTimeoutAssigner.cs

Namespace:
Tooba.Payment.Infrastructure.Directories.Shared

Use only if they remove real duplication.

F. DI

Edit:
src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs

Final direct registrations:

IPaymentDirectory -> PaymentDirectory

IPaymentReconciliationDirectory -> PaymentReconciliationDirectory

IPaymentAdminDirectory -> PaymentAdminDirectory

IPaymentExpiryDirectory -> PaymentExpiryDirectory

Remove all cast-based registrations such as:
(PaymentDirectory)sp.GetRequiredService<IPaymentDirectory>()

Do not alter unrelated registration behavior.

G. Safety locks

No change to:

SaveChanges placement

DbContext ownership

transaction boundaries

Outbox

idempotency

gateway selection

status transitions

error codes

refund/reconciliation/expiry semantics

contracts/events

No foreign DbContext.
No Payment -> Host dependency.
No new cross-module ACID flow.

H. Guards

Strengthen PaymentArchitectureGuardTests:

PaymentDirectory implements only IPaymentDirectory

PaymentReconciliationDirectory only IPaymentReconciliationDirectory

PaymentAdminDirectory only IPaymentAdminDirectory

PaymentExpiryDirectory only IPaymentExpiryDirectory

no class implements >1 of these four ports

no interface down-cast registration remains

exact path↔namespace for new files

PaymentDirectory <700 LOC

no new Payment production file >=800 LOC

no Common/Helpers/Utils/Manager dumping folder

Payment -> Host ZERO

Host Payment residue remains exactly two approved thin security adapters

no Message classification reintroduced

Do NOT mark structure certification here.

I. Focused tests

Prove:

each port resolves to its focused implementation

all four implementations are distinct

storefront initiation/verification remains green

stale reconciliation still uses canonical Verify path

admin reconcile still uses canonical Verify path

manual admin operations remain green

cancel/refund behavior remains green

unpaid expiry/reopen remains green

PaymentContractBridge still resolves all three Contracts gateways

no schema/migration change

J. Recovery

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

durable guards

Stamp:

lastAcceptedTask = TB-TMAR-PAYMENT-PRECERT-HYGIENE-001

lastAcceptedCommit = 22849a17e31aa3357277c3979ac351a3f3f9f8e5

Record:

paymentDirectoryArchitecture = FOCUSED_DIRECTORIES_PAYMENT_RECONCILIATION_ADMIN_EXPIRY

paymentDirectoryGodFile = REMOVED

paymentHostResidue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY

paymentPrecertHygiene = DEAD_PORTS_REMOVED_TYPED_FAULTS_NO_LOCALIZED_APPLICATION_FALLBACK

structureCertification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001

Set:
nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001

Do NOT certify Payment here.
Do NOT advance Settlement.

Preserve Cart/Order/StoreContext/Offer certifications, Checkout W5 pause, frontendFrozen=true.

Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001/payment-directory-split.md

Record before/after LOC, method ownership, DI, collaborator choice, behavior evidence, no schema changes, recovery state.

Validation

Run only:

focused Payment directory split tests

reconciliation focused tests

admin/refund focused tests

expiry/retry focused tests

PaymentArchitectureGuardTests

full Tooba.Payment.Tests

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

dotnet build src/backend/Tooba.slnx

PASS criteria

PASS only if:

four ports resolve to four focused implementations

PaymentDirectory only implements IPaymentDirectory

no multi-interface directory god-file remains

PaymentDirectory <700 LOC

no new >=800 LOC Payment production file

behavior unchanged

no interface down-cast

PaymentContractBridge intact

no Host residue returns

Payment -> Host ZERO

no Message classification returns

no schema/migration change

Payment still NOT structure-certified

recovery points to structure certification

Checkout/frontend unchanged

Payment tests and build pass

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001
Parent-Task: TB-TMAR-PAYMENT-PRECERT-HYGIENE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Hygiene-Acceptance-State:
Before-PaymentDirectory-State:
PaymentDirectory-State:
PaymentReconciliationDirectory-State:
PaymentAdminDirectory-State:
PaymentExpiryDirectory-State:
Shared-Collaborator-State:
Directory-Line-Counts:
DI-Registration-State:
Interface-Downcast-State:
Behavior-Parity:
Transaction-Semantics-State:
Schema-Migration-State:
PaymentContractBridge-State:
Message-Classification-State:
Payment-To-Host-Dependency-State:
Host-Payment-Residue-State:
Architecture-Guards:
Focused-Validation:
Payment-Tests:
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
Do not start Payment structure certification automatically.
Do not start Settlement.
Do not resume Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK