PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1
Parent-Task: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ROOT_GLOBAL_BOUNDARIES
Title: Resolve certification blockers, reconcile ChatGPT branch with latest main, certify, and land on main

START CONDITION — MANDATORY

DO NOT START while another Cursor worker is still changing Authorization or main.

Start ONLY after the current Authorization task has:

returned its canonical BRIDGE-WAKE-V1 result,
committed and pushed,
HEAD == origin/main,
tracked working tree clean.

At start:

git fetch origin
record exact latest origin/main SHA
verify current Authorization work is present on origin/main
verify branch chatgpt/host-root-boundaries-001 exists at or descends from:
70d1375969af80177983735b02614056d427d6f9
if either state is ambiguous => RECOVERY_CONFLICT + STOP

DO NOT overwrite or discard Authorization work.

SOURCE WORK TO RECONCILE

ChatGPT completed Analyze + Migration on:
chatgpt/host-root-boundaries-001

Current known branch head:
70d1375969af80177983735b02614056d427d6f9

Base before parallel Authorization work:
3ad429dcfef9b8b254ca81e02a4097d520a3eae7

The branch contains the bounded migration for exactly these former Host-root files:

CheckoutReservationHoldPolicy.cs
CommerceHoldPolicy.cs
GlobalUsings.SettlementApp.cs
GlobalUsings.SettlementDomain.cs
UnpaidOrderExpiryHostedService.cs
UnpaidOrderExpiryHostOptions.cs

The six Host files are removed on the branch.

Existing evidence/task:

docs/ai/tasks/TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001.task.md
docs/evidence/TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001/
HostRootGlobalBoundariesGuardTests.cs

CURRENT MIGRATION SHAPE TO PRESERVE UNLESS REPAIR REQUIRES A CLEANER EQUIVALENT

Catalog:

IStoreHoldPolicyHoursReader in Tooba.Catalog.Contracts.Reservation
existing store hold settings persistence implements the read seam

Payment:

hold-resolution responsibility moved to Payment
Payment consumes Catalog only through Catalog.Contracts

Order:

checkout reservation hold consumes Payment through Payment.Contracts
unpaid-order-expiry worker moved into Order ownership
Host root no longer owns worker/options

Settlement:

Host global-using shims removed

Host:

root business registrations for these six responsibilities removed
composition-only behavior must remain

WHY R1 EXISTS

The initial migration was deliberately NOT declared certified because:

ChatGPT's connector environment could not execute required dotnet build/tests.
The migration touched Tooba.Order.Infrastructure/OrderModule.cs and
Tooba.Order.Infrastructure.csproj, and that pre-existing surface contains direct foreign
*.Application dependencies. The architecture certify skill forbids certifying a touched
destination surface while a known foreign Application/Infrastructure/Domain violation remains.

This blocker MUST be resolved, not waived.

MANDATORY SKILL ORDER

Run the repository skills in this exact order for THIS bounded recovery unit:

.cursor/skills/tooba-architecture-analyze/SKILL.md
.cursor/skills/tooba-architecture-migrate/SKILL.md
.cursor/skills/tooba-architecture-certify/SKILL.md

Read current repository architecture SoT/locks first.

Do not downgrade any skill requirement.

PHASE A — SAFE RECONCILIATION

Create a temporary integration branch FROM LATEST origin/main.

Do NOT rebase main.
Do NOT reset/clean.
Do NOT overwrite user work.
Do NOT blindly merge a stale branch over latest main.

Recommended safe flow:

switch to latest main
create bounded integration branch, e.g.
cursor/host-root-global-boundaries-001-r1
reconcile the diff from chatgpt/host-root-boundaries-001
preserve BOTH:
all latest Authorization/main changes
the bounded six-file migration

If conflicts occur:

resolve only exact overlapping files
preserve latest main architecture changes first
reapply only the semantically required root-boundary migration
if conflict requires guessing => RECOVERY_CONFLICT + STOP

Pay special attention to:

Program.cs
docs/architecture/tmar-current-state.json
project references / module registration
architecture guards

PHASE B — RE-ANALYZE THE CERTIFICATION BLOCKER

Audit the touched destination surface.

Required question:
Can the Order portion of this migration be made certifiable WITHOUT making the already-dirty
OrderModule.cs / Tooba.Order.Infrastructure.csproj part of this touched recovery surface?

Preferred smallest lawful solution:

isolate registration for the new Order-owned worker/adapter into a NEW focused,
capability-owned DI registration file under Order.Infrastructure, OR reuse an existing clean
module-owned registration seam if one already exists;
remove this migration's edits to pre-existing dirty OrderModule.cs if not required;
remove this migration's edit to the pre-existing dirty Tooba.Order.Infrastructure.csproj
if the required Contracts dependency is already legally available through an existing project
dependency;
if an explicit project reference is truly required, the resulting project dependency graph must
still satisfy certification: NO new foreign Application/Infrastructure/Domain edge.

Alternative lawful solution is allowed if repository inspection proves it cleaner, for example:

implement the tiny checkout hold adapter in an Order Application capability that depends only on
Payment.Contracts, if that follows the repository's semantic ownership and project references;
expose a narrow module-owned DI extension consumed by Host composition.

Do NOT create:

a sixth module project
shared god contracts
Host business implementation
Application-to-Application references
Infrastructure-to-Infrastructure references
Domain-to-Domain references
compatibility aliases/shims
hidden global usings

If certifying this migration genuinely requires removing pre-existing foreign-Application
dependencies from the touched Order registration/project surface, repair those exact blockers now
using Contracts seams. Do not simply label them pre-existing debt.

The blocker must end in one of:

ISOLATED_FROM_TOUCHED_SURFACE_AND_CERTIFIABLE
REPAIRED_TO_LEGAL_CONTRACTS_ONLY

Anything else => INCOMPLETE.

PHASE C — BEHAVIOR PRESERVATION

Preserve exactly:

Commerce hold precedence:
payment method > store override > Payment:Gateway default

Preserve:

clamp semantics
online hold
manual initial hold
manual review hold
ResolveInitialExpiresAt
ResolveUnpaidTimeoutAt
ResolveManualReviewExpiresAt

Preserve unpaid expiry:

config section Tooba:UnpaidOrderExpiry
default Enabled=true
PollIntervalSeconds=15
BatchSize=20
worker name unpaid-order-expiry
metric tooba.unpaid_expiry.expired
tenant iteration
per-tenant fault isolation
logging semantics
IUnpaidOrderExpiryReconciler behavior

No schema or migration changes.

PHASE D — REQUIRED CERTIFICATION AUDIT

Certification must cover BOTH Host removal and touched destinations.

Required zero states:

HOST ROOT

all six requested root files = ZERO
Settlement Application/Infrastructure/Domain global-usings = ZERO
Host root business registration for these responsibilities = ZERO

NEW / TOUCHED BOUNDARIES

Payment hold implementation -> Host = ZERO
Payment hold implementation -> Catalog.Infrastructure = ZERO
Payment hold implementation -> Catalog.Domain = ZERO
Payment -> Catalog boundary = Contracts-only
Order checkout hold -> Payment boundary = Contracts-only
moved Order worker -> Host = ZERO

CERTIFICATION QUALITY

foreign Application dependency introduced/touched by this recovery = ZERO
foreign Infrastructure dependency introduced/touched by this recovery = ZERO
foreign Domain dependency introduced/touched by this recovery = ZERO
cross-module DbContext access = ZERO
cross-module EF join = ZERO
service locator in migrated business code = ZERO except canonical scoped worker composition where
the architecture skill explicitly permits module worker scope resolution
message parsing = ZERO
no hardcoded new user-facing error contract
no parallel logging/telemetry/correlation pipeline
path/namespace exact
no stale copies / compatibility aliases

RE-READ every touched production file before declaring PASS.

PHASE E — FOCUSED VALIDATION

Run builds ONCE each unless one fails:

Tooba.Catalog.Contracts
Tooba.Catalog.Infrastructure
Tooba.Payment.Infrastructure
Tooba.Order.Application if changed
Tooba.Order.Infrastructure
Tooba.Host
Tooba.Host.Tests

Focused tests only:

HostRootGlobalBoundariesGuardTests
relevant commerce-hold tests
relevant checkout reservation hold tests
relevant unpaid-order-expiry tests
any architecture guard directly protecting the touched Catalog/Payment/Order boundaries

Also run the specific architecture guard proving no forbidden foreign layer dependency in the
touched surface.

DO NOT run solution-wide tests.
DO NOT repair unrelated failures.
ONE bounded repair iteration maximum per deterministic local failure.
Second failure or ambiguous failure => INCOMPLETE + exact blocker + STOP.

MAX_REPAIR_ITERATIONS = 1
NO OPEN-ENDED LOOP.

PHASE F — EVIDENCE / SOT

Create R1 evidence:

docs/evidence/TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1/

reconcile.md
blocker-analysis.md
migration-repair.md
validation.md
certification.md

Persist this task:
docs/ai/tasks/TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1.task.md

Update SoT honestly.

On PASS:
hostRootGlobalBoundaries001 must become:

state = CERTIFIED
hostRootResidue = ZERO
blockerState = RESOLVED
certificationState = PASS
mergedToMain = true
workflowStop = USER_REVIEW_HOST_ROOT_GLOBAL_BOUNDARIES_001_R1

Do NOT claim ARCH-COMPLETE-002 for whole Order/Payment/Catalog modules unless the certify skill
actually proves the whole-module standard. This task certifies the bounded Host-root migration
surface.

PHASE G — LAND ON MAIN

ONLY if certification PASS:

verify integration branch contains latest origin/main Authorization work
verify focused validation PASS
verify tracked working tree clean
commit R1 repair/certification evidence
move to main
update main by a normal merge/fast-forward of the certified integration branch
push origin/main
verify HEAD == origin/main
verify the six Host files are absent on main
verify Authorization work is still present on main

Do NOT force-push.
Do NOT reset main.
Do NOT use unsafe checkout/restore.
Do NOT drop parallel commits.

If certification is not PASS:

DO NOT merge/push this migration to main
return exact blocker
STOP

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001-R1
Parent-Task: TB-TMAR-HOST-ROOT-GLOBAL-BOUNDARIES-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Authorization-Main-Preservation-State:
Latest-Main-Before-Reconcile:
ChatGPT-Branch-Source-State:
Reconciliation-State:
Six-Host-Root-Files-State:
Settlement-GlobalUsing-State:
CommerceHold-Owner-State:
Catalog-Hold-Boundary-State:
CheckoutHold-OrderPayment-Boundary-State:
UnpaidExpiry-Owner-State:
Order-Certification-Blocker-State:
Foreign-Application-State:
Foreign-Infrastructure-State:
Foreign-Domain-State:
CrossModule-Persistence-State:
Behavior-Parity-State:
Focused-Build-State:
Focused-Test-State:
Certification-State:
Evidence-Path:
SoT-State:
Integration-Commit-SHA:
Main-Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
Repair-Iterations:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After returning the result:
STOP COMPLETELY.

Do not start another Host folder.
Do not continue Seller/Storefront/Development.
Wait for Architect review.

END_TOOBA_TASK
