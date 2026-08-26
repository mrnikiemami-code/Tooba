PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

TASK-ID: TB-P05-T018-UNBLOCK-01
PROJECT: Tooba
PHASE: P05
CHANNEL: tooba-main
STATUS: ISSUED
TASK-TYPE: GIT TRANSPORT / REMOTE SYNC RECOVERY
PARENT-TASK: TB-P05-T018
WORKER-POLICY: ONE WORKER = ONE ACTIVE TASK

Title

Unblock Home Fidelity Acceptance — Safely Synchronize Completed T018 Commit to origin/main

Context

TB-P05-T018 is BLOCKED only because the completed local commit could not be pushed.

Known state:

local HEAD: 1497690d00f9901ba803f8488e5deb3a01b3bea1

origin/main: 11b7ee9b2fe71edca682a71c5388511c453cbdca

push failure: HTTP 408 during send-pack

implementation/tests/evidence otherwise complete

Do NOT reimplement Home.
Do NOT redesign.
Do NOT discard local T018 work.

Objective

Safely synchronize the completed T018 work to origin/main.

PASS requires:

push succeeds

HEAD == origin/main

working tree clean

T018 implementation/evidence intact

Repository Recovery

Run:
git rev-parse --show-toplevel
git branch --show-current
git rev-parse HEAD
git fetch origin
git rev-parse origin/main
git status --short --branch
git log --oneline --decorate -5

Expected local HEAD:
1497690d00f9901ba803f8488e5deb3a01b3bea1

Expected origin predecessor:
11b7ee9b2fe71edca682a71c5388511c453cbdca

If local T018 commit is missing: RECOVERY_CONFLICT.

Safety

Forbidden:

force push

destructive reset

discarding T018

recreating Home

weakening TLS

exposing credentials

product changes

PDP/Mega Menu changes

Diagnose Push

Inspect:
git remote -v
git count-objects -vH
git show --stat --oneline 1497690d00f9901ba803f8488e5deb3a01b3bea1

Measure docs/evidence/TB-P05-T018 file sizes.

Create:
docs/evidence/TB-P05-T018-UNBLOCK-01/01-push-failure-diagnosis.md

Safe Recovery

Try non-destructive transport recovery first:

normal retry after network recovery

HTTP/1.1 if HTTP/2/proxy is unstable

safe timeout/low-speed settings

existing authenticated SSH transport if already available

Do not change repository identity.

If large PNG evidence is conclusively the issue, losslessly optimize required PNGs while keeping filenames and review usefulness.

IMPORTANT:
Prefer no history rewrite.
If an unpushed local-only commit must be rewritten solely to remove oversized blobs, do so ONLY if:

origin/main is still the known predecessor

T018 was never pushed

no remote commit depends on it

all implementation/evidence is preserved

no force push is used

operation is fully documented

Otherwise return BLOCKED.

Remote Race Check

Immediately before final push:
git fetch origin
git rev-parse origin/main

If origin/main moved, do not blindly push.
Reconcile per repository governance or return RECOVERY_CONFLICT.

Synchronize

Run:
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require:
HEAD == origin/main
working tree clean

Integrity Proof

Verify:

T018 Home implementation intact

homeCategories still 8

full Catalog still not dumped on Home

Mega Menu hierarchy intact

PDP untouched

T018 evidence intact/readable

no redesign

Create:
docs/evidence/TB-P05-T018-UNBLOCK-01/
01-push-failure-diagnosis.md
02-transport-recovery-method.md
03-origin-race-check.md
04-successful-sync-proof.md
05-t018-integrity-proof.md

Validation

If only transport changed, no full product rebuild needed.
If tracked evidence/files were modified, run:
cd src/frontend
npm run typecheck
npm run lint
npm run test
npm run build

Backend only if backend/product code changed.

Always:
git diff --check
git status --short --branch

Source of Truth

Update after successful sync:

PIPELINE = BRIDGE-WAKE-V1
TB-P05-T018 = BLOCKED_REMOTE_SYNC
TB-P05-T018-UNBLOCK-01 = AWAITING_ARCHITECT_ACCEPT
P05 = IN_PROGRESS

Worker must NOT mark Architect ACCEPT.

Result Contract

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P05-T018-UNBLOCK-01

Channel:
tooba-main

Status:
PASS | FAIL | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:

initial local HEAD:

initial origin/main:

T018 commit present:

Push-Diagnosis:

root cause:

large evidence:

remote protocol:

Recovery:

method:

TLS weakened:

force push:

history rewrite:

evidence optimization:

Synchronization:

push:

final HEAD:

origin/main:

equal:

working tree clean:

T018-Integrity:

Home:

homeCategories:

full Catalog preserved:

Mega Menu:

PDP:

evidence:

redesign:

Validation:

frontend:

backend:

git diff --check:

Source-of-Truth:

T018:

UNBLOCK-01:

P05:

Git:

commits pushed:

final status:

Architectural-Concerns:
...

Visual-Concerns:
...

Blockers:
...

END_TOOBA_WORKER_RESULT

After Result submission return to IDLE.
Do not self-issue another Task.

END_TASK
