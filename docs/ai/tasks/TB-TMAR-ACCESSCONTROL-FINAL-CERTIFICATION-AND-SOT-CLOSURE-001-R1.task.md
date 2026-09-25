PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1
Parent-Task: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: ACCESSCONTROL_FINAL_CERTIFICATION
Title: Repair final AccessControl Recovery SoT closure drift only
Docs-Only: YES

PARENT-COMMIT:
53365a7ec09f7d3123889cca008354857e16c56b

ARCHITECT-VERDICT-ON-PARENT:
PRODUCTION/CERTIFICATION LOOKS COMPLETE
RECOVERY CLOSURE = INCOMPLETE

TIMEBOX:
Target <= 5 minutes.
Hard maximum 8 minutes.
No production changes.
No test changes unless a tiny existing SoT assertion is required.
No retry loop.

ONE OBJECTIVE:

Repair the Recovery SoT drift left by the parent certification task.

DO NOT touch AccessControl production code.
DO NOT change manifest certification.
DO NOT rerun migration work.
DO NOT broaden scope.

MANDATORY FIX 1 — tmar-current-state.json:

Update top-level canonical pointers so they no longer point to the pre-cert validator task.

Set:

lastAcceptedTask =
TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001

lastAcceptedCommit =
53365a7ec09f7d3123889cca008354857e16c56b

lastAcceptedSoTStamp =
53365a7ec09f7d3123889cca008354857e16c56b

lastAcceptedNote =
concise final AccessControl closure note:
COMPLETE_REFERENCE_PATTERN + ARCH-COMPLETE-002 STRUCTURE_CERTIFIED, Host ZERO, validator coverage 6/6 required + 13 no-validator-required, next Host folder AddressBook.

Ensure:
nextTask = TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001
nextTaskGate = HOST_FIRST_FOLDER_BY_FOLDER_AFTER_ACCESSCONTROL_FINAL_CERTIFICATION

currentHostEvacuation must clearly show:
activeModule = AddressBook
AccessControl closure = COMPLETE
AccessControl Host folder = ZERO
AccessControl structure certification = COMPLETE
AccessControl COMPLETE_REFERENCE_PATTERN = COMPLETE

Do not reintroduce stale IN_PROGRESS/PRECERT fields.

MANDATORY FIX 2 — TOOBA-TMAR-MASTER-RECOVERY.md:

Remove or supersede the stale live AccessControl section that still says:

AccessControl IN_PROGRESS
NOT COMPLETE_REFERENCE_PATTERN
NOT STRUCTURE_CERTIFIED
scope-resources/demo-preview still remain
next task = SCOPE-RESOURCES-001

Replace with one concise authoritative current closure section:

AccessControl:

Host ZERO
COMPLETE_REFERENCE_PATTERN
ARCH-COMPLETE-002 STRUCTURE_CERTIFIED
19 endpoint-reachable requests
6 validator-required / 6 present / 13 no-validator-required
Contracts-only boundaries
final certification task + commit
next Host folder = AddressBook
next task = TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001
frontend FROZEN
Checkout PAUSED_AT_SAFE_W5_CHECKPOINT

Historical AccessControl task history may remain, but there must be no competing section labelled/currently phrased as live state that contradicts closure.

MANDATORY FIX 3 — OPTIONAL BOOTSTRAP CONSISTENCY:

If docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md contains the same stale current AccessControl state, update only that stale current-state block to match final closure.

Do not broadly rewrite the file.

VALIDATION:

Docs-only checks:

parse tmar-current-state.json
verify exact lastAcceptedTask/Commit above
verify nextTask exact
verify AccessControl is in structureLock.certifiedModules
verify AccessControl manifest entry remains structureCertified=true
verify no live-current section says AccessControl IN_PROGRESS
verify no production/test files changed

NO dotnet build.
NO broad tests.
NO solution build.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1/sot-closure-repair.md

Evidence must show:

stale before values
corrected final values
stale live section removed/superseded
production changes = NONE
test changes = NONE
next task exact

PASS ONLY IF:

Recovery can start in a fresh chat and unambiguously determine:
AccessControl = COMPLETE_REFERENCE_PATTERN + STRUCTURE_CERTIFIED
Host AccessControl = ZERO
latest accepted task = final certification parent
next folder = AddressBook
next task = TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001-R1
Parent-Task: TB-TMAR-ACCESSCONTROL-FINAL-CERTIFICATION-AND-SOT-CLOSURE-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Certification-State:
Current-State-Json-State:
Last-Accepted-State:
AccessControl-Closure-State:
Master-Recovery-State:
Architect-Bootstrap-State:
Certified-Modules-State:
Manifest-State:
Next-Host-Folder:
Next-Task-State:
Production-Code-Changes:
Test-Code-Changes:
Json-Parse-State:
Canonical-Task-State:
Evidence:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start AddressBook.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
