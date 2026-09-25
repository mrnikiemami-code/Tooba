PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001
Parent-Task: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: SETTLEMENT_ARCH_COMPLETE_002_BOUNDED_AUDIT
Title: Audit Settlement for ARCH-COMPLETE-002 certification readiness
Backend-Only: YES
Audit-Only: YES

Architect verdict

TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001 is ARCHITECT-ACCEPTED.

Accepted certification commit:
3e403aaffb4e1f79f41bd7fd25de6fdaf0f708b0

Accepted SoT stamp:
0ae295e50b2e7adcefd6e2ca40001fbdb601d7d7

Payment is now:

COMPLETE_REFERENCE_PATTERN
HTTP_OWNING
MODULE_ENDPOINTS
MEDIATR_12_5
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED

Certified set:
Order, Cart, StoreContext, Offer, Payment

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.
frontendFrozen = true.

Why Settlement is next

Current uncertifiedHttpOwningModules begins with Settlement.

Settlement is already Golden / COMPLETE_REFERENCE_PATTERN.
This task does NOT change Settlement production code.
It only determines the smallest exact certification path.

One objective

Produce a bounded, deterministic ARCH-COMPLETE-002 readiness audit for Settlement.

Do NOT:

change Settlement production code
move files
add validators
change namespaces
change Host code
change manifest certification
certify Settlement
touch Payment
touch Checkout
touch frontend
audit unrelated modules
Audit A — endpoint + MediatR inventory

Exhaustively inventory every Settlement.Endpoints request construction.

For each request record:

request type
Command or Query
endpoint route
IRequest status
real IRequestHandler present
dispatched through ISender
any direct Application service/Directory/DbContext call from endpoint

Record:

exact endpoint-reachable request count
total Settlement MediatR request count
any worker/internal-only request separately

No representative sampling.

Audit B — FluentValidation coverage

For every endpoint-reachable request classify exactly:

VALIDATOR_REQUIRED
or
NO_VALIDATOR_REQUIRED_<REASON>

For VALIDATOR_REQUIRED record:

expected validator type
present/missing
whether discoverable by existing AddValidatorsFromAssembly / AddToobaCqrsFoundation

Transport shape only.
Do not propose business/domain rules as FluentValidation rules.

Produce totals:

endpoint requests
validator required
validators present
validators missing
no-validator-required
Audit C — physical structure

Audit current live folders for:

Tooba.Settlement.Application
Tooba.Settlement.Endpoints
Tooba.Settlement.Infrastructure
Contracts if relevant to path/namespace certification

Record:

exact top-level folders
root .cs files
likely root allowlist
forbidden flattened files
capability/integration grouping quality

Do NOT move anything.

Audit D — exact path <-> namespace

Scan Settlement production .cs files.

Classify:

exact match
mismatch
generated migration/model snapshot exemption only where legitimate

Do not accept StartsWith/prefix-only alignment.

Record every mismatch by exact path.

Audit E — namespace alias / global using workarounds

Inventory:

GlobalUsings*.cs
namespace aliases
type forwarding
compatibility shims

Classify each as:

legitimate project-wide import
narrow self-namespace import
workaround/debt requiring repair

No changes.

Audit F — Host Settlement residue

Exhaustively inventory Host files/call sites containing Settlement ownership or Settlement-specific route/runtime/grid/business logic.

Classify each:

KEEP_AS_EXPLICIT_THIN_HOST_SECURITY_ADAPTER
MOVE_TO_SETTLEMENT_MODULE
REMOVE_DEAD_RESIDUE
NOT_SETTLEMENT_AUTHORITY

Prove whether:
Settlement -> Host dependency = ZERO

Do not modify Host.

Audit G — cross-module boundaries

Check Settlement production project references/usings for:

foreign Application
foreign Infrastructure
foreign Domain
foreign DbContext
Tooba.Host

Contracts-only boundaries are allowed where already architecturally legitimate.

Record exact violations if any.

Audit H — structure guard quality

Review existing Settlement architecture tests/guards.

Identify whether guards currently prove:

exact path-derived namespaces
root allowlists
alias-workaround rejection
endpoint exhaustive request inventory
validator coverage
MediatR 12.5
ISender-only endpoints
Host residue boundary
cross-module boundary

Record exact gaps.

Do NOT strengthen guards in this audit.

Audit I — certification plan

End with one deterministic decision:

READY_FOR_SINGLE_STRUCTURE_TASK
or
NEEDS_PRECERT_REPAIR_THEN_STRUCTURE

If repair is needed:

enumerate exact repair task scope
do not execute it

If no repair is needed:

next task must be:
TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-STRUCTURE-001
Protected state

Preserve:

ARCH-COMPLETE-002
HOST-MODULE-ENDPOINT-001
ARCH-CQRS-001/002
Payment certification
Cart/Order/StoreContext/Offer certification
Checkout pause
frontend freeze
FAST-AUDIT-BUDGET

Prefer static inspection only.

Run ONLY if needed to verify existing guard baseline:

focused Settlement architecture test class(es)
one Settlement test-project build:
dotnet build src/backend/Modules/Settlement/Tooba.Settlement.Tests/Tooba.Settlement.Tests.csproj --no-restore

Do NOT run:

full Settlement tests
Host suite
TMAR broad suite
solution tests
full solution build
Testcontainers
database integration
retries

If the exact Settlement test project path differs, locate the existing Settlement test csproj and build only that project.

If any validation hangs:
return INCOMPLETE and STOP.

Recovery

Audit-only SoT update may record:

audit task
audit state
counts/findings
split decision
exact next task

Do NOT:

mark Settlement structureCertified
add Settlement to certifiedModules
remove Settlement from uncertifiedHttpOwningModules

Set nextTask according to the audit decision only.

Evidence

Create:
docs/evidence/TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001/settlement-arch-complete-002-audit.md

Evidence must include:

endpoint inventory
validator matrix
physical folders/root files
namespace findings
alias/global-using findings
Host residue classification
cross-module findings
guard gaps
deterministic next-task decision
Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-SETTLEMENT-ARCH-COMPLETE-002-AUDIT-001
Parent-Task: TB-TMAR-PAYMENT-ARCH-COMPLETE-002-STRUCTURE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Payment-Certification-State:
Settlement-Current-State:
Endpoint-Reachable-Request-Count:
Total-MediatR-Request-Count:
Worker-Internal-Request-Count:
Validator-Required-Count:
Validators-Present-Count:
Validators-Missing-Count:
No-Validator-Required-Count:
Validator-Coverage-State:
Application-Physical-Structure:
Endpoints-Physical-Structure:
Infrastructure-Physical-Structure:
Root-File-State:
Path-Namespace-State:
Alias-Workaround-State:
Host-Settlement-Residue-State:
Settlement-To-Host-Dependency-State:
CrossModule-Boundary-State:
Architecture-Guard-State:
Production-Code-Changed:
Audit-Decision:
Focused-Validation:
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
Do not repair Settlement automatically.
Do not structure-certify Settlement automatically.
Do not start another module.
Do not resume Checkout.
Do not touch frontend.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK