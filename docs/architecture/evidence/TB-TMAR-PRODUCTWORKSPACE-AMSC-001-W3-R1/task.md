PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1
Parent-Task: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Mode: CERT_BLOCKER_REPAIR_ONLY

STARTING HEAD
c0b86feacd897d26172fef7dff47b93db75d8e9d

ARCHITECT VERDICT
W2 Structure is accepted.
W3 certification is NOT accepted as final because current disk/SoT violates two Certify hard rules.

BLOCKERS

Request inventory is inconsistent:

17 routes
3 query request types + 14 command request types = 17 endpoint-reachable CQRS requests
SoT records endpointReachableRequests = 18
validatorCoverageState says 0 REQUIRED / 0 NO_VALIDATOR_REQUIRED
Certify requires EVERY endpoint-reachable request to be classified exactly VALIDATOR_REQUIRED or NO_VALIDATOR_REQUIRED.

ProductWorkspaceEndpointModule contains raw Results.Json(..., statusCode: 201) for successful CreateProduct/CreateVariant paths.
Certify requires canonical ApiResponseFactory.From/Created.

GOAL
Repair only these blockers and return READY_FOR_FRESH_CERTIFY.

IMPLEMENT

Replace the two raw 201 Results.Json success mappings with the existing canonical ApiResponseFactory.Created equivalent, preserving exact 201 response body/contract.
Build an explicit exhaustive matrix of the 17 endpoint-reachable request TYPES:
3 queries
14 commands
Classify each exactly once as VALIDATOR_REQUIRED or NO_VALIDATOR_REQUIRED.
Do NOT invent validators blindly.
If a request's transport shape is deliberately owned/validated by the Catalog Contracts mutation boundary or an existing canonical policy, classify NO_VALIDATOR_REQUIRED with a concrete durable reason.
If ProductWorkspace itself owns unvalidated transport shape, add only the necessary FluentValidation validator with stable Application validation code.
Add/strengthen one scoped guard proving:
exact request count = 17
classification count REQUIRED + NO_VALIDATOR_REQUIRED = 17
every request appears exactly once
raw Results.Json in ProductWorkspace Endpoints = ZERO
Update SoT additively:
prior W3 c0b86fea = SUPERSEDED_PENDING_FRESH_CERTIFY
endpointReachableRequests = 17
validator coverage = exact matrix result, not 0/0
apiResultPatternState = CANONICAL
state = READY_FOR_FRESH_CERTIFY
Do not change structure, routes, verbs, DTO shapes, Catalog business rules, schema/migrations, error ownership, Host ownership, or manifest project layout.
Do not perform fresh Certify in this task.

ALLOWED

ProductWorkspace endpoint file
ProductWorkspace Application validator/code files only if classification proves required
ProductWorkspace W3/R1 guards
SoT + Master Recovery + R1 evidence/task

FORBIDDEN

unrelated production changes
manifest promotion/demotion
schema/migrations
structure moves
route/DTO/permission behavior changes
guard weakening/baseline widening

VALIDATE

build affected projects
focused ProductWorkspace AMSC guards
exact 17-request matrix
zero ProductWorkspace Endpoints Results.Json
Host global checkpoint unchanged

RESULT CONTRACT
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3-R1
Parent-Task: TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W3
Status: PASS | INCOMPLETE | CERT_REPAIR_CONFLICT
Summary: <short>
Request-Inventory-State: EXACT_17 | CONFLICT
Validator-Matrix-State: EXHAUSTIVE_17 | CONFLICT
Raw-Results-State: ZERO | NONZERO
Api-Result-State: CANONICAL | REGRESSED
Structure-State: UNCHANGED_W2_ACCEPTED | REGRESSED
Schema-Migration-State: UNCHANGED | REGRESSED
Global-Host-Checkpoint-State: PRESERVED | REGRESSED
Fresh-Certify-State: READY | BLOCKED
Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PRODUCTWORKSPACE_AMSC_001_W3_R1
STOP
END_TOOBA_WORKER_RESULT
STOP RULE
STOP. No fresh Certify automatically. No next module.
END_TOOBA_TASK