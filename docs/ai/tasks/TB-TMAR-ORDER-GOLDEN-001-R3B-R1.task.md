PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B-R1
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_ADMIN_OPERATIONS_CQRS_REPAIR
Title: Remove Fake CQRS Dispatcher from AdminOrderOperations
Backend-Only: YES

Architect verdict

R3B Host-removal work is accepted.

However R3B overall PASS is REJECTED because CQRS is still facade-style.

Architect directly verified:

CancelOrderHandler sets Code = "cancel" then calls AdminOrderOperationsOrchestrator.ExecuteAsync(...)

MarkFulfillmentProcessingHandler does the same with Code = "mark_processing"

other commands follow the same pattern

AdminOrderOperationsOrchestrator.ExecuteAsync(...) remains a central code-driven dispatcher

This violates the intended Offer-style CQRS boundary.

Exact repair goal

Keep all successful R3B Host removal.

Repair ONLY AdminOrderOperations CQRS so each explicit Command handler owns its actual use-case flow instead of forwarding to one generic code dispatcher.

Do NOT:

restore Host composer/endpoints

touch OrdersGrid

migrate Recovery/Supply wholesale

touch Storefront

start Checkout W6

Required target

Each command must dispatch to a typed use-case method/service, not to:

ExecuteAsync(checkoutId, actorId, request-with-Code)

Examples:

CancelOrderHandler -> typed CancelOrder use-case service/method

MarkFulfillmentProcessingHandler -> typed MarkProcessing use-case service/method

ApproveReturnHandler -> typed ApproveReturn use-case service/method

Shared policy/helper code is allowed.

A shared service is allowed only if it exposes typed methods such as:

CancelOrderAsync(...)

MarkProcessingAsync(...)

ApproveReturnAsync(...)

Forbidden:

generic ExecuteAsync(... request.Code ...)

central string switch dispatcher

handler that only rewrites Code

command routing by string inside Application

Queries

Queries may share typed projection services.

Keep:

GetAdminOrderOperations

ListAdminOrderReturnEligibility

No regression.

Models

If AdminOrderOperationRequest.Code remains for wire compatibility, Endpoint may use it ONLY to choose which Command to send.

After the endpoint chooses the Command, Application command execution must no longer depend on Code routing.

Prefer command-specific request parameters where practical.

Result semantics

Preserve:

Result / Result<T>

SemanticError stable codes

ApiResponseFactory

no PlatformHttpException in Order Application

no ex.Message classification

Host state

These must remain absent:

AdminOrderOperationsEndpoints.cs

AdminOrderOperationsComposer.cs

AdminOrderOperationsModels.cs

AdminFulfillmentCapabilityProjector.cs

Do not reintroduce them.

Guards

Add/strengthen guard proving:

no AdminOrderOperationsOrchestrator.ExecuteAsync

no Application switch on operation code

no command handler sets Request with { Code = ... } merely to route execution

every operation Command has a real typed handler path

Host removed files remain absent

Endpoints still use ISender

Validation

Run:

Order.Tests

AdminOrderOperations tests

Order architecture guards

focused Host regression tests

dotnet build src/backend/Tooba.slnx

PASS criteria

PASS only if:

generic ExecuteAsync dispatcher removed

central operation-code switch removed from Application

command handlers invoke typed use-case paths

behavior parity preserved

Host removal preserved

foreign boundaries remain Contracts-only

full backend build passes

Checkout remains W5 paused

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R3B-R1
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R3B
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Generic-Dispatcher-State:
Command-Handler-State:
Application-Code-Switch-State:
Host-Removal-Preserved:
Order-CQRS-State:
Behavior-Parity:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Residual-Defects:
Order-Overall-State:
Checkout-State:
Recovery-Next-Task:
Git:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start R4.
Do not migrate another Order surface.
Do not poll.

END_TOOBA_TASK