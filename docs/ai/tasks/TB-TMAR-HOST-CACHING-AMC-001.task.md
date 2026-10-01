PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-HOST-CACHING-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Host Caching AMC
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_CACHING_PROFESSIONALIZATION
Title: Professionalize and certify Host/Caching as generic Host cache infrastructure

ARCHITECT DECISION
src/backend/Host/Tooba.Host/Caching/ is NOT a HOST_ZERO target.

Target disposition:
KEEP_AS_GENERIC_HOST_CACHE_INFRASTRUCTURE

Current retained files:

Caching/CacheHostOptions.cs
Caching/CacheInstrumentation.cs
Caching/CacheRegistration.cs
Caching/MemoryToobaCache.cs

Current neutral cache contracts remain in:
BuildingBlocks/Tooba.BuildingBlocks/Cache.cs

GOAL
Finish Caching as a production-grade, architecture-compliant Host platform capability without widening scope.

This task must repair the known correctness blockers and then certify the folder.

MANDATORY REPAIRS

PATH ↔ NAMESPACE EXACT
All production files under:
Host/Tooba.Host/Caching/

must use:
namespace Tooba.Host.Caching;

Repoint all consumers/usings.

No aliases.
No shims.
No compatibility duplicate namespace.

SINGLE-FLIGHT / STAMPEDE CORRECTNESS
Current MemoryToobaCache.GetOrCreateAsync removes a per-key semaphore after release using CurrentCount == 1.

This can race:

waiter acquires old semaphore
old semaphore is removed from dictionary
a later request creates a new semaphore
two factories can execute concurrently for the same key

Repair this professionally.

Required semantics:

at most one factory execution per cache key within the process while a miss is being filled
all concurrent waiters for the same key converge on the same in-flight coordination object
cancellation of one waiter must not corrupt/remove coordination while other waiters still depend on it
failed factory must not cache a result
no semaphore disposal/use-after-remove race
no unbounded coordination-object leak

Use a robust reference-counted / lazy-task / in-flight entry strategy as appropriate.
Do not add distributed locking.
Redis is NOT part of this task.

TYPE SAFETY
Current TryRead<T> can treat an existing cache entry with the wrong payload type as a cache hit with null.

This is forbidden.

Required behavior:

stored non-null payload of incompatible type must never silently become a successful hit
choose one explicit safe behavior:
a) fail fast with a stable internal exception, or
b) remove corrupt/incompatible entry and treat as miss
document and test the chosen behavior
negative-cache sentinel remains distinguishable from a real type mismatch

Preferred for runtime resilience:
remove incompatible entry + telemetry/diagnostic signal + miss, without logging payload/full key.

CACHE POLICY VALIDATION
Strengthen CachePolicy.EnsureBounded().

Reject:

zero TTL
negative TTL
zero/negative sliding expiration
zero/negative negative-cache TTL

Also ensure:

negative caching requires explicit finite positive NullAbsoluteExpiration
no unbounded mutable cache
if both absolute and sliding TTL are present, both are valid positive durations

Do not invent arbitrary global maximum TTL unless an existing architecture rule already defines one.

PROVIDER CONTRACT PARITY
MemoryToobaCache and DisabledToobaCache must enforce the same argument contract.

Examples:

blank invalidation tag must be rejected consistently
blank namespace must be rejected consistently
invalid CachePolicy must be rejected consistently
cancellation semantics must be consistent

Provider=None may no-op storage/invalidation after validation, but must not silently accept invalid input that Memory rejects.

TENANT / EDITION ISOLATION
Preserve and guard the existing good behavior in CanonicalCacheKeyBuilder:
ToobaEdition.Unset rejected
Marketplace must not carry SingleStore TenantId
TenantScoped requires SingleStore + durable TenantId
DeploymentId remains part of key
Market / Locale / Currency / Theme / AuthorizationScope remain explicit independent dimensions
no hostname as tenant identity
no secret/PII in canonical cache key

Do not weaken this.

TELEMETRY / LOGGING
Preserve bounded-cardinality telemetry:
provider
namespace
edition

Forbidden in metric dimensions/logs:

TenantId
full cache key
ResourceId
user id
payload
connection string
secrets

Add only the smallest telemetry needed for:

type mismatch/corrupt entry
factory failure
stampede wait
if not already covered.

Do not log exception messages to classify failures.

CACHE OWNERSHIP / BOUNDARY
Host/Caching must remain generic platform infrastructure only.

Allowed:

Memory/None provider selection
process-local cache implementation
options
instrumentation
DI registration

Forbidden:

module-specific cache policies
Catalog/Order/etc business invalidation policy
foreign module Application/Domain/Infrastructure/Persistence references
module DbContext
HttpContext-based cache key authority
module-specific constants/branches

Modules continue to see only neutral BuildingBlocks cache contracts.

REDIS READINESS — NO REDIS IMPLEMENTATION NOW
Preserve the provider abstraction so a future Redis provider can implement the same contracts.

Do NOT:

add StackExchange.Redis
add Redis configuration
add distributed invalidation
add distributed lock
change current Provider support beyond Memory/None

The task is to make the current foundation correct and future-ready, not to introduce Redis now.

DURABLE GUARDS
Create/strengthen HostCachingAmcGuardTests.

Must lock:

Host/Caching PRESENT
exactly 4 production files
exact namespace Tooba.Host.Caching
foreign module layers ZERO
module DbContext ZERO
IMemoryCache not exposed to modules
provider support remains Memory/None only
Redis package/reference absent
no TenantId/full key/payload high-cardinality logging/metrics
no message-text classification
single-flight correctness guard/test exists
type mismatch does not return false-success hit
provider parity tests exist
positive TTL validation exists
canonical key tenant/edition guards remain
FOCUSED TESTS
Add focused deterministic tests for:

A. Single-flight

many concurrent callers, same key
factory executes exactly once
all callers get same successful result

B. Single-flight failure

factory throws
result is not cached
next call can retry successfully
no coordination deadlock/leak

C. Cancellation

cancelled waiter does not corrupt in-flight state for other callers

D. Type mismatch

cache same key with type A
read as type B
must not report successful hit-null
selected repair behavior verified

E. TTL validation

zero and negative absolute/sliding/null TTL rejected

F. Provider parity

Memory and None reject invalid tag/namespace consistently

G. Tenant/edition key isolation

preserve existing key-builder invariants

No timing-flaky sleeps if avoidable.
Use synchronization primitives/barriers/tasks for deterministic concurrency tests.

RECOVERY / SOT — MANDATORY DoD
Update all authoritative surfaces:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md

Required final state:

lastAcceptedTask = TB-TMAR-HOST-CACHING-AMC-001
lastAcceptedCommit = <actual caching implementation commit SHA>
lastAcceptedCommitKind = IMPLEMENTATION_COMMIT
latestAcceptedImplementationWave = TB-TMAR-HOST-CACHING-AMC-001
currentHostEvacuation.currentTask = TB-TMAR-HOST-CACHING-AMC-001
currentHostEvacuation.activeModule = Caching
currentHostEvacuation.currentHostCheckpoint = Caching
active state = CACHING_KEEP_GENERIC_HOST_INFRASTRUCTURE_USER_REVIEW_REQUIRED
workflowStop = USER_REVIEW_HOST_CACHING_AMC_001_KEEP_GENERIC_HOST_INFRASTRUCTURE
nextTask = USER_REVIEW_HOST_CACHING_AMC_001_KEEP_GENERIC_HOST_INFRASTRUCTURE
nextTaskState = USER_DECISION_REQUIRED
automaticNextImplementationTask = NONE
staleCurrentPointerState = ZERO

Historical accepted lineage must remain intact, including:

OperatorProfile R1
Support R1
Transport
Wallet
ProductQnA
Preferences
Reviews
Security
Persistence

If docs/stamp is a separate commit:
lastAcceptedCommit MUST still point to the implementation commit.

SCOPE LIMIT
Do NOT:

make Caching HOST_ZERO
add Redis
redesign all module cache usage
add business cache policies
change frontend
change database schema/migrations
start another Host folder
run solution-wide refactors
touch unrelated user file accesscontrol-first-slice-map.md

FOCUSED VALIDATION ONLY

Build:

BuildingBlocks
Host
directly affected tests

Run:

HostCachingAmcGuardTests
focused cache correctness tests
focused cache key builder tests
TmarDurableGuardTests

No solution-wide test run.

EVIDENCE
Create:
docs/evidence/TB-TMAR-HOST-CACHING-AMC-001/

Required:

analyze.md
single-flight.md
type-safety.md
policy-validation.md
provider-parity.md
boundary-and-telemetry.md
recovery.md
validation.md

Persist exact task:
docs/ai/tasks/TB-TMAR-HOST-CACHING-AMC-001.task.md

SUCCESS CRITERIA
PASS only if all are true:

disposition = KEEP_AS_GENERIC_HOST_CACHE_INFRASTRUCTURE
Host/Caching remains PRESENT
exact 4-file allowlist
path↔namespace = EXACT_Tooba.Host.Caching
foreign module layers = ZERO
module DbContext = ZERO
single-flight race = CLOSED
same-key concurrent factory execution = EXACTLY_ONE
failed factory retry = SAFE
cancellation does not corrupt in-flight coordination
type mismatch silent-hit-null = ZERO
zero/negative TTL accepted = ZERO
Memory/None validation parity = PRESERVED
tenant/edition key isolation = PRESERVED
high-cardinality telemetry leakage = ZERO
payload/full-key/TenantId logging = ZERO
Redis implementation/package = ZERO
behavior parity for valid cache operations = PRESERVED
frontend unchanged
schema unchanged
Recovery reconciled to Caching
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
Task-ID: TB-TMAR-HOST-CACHING-AMC-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE
Summary:
Caching-Disposition-State:
Host-Caching-File-Count:
Path-Namespace-State:
Foreign-Module-Layer-State:
Module-DbContext-State:
Single-Flight-State:
Single-Flight-Failure-Retry-State:
Cancellation-Safety-State:
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