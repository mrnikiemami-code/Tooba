PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001
Parent-Task: TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Admin Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_ACCESS_ANALYZE
Title: Analyze Host/Admin/Access platform seam and module authorizer adapters after Panel/Grid closure
Estimated-Time-Minutes: 16
Hard-Timebox-Minutes: 20

ARCHITECT REVIEW STATE

Admin/Grid W1:
ACCEPTED

Architect independently verified:

AdminGridQueryEndpoint removed
Admin/Grid directory absent
Host/Admin recursive production file count = 18
exact allowlists updated
Panel certification preserved
Admin/Development certification preserved
Party sellers certification preserved
runtime behavior change = NONE
implementation commit:
aee55d7f6ede1d3d817fc8afabe6ac4ab68fd80a

NEXT ACTIVE RECOVERY UNIT

src/backend/Host/Tooba.Host/Admin/Access/

This includes:

AdminPanelAccess.cs
HostAdminPanelAccess.cs
Access/Authorizers/*.cs

Known current production file count:
13
(2 direct Access files + 11 Authorizers)

Do NOT assume the count; re-enumerate from disk.

SKILL — MANDATORY

Apply:

.cursor/skills/tooba-architecture-analyze/SKILL.md

ANALYSIS ONLY.

Do NOT edit production code.
Do NOT migrate.
Do NOT certify.
Do NOT start another Host folder.
Do NOT reopen Panel/Development/Party sellers/Grid.

PROTECTED ACCEPTED STATE

Must remain untouched:

HOST_ADMIN_PANEL_AMC_CERTIFIED
Panel = PANEL_KEEP_CERTIFIED
Admin/Development dev-context = CERTIFIED
Party sellers ownership = CERTIFIED
Admin/Grid = HOST_ZERO / ABSENT

MANDATORY ANALYSIS

EXACT TREE / FILE ENUMERATION

Enumerate:

src/backend/Host/Tooba.Host/Admin/Access/
src/backend/Host/Tooba.Host/Admin/Access/Authorizers/

Record:

exact file count
exact filenames
exact namespaces
root vs nested responsibility

Expected current set, verify independently:

Access/

AdminPanelAccess.cs
HostAdminPanelAccess.cs

Access/Authorizers/

HostLocalizationAdminAuthorizer.cs
HostOperatorProfileAdminAuthorizer.cs
HostOrderAdminAuthorizer.cs
HostOrderAdminEffectiveAccessReader.cs
HostPaymentAdminAuthorizer.cs
HostPromotionAdminAuthorizer.cs
HostReturnAdminAuthorizer.cs
HostSettlementAdminAuthorizer.cs
HostSupportAdminAuthorizer.cs
HostUserPreferenceAdminAuthorizer.cs
HostWalletAdminAuthorizer.cs
RESPONSIBILITY CLASSIFICATION — EACH FILE

For every file classify exactly one primary disposition:

KEEP_AS_HOST_PLATFORM_ACCESS_SEAM
KEEP_AS_THIN_HOST_MODULE_AUTH_ADAPTER
MOVE_TO_MODULE
DEAD_ZERO_CONSUMER_RESIDUE
MUST_SPLIT
BLOCKED_NEEDS_ARCHITECT_DECISION

Do NOT classify the whole folder with one broad label before per-file analysis.

CONSUMER / DI INVENTORY

For every type/interface:

identify interface implemented or static seam exposed
identify Program DI registration
identify all production consumers
identify whether consumer lives in Host or Module.Endpoints/Contracts
identify any test-only references separately

Explicitly inspect:

AdminPanelAccess
HostAdminPanelAccess : IAdminPanelAccess
HostOrderAdminEffectiveAccessReader
all Host*AdminAuthorizer types

No consumer guessing.

AUTHORITY / OWNERSHIP TEST

For each retained Host adapter determine whether it is truly:

platform/session/tenant/authorization composition only
or whether it contains:
module business policy
module permission policy ownership
module error semantics ownership
module data access
module-specific orchestration

Host may retain thin adapters only.
Host must not own module business authority.

DEPENDENCY BOUNDARY AUDIT

For every Access/Authorizers file audit:

Forbidden:

foreign .Application
foreign .Infrastructure
foreign .Domain
foreign DbContext
cross-module persistence
cross-module entity joins
module internal service-locator access

Allowed only if justified:

BuildingBlocks neutral security/auth
module Contracts
module Endpoints-owned authorization seam interfaces/codes where this is the accepted edge adapter pattern

Produce a table:
File | Foreign dependency | Layer | Allowed? | Reason

HARDCODED USER-FACING TEXT — CRITICAL

The architecture lock is strict:
NO hard-coded runtime Persian/English user-facing presentation text.

Architect has already observed current hard-coded Persian runtime exception titles in:

AdminPanelAccess.cs:

admin.tenant.missing
admin.authorization.unavailable
admin.authorization.denied
admin.actor.missing

HostAdminPanelAccess.cs:

admin.tenant.missing
admin.authorization.unavailable
admin.authorization.denied

Audit ALL 13 files for:

PlatformHttpException hard-coded title/message
SemanticException hard-coded title/message
inline Persian/English runtime text
user-facing fallback text
exception.Message or ex.Message classification/presentation

For every stable code determine:

current descriptor owner
localization owner
status/classification
whether code is registered exactly once
whether hard-coded prose can be removed via existing canonical exception/result/catalog mechanisms

Do NOT fix in Analyze.

ERROR-CODE INVENTORY

Build complete matrix for codes used by Access surfaces.

At minimum inspect:

admin.actor.missing
admin.tenant.missing
admin.authorization.unavailable
admin.authorization.denied
module-specific unavailable/denied codes used by authorizers

For each:
Code | Producer | HTTP | Descriptor owner | Localization key | Duplicate? | Hardcoded title currently?

Flag:

unregistered codes
duplicate descriptors
status mismatches
code ownership mismatches
MODULE AUTHORIZER ADAPTER PATTERN

Determine whether the 11 files should remain independent thin Host adapters or whether any are redundant/dead.

For each:

interface implemented
direct production consumer / DI registration
auth decision path
stable error codes
whether adapter is pass-through/thin
whether module-specific authorization logic leaked into Host

Group only after per-file proof.

ADMIN PANEL ACCESS SEAM DESIGN

Analyze relation between:

AdminPanelAccess (static internal helper)
HostAdminPanelAccess : IAdminPanelAccess

Answer decisively:

Is duplication justified?
Is AdminPanelAccess still directly consumed outside HostAdminPanelAccess?
Can one become implementation detail of the other?
Is there dead/duplicate authorization logic?
Does Marketplace Development branch require HostAdminPanelAccess to stay Host-owned?
What is the minimum target shape that removes hard-coded presentation debt without destabilizing auth semantics?

Do NOT redesign broader auth/session.

SECURITY / SENSITIVE DATA

Audit:

auth headers
cookies
actor ids
tenant ids
IP
password/token logging
request header logging
unsafe exception detail

Specifically inspect DevActorHeader handling:
X-Tooba-Dev-Actor-User-Id

Verify it is Development-only and cannot become production auth authority.

OBSERVABILITY

Audit for:

custom ActivitySource
custom Meter
manual traceparent
custom correlation
logging scope
sensitive logging

Do not add anything.

PATH / NAMESPACE / COHESION

Verify exact namespaces:

Tooba.Host.Admin.Access
Tooba.Host.Admin.Access.Authorizers

Check:

aliases
shims
TypeForwardedTo
duplicate type names
flat misplaced files
mixed responsibility/god file
STALE CERTIFICATION METADATA / GUARD WORDING

Inspect current HostAdmin canonical guard comments/metadata.

Architect observed historical wording such as:
"Host/Admin is CERTIFIED as a canonical Host platform boundary"

But current Recovery explicitly says:
Access = NOT_CERTIFIED_BY_PANEL_CERT
Grid = now HOST_ZERO

Classify stale/misleading guard comments or test names separately from actual assertions.

Do NOT use stale historical "whole Host/Admin certified" wording as evidence that Access is certified.

Recommend exact metadata/comment cleanup if needed in a future migration/certification task.

MIGRATION PLAN — FEWEST SAFE WAVES

Produce a decisive plan with bounded waves <=20 minutes.

Preferred planning shape only if evidence supports it:

W1 — Core Admin access error/localization hygiene

AdminPanelAccess + HostAdminPanelAccess
preserve exact auth semantics
remove hard-coded runtime titles
canonical codes/catalog/localizer
no auth redesign

W2 — Authorizer family cleanup/hygiene

only if common systematic debt exists
otherwise split by concrete blocker/owner, not arbitrary count

W3 — Access certification

But do NOT force this shape if repo evidence suggests another.

Every wave must state:

exact files
exact behavior preservation
exact code/status preservation
exact dependencies
exact tests/guards
expected time
NO SCOPE EXPANSION

Do NOT:

redesign Authentication
redesign AccessControl module
redesign tenant resolution
change authorization model
change permissions
change endpoints
change frontend
change schema
move files outside Admin/Access in Analyze

FOCUSED ANALYSIS VALIDATION

No production build required unless needed to verify an ambiguity.
No solution-wide tests.

You may inspect existing focused guards/tests.

EVIDENCE

Create:

docs/evidence/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001/

Required:

analyze.md
file-dispositions.md
consumers-di.md
dependency-boundary.md
error-localization.md
authorization-semantics.md
security-observability.md
guard-metadata.md
migration-plan.md
recovery.md

Persist exact task:

docs/ai/tasks/TB-TMAR-HOST-ADMIN-ACCESS-AMC-001.task.md

RECOVERY / SOT

Analysis-only.

Do NOT advance implementation SHA.

Record:

lastAcceptedTask may identify analysis task according to repository convention
lastAcceptedCommit = aee55d7f6ede1d3d817fc8afabe6ac4ab68fd80a
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
currentHostCheckpoint = Admin/Access
Panel certification preserved
Development certification preserved
Party sellers certification preserved
Admin/Grid = HOST_ZERO
automaticNextImplementationTask = NONE
workflowStop = USER_REVIEW_HOST_ADMIN_ACCESS_AMC_001
staleCurrentPointerState = ZERO

GIT

Work from latest origin/main.

No reset.
No clean.
No rebase.
No force push.

Docs-only task/evidence/SoT stamp allowed.
No production code change.

Preserve unrelated user work.

SUCCESS CRITERIA

PASS only if:

exact Access tree enumerated
all 13 files independently dispositioned
every production consumer/DI registration mapped
every foreign dependency classified
every runtime hard-coded title/message audited
full stable error-code/descriptor/localization matrix produced
AdminPanelAccess vs HostAdminPanelAccess duplication resolved analytically
every authorizer active/dead/thin/business-owning state proven
sensitive/security posture audited
stale certification metadata identified
fewest safe migration waves proposed
protected certified areas untouched
production code change = NONE
implementation SHA unchanged
automaticNextImplementationTask = NONE
STOP for Architect review

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-ACCESS-AMC-001
Parent-Task: TB-TMAR-HOST-ADMIN-GRID-AMC-001-W1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Mode-State:
Active-Host-Folder:
Initial-Production-File-Count:
Final-Production-File-Count:
File-Enumeration-State:
File-Disposition-State:
Production-Consumer-Audit-State:
DI-Registration-Audit-State:
AdminPanelAccess-State:
HostAdminPanelAccess-State:
AdminPanelAccess-Direct-Consumer-State:
Authorizer-Family-State:
Active-Authorizer-Count:
Dead-Authorizer-Count:
Contracts-Boundary-State:
Foreign-Application-Dependency-State:
Foreign-Infrastructure-Dependency-State:
Foreign-Domain-Dependency-State:
Foreign-DbContext-State:
Cross-Module-Persistence-State:
Hardcoded-Runtime-User-Facing-Text-State:
Hardcoded-Title-Code-Count:
Exception-Message-Classification-State:
Error-Code-Matrix-State:
Descriptor-Ownership-State:
Localization-State:
DevActorHeader-Security-State:
Sensitive-Logging-State:
Observability-State:
Path-Namespace-State:
Cohesion-State:
Stale-Certification-Metadata-State:
Panel-Certification-Protection-State:
Admin-Development-Certification-Protection-State:
Party-Sellers-Certification-Protection-State:
Admin-Grid-State:
Recommended-Wave-Count:
Recommended-Next-Task:
Production-Code-Change-State:
Recovery-State:
Last-Accepted-Implementation-Commit-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.

Do not start Access W1.
Do not start another Host folder.

Wait for Architect/user review.

END_TOOBA_TASK