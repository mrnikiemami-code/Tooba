PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W0
Parent-Task: NONE
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — PlatformProbe
Mode: ARCHITECT_DIRECT_AMSC
Track: PLATFORMPROBE_AMC
Title: Analyze PlatformProbe ownership, runtime necessity, test-fixture disposition, and migration safety

ARCHITECT DIRECTIVE

ANALYZE ONLY.

Do not modify production behavior.
Do not move files.
Do not delete PlatformProbe.
Do not create Domain/Application/Contracts/Endpoints projects.
Do not change solution grouping.
Do not change migrations, migration order, Host registration, tests, schema, or runtime composition.
Do not start W1 automatically.

CURRENT VERIFIED MAIN

Expected starting HEAD:
e89414fb691eb8e38c21f4d3735d49a80679a7a6

Current physical state independently verified:

src/backend/Modules/PlatformProbe/Tooba.PlatformProbe.Infrastructure/

contains:

PlatformProbeModule.cs
PlatformProbeOutboxRegistration.cs
Events/ProbeEvents.cs
Persistence/PlatformProbeDbContext.cs
Persistence/Migrations/*
Tooba.PlatformProbe.Infrastructure.csproj

Current semantic intent is explicitly described in source/docs as:

not a business capability
disposable sample / convention proof
persistence foundation proof
outbox/domain/integration-event foundation proof

Current solution placement:

project is flat under /Modules/ rather than /Modules/PlatformProbe/

Current runtime characteristics already observed:

owns schema platform_probe
owns probe_records
owns module-local outbox_messages
registers PlatformProbeDbContext
registers IOutboxModuleRegistration
participates in module schema migration ordering
Host references Tooba.PlatformProbe.Infrastructure
Host persistence/outbox tests consume PlatformProbe types heavily

ARCHITECT QUESTION

Determine the correct professional end-state under the four canonical skills:

Analyze -> Migrate -> Structure -> Certify

The analysis must decide whether PlatformProbe is:

A. a legitimate INTERNAL_ONLY production module that should remain under Modules/ with only Infrastructure, or

B. a legacy disposable foundation probe whose production participation should be removed and whose remaining value should be rehomed as a test fixture / architecture test support, or

C. another clearly justified canonical category already present in repository standards.

Do NOT create ceremonial Domain/Application/Contracts/Endpoints projects merely for symmetry.

ANALYSIS SCOPE

OWNERSHIP / RESPONSIBILITY

Classify every production responsibility currently inside PlatformProbe:

PlatformProbeModule
PlatformProbeDbContext
PlatformProbeRecord
PlatformProbePersistence
PlatformProbeOutboxRegistration
ProbeRecordCreatedDomainEvent
ProbeInternalNoteDomainEvent
ProbeRecordCreatedIntegrationEvent
migrations

For each, classify using canonical responsibility vocabulary where applicable:

DOMAIN_RULE
APPLICATION_USE_CASE
CONTRACT
PERSISTENCE
INTEGRATION_ADAPTER
HOST_COMPOSITION_ROOT
DEVELOPMENT_SEED
TEST_FIXTURE / FOUNDATION_PROBE if justified by current repository semantics

Explicitly state whether PlatformProbe has any real business/domain capability.

COMPLETE CONSUMER INVENTORY

Find every current consumer/reference of PlatformProbe across production, tests, docs, migration infrastructure, solution metadata, and artifacts relevant to source truth.

At minimum verify:

Host project reference
runtime module registration/discovery path
migration registration/order
migration runner implications
outbox registration/polling implications
Host tests that directly use PlatformProbe
any architecture guards
docs that still define PlatformProbe as a disposable sample
solution .slnx
current SoT / manifest references, if any

Do not infer from filenames only; inspect the actual references.

Produce a consumer matrix:
Consumer | Surface | Production/Test/Docs | Why It Depends | Required If Probe Removed?

PRODUCTION NECESSITY

Answer explicitly:

Is any production request/use-case dependent on PlatformProbe?
Is any production business module dependent on PlatformProbe?
Is any production data flow dependent on PlatformProbe events?
Is any production outbox dispatcher behavior dependent on PlatformProbe specifically rather than generic registrations?
Is PlatformProbe required for application startup?
Is PlatformProbe required only because it is currently registered in the module graph?
Would removal from production change customer/admin/seller behavior?

Separate:
GENERIC FOUNDATION DEPENDENCY
from
PLATFORMPROBE-SPECIFIC DEPENDENCY.

MIGRATION / SCHEMA SAFETY

This is mandatory and must be precise.

Analyze:

exact current migration-order entry for PlatformProbe
how module schema migrators are registered/discovered
whether removing PlatformProbe from future runtime migration orchestration is safe
whether an already-deployed platform_probe schema/table can remain orphaned safely
whether deleting old migrations from source would be unsafe
whether any future migration runner assumes exact contiguous module count/order
whether tests/guards assert the current migration count/order
whether safe production removal requires:
keeping historical migrations in a non-runtime fixture,
retaining migration metadata somewhere,
leaving schema untouched,
or another bounded compatibility mechanism

Do NOT propose DROP SCHEMA unless current architecture explicitly requires it and evidence supports it.

Prefer preservation of historical deployed data/schema over destructive cleanup.

TEST-FIXTURE REHOME FEASIBILITY

If removal from production is the correct end-state, identify the smallest professional destination for the probe functionality used by tests.

Evaluate concrete options against repository conventions, for example:

Host/Tooba.Host.Tests/Fixtures/PlatformProbe/...
dedicated test-support project only if genuinely justified
another existing test-support location if canonical

Do NOT put module-specific probe tables into BuildingBlocks production.
Do NOT create a new shared production layer for test-only code.

Specify:

which types must move/recreate for tests
whether EF migrations are actually required by the integration tests
whether tests can define a test-only DbContext/schema without production module registration
how Outbox tests preserve realistic same-transaction behavior
how production Host project reference can be removed without losing test evidence
FOUR-SKILL APPLICABILITY

Give exact classifications:

Analyze:

business capability state
ownership state
current structural anomalies

Migrate:

whether production evacuation is required
exact bounded migration waves if required

Structure:

expected final filesystem/solution shape
whether /Modules/PlatformProbe/ should remain at all
whether INTERNAL_ONLY applies or TEST_FIXTURE_ONLY is more accurate

Certify:

exact final certification target
whether COMPLETE_REFERENCE_PATTERN applies to PlatformProbe as a business module at all
what final absence/presence guards are required
DECISION

Return exactly one recommended disposition:

KEEP_INTERNAL_ONLY_PRODUCTION_MODULE
or
REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE
or
BLOCKED_ARCHITECT_DECISION

Do not return multiple preferred options.

If REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE, provide a bounded wave plan. Keep each future wave narrow; do not combine all migration work into one giant task.

EXPECTED WAVE PLAN SHAPE IF REMOVAL IS CORRECT

Do not implement these now, but analyze whether a sequence similar to this is sufficient:

W1: test fixture extraction / parity first
W2: detach PlatformProbe from production Host/module/migration graph
W3: structure + stale-copy cleanup + solution cleanup
W4: certify absence from production + SoT/recovery

Change this sequence only if repository evidence proves a better bounded order.

MANDATORY EVIDENCE

Create:

docs/architecture/evidence/TB-TMAR-PLATFORMPROBE-AMC-001-W0/

At minimum:

analyze.md
consumer-matrix.md
migration-safety.md
physical-tree-before.md
recommended-disposition.md

Persist exact task:
docs/ai/tasks/TB-TMAR-PLATFORMPROBE-AMC-001-W0.task.md

VALIDATION

Analysis validation only:

repository starts from expected HEAD or report divergence
complete PlatformProbe source tree captured
consumer inventory includes production + tests + docs
migration-order/runtime registration inspected
no production files changed
no test production behavior changed
no schema/migration changed
no frontend changed

Do not run the full test suite.
Build/test execution is not required for analysis unless needed to resolve one specific factual uncertainty.
If one focused command is required, keep it minimal and record why.

CANONICAL RESULT CONTRACT

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-PLATFORMPROBE-AMC-001-W0
Parent-Task: NONE
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary: <bounded summary>
Starting-HEAD-State: E89414FB | DIVERGED
Business-Capability-State: NONE | PRESENT | UNCLEAR
Current-Ownership-State: FOUNDATION_PROBE | INTERNAL_PRODUCTION_MODULE | MIXED | UNCLEAR
Production-Business-Consumer-State: ZERO | PRESENT | UNCLEAR
PlatformProbe-Specific-Runtime-Dependency-State: <state>
Host-ProjectReference-State: PRESENT | ABSENT
Migration-Order-State: <exact state>
Migration-Safety-State: SAFE_TO_DETACH_KEEP_SCHEMA | REQUIRES_BOUNDED_COMPATIBILITY | BLOCKED
Test-Fixture-Rehome-State: FEASIBLE | NOT_FEASIBLE | BLOCKED
Recommended-Disposition: KEEP_INTERNAL_ONLY_PRODUCTION_MODULE | REMOVE_FROM_PRODUCTION_REHOME_TEST_FIXTURE | BLOCKED_ARCHITECT_DECISION
Future-Wave-Plan-State: BOUNDED | NOT_REQUIRED | BLOCKED
Production-Change-State: ZERO
Schema-Change-State: ZERO
Frontend-State: UNTOUCHED
Evidence-State: COMPLETE | INCOMPLETE
Analysis-Commit-SHA: <sha>
HEAD-Equals-Origin-Main: YES | NO
Working-Tree-State: CLEAN | <state>
User-Work-Preserved: YES | NO
Automatic-Next-Implementation-Task-State: NONE
Workflow-Stop-State: USER_REVIEW_PLATFORMPROBE_AMC_001_W0
STOP
END_TOOBA_WORKER_RESULT

STOP RULE

After result:
STOP completely.
Do not implement W1.
Do not move PlatformProbe.
Do not delete the schema or migrations.
Do not start another module.
Wait for Architect review.

END_TOOBA_TASK
