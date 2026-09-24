PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-PRECERT-VALIDATION-001
Parent-Task: TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: PAYMENT_PRECERT_VALIDATION_STOREFRONT
Title: Add Payment storefront transport validators without starting structure certification
Backend-Only: YES

Architect verdict

TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1 is ARCHITECT-ACCEPTED at:
1ef08cb5b35f7104baee74b6ad1d0822d1feff24

Accepted state:

focused Payment directory decomposition is valid

real scoped DI resolution is proven

no construction cycle

Payment -> Host remains ZERO

Payment remains NOT ARCH-COMPLETE-002 structure-certified

Payment validation coverage is still incomplete

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Bounded objective

The Payment audit found 16 endpoint-reachable requests:

15 VALIDATOR_REQUIRED

1 NO_VALIDATOR_REQUIRED_NO_INPUT

Do NOT add all 15 in one task.

This task closes only the STOREFRONT slice:

9 validator-required storefront requests

1 zero-input storefront request explicitly NO_VALIDATOR_REQUIRED

Admin + Webhook stay for:
TB-TMAR-PAYMENT-PRECERT-VALIDATION-002

Target folder

Create:
src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Storefront/

Namespace:
Tooba.Payment.Application.Validators.Storefront

Create exactly these 9 validators:

InitiateStorefrontPaymentCommandValidator

GetStorefrontWalletQuoteQueryValidator

GetStorefrontPaymentQueryValidator

GetStorefrontPaymentSandboxContextQueryValidator

CompleteSandboxPaymentCommandValidator

SubmitManualPaymentEvidenceCommandValidator

RetryManualPaymentCommandValidator

RetryUnpaidPaymentCommandValidator

UploadManualPaymentProofCommandValidator

Explicitly classify:
ListStorefrontPaymentMethodsQuery = NO_VALIDATOR_REQUIRED_NO_INPUT

Do NOT create a validator for it.

Validation rules

Transport/input shape only.

Common:

any required Guid => != Guid.Empty

nullable AuthenticatedUserId => if present, != Guid.Empty

nullable ProofMediaAssetId => if present, != Guid.Empty

optional string => null allowed, supplied whitespace rejected

do NOT validate ownership, authorization, DB existence, gateway availability, payment state, eligibility, payable amount, or other business rules

InitiateStorefrontPaymentCommand:

CheckoutId != Guid.Empty

CartId != Guid.Empty

IdempotencyKey non-empty after trim

ProviderCode: if supplied, non-whitespace

GuestSecret: if supplied, non-whitespace

AuthenticatedUserId: if supplied, != Guid.Empty

do not invent provider allowlist or guest/auth business policy

GetStorefrontWalletQuoteQuery:

CheckoutId != Guid.Empty

CartId != Guid.Empty

GuestSecret optional/non-whitespace when supplied

AuthenticatedUserId optional/non-empty Guid when supplied

GetStorefrontPaymentQuery:

PaymentId != Guid.Empty

CartId != Guid.Empty

GuestSecret optional/non-whitespace

AuthenticatedUserId optional/non-empty Guid

GetStorefrontPaymentSandboxContextQuery:

same transport rules as GetStorefrontPaymentQuery

CompleteSandboxPaymentCommand:

PaymentId != Guid.Empty

CartId != Guid.Empty

AttemptId != Guid.Empty

ProviderRequestReference non-empty after trim

Outcome non-empty after trim

GuestSecret optional/non-whitespace

AuthenticatedUserId optional/non-empty Guid

do not invent Outcome allowlist unless an existing canonical transport enum/constant already exists

SubmitManualPaymentEvidenceCommand:

PaymentId != Guid.Empty

CartId != Guid.Empty

TransferReference non-empty after trim

ProofMediaAssetId optional/non-empty Guid

GuestSecret optional/non-whitespace

AuthenticatedUserId optional/non-empty Guid

RetryManualPaymentCommand:

PaymentId != Guid.Empty

CartId != Guid.Empty

GuestSecret optional/non-whitespace

AuthenticatedUserId optional/non-empty Guid

RetryUnpaidPaymentCommand:

same transport rules as RetryManualPaymentCommand

UploadManualPaymentProofCommand:

PaymentId != Guid.Empty

CartId != Guid.Empty

Content not null

FileName non-empty after trim

ContentType non-empty after trim

GuestSecret optional/non-whitespace

AuthenticatedUserId optional/non-empty Guid

do NOT read/seek the Stream

do NOT invent file-size/media business policy

FluentValidation / MediatR lock

Use FluentValidation only.

Validators must use the existing validation discovery/pipeline.

Do NOT:

call validators manually from endpoints

call validators manually from handlers

add endpoint-specific validation middleware

change MediatR version

duplicate domain/business checks

MediatR remains 12.5.0.

If no existing central validator discovery can discover these validators:
return RECOVERY_CONFLICT with exact evidence.
Do NOT invent a new validation framework.

Guard update

Strengthen Payment architecture guard only for this slice.

Storefront endpoint request inventory must be exactly 10:

9 VALIDATOR_REQUIRED_PRESENT

1 NO_VALIDATOR_REQUIRED_NO_INPUT

Guard proves:

all 9 validator types exist

ListStorefrontPaymentMethodsQuery is explicitly NO_VALIDATOR_REQUIRED_NO_INPUT

no direct validator call in Payment.Endpoints

no direct validator call in the 9 handlers

all 10 remain IRequest

storefront endpoints remain ISender-based

Do NOT claim full Payment validator coverage.
Admin/Webhook remain explicitly PENDING_VALIDATION_002.

Protected architecture state

Preserve:

ARCH-COMPLETE-002

HOST-MODULE-ENDPOINT-001

ARCH-CQRS-001/002

Payment -> Host = ZERO

Host Payment residue = exactly HostPaymentAdminAuthorizer + HostPaymentStorefrontAuthorizer

PaymentContractBridge intact

focused directory split intact

Payment structure certification = PENDING

Cart/Order/StoreContext/Offer certifications unchanged

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Do NOT:

structure-certify Payment

certify Payment in tmar-module-structure-manifests.json

add Payment to structureLock.certifiedModules

remove Payment from uncertifiedHttpOwningModules

add Admin/Webhook validators

refactor directories

change schema/migrations

touch Settlement

resume Checkout

touch frontend

FAST-VALIDATION-BUDGET

Run ONLY:

one focused direct validator test class for these 9 validators

focused Payment storefront validator/architecture guard test(s)

one project build:
dotnet build src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-restore

Do NOT run:

full Tooba.Payment.Tests

Host suites

TMAR full suites

solution-wide tests

Testcontainers/integration suites

full solution build

repeated retries

If focused validation hangs or requires unrelated infrastructure:
return INCOMPLETE and STOP.

Focused tests must be in-memory. No web host. No database.

At minimum prove:

required Guid.Empty rejected

nullable Guid.Empty rejected only when supplied

required strings reject null/empty/whitespace where applicable

optional strings accept null but reject supplied whitespace

Upload proof rejects null Content without reading Stream

one valid minimal request per validator passes

Recovery

On PASS update only minimum SoT:

Record:

parent directory split R1 = ARCHITECT_ACCEPTED

paymentValidationStorefront = 9_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED

paymentValidationAdminWebhook = PENDING_TB_TMAR_PAYMENT-PRECERT-VALIDATION-002

paymentValidationOverall = PARTIAL_9_OF_15_REQUIRED_PRESENT

structure certification remains PENDING

Set:
nextTask = TB-TMAR-PAYMENT-PRECERT-VALIDATION-002
nextTaskGate = NEXT_TMAR_WAVE_AFTER_PAYMENT_PRECERT_VALIDATION_001

Update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

only durable guard expectations needed for this partial state

Do NOT claim COMPLETE validation.
Do NOT structure-certify Payment.

Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-PRECERT-VALIDATION-001/storefront-validation.md

Keep concise:

exact 10-request storefront inventory

9 validator names

NO_VALIDATOR_REQUIRED rationale

discovery/pipeline evidence

focused test result

project build result

Admin/Webhook explicitly pending

PASS criteria

PASS only if:

exactly 9 storefront validators added

ListStorefrontPaymentMethodsQuery explicitly NO_VALIDATOR_REQUIRED

transport shape only is validated

existing FluentValidation/MediatR discovery is used

no endpoint/handler direct validation invocation

storefront inventory exhaustive at 10

Admin/Webhook untouched and pending

Payment -> Host remains ZERO

Payment remains NOT structure-certified

Checkout/frontend unchanged

focused validator tests pass

focused architecture guard passes

Payment test project builds

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-PRECERT-VALIDATION-001
Parent-Task: TB-TMAR-PAYMENT-PRECERT-DIRECTORY-SPLIT-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Directory-Split-R1-State:
Storefront-Endpoint-Reachable-Request-Count:
Storefront-Validator-Required-Count:
Storefront-No-Validator-Required-Count:
Storefront-Validators-Added:
ListStorefrontPaymentMethods-State:
Validation-Scope-State:
FluentValidation-Discovery-State:
MediatR-State:
ISender-State:
Direct-Validator-Invocation-State:
Admin-Webhook-Validation-State:
Payment-Validation-Overall-State:
Payment-To-Host-Dependency-State:
Host-Payment-Residue-State:
Payment-Structure-Certification-State:
Focused-Validator-Tests:
Focused-Architecture-Guard:
Project-Build:
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
Do not start VALIDATION-002 automatically.
Do not structure-certify Payment.
Do not run broader tests.
Do not resume Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
