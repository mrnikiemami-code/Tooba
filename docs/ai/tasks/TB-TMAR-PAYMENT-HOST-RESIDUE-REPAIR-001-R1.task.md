PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1
Parent-Task: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: PAYMENT_HOST_RESIDUE_PARITY_REPAIR
Title: Restore Payment reconciliation/grid behavior parity after Host extraction
Backend-Only: YES

Architect verdict

Parent is NOT YET ACCEPTED.

Verified on main commit:
5d79cbfd8812fba010e3ec771e30b934ea80973d

Two concrete regressions were introduced during the ownership move.

Defect 1 — poll cadence changed

Old Host behavior:
TimeSpan.FromSeconds(Math.Max(15, _options.PollIntervalSeconds))

New Payment behavior:
PollIntervalSeconds < 5 ? 5 : PollIntervalSeconds

Required:

restore minimum poll interval to 15 seconds

default remains 60

keep PendingAge minimum 1 minute

keep non-positive BatchSize fallback 20

File:
src/backend/Modules/Payment/Tooba.Payment.Infrastructure/Workers/PaymentReconciliationOptions.cs

Update XML docs too.

Defect 2 — advanced grid connector errors can become 500

GridQueryPolicyBase.ValidateAdvancedConnectors(...) throws GridQueryValidationException.

SafeErrorMapper does NOT map GridQueryValidationException; unknown exceptions map to 500.

Old Host policy caught GridQueryValidationException and produced HTTP 400.

Current Payment normalizer does not catch connector-count / connector-value exceptions.

Required in:
src/backend/Modules/Payment/Tooba.Payment.Endpoints/Admin/PaymentAdminGridQueryNormalizer.cs

Use one narrow boundary around Normalize:

catch GridQueryValidationException ex

rethrow new SemanticException(new SemanticError(ex.ErrorCode))

Do NOT parse messages.
Do NOT use PlatformHttpException.
Do NOT duplicate connector validation.

All grid structural errors must surface as stable semantic codes:

grid.filter.field.invalid

grid.filter.operator.invalid

grid.advancedFilter.field.invalid

grid.advancedFilter.connector.count

grid.advancedFilter.connector.invalid

You may simplify current manual wrappers if the one boundary catch makes them redundant.

Scope

Only repair these two parity regressions.

Do NOT:

structure-certify Payment

add 15 validators

refactor PaymentDirectory

rename PaymentHostContractBridge

repair unrelated ex.Message debt

touch Settlement

resume Checkout

touch frontend

Focused tests

Prove:

poll=1 -> 15s

poll=5 -> 15s

poll=15 -> 15s

poll=60 -> 60s

bad advanced connector count -> semantic grid.advancedFilter.connector.count

bad advanced connector value -> semantic grid.advancedFilter.connector.invalid

bad filter field -> grid.filter.field.invalid

bad operator -> grid.filter.operator.invalid

no GridQueryValidationException escapes the Payment normalizer

Host Payment runtime/grid residue is not reintroduced

If easy, assert SafeErrorMapper maps those semantic codes to HTTP 400.

Guards

Strengthen Payment guard:

minimum cadence 15s

normalizer has exhaustive GridQueryValidationException -> SemanticException boundary

no ex.Message classification

no PlatformHttpException in Payment normalizer

only the two approved Host Payment security adapters remain

Payment -> Host dependency remains zero

Recovery

On PASS update:

docs/architecture/tmar-current-state.json

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

durable guard expectations

Record:

parent repair = ACCEPTED_AFTER_R1_BEHAVIOR_PARITY_REPAIR

reconciliation cadence = MIN_15_SECONDS_PRESERVED

grid validation mapping = ALL_GRID_VALIDATION_ERRORS_STABLE_SEMANTIC_400

Payment Host residue = PAYMENT_RUNTIME_RESIDUE_REMOVED_SECURITY_ADAPTERS_ONLY

structure certification = PENDING_TB_TMAR_PAYMENT_ARCH_COMPLETE_002_STRUCTURE_001

Set:
nextTask = TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001

Do NOT certify Payment here.

Evidence

Create:
docs/evidence/TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1/payment-host-residue-parity-repair.md

Validation

Run only:

focused Payment reconciliation options/worker tests

focused Payment grid normalizer tests

PaymentArchitectureGuardTests

relevant Host Payment residue guards

TmarDurableGuardTests

TmarCompleteReferenceStructureGateTests

one final dotnet build src/backend/Tooba.slnx

PASS criteria

PASS only if:

effective minimum poll interval is 15 seconds

no connector validation path can escape as GridQueryValidationException

connector count/value produce stable grid.* semantic codes

central presentation maps them to 400

Host residue remains removed

only two approved Host Payment security adapters remain

Payment -> Host dependency remains zero

Payment is still NOT structure-certified

Checkout/frontend unchanged

focused tests pass

build passes

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001-R1
Parent-Task: TB-TMAR-PAYMENT-HOST-RESIDUE-REPAIR-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Verdict-State:
Reconciliation-Min-Cadence-State:
Grid-Exception-Boundary-State:
Advanced-Connector-Count-State:
Advanced-Connector-Value-State:
Filter-Field-State:
Filter-Operator-State:
SafeErrorMapper-400-State:
Host-Payment-Residue-State:
Approved-Host-Security-Adapters-State:
Payment-To-Host-Dependency-State:
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