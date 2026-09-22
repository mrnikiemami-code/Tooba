PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B-R3
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B-R2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_ADMIN_OPERATIONS_TYPED_FAULT_SOURCE_REPAIR
Title: Remove Message Parsing from Contract Fault Boundaries
Backend-Only: YES

Architect verdict

R3B-R2 is NOT accepted as PASS.

Accepted:

Order.Application/Admin/Operations no longer classifies ex.Message

typed CQRS from R3B-R1 remains intact

Host AdminOrderOperations removal remains intact

Recovery SoT is synchronized

Rejected defect:
message-based classification was moved into shared/owning adapters instead of removed.

Directly verified examples:

BuildingBlocks ContractOperationFault.LooksLikeStableCode(ex.Message)

Fulfillment adapter TryMapExact(ex.Message) / LooksLikeStableCode(ex.Message)

Returns adapter same pattern

Settlement adapter same pattern

Payment adapter promotes ex.Message

Inventory lifecycle adapter promotes/maps ex.Message

Order CheckoutDirectory still promotes IOE by inspecting ioe.Message

This is forbidden architectural debt.

Reference

Use Offer-style stable Result/error semantics.

Expected business failures must originate as typed/stable failures at the owning use-case/domain boundary.

Do NOT infer semantic identity from exception text.

Exact repair goal

Eliminate message parsing/classification introduced or relied upon by R3B-R2.

Forbidden in touched operation path

ContractOperationFault.LooksLikeStableCode(ex.Message)

TryMapExact(ex.Message, ...)

StartsWith/Contains/Switch on exception message

converting arbitrary InvalidOperationException.Message into a contract code

Required

At the owning module source, emit one of:

typed exception carrying stable Code

Result/SemanticError

typed operation outcome/fault

Adapters may translate typed Code -> contract Code.
Adapters must NOT parse Message.

Unknown InvalidOperationException must propagate unchanged.

BuildingBlocks

ContractOperationFault must not remain as a generic message classifier.

Either:

remove it if no longer needed, or

change it to operate only on typed code-bearing failures without inspecting Message.

Do not create another helper with the same behavior under a different name.

Scope

Repair only paths required by AdminOrderOperations:

Fulfillment operation contracts/adapters

Returns operation contracts/adapters

Settlement order accrual path

Payment admin operation bridge

Inventory recovery/lifecycle path

Order restore/checkout path used by AdminOrderOperations

Do not perform repository-wide exception redesign.

Do not:

touch OrdersGrid

restore Host ops

migrate Recovery/Supply wholesale

touch Storefront

start R4

start Checkout W6

CQRS / Host locks

Preserve:

typed command handlers

no generic ExecuteAsync dispatcher

Order.Endpoints -> ISender

Host AdminOrderOperations files absent

Contracts-only foreign boundaries

Guards

Add durable guard(s) for touched paths proving:

no .Message semantic classification

no LooksLikeStableCode

no TryMapExact(ex.Message

no generic promotion from InvalidOperationException text

unknown exceptions propagate

typed stable code is present before adapter translation

Validation

Run:

Order.Tests

OrderAdminOperationsArchitectureGuardTests

focused operation tests

affected Fulfillment/Returns/Settlement/Payment/Inventory tests

affected architecture guards

dotnet build src/backend/Tooba.slnx

Recovery

On PASS:

Order remains INCOMPLETE_REFERENCE_REPAIR

record R3B-R3

nextTask = TB-TMAR-ORDER-GOLDEN-001-R4

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

Do NOT start R4.

PASS criteria

PASS only if:

No semantic error identity is inferred from exception Message in touched operation paths.

ContractOperationFault is not a message classifier.

Owning modules emit typed/stable failures at source.

Adapters consume typed codes only.

Unknown exceptions propagate.

R3B-R1 CQRS remains intact.

Host removal remains intact.

Tests/build pass.

Recovery SoT synced.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B-R3
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B-R2
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Message-Parsing-State:
Typed-Fault-Origin-State:
ContractOperationFault-State:
Adapter-State:
CQRS-Repair-Preserved:
Host-Removal-Preserved:
Contracts-Changed:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Recovery-State:
Recovery-Next-Task:
Residual-Defects:
Order-Overall-State:
Checkout-State:
Git:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start R4.
Do not migrate another surface.
Do not poll.

END_TOOBA_TASK