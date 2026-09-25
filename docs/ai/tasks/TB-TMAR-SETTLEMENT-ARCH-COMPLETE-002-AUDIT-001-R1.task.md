PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Parent-Task: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SETTLEMENT_ARCH_COMPLETE_002_AUDIT_CLASSIFICATION_REPAIR
Title: Correct Settlement validator classification before any validator implementation
Backend-Only: YES
Audit-Repair-Only: YES

Architect verdict

Parent audit commit:
291f8bf1459e56c2c3578fdabcbcedc8d2e43fb4

Parent audit is NOT YET ARCHITECT-ACCEPTED.

The static inventory/structure/Host/cross-module findings are acceptable, but the validator classification is materially wrong.

The audit classified all 10 endpoint-reachable requests as VALIDATOR_REQUIRED.

That conflicts with the established ARCH-COMPLETE-002 validation rule:
transport/input validation belongs in FluentValidation; trusted authorization-derived values and zero-input requests do not require ceremonial validators.

Exact classification defect

Settlement endpoints show:

Trusted seller-authorized queries

These four requests receive SellerPartyId only from:
ISettlementSellerAuthorizer.RequireAuthorizedAsync(...)

They have no untrusted request payload.

Therefore classify:

GetSellerSettlementBalanceQuery = NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY
ListSellerSettlementEntriesQuery = NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY
ListSellerSettlementStatementsQuery = NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY
ListSellerPayoutRequestsQuery = NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY

This follows the already accepted Offer precedent for auth-scoped seller identity.

Zero-input admin queries

These two requests are parameterless and are created only after admin authorization:

ListAdminSettlementBalancesQuery = NO_VALIDATOR_REQUIRED_NO_INPUT
ListAdminPayoutQueueQuery = NO_VALIDATOR_REQUIRED_NO_INPUT

Do NOT create ceremonial empty validators.

VALIDATOR_REQUIRED requests

Exactly four remain:

RequestSellerPayoutCommand

untrusted body: Amount, IdempotencyKey
validator required

ProcessAdminPayoutCommand

untrusted route: PayoutRequestId
validator required

RetryAdminPayoutCommand

untrusted route: PayoutRequestId
validator required

QueryAdminPayoutGridQuery

untrusted GridQueryRequest body
validator required for envelope/null/basic shape only
grid field/operator/sort/connector policy remains owned by existing AdminPayoutGridQueryPolicy

ActorUserId / SellerPartyId values supplied by trusted authorizers must NOT be the reason for creating validators.

Correct totals

Endpoint-Reachable-Request-Count = 10
Validator-Required-Count = 4
Validators-Present-Count = 0
Validators-Missing-Count = 4
No-Validator-Required-Count = 6

No-Validator-Required breakdown:

4 AUTH_SCOPED_QUERY
2 NO_INPUT
One objective only

Correct the audit evidence and Recovery SoT classification.

Do NOT:

add validators
change Settlement production code
change endpoints/handlers
change namespaces/folders
strengthen architecture guards
change Host production
structure-certify Settlement
touch Payment
resume Checkout
touch frontend
Required changes

Update only:

docs/evidence/TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001/settlement-arch-complete-002-audit.md

Correct the validator matrix and totals to 4 required / 6 no-validator-required.

docs/architecture/tmar-current-state.json

Correct:

validatorRequiredCount = 4
validatorsPresentCount = 0
validatorsMissingCount = 4
noValidatorRequiredCount = 6
validatorCoverageState = 0_OF_4_REQUIRED_PRESENT_6_NO_VALIDATOR_REQUIRED
repairScope = four transport validators only

Keep:

auditDecision = NEEDS_PRECERT_REPAIR_THEN_STRUCTURE
Settlement NOT certified
nextTask = TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

Correct only the Settlement audit summary/counts.

src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs

Correct only the Settlement audit expected counts/state.

Next implementation scope after this repair

The next task remains:
TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001

But its implementation scope must be exactly four validators:

RequestSellerPayoutCommandValidator
ProcessAdminPayoutCommandValidator
RetryAdminPayoutCommandValidator
QueryAdminPayoutGridQueryValidator

No validator for the six NO_VALIDATOR_REQUIRED requests.

FAST-VALIDATION-BUDGET

This is documentation/SoT classification repair only.

Run ONLY:

dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --no-restore --filter FullyQualifiedName~TmarDurableGuardTests

Do NOT run:

Settlement tests
full Host suite
structure gate
full build
solution build
Testcontainers
DB tests
retries

If the focused durable guard hangs:
return INCOMPLETE and STOP.

PASS criteria

PASS only if:

audit totals are 10 / 4 required / 0 present / 4 missing / 6 no-validator-required
four auth-scoped seller queries are explicitly classified NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY
two admin parameterless queries are explicitly NO_VALIDATOR_REQUIRED_NO_INPUT
exactly four future validators are named
no Settlement production code changed
Settlement remains NOT structure-certified
Payment certification unchanged
Checkout/frontend unchanged
next task remains bounded precert repair
focused durable guard passes
Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Parent-Task: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Audit-Acceptance-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Present-Count:
Validators-Missing-Count:
No-Validator-Required-Count:
Auth-Scoped-No-Validator-Requests:
No-Input-No-Validator-Requests:
Required-Validator-Requests:
Audit-Decision:
Production-Code-Changed:
Settlement-Structure-Certification-State:
Payment-Certification-State:
Focused-Durable-Guard:
Recovery-State:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not add Settlement validators automatically.
Do not structure-certify Settlement.
Do not start another module.
Do not run broader tests.
Do not resume Checkout.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK