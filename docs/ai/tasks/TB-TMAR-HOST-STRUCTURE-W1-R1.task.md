PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_TASK

Task-ID:
TB-TMAR-HOST-STRUCTURE-W1-R1

Parent-Task:
TB-TMAR-HOST-STRUCTURE-W1

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
ISSUED

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Title:
HOST-STRUCTURE-W1 Repair — Install Missing Durable TMAR Guardrails Before Any Further Backend Work

Task Type:
REPAIR / ARCHITECTURE-GUARD INSTALLATION

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

0. Why This Repair Exists

TB-TMAR-HOST-STRUCTURE-W1 structural work is accepted provisionally for its 41-file Host reorganization and Host folder/hygiene guards.

However, the canonical Result does NOT report the mandatory amendment locks requested by Architect/User:

ARCH-FE-FREEZE-001

ARCH-FOLDER-OWNERSHIP-001

ARCH-RECOVERY-001

ARCH-USERWORK-001

ARCH-BASELINE-001

ARCH-NOWORKAROUND-001

ARCH-DATA-001

TMAR-Execution-Mode = BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Therefore W1 is NOT fully accepted until this repair installs and verifies those durable locks.

Do NOT begin CHECKOUT-IMPL-W4 before this repair passes.

Expected previous tip:
9c83fe01df4a1b61763f328d9ccd560019d2c31c

1. Git/User-Work Safety

Verify:

branch main

HEAD == origin/main

18ca10c9 remains ancestor

git status --short

user work preserved

If tracked user work conflicts:
STOP with RECOVERY_CONFLICT.

Never:

git reset

git clean

destructive checkout/restore

unsafe rebase

blind stash manipulation

broad git add .

2. Install Durable Locks

Update:
docs/architecture/TMAR-architecture-locks.md

Add and make canonical:

ARCH-FE-FREEZE-001

Until explicit Architect/User release:

no production modification under src/frontend/**

no frontend refactor

no frontend package/dependency changes

no frontend folder migration

no frontend Orders work

no frontend god-file work

backend tasks may READ frontend only for compatibility evidence

any production frontend write during backend-only mode must fail architecture validation unless explicitly released

ARCH-FOLDER-OWNERSHIP-001

No new production source may be placed in a project/module root when it belongs to an identifiable responsibility/capability.

small explicit root allowlist only

existing flat debt is shrink-only

no Common / Misc / Helpers dumping grounds when ownership is knowable

baseline may not widen

ARCH-RECOVERY-001

TMAR is incremental recovery, not rewrite.

no Big Bang rewrite

no broad replacement of working subsystems

characterization/evidence before risky extraction

behavior preserved unless explicit product-change task says otherwise

cosmetic relocation must not hide unresolved ownership

ARCH-USERWORK-001

Protect user-authored/local work.

destructive git operations prohibited

conflict => RECOVERY_CONFLICT

protected commit 18ca10c9 remains ancestor

every Result reports user work preserved

ARCH-BASELINE-001

Architecture baselines are shrink-only.

no widening to pass tests

no wildcard suppression

removed debt must shrink baseline in same task

new violation is failure, not baseline addition

ARCH-NOWORKAROUND-001

No temporary/dirty workaround.
Explicitly prohibit without architectural justification:

polling loops

magic sleeps/timeouts/intervals

silent catch-and-ignore

suppressing errors

hardcoded business shortcuts

first-item / first-seller shortcuts

test-only branches

duplicated fallback logic instead of fixing ownership/boundary

ARCH-DATA-001

Safe incremental data evolution.

no destructive migration without explicit dedicated approval

schema/migration owned by module

no new cross-module DB FK

no shared mega-DbContext expansion

additive migration preferred

rollback/compatibility documented

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1-R1/locks.md

3. Persist Backend-Only Program Mode

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

Add exact canonical line:

TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Also record:
Frontend remains frozen until explicit Architect/User release.

Do not modify production frontend code.

4. Machine-Enforced Guards

Implement practical architecture tests/guards for at least:

A. frontend freeze:

backend-only TMAR mode rejects production changes under src/frontend/**

allow read-only evidence

future explicit release mechanism must require deliberate architecture update, not accidental bypass

B. root/folder ownership:

new arbitrary production .cs file at Host/project root fails unless allowlisted

HOST-FOLDER-001 remains active

existing approved root files still pass

C. baseline integrity:

guard code/baselines cannot silently broaden wildcard allowances

existing debt remains exact/shrink-only

D. user-work safety may be documentation/process enforced if not technically testable, but it must be canonical and referenced by bootstrap/recovery.

Do not build a brittle guard that blocks legitimate generated files or project files.

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1-R1/guard-tests.md

5. Preserve W1 Structural Result

Do NOT undo the 41-file Host reorganization.

Verify:

moved Host files remain in responsibility folders

HOST-FOLDER-001 remains green

HOST-HYGIENE-001 remains green

Host build remains green

no production behavior change

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1-R1/w1-preservation.md

6. Frontend Integrity Check

Required:

git diff confirms NO production file under src/frontend/** modified by this repair

only architecture docs/tests outside frontend may be changed

Return:
Frontend-Production-Changes: NONE

Evidence:
docs/evidence/TB-TMAR-HOST-STRUCTURE-W1-R1/frontend-integrity.md

7. Validation

Run:

architecture tests

Host build

focused Host tests

TMAR foundation tests

folder guard tests

frontend freeze guard tests

baseline guard tests

Required:
NEW_FAILURES=0

8. Checkout Resume Gate

After guard installation verify W3 checkout state remains intact.

Return:
Checkout-W4-Resume-Readiness: READY
or
Checkout-W4-Resume-Readiness: BLOCKED

Do not implement Checkout W4 here.

9. Recovery Updates

Update:

docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md

docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md

docs/architecture/TMAR-architecture-locks.md

docs/evidence/TB-TMAR-HOST-STRUCTURE-W1-R1/recovery-sot.md

Record that frontend is frozen and all subsequent TMAR tasks are backend-only until explicit release.

10. Acceptance Criteria

PASS only if:

all seven missing locks are ACTIVE

backend-only mode is durable

machine guard exists for frontend freeze

folder ownership guard exists

baseline shrink-only policy is durable

no production frontend changes

W1 Host structure remains intact

tests pass

NEW_FAILURES=0

user work preserved

Checkout W4 readiness assessed

canonical Result delivered

Worker stops completely

11. Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Recovery-Start
TMAR-Execution-Mode
Frontend-Freeze-Lock
Folder-Ownership-Lock
Recovery-Lock
User-Work-Lock
Baseline-Lock
No-Workaround-Lock
Data-Safety-Lock
Machine-Guards
W1-Preservation
Frontend-Production-Changes
Tests
Checkout-W4-Resume-Readiness
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Next-Recommended-Task

Expected:
TMAR-Execution-Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Frontend-Production-Changes: NONE
Product-Resume-Safety: SAFE_WITH_TMAR_PARALLEL
Next-Recommended-Task: TB-TMAR-CHECKOUT-IMPL-W4

After canonical Result:
STOP completely.

Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_TASK