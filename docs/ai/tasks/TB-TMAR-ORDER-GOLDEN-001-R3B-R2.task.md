PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B-R2
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_ADMIN_OPERATIONS_TYPED_ERROR_REPAIR
Title: Remove Message-Based Exception Classification from AdminOrderOperations
Backend-Only: YES

Architect verdict

R3B-R1 CQRS repair is accepted:

generic ExecuteAsync/ExecuteCoreAsync removed

typed command paths exist

Host AdminOrderOperations files remain removed

But overall PASS is rejected because Order Application still classifies expected failures using ex.Message.

Architect directly verified in:
Tooba.Order.Application/Admin/Operations/Services/AdminOrderOperationsOrchestrator.cs

Remaining debt includes:

ex.Message.StartsWith("order.cancel.forbidden"...

ex.Message == "fulfillment.cancel.already_dispatched"

ex.Message == "settlement.cancel.payout_completed"

MapKnownOperationException(ex.Message)

TryMapReturnCode(ex.Message, ...)

Recovery SoT also still points active task to R3B instead of recording R3B-R1/R2 progress.

Exact goal

Remove expected business failure classification by exception message from AdminOrderOperations.

Do NOT:

change CQRS structure

restore Host ops files

touch OrdersGrid

migrate Recovery/Supply

touch Storefront

start R4

Required approach

Use typed/stable outcomes from owning module boundaries.

For touched foreign operations:

Fulfillment.Contracts

Returns.Contracts

Settlement.Contracts

Payment.Contracts

Inventory.Contracts if needed

Expected failures must cross boundaries as:

Result / typed outcome / stable error code
or equivalent stable contract semantics.

Order Application may map stable codes to Order Result semantics.

Forbidden:

ex.Message

exception.Message

StartsWith/Contains message classification

switch on exception text

localized prose classification

Unknown exceptions must propagate.

Cancel path

Replace message checks for:

order.cancel.forbidden

fulfillment.cancel.already_dispatched

settlement.cancel.payout_completed

with typed/stable contract outcomes.

Fulfillment / Returns path

Remove:

MapKnownOperationException(ex.Message)

TryMapReturnCode(ex.Message, ...)

Use stable contract codes/outcomes instead.

Guards

Strengthen OrderAdminOperationsArchitectureGuardTests:

Fail if Admin Operations Application contains:

ex.Message

exception.Message

when (ex.Message

StartsWith( against error text

message-based mapping helpers

Also keep guards for:

no generic ExecuteAsync dispatcher

Host ops files absent

typed handlers preserved

Recovery SoT

On PASS:

record R3B-R1 and R3B-R2 repair progression

active Order recovery remains INCOMPLETE_REFERENCE_REPAIR

nextTask = TB-TMAR-ORDER-GOLDEN-001-R4

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

Do NOT start R4.

Validation

Run:

Order.Tests

OrderAdminOperationsArchitectureGuardTests

focused Host ops regression tests

protected module guards for any Contracts changed

dotnet build src/backend/Tooba.slnx

PASS criteria

PASS only if:

no expected failure classification uses exception message in Admin Operations Application

stable typed/contract outcomes used

unknown exceptions propagate

CQRS R3B-R1 structure preserved

Host removal preserved

tests/build pass

Recovery SoT synced

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B-R2
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Message-Classification-State:
Typed-Outcome-State:
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