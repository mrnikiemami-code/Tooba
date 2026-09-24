PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-PRECERT-VALIDATION-002
Parent-Task: TB-TMAR-PAYMENT-PRECERT-VALIDATION-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: PAYMENT_PRECERT_VALIDATION_ADMIN_WEBHOOK
Title: Complete remaining Payment Admin + Webhook transport validators
Backend-Only: YES

Architect verdict

TB-TMAR-PAYMENT-PRECERT-VALIDATION-001 is ARCHITECT-ACCEPTED.

Verified on main at:
7ce560c18454a8f8b44d95cade00be6f8605696b

Accepted state:

storefront endpoint inventory = 10

9 storefront VALIDATOR_REQUIRED validators present

ListStorefrontPaymentMethodsQuery = NO_VALIDATOR_REQUIRED_NO_INPUT

existing AddValidatorsFromAssembly / AddToobaCqrsFoundation discovery is used

MediatR remains 12.5.0

no direct endpoint/handler validator invocation

Payment validation overall = PARTIAL_9_OF_15_REQUIRED_PRESENT

Payment remains NOT ARCH-COMPLETE-002 structure-certified

Payment -> Host remains ZERO

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

One objective

Close the remaining Payment endpoint-reachable transport validation coverage.

Remaining endpoint requests = exactly 6:

5 Admin

1 Webhook

After this task:

15/15 VALIDATOR_REQUIRED must be present

1/1 NO_VALIDATOR_REQUIRED_NO_INPUT remains unchanged

total endpoint-reachable inventory remains exactly 16

Do NOT structure-certify Payment in this task.

Target folders

Create:
src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Admin/

Namespace:
Tooba.Payment.Application.Validators.Admin

Create exactly 5 validators:

GetAdminPaymentQueryValidator

ReconcileAdminPaymentCommandValidator

ConfirmAdminDepositCommandValidator

RejectAdminDepositCommandValidator

QueryAdminPaymentsGridQueryValidator

Create:
src/backend/Modules/Payment/Tooba.Payment.Application/Validators/Webhooks/

Namespace:
Tooba.Payment.Application.Validators.Webhooks

Create exactly 1 validator:
6. ProcessPaymentWebhookCommandValidator

Do NOT create any additional Payment validator in this task.

Admin validator rules
GetAdminPaymentQueryValidator

PaymentId != Guid.Empty

ReconcileAdminPaymentCommandValidator

PaymentId != Guid.Empty

ConfirmAdminDepositCommandValidator

PaymentId != Guid.Empty

RejectAdminDepositCommandValidator

PaymentId != Guid.Empty

QueryAdminPaymentsGridQueryValidator

Transport shape only:

Input must not be null

Input.Filters must not be null

Input.SortField non-empty after trim

Input.SortDirection non-empty after trim

Page >= 1

PageSize >= 1

Do NOT duplicate PaymentAdminGridQueryNormalizer policy.

Specifically do NOT validate here:

allowed grid field names

allowed operators

advanced connector rules

sort whitelist

search semantics

supply/reservation enrichment semantics

Those remain owned by the existing Payment endpoint grid normalizer and stable semantic grid codes.

Webhook validator rules
ProcessPaymentWebhookCommandValidator

Transport envelope only:

ProviderCode non-empty after trim

RawBody not null

RawBody length > 0

BodyText non-empty after trim

SignatureHeader may be null

if SignatureHeader is supplied, reject whitespace-only

Do NOT:

parse JSON in the validator

validate PaymentId/AttemptId/provider event fields inside webhook JSON

verify signature in the validator

duplicate IPaymentWebhookSignatureVerifier

duplicate ProcessPaymentWebhookHandler payload/business logic

invent provider allowlists

invent signature format/length rules

Webhook signature and payload semantics remain in the existing handler/verifier boundary.

FluentValidation / MediatR lock

Use the existing central discovery only:
AddValidatorsFromAssembly through AddToobaCqrsFoundation.

Do NOT:

manually call validators from endpoint/handler

add endpoint-specific validation middleware

create a second validation pipeline

change MediatR version

move business/domain rules into FluentValidation

MediatR remains:
12.5.0

Complete Payment validator coverage guard

Add/extend a focused Payment validator coverage guard that proves the COMPLETE endpoint inventory.

Canonical endpoint-reachable inventory = exactly 16:

Storefront = 10:

9 VALIDATOR_REQUIRED_PRESENT

ListStorefrontPaymentMethodsQuery = NO_VALIDATOR_REQUIRED_NO_INPUT

Admin = 5:

all 5 VALIDATOR_REQUIRED_PRESENT

Webhook = 1:

VALIDATOR_REQUIRED_PRESENT

Guard must prove:

total endpoint-reachable requests = 16

validator-required = 15

validators present = 15

no-validator-required = 1

zero missing validators

all 16 are IRequest

endpoints use ISender

no endpoint directly invokes validators

relevant handlers do not directly invoke validators

ReconcileStalePaymentsCommand remains worker-only and is NOT counted in endpoint inventory

ReconcileStalePaymentsCommand remains NO_VALIDATOR_REQUIRED_INTERNAL_WORKER

MediatR remains 12.5.0

No representative substitution.

Protected architecture state

Preserve all locks:

ARCH-COMPLETE-002

HOST-MODULE-ENDPOINT-001

ARCH-CQRS-001/002

Preserve:

Payment -> Host = ZERO

Host Payment residue = exactly two approved thin security adapters

PaymentContractBridge intact

directory split intact

no Message classification

no schema/migration changes

Cart/Order/StoreContext/Offer certifications unchanged

Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT

frontendFrozen = true

Do NOT:

certify Payment yet

edit Payment manifest entry as certified

add Payment to structureLock.certifiedModules

remove Payment from uncertifiedHttpOwningModules

reorganize Application/Endpoints/Infrastructure folders

refactor Payment directories

touch Settlement

resume Checkout

touch frontend

FAST-VALIDATION-BUDGET

Run ONLY:

one focused Admin/Webhook validator test class

focused complete Payment validator coverage guard test(s)

one project build:
dotnet build src/backend/Modules/Payment/Tooba.Payment.Tests/Tooba.Payment.Tests.csproj --no-restore

Do NOT run:

full Tooba.Payment.Tests

Host suites

TMAR broad suites

solution tests

full solution build

Testcontainers

DB integration tests

repeated retries

All validator tests must be direct/in-memory.
No WebApplicationFactory.
No database.

If validation unexpectedly requires unrelated infrastructure or a focused test hangs:
return INCOMPLETE and STOP.

Focused tests

At minimum prove:

Admin:

all four PaymentId-only requests reject Guid.Empty and accept non-empty Guid

QueryAdminPaymentsGridQuery rejects null Input

rejects null Filters

rejects blank SortField

rejects blank SortDirection

rejects Page < 1

rejects PageSize < 1

accepts a minimal normalized valid input

Webhook:

rejects blank ProviderCode

rejects null RawBody

rejects empty RawBody

rejects blank BodyText

allows null SignatureHeader

rejects supplied whitespace-only SignatureHeader

valid minimal envelope passes

validator does not invoke signature verifier or JSON parsing

Recovery

On PASS update only minimum SoT.

Record:

TB-TMAR-PAYMENT-PRECERT-VALIDATION-001 = ARCHITECT_ACCEPTED

paymentValidationStorefront = 9_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED

paymentValidationAdmin = 5_REQUIRED_PRESENT

paymentValidationWebhook = 1_REQUIRED_PRESENT

paymentValidationOverall = COMPLETE_15_OF_15_REQUIRED_PRESENT_1_NO_VALIDATOR_REQUIRED

endpointReachableRequests = 16

workerOnlyRequest = ReconcileStalePaymentsCommand_NO_VALIDATOR_REQUIRED_INTERNAL_WORKER

structureCertification remains PENDING

Set:
nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001
nextTaskGate = NEXT_TMAR_WAVE_AFTER_PAYMENT_PRECERT_VALIDATION_002

Update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

only the durable guard expectations needed for completed Payment validator coverage

Do NOT structure-certify Payment here.

Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-PRECERT-VALIDATION-002/admin-webhook-validation.md

Keep evidence concise:

exact remaining 6-request inventory

six validator names

complete 16-request coverage totals

worker-only exclusion

discovery proof

focused tests

project build

explicit structure-certification PENDING state

PASS criteria

PASS only if:

exactly six remaining validators are added

Admin = 5/5 required present

Webhook = 1/1 required present

overall Payment endpoint validation = 15/15 required present + 1 no-validator-required

total endpoint inventory = exactly 16

worker-only ReconcileStalePaymentsCommand excluded correctly

grid validator does not duplicate grid-normalizer business/policy rules

webhook validator does not duplicate signature/JSON/business logic

existing central validator discovery is preserved

no direct validator invocation

Payment -> Host remains ZERO

Payment remains NOT structure-certified

Checkout/frontend unchanged

focused tests pass

focused coverage guard passes

Payment test project builds

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-PRECERT-VALIDATION-002
Parent-Task: TB-TMAR-PAYMENT-PRECERT-VALIDATION-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Validation-001-State:
Admin-Endpoint-Reachable-Request-Count:
Webhook-Endpoint-Reachable-Request-Count:
Validators-Added-This-Task:
Admin-Validator-Coverage-State:
Webhook-Validator-Coverage-State:
Total-Endpoint-Reachable-Request-Count:
Total-Validator-Required-Count:
Total-No-Validator-Required-Count:
Payment-Validation-Overall-State:
Worker-Only-Request-State:
Grid-Validation-Boundary-State:
Webhook-Validation-Boundary-State:
FluentValidation-Discovery-State:
MediatR-State:
ISender-State:
Direct-Validator-Invocation-State:
Payment-To-Host-Dependency-State:
Host-Payment-Residue-State:
Payment-Structure-Certification-State:
Focused-Validator-Tests:
Focused-Coverage-Guard:
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
Do not start Payment structure certification automatically.
Do not run broader tests.
Do not resume Checkout.
Do not touch Settlement.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK