PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2-R1
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Panel AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_PANEL_W2_STRUCTURE_REPAIR
Title: Repair Party Admin/Sellers validator placement to canonical capability-first structure
Estimated-Time-Minutes: 8
Hard-Timebox-Minutes: 12

ARCHITECT REVIEW

W2 functional migration is ACCEPTED:

POST /v1/admin/sellers/query owner = Party.Endpoints
Host POST residue = ZERO
Panel -> AdminGridQueryEndpoint dependency = ZERO
ISender + Result + ApiResponseFactory present
IAdminPanelAccess used
grid.* error codes preserved
ex.Message presentation/classification = ZERO
dashboard/dev-context unchanged
implementation commit = 5179aeac3b247948f35f0c8d40edb11abb8442b3

ONE STRUCTURAL BLOCKER remains in the W2 touched Party surface.

Current files:

src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/QueryAdminSellersGridQueryValidator.cs

src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/PartyAdminSellersValidationCodes.cs

This is not the preferred canonical responsibility structure from the TMAR skills.

Validator responsibility must live under Validators, not Queries.
Validation-code responsibility should be colocated with validators for this capability.

No behavior problem has been found.
This is STRUCTURE_ONLY repair.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-migrate/SKILL.md

This repair is MIGRATE/STRUCTURE only.

Do NOT certify.
Do NOT start W3.
Do NOT touch Host/Admin/Panel runtime behavior.
Do NOT touch dashboard/dev-context.
Do NOT change seller route behavior.

REQUIRED REPAIR

Move:

src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Queries/QueryAdminSellersGridQueryValidator.cs

to:

src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Validators/QueryAdminSellersGridQueryValidator.cs

Move:

src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/PartyAdminSellersValidationCodes.cs

to:

src/backend/Modules/Party/Tooba.Party.Application/Admin/Sellers/Validators/PartyAdminSellersValidationCodes.cs

Required namespaces:

Tooba.Party.Application.Admin.Sellers.Validators

Repoint only the required using/reference from the query validator / request surface.

Do NOT create aliases.
Do NOT create forwarding types.
Do NOT leave stale copies.
Do NOT duplicate validation codes.
Do NOT change machine code values.

PRESERVE EXACTLY

QueryAdminSellersGridQuery type
QueryAdminSellersGridQueryHandler behavior
QueryAdminSellersGridQueryValidator behavior
GridRequestRequired code:
party.admin.sellers.validation.grid_request_required
VALIDATOR_REQUIRED classification
GET /v1/admin/sellers
POST /v1/admin/sellers/query
IAdminPanelAccess
ISender
Result<GridPageResponse<AdminSellerListItem>>
ApiResponseFactory
GridQueryValidationException -> SemanticError(ex.ErrorCode)
grid.* error semantics
Host/Admin/Panel file count = 3
dashboard unchanged
dev-context unchanged
schema unchanged
frontend unchanged

PATH / NAMESPACE

After repair:

Admin/Sellers/Queries/

ListAdminSellersQuery.cs
QueryAdminSellersGridQuery.cs

Admin/Sellers/Validators/

QueryAdminSellersGridQueryValidator.cs
PartyAdminSellersValidationCodes.cs

All namespaces must exactly match physical path.

No root dump.
No one-file wrapper folder.
No duplicate old physical copy.

FOCUSED VALIDATION ONLY

Build:

Tooba.Party.Application
Tooba.Party.Endpoints
Host only if a compile reference requires it

Run only directly affected:

validator discovery/coverage guard
Party/Host admin sellers focused guard
path/namespace guard if available

No solution-wide tests.
No open-ended test loop.

At most:
ONE deterministic repair if validation fails, then ONE affected rerun.

RECOVERY / SOT

On PASS:

lastAcceptedTask = TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2-R1
lastAcceptedCommit = actual R1 implementation SHA
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
W2 behavior remains POST_SELLERS_QUERY_MOVED_TO_PARTY
W2-R1 state = PARTY_ADMIN_SELLERS_VALIDATOR_STRUCTURE_REPAIRED
current Host checkpoint remains Admin/Panel
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_PANEL_AMC_001_W2_R1
staleCurrentPointerState = ZERO

If docs/stamp is separate, do not replace lastAcceptedCommit with docs SHA.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2-R1/

Required:

structure.md
namespace.md
validation.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2-R1.task.md

SUCCESS CRITERIA

PASS only if:

validator physically under Admin/Sellers/Validators
validation codes physically under Admin/Sellers/Validators
namespaces = Tooba.Party.Application.Admin.Sellers.Validators
stale old copies = ZERO
duplicate validation code type = ZERO
GridRequestRequired value unchanged
validator still discovered
sellers GET/POST behavior unchanged
Host Panel unchanged
hard-coded runtime FA/EN state unchanged ZERO on W2 route
ex.Message presentation/classification remains ZERO
focused validation passes
Recovery points to actual R1 implementation SHA
automaticNextImplementationTask = NONE
STOP for Architect review

GIT

Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2-R1
Parent-Task: TB-TMAR-HOST-ADMIN-PANEL-AMC-001-W2
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Repair-State:
Validator-Path-State:
ValidationCodes-Path-State:
Path-Namespace-State:
Old-Physical-Copy-State:
Duplicate-Type-State:
Validation-Code-Value-State:
Validator-Discovery-State:
Get-Sellers-State:
Post-Sellers-Query-State:
Dashboard-State:
Dev-Context-State:
Host-Panel-State:
Hardcoded-User-Facing-Text-State:
Exception-Message-Classification-State:
Schema-Change-State:
Frontend-State:
Focused-Build-State:
Focused-Test-State:
Guard-State:
Recovery-State:
Last-Accepted-Commit-State:
Last-Accepted-Commit-Kind:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start W3.
Do not move dev-context.
Do not change dashboard.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK