PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001
Parent-Task: TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook pre-certification structure audit
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001

ACCEPTED-PARENT-COMMIT:
8893bdfc530f8f2599b62072a61a2d7b1e2f9489

RECOVERY-SOT-CHECKPOINT:
c49287d574da5143e09ee290d5ff484710f6e32c

CURRENT-STATE:

Host AddressBook HTTP ownership = ZERO.
Host AddressBook folder residue = ZERO.
All 6 AddressBook routes are module-owned via ISender.
AddressBook CQRS = COMPLETE_6_USECASES.
Validator coverage = COMPLETE_5_OF_5_REQUIRED + 1 NO_VALIDATOR_REQUIRED.
Actor seam = COMPLETE_INTERFACE_BASED.
AddressBook remains IN_PROGRESS.
AddressBook is NOT yet ARCH-COMPLETE-002 STRUCTURE_CERTIFIED.
Frontend remains FROZEN.
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.

TIMEBOX:
Target <= 8 minutes.
Hard maximum 12 minutes.
If a complete and reliable audit cannot finish inside the hard limit:
Status = INCOMPLETE
STOP.
No refactor.
No retry loop.
No scope expansion.

ONE OBJECTIVE ONLY:

Perform a bounded, read-mostly pre-certification architecture audit of the current AddressBook module against ARCH-COMPLETE-002 and produce an exact gap list for the final repair/certification path.

THIS TASK IS AUDIT-ONLY.

Do NOT refactor production structure.
Do NOT move files.
Do NOT rename namespaces.
Do NOT add/remove validators.
Do NOT alter endpoint ownership.
Do NOT modify CQRS behavior.
Do NOT certify the module.
Do NOT edit frontend.
Do NOT touch checkout behavior.

MANDATORY AUDIT — ADDRESSBOOK MODULE:

Audit these projects physically and semantically:

src/backend/Modules/AddressBook/Tooba.AddressBook.Contracts
src/backend/Modules/AddressBook/Tooba.AddressBook.Domain
src/backend/Modules/AddressBook/Tooba.AddressBook.Application
src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure
src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints

Compare against:

docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
docs/architecture/TMAR-architecture-locks.md
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json

AUDIT DIMENSIONS:

APPLICATION CAPABILITY STRUCTURE
Classify every production .cs file under Tooba.AddressBook.Application by:
physical path
namespace
capability ownership
whether it is valid under ARCH-COMPLETE-002
whether it is an illegal project-root capability file
whether it belongs under Commands / Queries / Models / Ports / Policies / Services / Validators or a coherent capability folder

Explicitly identify all root .cs files.
Determine exact root allowlist candidate.
Do not move anything.

ENDPOINTS CAPABILITY STRUCTURE
Audit all production .cs files under Tooba.AddressBook.Endpoints.
Confirm:
capability grouping is coherent
endpoint files are not improperly dumped at project root
AddressBookEndpointModule is the single composition entry
every module route is mapped exactly once
no duplicate Host ownership remains
namespace matches physical path exactly
no alias workaround is hiding folder debt

Determine exact root allowlist candidate.
Do not move anything.

INFRASTRUCTURE CAPABILITY STRUCTURE
Audit all production .cs files under Tooba.AddressBook.Infrastructure.
Classify:
Persistence
Development
adapters/integrations
module composition
directory/service implementation
any project-root implementation file
any competing/duplicated top-level folder responsibility

Confirm the moved development seed is correctly placed in:
Tooba.AddressBook.Infrastructure/Development

Determine exact root allowlist candidate.
Do not move anything.

CONTRACTS / DOMAIN STRUCTURE
Audit Contracts and Domain for:
root dumping
misplaced DTO/error/port files
path↔namespace mismatch
foreign Application/Infrastructure/Host dependency
capability ownership ambiguity

Do not invent ceremonial folders where none are required.
Only report real structural debt.

PATH ↔ NAMESPACE EXACTNESS
For all non-generated AddressBook production .cs files:
compare namespace to physical path-derived namespace
list every mismatch exactly
distinguish legitimate root namespace files from mismatches
list any namespace aliases used only to hide physical-structure debt

Generated migrations/snapshots may be excluded only if the standard already permits that.

ENDPOINT-REACHABLE REQUEST INVENTORY
Produce an exact inventory of all AddressBook MediatR requests reachable from AddressBook.Endpoints.

Expected current total from accepted recovery state:
6 requests.

For each request record:

endpoint capability/route
request type
command/query
validator classification:
VALIDATOR_REQUIRED
NO_VALIDATOR_REQUIRED
validator type if present
justification when NO_VALIDATOR_REQUIRED

Verify current accepted expectation:
5 VALIDATOR_REQUIRED / 5 PRESENT
1 NO_VALIDATOR_REQUIRED

If reality differs, report it.
Do NOT repair it in this task.

MEDIATR / CQRS CHECK
Confirm all 6 HTTP use cases:
are IRequest-based
are dispatched via ISender
have real MediatR handlers
do not call Infrastructure/Directory directly from endpoints
do not reintroduce legacy dispatcher abstractions
do not bypass CQRS through Host

Do NOT change code.

HOST → ADDRESSBOOK COMPOSITION EDGES
Audit remaining Host references to AddressBook.

Especially inspect:
src/backend/Host/Tooba.Host/Program.cs

Classify each remaining Host → AddressBook reference as one of:

ALLOWED_COMPOSITION_ROOT
ALLOWED_SECURITY_ADAPTER
ALLOWED_CONTRACT_CONSUMPTION
ILLEGAL_BUSINESS_AUTHORITY
ILLEGAL_PERSISTENCE_AUTHORITY
ILLEGAL_ENDPOINT_OWNERSHIP
STRUCTURAL_DEBT_ONLY

The audit must explicitly confirm whether:

AddressBook.Application assembly registration in Host is legitimate composition
AddressBook.Infrastructure registration in Host is legitimate composition
AddressBook.Contracts checkout lookup/reference is legitimate cross-module contract consumption
any Host implementation/business logic for AddressBook still exists

Do not remove legitimate composition edges merely to make Host references zero.

CROSS-MODULE BOUNDARIES
Audit AddressBook references to other Tooba modules.

Classify every foreign module dependency:

Contracts = allowed when justified
Application = suspicious/forbidden unless explicitly architected
Infrastructure = forbidden cross-module
Domain = forbidden cross-module
Host = forbidden

Explicitly confirm the current Order dependency in AddressBook.Infrastructure is:
Tooba.Order.Contracts only
and no Order.Application dependency remains.

ROOT ALLOWLIST / MANIFEST READINESS
Using the real current tree, propose the exact ARCH-COMPLETE-002 manifest values needed for AddressBook:
project root .cs allowlists
httpApplicability
endpointOwnership
cqrs
validator coverage
pathNamespace state
alias state
Host residue state
structureCertified MUST remain false in this task

Do NOT edit:
docs/architecture/tmar-module-structure-manifests.json

This task only produces the proposed values and gap list.

FINAL GAP CLASSIFICATION
Every discovered issue must be classified into exactly one of:

A. CERTIFICATION_BLOCKER — must be repaired before ARCH-COMPLETE-002 certification
B. NON_BLOCKING_DEBT — real debt but not required for certification
C. ACCEPTABLE_BY_STANDARD — intentionally valid, no repair needed

For every CERTIFICATION_BLOCKER include:

exact file(s)
exact problem
exact required fix
whether production behavior should remain unchanged
estimated repair grouping

Do not propose broad redesign.

PROTECTED STATE — MUST REMAIN UNCHANGED:

Host AddressBook HTTP ownership = ZERO
Host AddressBook residue = ZERO
all 6 AddressBook routes module-owned
ISender/MediatR path
AddressBook behavior
AddressBook actor seam behavior
seed semantics
Order.Contracts guest actor authority
checkout behavior
frontendFrozen = true
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
existing certified modules and their manifests
no unrelated module changes

ALLOWED FILE CHANGES:

ONLY:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001.task.md
docs/evidence/TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001/addressbook-structure-audit.md

Optionally, a narrowly scoped test/evidence helper file ONLY if absolutely required to make the audit deterministic and it does not change production behavior.

Prefer no code changes.

FORBIDDEN FILE CHANGES:

Any AddressBook production .cs file
Any AddressBook .csproj
Any Host production .cs file
Any frontend file
Any checkout file
docs/architecture/tmar-module-structure-manifests.json
docs/architecture/tmar-current-state.json
certification lock files
unrelated tests/baselines

FOCUSED VALIDATION:

This is primarily a structural/source audit.

Required:

verify repository main state is safe before work
inspect the exact AddressBook production tree
inspect AddressBook.Endpoints route/request mapping
inspect validator types and discovery
inspect Host references to AddressBook
inspect project references for forbidden cross-module dependencies

Run only focused build/test commands if needed to validate audit claims.

Preferred maximum:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Application/Tooba.AddressBook.Application.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Infrastructure/Tooba.AddressBook.Infrastructure.csproj --no-restore

If AddressBookFoundationTests materially validate a claim:
dotnet test src/backend/Host/Tooba.Host.Tests --filter FullyQualifiedName~AddressBookFoundationTests

No solution build.
No broad integration suite.
No broad architecture suite.
No retry cascade.

CANONICAL TASK ARTIFACT:

Commit this exact received task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001/addressbook-structure-audit.md

Evidence must contain at minimum:

Current accepted checkpoint
Physical tree summary for all 5 AddressBook projects
Application structure classification
Endpoints structure classification
Infrastructure structure classification
Contracts/Domain structure classification
Exact path↔namespace mismatch list
Exact root .cs file lists per project
Proposed root allowlists
Endpoint route → request → handler → validator matrix
Exact validator coverage counts
Host → AddressBook reference inventory and classification
Cross-module project/reference inventory
Alias workaround audit
CERTIFICATION_BLOCKER list
NON_BLOCKING_DEBT list
ACCEPTABLE_BY_STANDARD list
Exact proposed manifest values, with structureCertified=false
Focused validation results
Explicit untouched list
Exact next recommended task

SUCCESS CRITERIA:

PASS only if:

no production behavior was changed
complete AddressBook physical tree was audited
every production .cs file relevant to structure was classified
path↔namespace state is explicitly established
root allowlists are proposed from real current files
exact 6 endpoint-reachable CQRS requests are inventoried, or any discrepancy is explicitly proven
validator coverage is proven from source
Host → AddressBook references are exhaustively classified
Order.Contracts-only dependency state is verified
all certification blockers are explicitly listed
no refactor/certification is performed
task artifact and evidence are committed
commit is pushed to origin/main
after fetch, HEAD == origin/main
working tree is safe/clean except explicitly preserved user work

EXPECTED NEXT DECISION:

If audit finds certification blockers:
next task must be ONE bounded repair task based on the exact blocker set.

If audit finds no certification blockers:
next task may be:
TB-TMAR-ADDRESSBOOK-ARCH-COMPLETE-002-STRUCTURE-001

Do NOT start either automatically.

RECOVERY HONESTY:

After this audit:
AddressBook remains IN_PROGRESS.
AddressBook remains NOT STRUCTURE_CERTIFIED.
No COMPLETE_REFERENCE_PATTERN / ARCH-COMPLETE-002 certification may be newly claimed by this task.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-STRUCTURE-AUDIT-001
Parent-Task: TB-TMAR-ADDRESSBOOK-DEVELOPMENT-SEED-CLEANUP-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Audit-Mode-State:
Physical-Tree-State:
Application-Structure-State:
Endpoints-Structure-State:
Infrastructure-Structure-State:
Contracts-Domain-Structure-State:
Path-Namespace-State:
Root-Allowlist-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Present-Count:
No-Validator-Required-Count:
Validator-Coverage-State:
MediatR-CQRS-State:
Host-AddressBook-Reference-State:
Host-Business-Authority-State:
Cross-Module-Boundary-State:
Order-Contracts-Dependency-State:
Alias-Workaround-State:
Certification-Blocker-Count:
Certification-Blockers:
Non-Blocking-Debt:
Acceptable-By-Standard:
Proposed-Manifest-State:
Structure-Certified-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start repair.
Do not auto-start certification.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK