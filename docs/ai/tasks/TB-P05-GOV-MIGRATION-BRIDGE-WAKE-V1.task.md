PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

TASK-ID: TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1
PROJECT: Tooba
PHASE: P05 — Operational Surface Integration
CHANNEL: tooba-main
STATUS: ISSUED
TASK-TYPE: PIPELINE GOVERNANCE MIGRATION

Objective

Migrate CURRENT operational repository governance from BRIDGE-V2 continuous/online-worker assumptions to BRIDGE-WAKE-V1.

This is governance-only.

DO NOT execute product implementation.
DO NOT change domain behavior, APIs, schema, UI, Shopeiva, or accepted architecture.

Authoritative Pipeline

ARCHITECT
→ downloadable .task.md
→ Tampermonkey
→ Bridge Task = Pending
→ External Watchdog
→ BRIDGE-WAKE
→ Coding Agent wakes
→ claims exactly one Task
→ implements
→ Result
→ Bridge
→ Tampermonkey
→ ARCHITECT
→ ACCEPT / REPAIR / BLOCK
→ next .task.md

Critical Idle Rule

The Coding Agent is normally IDLE/OFFLINE between Tasks.

Do NOT require:

continuous GET /api/tasks/next polling

permanently online Worker

idle heartbeat

Worker waiting loop between Tasks

Worker offline + no active Task is NORMAL and is NOT an infrastructure failure.

Watchdog Authority

External Watchdog MAY:

inspect Bridge for new Pending Task

send BRIDGE-WAKE to configured Coding Agent

Watchdog MUST NOT:

create Tasks

modify Task scope

make architectural decisions

ACCEPT / REPAIR / BLOCK

judge implementation success

invent recovery work

advance roadmap

BRIDGE-WAKE is infrastructure control traffic, not a Task, Result, architectural instruction, or implementation evidence.

One Task Rule

ONE WORKER = ONE ACTIVE TASK

Watchdog should wake once for a newly observed Pending Task and must not spam repeated wakes for the same Pending Task.

After claim, Watchdog has no implementation role.

After successful Result submission, Coding Agent returns to IDLE.

Result Lifecycle

Result
→ Architect Review
→ ACCEPT / REPAIR / BLOCK

Worker PASS != Architect ACCEPT.

ACCEPT: issue exactly one next safe .task.md.
REPAIR: issue exactly one focused repair .task.md.
BLOCK: stop only for genuine blocker.

SYSTEM-BRIDGE-ALERT

SYSTEM-BRIDGE-ALERT is NOT a Task Result.

Under BRIDGE-WAKE-V1, do NOT emit/interpret an alert merely because Worker is offline between Tasks.

Valid alerts include real failures such as:

Bridge API unavailable

Task dispatch failure

Result transport failure

Watchdog failure preventing a Pending Task from waking Coding Agent

real active-task transport/execution failure

On SYSTEM-BRIDGE-ALERT:

do not advance project state

do not ACCEPT/REPAIR previous Task based on alert

do not issue another Task because of alert

Current Project State — Preserve

Preserve all accepted product/architecture history.

Record:

P05 = IN_PROGRESS

TB-P05-T014 = ACCEPTED

TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1 = AWAITING_ARCHITECT_ACCEPT

Do NOT issue or implement the next product Task inside this migration.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

56ba6011cdae6e4cb2a4a734340f0489664abac7

Require:

main

HEAD == origin/main

safe/known tree

Current Governance Files to Audit

At minimum:

AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md

Search current operational docs for BRIDGE-V2 assumptions, especially:

continuous polling

permanent Worker online requirement

heartbeat as idle prerequisite

polling loops while idle

alerts caused only by Worker offline between Tasks

Historical artifacts may keep old syntax as evidence.

Required Canonical Marker

Current operational governance must state:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

and clearly mark conflicting BRIDGE-V2 operational behavior as retired where appropriate.

Task Artifact Rule

Every implementation Task remains a downloadable:

<TASK-ID>.task.md

Tampermonkey dispatches it to Bridge.
Watchdog wakes the idle Coding Agent after Pending appears.

No manual user relay.

Evidence

Create:

docs/evidence/TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1/

Required:
01-governance-files-reviewed.md
02-bridge-v2-conflict-audit.md
03-bridge-wake-v1-proof.md
04-idle-watchdog-semantics-proof.md
05-recovery-state-proof.md

02 must classify every current BRIDGE-V2 conflict as fixed or historical-only.

03 must prove:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

.task.md artifact flow

Pending → Watchdog → BRIDGE-WAKE → claim

Result → Architect

04 must prove:

offline/idle between Tasks is normal

no continuous polling requirement

Watchdog authority is narrow

BRIDGE-WAKE is not Task/Result

no wake spam for same Pending Task

05 must prove:

P05 remains current

T014 accepted

no product task executed in migration

next product work remains to be chosen by Architect after ACCEPT

Validation

Run:

git diff --check
git status --short --branch

Run any repository governance/doc validation if available.

No expensive product build required for docs-only change unless repository policy requires it.

Acceptance Conditions

PASS requires:

current protocol = BRIDGE-WAKE-V1

old conflicting BRIDGE-V2 transport semantics retired

no current continuous polling requirement

no current permanently-online Worker requirement

idle/offline between Tasks explicitly normal

Watchdog role explicit and narrow

BRIDGE-WAKE explicitly control traffic, not Task/Result

ONE WORKER = ONE ACTIVE TASK preserved

Worker PASS != Architect ACCEPT preserved

SYSTEM-BRIDGE-ALERT semantics updated

historical task artifacts preserved

P05 preserved

T014 accepted

no product code/API/schema/UI change

repo clean and synchronized

Source of Truth

Update current governance/recovery files appropriately.

Expected state after Worker execution:

PIPELINE = BRIDGE-WAKE-V1
TB-P05-T014 = ACCEPTED
TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1 = AWAITING_ARCHITECT_ACCEPT
P05 = IN_PROGRESS

Worker must NOT mark Architect ACCEPT.

Git

git diff --check
git status --short --branch
git add ...
git commit -m "docs migrate Tooba pipeline governance to Bridge-Wake-V1 [TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1]"
git push origin main
git fetch origin
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Require:

HEAD == origin/main

working tree clean

Result Contract

Return through Bridge:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_WORKER_RESULT

Task-ID:
TB-P05-GOV-MIGRATION-BRIDGE-WAKE-V1

Channel:
tooba-main

Status:
PASS | FAIL | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
...

Governance:
...

Evidence:
...

Validation:
...

Source-of-Truth:
...

Git:
...

Architectural-Concerns:
...

Blockers:
...

END_TOOBA_WORKER_RESULT

Do not self-issue product work.
After Result, control returns to Architect.

END_TASK
