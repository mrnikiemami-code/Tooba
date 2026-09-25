PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Parent-Task: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SETTLEMENT_ARCH_COMPLETE_002_PRECERT_VALIDATION
Title: Add exactly four Settlement transport validators before structure certification
Backend-Only: YES

Architect verdict

TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1 is ARCHITECT-ACCEPTED at:
dad29faf78befa72bfc1971c1510ac50c63ba692

Accepted audit state:

Settlement = COMPLETE_REFERENCE_PATTERN / HTTP_OWNING / MODULE_ENDPOINTS / MEDIATR_12_5
Settlement is NOT yet ARCH-COMPLETE-002 structure-certified
endpoint-reachable requests = 10
VALIDATOR_REQUIRED = 4
validators present = 0
validators missing = 4
NO_VALIDATOR_REQUIRED = 6
4 AUTH_SCOPED_QUERY
2 NO_INPUT
Settlement -> Host dependency = ZERO
Host residue = exactly HostSettlementAdminAuthorizer + HostSettlementSellerAuthorizer
path/namespace audit = exact
no alias/type-forwarding debt found
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true
One objective only

Add exactly the four missing Settlement transport/input validators and add one exhaustive validator coverage guard.

Do NOT structure-certify Settlement in this task.

Validator folders

Create:

src/backend/Modules/Settlement/Tooba.Settlement.Application/Validators/Seller/

Namespace:
Tooba.Settlement.Application.Validators.Seller

Create exactly:

RequestSellerPayoutCommandValidator

Create:

src/backend/Modules/Settlement/Tooba.Settlement.Application/Validators/Admin/

Namespace:
Tooba.Settlement.Application.Validators.Admin

Create exactly:

ProcessAdminPayoutCommandValidator
RetryAdminPayoutCommandValidator
QueryAdminPayoutGridQueryValidator

Do NOT create validators for the six NO_VALIDATOR_REQUIRED requests.

Validation rules

Transport/input shape only.

RequestSellerPayoutCommandValidator

Validate only untrusted body shape:

Amount > 0
IdempotencyKey non-null/non-empty/non-whitespace after trim

Do NOT validate:

SellerPartyId
ActorUserId
available balance
payout eligibility
seller existence
idempotency uniqueness
payout state/business policy

Reason:
SellerPartyId and ActorUserId come from the trusted seller authorization boundary.

Do NOT invent a new IdempotencyKey max length unless an existing canonical Settlement/BuildingBlocks constant already applies to this same field.

ProcessAdminPayoutCommandValidator

Validate only:

PayoutRequestId != Guid.Empty

Do NOT validate ActorUserId because it is supplied by the trusted admin authorization boundary.

Do NOT validate payout existence/state/eligibility.

RetryAdminPayoutCommandValidator

Validate only:

PayoutRequestId != Guid.Empty

Do NOT validate ActorUserId because it is supplied by the trusted admin authorization boundary.

Do NOT validate retry eligibility/state/business policy.

QueryAdminPayoutGridQueryValidator

Validate only:

Request is not null

Do NOT duplicate AdminPayoutGridQueryPolicy.

Specifically do NOT validate in FluentValidation:

Page/PageSize normalization
allowed field names
operators
sort fields/directions
filter semantics
advanced-filter connectors
search semantics

Those remain owned by the existing module-owned grid policy.

Six explicit NO_VALIDATOR_REQUIRED requests

Guard and evidence must classify exactly:

AUTH_SCOPED_QUERY
GetSellerSettlementBalanceQuery
ListSellerSettlementEntriesQuery
ListSellerSettlementStatementsQuery
ListSellerPayoutRequestsQuery

Reason:
SellerPartyId is produced only by ISettlementSellerAuthorizer.RequireAuthorizedAsync(...); no untrusted payload.

NO_INPUT
ListAdminSettlementBalancesQuery
ListAdminPayoutQueueQuery

Reason:
parameterless requests created only after admin authorization.

No ceremonial validators.

FluentValidation / MediatR discovery

Use only the existing central mechanism:
AddToobaCqrsFoundation / AddValidatorsFromAssembly.

Do NOT:

manually invoke validators in endpoints
manually invoke validators in handlers
add new middleware/pipeline
change MediatR version
change handler behavior

MediatR remains 12.5.0.

If existing central discovery cannot resolve these four validators:
return RECOVERY_CONFLICT with exact evidence.
Do not invent a second registration mechanism.

Coverage guard

Add one focused Settlement validator coverage guard.

It must prove the exact endpoint-reachable inventory = 10:

Required = 4:

RequestSellerPayoutCommand
ProcessAdminPayoutCommand
RetryAdminPayoutCommand
QueryAdminPayoutGridQuery

No validator required = 6:

4 AUTH_SCOPED_QUERY
2 NO_INPUT

Guard must prove:

exactly 10 endpoint-reachable requests
exactly 4 VALIDATOR_REQUIRED
exactly 4 concrete validators discoverable via foundation DI
exactly 6 NO_VALIDATOR_REQUIRED
no validator registered for those six
all 10 are real IRequest types
Settlement endpoints use ISender
no direct IValidator/ValidateAsync usage in endpoints or handlers
MediatR remains 12.5.0
Settlement remains NOT structure-certified

No representative/sample checks.

Protected architecture state

Preserve:

ARCH-COMPLETE-002
HOST-MODULE-ENDPOINT-001
ARCH-CQRS-001/002
Payment/Order/Cart/StoreContext/Offer certifications
Settlement -> Host = ZERO
Host Settlement residue = two thin security adapters only
no foreign DbContext
no schema/migration change
current Settlement foldering/namespaces
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true

Do NOT:

strengthen the full physical structure guard yet
edit tmar-module-structure-manifests.json to certify Settlement
add Settlement to structureLock.certifiedModules
remove Settlement from uncertifiedHttpOwningModules
move production files
touch Host production
touch Payment
resume Checkout
touch frontend
FAST-VALIDATION-BUDGET

Run ONLY:

direct in-memory validator tests for the four new validators
focused Settlement validator coverage guard
one project build:
dotnet build src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj --no-restore

Do NOT run:

full Settlement tests
SettlementArchitectureGuardTests unless required for compilation
Host tests
TMAR gates
solution tests
solution build
Testcontainers
database tests
retries

If a focused test hangs or requires unrelated infrastructure:
return INCOMPLETE and STOP.

Focused test expectations

Direct validator tests only; no web host, no database.

Prove:

RequestSellerPayoutCommandValidator:

Amount <= 0 rejected
IdempotencyKey null/empty/whitespace rejected
valid minimal command passes
SellerPartyId/ActorUserId are not validator rules

ProcessAdminPayoutCommandValidator:

Guid.Empty PayoutRequestId rejected
non-empty PayoutRequestId accepted
ActorUserId not validated

RetryAdminPayoutCommandValidator:

Guid.Empty PayoutRequestId rejected
non-empty PayoutRequestId accepted
ActorUserId not validated

QueryAdminPayoutGridQueryValidator:

null Request rejected
a non-null GridQueryRequest passes even if its field/operator/sort/paging content would later be normalized/rejected by AdminPayoutGridQueryPolicy
validator does not duplicate grid policy
Recovery

On PASS update only minimum SoT.

Record:

audit + R1 = ARCHITECT_ACCEPTED
settlementPrecertValidation = 4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED
validatorCoverage = COMPLETE_4_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED
structure certification remains PENDING

Set:
nextTask = TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001
nextTaskGate = NEXT_TMAR_WAVE_AFTER_SETTLEMENT_PRECERT_VALIDATION

Update:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
only durable guard expectations needed for this validation completion

Do NOT certify Settlement here.

Evidence

Create:
docs/evidence/TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001/settlement-precert-validation.md

Keep concise:

exact 10-request classification
four validator names
six NO_VALIDATOR_REQUIRED classifications/reasons
central discovery proof
focused tests
project build
explicit structure certification PENDING
PASS criteria

PASS only if:

exactly four validators are added
no validator is added for the six NO_VALIDATOR_REQUIRED requests
validation remains transport/input shape only
grid policy is not duplicated
central discovery resolves all four
endpoint inventory remains exactly 10
4 required / 6 no-validator-required is guarded exhaustively
no direct validator invocation exists
MediatR remains 12.5.0
Settlement -> Host remains ZERO
Settlement remains NOT structure-certified
Checkout/frontend unchanged
focused tests pass
focused coverage guard passes
Settlement test project builds
Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Parent-Task: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Audit-R1-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Added:
No-Validator-Required-Count:
Auth-Scoped-No-Validator-State:
No-Input-No-Validator-State:
Validation-Scope-State:
Grid-Validation-Boundary-State:
FluentValidation-Discovery-State:
MediatR-State:
ISender-State:
Direct-Validator-Invocation-State:
Settlement-To-Host-Dependency-State:
Host-Settlement-Residue-State:
Settlement-Structure-Certification-State:
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
Do not start Settlement structure certification automatically.
Do not run broader tests.
Do not start another module.
Do not resume Checkout.
Do not touch frontend.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK