PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CACHING-AMC-001-R1
Parent-Task: TB-TMAR-HOST-CACHING-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Caching AMC Repair
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_CACHING_SINGLE_FLIGHT_REPAIR
Title: Close remaining single-flight lifecycle race in Host Caching and reconcile Recovery

BASELINE
Parent implementation:
8b2e97c7375b2b0ebe0e468346917b9a708b6b55

PRESERVE

Host/Caching = PRESENT
disposition = KEEP_AS_GENERIC_HOST_CACHE_INFRASTRUCTURE
exactly 4 production files
namespace = Tooba.Host.Caching
foreign module layers = ZERO
module DbContext = ZERO
type mismatch behavior = REMOVE_ENTRY + TELEMETRY + MISS
positive TTL validation
Memory/None argument parity
tenant/edition isolation
bounded telemetry
Redis = ZERO
schema unchanged
frontend unchanged

R1 BLOCKER
The current reference-counted single-flight lifecycle still has a race.

Current pattern:

AcquireInflight gets slot from dictionary and increments RefCount
ReleaseInflight decrements to zero
then separately calls:
_inflight.TryRemove(KeyValuePair.Create(cacheKey, slot))

Problem interleaving:

caller A decrements RefCount 1 -> 0
before A removes dictionary entry, caller B reads same slot and increments 0 -> 1
caller A still removes the active slot successfully by key/value
caller C creates a new slot
B and C can now coordinate on different gates for the same key
duplicate factory execution becomes possible

This must be closed.

ARCHITECTURE DECISION
Single-flight lifecycle must guarantee:

one coordination object per cache key while any caller is attached
acquire and retirement/removal cannot race into split-brain slots
no active slot may be removed after a new caller has attached
no semaphore/use-after-dispose race
failed factory remains retryable
cancelled waiter does not corrupt lifecycle
no unbounded slot leak

Do not rely on timing assumptions.

REQUIRED REPAIR

SINGLE-FLIGHT LIFECYCLE
Replace the current lifecycle with an atomic-safe design.

Accepted approaches include:

lock-protected slot lifecycle per dictionary entry
immutable holder with atomic retired/ref-state
Lazy<Task<...>> / task-based in-flight map with safe completion removal
another minimal design that can be proven race-safe

The design MUST prevent an acquire from attaching to a slot that can still be removed concurrently.

Do NOT merely:

add another RefCount check before TryRemove
add sleeps/retries
wrap only TryRemove in a superficial condition
depend on SemaphoreSlim.CurrentCount
leave the same remove-after-zero race structurally possible
FACTORY SEMANTICS
Preserve:
same-key concurrent callers execute exactly one factory
waiters re-read cache after owner completes
failed factory does not cache a value
next caller can retry successfully
one cancelled waiter does not cancel/corrupt unrelated waiters unless it is the factory owner and existing semantics explicitly require that behavior
no deadlock
SLOT CLEANUP
Prove:
coordination entries are removed when no longer needed
active entries are not removed
completed/faulted/cancelled flow does not leak entries
semaphore/coordination objects are not disposed while a caller can still use them
DETERMINISTIC CONCURRENCY TEST
Add a focused test that reproduces the exact lifecycle boundary, not only “many callers usually run once”.

The test must deterministically coordinate:

last release / retirement path
a new acquire arriving during retirement
a subsequent caller

Use barriers, TaskCompletionSource, hooks/internal test seam, or another deterministic mechanism.

The test must fail under the parent implementation pattern and pass with the repaired design.

Also retain/add:

many same-key callers => factory count exactly 1
factory throws => retry works
cancelled waiter => remaining callers complete correctly

No timing-flaky Task.Delay-based race test as the primary proof.

DURABLE GUARD
Strengthen HostCachingAmcGuardTests.

Do not certify merely by checking for:

InflightSlot
RefCount
absence of CurrentCount == 1

Guard/test must instead lock the actual design invariant:

no retire/remove path can remove a slot after a new attachment is accepted
focused concurrency test for the retirement race exists

Keep all prior Caching guards.

NO REGRESSION
Preserve:
type mismatch safe miss
TTL positivity
Memory/None parity
telemetry cardinality
sensitive logging = ZERO
Redis = ZERO
tenant/edition isolation
exact namespace
4-file allowlist
no foreign module layers

RECOVERY / SOT — MANDATORY DoD
Update all authoritative surfaces:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required final state:

lastAcceptedTask = TB-TMAR-HOST-CACHING-AMC-001-R1
lastAcceptedCommit = <actual R1 implementation SHA>
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-CACHING-AMC-001-R1
currentHostEvacuation.currentTask = TB-TMAR-HOST-CACHING-AMC-001-R1
currentHostEvacuation.activeModule = Caching
currentHostEvacuation.currentHostCheckpoint = Caching
active state = CACHING_KEEP_GENERIC_HOST_INFRASTRUCTURE_R1_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_CACHING_AMC_001_R1_KEEP_GENERIC_HOST_INFRASTRUCTURE
nextTask = USER_REVIEW_HOST_CACHING_AMC_001_R1_KEEP_GENERIC_HOST_INFRASTRUCTURE
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

If docs/stamp is a separate commit:

lastAcceptedCommit MUST still point to the R1 IMPLEMENTATION commit.

Historical accepted lineage remains intact.

SCOPE LIMIT
Do NOT:

redesign cache API
add Redis
alter module cache policies
touch frontend
change DB schema/migrations
start another Host folder
run solution-wide refactors
touch unrelated user file accesscontrol-first-slice-map.md

FOCUSED VALIDATION ONLY

Build:

BuildingBlocks if touched
Host
directly affected tests

Run:

focused single-flight correctness tests
retirement-race deterministic test
failure retry test
cancellation safety test
HostCachingAmcGuardTests
TmarDurableGuardTests

No solution-wide test run.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-CACHING-AMC-001-R1/

Required:

race-root-cause.md
single-flight-design.md
deterministic-race-proof.md
regression.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-CACHING-AMC-001-R1.task.md

SUCCESS CRITERIA
PASS only if all are true:

parent Caching KEEP state preserved
retirement/acquire race = CLOSED
active slot removal after new attachment = IMPOSSIBLE_BY_DESIGN
same-key concurrent factory execution = EXACTLY_ONE
failed factory retry = SAFE
cancellation safety = PRESERVED
in-flight slot leak = ZERO
no semaphore/use-after-dispose race
prior type-safety/TTL/provider-parity/telemetry/tenant-isolation guarantees preserved
Redis remains ZERO
schema unchanged
frontend unchanged
Recovery fully reconciled to Caching R1
lastAcceptedCommit points to actual R1 implementation SHA
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

GIT
Work from latest origin/main.
No reset.
No clean.
No rebase.
No force push.
Preserve unrelated user work.
Commit/push main only on PASS.

CANONICAL RESULT
Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CACHING-AMC-001-R1
Parent-Task: TB-TMAR-HOST-CACHING-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Caching-Disposition-State:
Host-Caching-State:
Retirement-Acquire-Race-State:
Active-Slot-Removal-State:
Single-Flight-State:
Single-Flight-Failure-Retry-State:
Cancellation-Safety-State:
Inflight-Leak-State:
Coordination-Disposal-Safety-State:
Type-Mismatch-State:
Cache-Policy-Validation-State:
Provider-Parity-State:
Tenant-Edition-Isolation-State:
Telemetry-Cardinality-State:
Sensitive-Logging-State:
Redis-State:
Behavior-Parity-State:
Schema-Change-State:
Frontend-State:
Guard-State:
Deterministic-Race-Test-State:
Focused-Build-State:
Focused-Test-State:
Recovery-State:
Last-Accepted-Commit-State:
Stale-Current-Pointer-State:
Automatic-Next-Implementation-Task-State:
Workflow-Stop-State:
Certification-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree-Tracked-State:
User-Work-Preserved:
STOP
END_TOOBA_WORKER_RESULT

STOP COMPLETELY AFTER RESULT.
Do not start another Host folder.
Wait for Architect/user review.

END_TOOBA_TASK