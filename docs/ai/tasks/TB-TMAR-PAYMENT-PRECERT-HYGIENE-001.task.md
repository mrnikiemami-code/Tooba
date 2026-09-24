PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-PRECERT-HYGIENE-001
Parent-Task: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: PAYMENT_PRECERT_EXTRACTION_HYGIENE
Title: Remove Payment pre-certification extraction and semantic hygiene debt
Backend-Only: YES

Architect verdict on parent

TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1 is ARCHITECT-ACCEPTED.

Implementation commit:
f43904967311a3ac3e3f2cca773af3c733458a76

Recovery stamp commit:
62e016d5f0bd623057e4d8abf3050005b2896c90

Verified:

effective reconciliation minimum = 15 seconds

all Payment admin grid structural failures are converted from GridQueryValidationException to stable SemanticException codes

central SafeErrorMapper maps those semantic codes to HTTP 400

Host Payment runtime/grid residue remains removed

only HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer remain as approved thin Host Payment adapters

Payment -> Host dependency remains zero

Payment is still NOT ARCH-COMPLETE-002 certified

Why one bounded pre-cert task exists

Independent review of the accepted Payment audit exposed four known extraction/semantic hygiene debts that should not be carried into ARCH-COMPLETE-002 certification:

four obsolete unused Payment Application ports remain

PaymentHostContractBridge is a legacy misleading name

two production paths still classify expected faults through ex.Message

Payment admin grid Application handler contains a localized Persian presentation fallback

This task removes only those known residuals.

Do NOT structure-certify Payment here.
Do NOT add the 15 validators here.
Do NOT split PaymentDirectory here.
Do NOT touch Settlement.
Do NOT resume Checkout.
Do NOT touch frontend.

A. Remove dead obsolete Payment ports

File:
src/backend/Modules/Payment/Tooba.Payment.Application/Ports/PaymentStorefrontBoundaryPorts.cs

Delete these dead zero-production-consumer members:

StorefrontCheckoutPaymentAccessDto

IStorefrontCheckoutPaymentAccessPort

IPaymentProofMediaPort

IPaymentUnpaidRetrySupplyPort

IPaymentAdminOrderEnrichmentPort

Preserve live members:

ICheckoutActorPolicyPort

IPaymentGatewayCatalogPort

IPaymentWebhookSignatureVerifier

AdminPaymentOrderEnrichmentDto only if it still has production consumers

Before deleting AdminPaymentOrderEnrichmentDto, perform exact reference check.
Delete it only if zero production consumers remain.
Do not invent replacement seams.

No behavior change.

B. Rename legacy internal bridge

Rename:

src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Adapters/PaymentHostContractBridge.cs

to:

PaymentContractBridge.cs

Rename class:
PaymentHostContractBridge -> PaymentContractBridge

Namespace remains:
Tooba.Payment.Infrastructure.Adapters

It must continue implementing:

IPaymentAdminGateway

IPaymentCustomerGateway

IPaymentHoldSettingsGateway

Update:
src/backend/Modules/Payment/Tooba.Payment.Infrastructure/DependencyInjection/PaymentModule.cs

Register PaymentContractBridge instead of old class.

Update every compile reference exactly, including tests/guards.

No type-forwarding.
No compatibility alias.
No obsolete shim.
The old type/file must disappear.

C. Remove Payment refund Message classification

Current defect:
PaymentDirectory.CloseOrStartRefundForOrderCancelAsync(...) catches:

InvalidOperationException by exact ex.Message

InvalidOperationException by ex.Message.StartsWith("payment.")

This is prohibited.

Required typed boundary

Edit:
src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/FakePaymentRefundGateway.cs

Change FailClosedPaymentRefundGateway expected unconfigured failure from:

InvalidOperationException("payment.refund.gateway.unconfigured")

to:

ContractOperationException("payment.refund.gateway.unconfigured")

using Tooba.BuildingBlocks.

For any other Payment-owned refund gateway expected failure that currently throws an exception code in Message, convert it to ContractOperationException(Code).

Then in:
src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Directories/PaymentDirectory.cs

catch typed fault only:

ContractOperationException ex when ex.Code == "payment.refund.gateway.unconfigured" => preserve RefundPending for admin action

if Payment-owned refund gateways can emit other stable payment.* ContractOperationException codes, handle using ex.Code, never Message

Do NOT catch arbitrary InvalidOperationException by text.
Do NOT use Contains / StartsWith / Message.

Unknown exceptions must propagate according to the existing failure model.

D. Remove Wallet Message classification at the Payment↔Wallet contract boundary

Current defect:
WalletPaymentGateway.VerifyAsync(...) catches InvalidOperationException and classifies by:

StartsWith("wallet.")

Contains("payment.wallet")

Contains("insufficient")

This is forbidden.

Narrow cross-module correction

Wallet owns its expected operational failures.

Edit only the Wallet order-payment boundary implementation required by Payment:

src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Directories/WalletDirectory.cs

For expected rejection paths inside SpendForOrderPaymentAsync(...) used by IWalletOrderPaymentPort, throw:

ContractOperationException(stableCode)

instead of InvalidOperationException(stableCode).

Do not rewrite unrelated Wallet use cases.
Do not redesign Wallet.
Do not change Wallet persistence/domain behavior.

Stable code values already present in the current Wallet code must remain unchanged.

Then edit:

src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Providers/WalletPaymentGateway.cs

Replace Message heuristic catch with typed catch:

catch (ContractOperationException ex)

No classification from Message.

For expected Wallet rejection codes, return:
GatewayVerification(false, null, "WALLET_SPEND_REJECTED")

Do not broaden to arbitrary unknown exceptions.
Unknown faults must still propagate.

If the exact IWalletOrderPaymentPort implementation cannot be converted without changing unrelated Wallet workflows, STOP INCOMPLETE and report the exact blocker rather than preserving Message parsing.

E. Remove localized fallback from Payment Application

File:
src/backend/Modules/Payment/Tooba.Payment.Application/Queries/QueryAdminPaymentsGrid/QueryAdminPaymentsGridQuery.cs

Current localized fallback:
order?.CustomerDisplayName ?? "مشتری توبا"

Application must not manufacture localized presentation prose.

Replace with locale-neutral data semantics.

Preferred:

order?.CustomerDisplayName ?? string.Empty

For ReservationLabel / ReservationLabelEn:

keep real enrichment values

for missing enrichment use string.Empty, not "—"

Machine-state values such as:

"NotApplicable"

"none"

may remain if they are stable semantic values, not localized display prose.

Do NOT introduce English UI prose into Application as replacement.

F. PaymentDirectory size rule

Do NOT split PaymentDirectory in this task.

But because it is already oversized:

no net expansion beyond current accepted baseline from this task

typed-fault cleanup should shrink or remain neutral

add/strengthen guard so it cannot grow beyond the current accepted baseline

Record its exact post-task line count in evidence.

Dedicated structural split may be scheduled only if certification gate proves it mandatory; do not mix it into this task.

G. Architecture guards

Strengthen:
src/backend/Modules/Payment/Tooba.Payment.Tests/Architecture/PaymentArchitectureGuardTests.cs

Assert:

old PaymentHostContractBridge.cs absent

old PaymentHostContractBridge type absent

PaymentContractBridge exists

no obsolete dead port types listed in section A remain

no Payment production code classifies expected faults through:

ex.Message

.Message.Contains(

.Message.StartsWith(

.Message ==

WalletPaymentGateway catches typed ContractOperationException, not InvalidOperationException message text

PaymentDirectory uses ContractOperationException.Code, never Message

Payment Application has no Persian string literal in QueryAdminPaymentsGridQuery.cs

Payment -> Host dependency remains zero

Host residue remains exactly the two approved security adapters

PaymentDirectory does not expand beyond accepted pre-task size

Do not weaken existing guards.

H. Focused tests

Add/update only focused tests proving:

fail-closed refund gateway throws typed ContractOperationException with code payment.refund.gateway.unconfigured

order-cancel refund path preserves RefundPending on that typed code

other typed refund failure uses Code, not Message

Wallet order-payment expected rejection is typed at boundary

WalletPaymentGateway converts typed Wallet rejection to WALLET_SPEND_REJECTED

unknown Wallet exception is not swallowed

admin grid missing customer enrichment returns empty display string

admin grid missing reservation labels returns empty strings

bridge rename preserves all three Contracts interfaces

obsolete port types are gone

No broad Wallet suite unless compile requires it.
If Wallet production file changes, run the smallest focused Wallet tests covering order-payment debit boundary.

I. Recovery / SoT

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

durable guard expectations

Stamp:

lastAcceptedTask = TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1

lastAcceptedCommit = f43904967311a3ac3e3f2cca773af3c733458a76

lastAcceptedSoTStamp = 62e016d5f0bd623057e4d8abf3050005b2896c90

Record:

Payment pre-cert hygiene = DEAD_PORTS_REMOVED_TYPED_FAULTS_NO_LOCALIZED_APPLICATION_FALLBACK

internal bridge = PAYMENT_CONTRACT_BRIDGE

Message classification = REMOVED

Payment Host residue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY

Payment structure certification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001

Set:
nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001

Do NOT certify Payment here.
Do NOT advance Settlement here.

Preserve:

Cart/Order/StoreContext/Offer certifications

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

J. Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-PRECERT-HYGIENE-001/payment-precert-hygiene.md

Record:

parent acceptance commits

deleted dead ports

bridge rename

typed refund fault change

typed Wallet boundary change

removal of Message heuristics

localized fallback removal

PaymentDirectory line count before/after

focused validation

recovery state

Validation

Run only:

focused Payment typed-fault tests

focused Payment admin-grid query tests

PaymentArchitectureGuardTests

smallest focused Wallet order-payment tests if Wallet changed

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

one final:
dotnet build src/backend/Tooba.slnx

No broad unrelated suites.

PASS criteria

PASS only if:

dead obsolete Payment ports are gone

old PaymentHostContractBridge file/type are gone

PaymentContractBridge preserves contract behavior

Payment refund path has no Message classification

WalletPaymentGateway has no Message classification

expected Wallet order-payment failures cross as typed ContractOperationException

Payment Application contains no localized Persian fallback in admin grid composition

PaymentDirectory does not expand

Payment -> Host dependency remains zero

Host Payment residue stays security-adapters-only

Payment remains NOT structure-certified

Checkout/frontend unchanged

focused tests pass

full build passes

recovery points directly to Payment structure certification

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-PRECERT-HYGIENE-001
Parent-Task: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-R1-Acceptance-State:
Dead-Obsolete-Ports-State:
Payment-Contract-Bridge-State:
Refund-Typed-Fault-State:
Wallet-Typed-Boundary-State:
Message-Classification-State:
Admin-Grid-Localized-Fallback-State:
PaymentDirectory-Size-State:
Payment-To-Host-Dependency-State:
Host-Payment-Residue-State:
Architecture-Guards:
Focused-Payment-Validation:
Focused-Wallet-Validation:
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