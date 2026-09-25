PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001-R1
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ACCESSCONTROL
Title: Canonicalize Permission Catalog task artifact and remove accidental bridge helper residue
Backend-Only: YES

ARCHITECT REVIEW:

Parent production migration at:

c9a994c37313e4d99fbce6a655844f0a153dc4a3

is semantically accepted for the Permission Catalog route family.

However parent Closure is NOT yet accepted because canonical repository state is inconsistent with the Worker Result.

Observed defects on main:

MISSING canonical task artifact:
docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001.task.md
does not exist on main.

ACCIDENTAL helper-script residue was committed:
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001/post-result.js
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1/post-result.js

The earlier accepted R1 explicitly treated such local helper scripts as non-canonical output.

TIMEBOX:

Target <= 10 minutes.
Hard maximum 15 minutes.

ONE OBJECTIVE:

Canonicalize documentation/protocol state only.

NO production code changes.
NO new route migration.
NO refactor.
NO behavior change.

MANDATORY ACTIONS:

Add the exact parent task artifact:

docs/ai/tasks/TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001.task.md

Use the actual Architect-issued task content that produced the parent work.
Do not invent a different scope retroactively.
Preserve Task-ID exactly.

Remove these accidental repository helper scripts:

docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001/post-result.js
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-CAPABILITY-GATE-001-R1/post-result.js

Do not replace them with another posting script.

Keep the accepted production migration from c9a994c3 unchanged.

PRODUCTION FREEZE FOR R1:

Do NOT modify anything under:

src/backend/

Do NOT touch:
AccessControl Application code
AccessControl Endpoints code
Host AccessControlEndpoints.cs
Program.cs
Checkout
Frontend
Fulfillment
schema/migrations

VALIDATION:

git diff must show docs-only changes.
canonical parent task artifact exists at exact path.
the two post-result.js files above are absent.
parent evidence remains present:
docs/evidence/TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001/permission-catalog-migration.md
no src/backend file changed in R1.
working tree clean after commit.
HEAD == origin/main after push.

No builds required because production code must not change.
No tests.
No solution build.
No retries.

RECOVERY STATE:

Do NOT start ROLE-FAMILY.
Do NOT auto-advance.
Permission Catalog parent becomes Architect-acceptable only after this R1 is verified.

AccessControl remains IN_PROGRESS.
Host AccessControl evacuation remains IN_PROGRESS.
Final track target remains Host/Tooba.Host/AccessControl = ZERO files, but that target is NOT part of this repair.

PASS ONLY IF:

missing canonical task file is committed
both accidental helper scripts are removed
production tree is byte-for-byte unchanged by R1
no unrelated files touched

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001-R1
Parent-Task: TB-TMAR-HOST-ACCESSCONTROL-PERMISSION-CATALOG-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Production-State:
Canonical-Task-State:
Helper-Script-Residue:
Production-Code-Changes:
Parent-Evidence-State:
Validation:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

On PASS:
Recovery-Next-Task: TB-TMAR-HOST-ACCESSCONTROL-ROLE-FAMILY-001
Next-Recommended-Task: TB-TMAR-HOST-ACCESSCONTROL-ROLE-FAMILY-001

STOP
Do not auto-start the next task.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
