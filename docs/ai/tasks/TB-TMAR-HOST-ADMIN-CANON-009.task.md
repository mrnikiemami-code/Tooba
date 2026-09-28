PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ADMIN-CANON-009
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-008
Parent-Commit: 33d6cefcc28a509ba80e1ec58c70a46df029db99
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Host Canonicalization
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_ADMIN_CANONICALIZATION
Title: Remove remaining Support/Wallet Application references from Host/Admin authorizers

ARCHITECT ACCEPTANCE OF PARENT

TB-TMAR-HOST-ADMIN-CANON-008 is ARCHITECT-ACCEPTED at:
33d6cefcc28a509ba80e1ec58c70a46df029db99

Verified:

Host/Admin root flat files = ZERO
recursive Host/Admin C# files = 15
Access / Access.Authorizers / Panel / Grid / Development structure = canonical
path ↔ namespace = exact
CANON-001..007 seams preserved
no ownership or behavior change

FINAL PRE-CERT DEFECT

Two remaining Host/Admin files still directly import foreign module Application namespaces:

Admin/Access/Authorizers/HostSupportAdminAuthorizer.cs
using Tooba.Support.Application.Errors;
Admin/Access/Authorizers/HostWalletAdminAuthorizer.cs
using Tooba.Wallet.Application.Errors;

Host/Admin must not depend on foreign *.Application.

GOAL

Remove ONLY these two foreign Application dependencies while preserving all current auth behavior and stable error codes.

MANDATORY AUDIT FIRST

Inspect:

current usages of SupportErrorCodes.AdminAuthorizationDenied
current usages of SupportErrorCodes.AuthorizationUnavailable
current usages of WalletErrorCodes.AdminAuthorizationDenied
current usages of WalletErrorCodes.AuthorizationUnavailable
Support/Wallet endpoint authorizer interfaces
Support/Wallet endpoint error catalog contributors

Choose the smallest lawful owner for the two Host-facing admin authorization codes.

PREFERRED BOUNDARY

Because Host already implements module Endpoint authorizer seams, prefer a small Endpoint-owned/admin-auth error-code surface colocated with the authorizer contract, unless an existing Contracts-owned error-code surface already exists and is more canonical.

Do NOT make Host reference Support.Application or Wallet.Application.

Do NOT move unrelated business error codes.

TARGET

After this task:

HostSupportAdminAuthorizer.cs

ZERO Tooba.Support.Application
may depend on Tooba.Support.Endpoints.Admin and neutral BuildingBlocks only for module-specific symbols

HostWalletAdminAuthorizer.cs

ZERO Tooba.Wallet.Application
may depend on Tooba.Wallet.Endpoints.Admin and neutral BuildingBlocks only for module-specific symbols

Stable machine codes MUST remain exactly:

denial: admin.authorization.denied
Support unavailable: support.authorization.unavailable
Wallet unavailable: wallet.authorization.unavailable

Error catalog status/classification MUST remain:

denial: existing shared 403 ownership
unavailable: module-specific Platform / 503

No duplicate registered error descriptor may be introduced.

APPLICATION ERROR CATALOG

If the old Application error constants are used elsewhere:

do NOT move the entire error catalog;
either keep unrelated constants where they are or update only legitimate users of these two admin-auth constants.

If these two constants become duplicate authorities after introducing the new canonical Host-facing surface:

remove the duplicate definitions and update consumers.

Exactly one canonical constant authority per stable admin-auth code is preferred.

OUT OF SCOPE

Do NOT:

change capability policy
change fail-closed behavior
change routes/interfaces/signatures
touch Order authorizer
touch Host/Admin structure
fix unrelated baseline tests
touch frontend/schema
redesign Support or Wallet modules

ANTI-LOOP / EXECUTION BUDGET

ONE concern only.

Validation:

build Tooba.Host
build Tooba.Host.Tests
run ONLY CANON-009 guard + CANON-008 + CANON-003 guards

On failure:

ONE repair attempt maximum
rerun ONLY exact failing command once
second failure => INCOMPLETE + exact blocker + STOP

MAX_REPAIR_ITERATIONS = 1
MAX_VALIDATION_COMMAND_RUNS = 6
No broad suite.
No loop.

DURABLE GUARD

Prove:

HostSupportAdminAuthorizer has ZERO .Application module reference
HostWalletAdminAuthorizer has ZERO .Application module reference
Support stable 403/503 codes unchanged
Wallet stable 403/503 codes unchanged
unavailable descriptors remain Platform/503
no duplicate descriptor introduced by this task
Host/Admin recursive file count = 15
CANON-008 physical structure preserved
CANON-003 fail-closed behavior preserved

EVIDENCE

Create only:
docs/evidence/TB-TMAR-HOST-ADMIN-CANON-009/

analyze.md
validation.md
closure.md

Persist task:
docs/ai/tasks/TB-TMAR-HOST-ADMIN-CANON-009.task.md

RECOVERY SOT

Append/update hostAdminCanon009:

parentCommit
supportForeignApplicationReference=ZERO
walletForeignApplicationReference=ZERO
supportAuthCodes=PRESERVED
walletAuthCodes=PRESERVED
unavailableDescriptors=PLATFORM_503_PRESERVED
adminFileCount=15
canon008=PRESERVED
workflowStop=USER_REVIEW_HOST_ADMIN_CANON_009

SUCCESS CRITERIA

PASS only if:

both Host authorizers have ZERO foreign Application reference
stable auth codes/statuses unchanged
no duplicate descriptor introduced
structure/count unchanged
focused validation passes
evidence/task/SoT committed and pushed
HEAD == origin/main
tracked working tree clean

CANONICAL RESULT

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ADMIN-CANON-009
Parent-Task: TB-TMAR-HOST-ADMIN-CANON-008
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Canon008-Preservation-State:
Host-Admin-File-Count-Before:
Host-Admin-File-Count-After:
Support-ForeignApplication-State:
Wallet-ForeignApplication-State:
Support-ErrorCode-Authority-State:
Wallet-ErrorCode-Authority-State:
Support-Denial-Code-State:
Support-Unavailable-Code-State:
Wallet-Denial-Code-State:
Wallet-Unavailable-Code-State:
Unavailable-Descriptor-State:
Duplicate-Descriptor-Introduced-State:
FailClosed-Behavior-State:
Focused-Validation:
Evidence-Path:
SoT-State:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
Repair-Iterations:
Validation-Command-Runs:
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP COMPLETELY.

Do not start final Host/Admin certification.
Wait for Architect review.

END_TOOBA_TASK