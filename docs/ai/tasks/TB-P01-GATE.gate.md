# Tooba — TB-P01-GATE — Platform Foundation Acceptance Gate

BEGIN_TOOBA_CURSOR_GATE_V1

Protocol-Version: 1
Gate-ID: TB-P01-GATE
Phase: P01 — Platform Foundation
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Depends-On: TB-P01-T009
Architect-Decision-On-Dependency: ACCEPTED

## Objective

Perform the final evidence-based acceptance gate for P01.

Do NOT add new product features.

Verify that the Platform Foundation created by T001–T009 is coherent, buildable, recoverable, and ready for P02.

## Repository Recovery

Run:

```bash
git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch
```

Expected predecessor:

```text
cdc7c3c42e72466dceef58c5ce82e4c352536c07
```

Require synchronized safe `main`.

Unsafe/ambiguous state => `RECOVERY_CONFLICT`.

## Gate Scope

Review and validate the accepted P01 chain:

```text
TB-P01-T001 Platform bootstrap
TB-P01-T002 Observability / ProblemDetails
TB-P01-T003 Edition / Tenant / DB resolution
TB-P01-T004 PostgreSQL persistence
TB-P01-T005 Persian code documentation enforcement
TB-P01-T006 Domain Events / transactional Outbox / background foundation
TB-P01-T007 MassTransit 8.5.10 PostgreSQL SQL Transport
TB-P01-T008 Cache abstraction
TB-P01-T009 Module composition / architecture guards
```

## Mandatory Architecture Checks

Verify all remain true:

```text
Modular Monolith
PostgreSQL canonical DB
Marketplace and SingleStore deployment models remain distinct
TenantId != Hostname
SingleStore DB-per-tenant
one messaging transport DB per deployment
MassTransit = 8.5.10
MassTransit.SqlTransport.PostgreSQL = 8.5.10
RabbitMQ absent
MassTransit v9 absent
module-owned DbContext
no global business DbContext
no cross-module SQL JOIN assumption
no cross-module EF navigation
no cross-module transaction assumption
Domain Event != Integration Event
module-owned transactional Outbox retained
Cache != Source of Truth
Redis deferred
Backend module boundary != UI boundary
```

## Dependency / Package Audit

Report exact relevant versions and verify no accidental drift for:

```text
.NET target
EF Core
Npgsql EF provider
Npgsql
MassTransit
MassTransit.SqlTransport.PostgreSQL
OpenTelemetry packages
Next.js
React
Tailwind
```

Do NOT upgrade packages in the Gate unless required to repair a build-breaking inconsistency.

Any proposed non-essential upgrade = Architectural Concern only.

Verify absence of:

```text
MassTransit.RabbitMQ
RabbitMQ.Client
MassTransit 9.x
Redis packages
```

## Security / Isolation Checks

Verify:

```text
unknown tenant fails closed
disabled tenant fails closed
Tenant A cannot resolve Tenant B DB
Tenant A cache != Tenant B cache
background consumers do not derive TenantId from Host
connection strings/secrets are not logged
ProblemDetails does not leak internals in production
forwarded Host trust is restricted
```

## Messaging Checks

Verify actual PostgreSQL SQL Transport integration remains real, not mocked-only.

Verify:

```text
T006 outbox -> MassTransit publisher -> PostgreSQL SQL Transport -> consumer
```

and:

```text
at-least-once assumption preserved
no exactly-once claim
retry/dead-letter domains documented
no duplicate EF Outbox for same outgoing message
```

## Persistence / Boundary Checks

Verify executable architecture tests still guard:

```text
Domain !-> Infrastructure
Application !-> Host
Module A Infrastructure !-> Module B Infrastructure/Persistence
no mega DbContext
```

PlatformProbe must remain explicitly disposable/non-business.

## Cache Checks

Verify:

```text
Memory provider only
no Redis package
tenant/edition isolation
Market/Locale/Currency dimensions supported
single-flight concurrency test
tag invalidation test
```

## Persian Documentation Gate

Hard requirement:

```text
All required Tooba-owned Classes / Interfaces / Methods / Properties
have strong meaningful Persian documentation.
```

Verify:

```text
CS1591 enforcement active
no blanket suppression
generated-code exclusions narrow
```

Weak/name-echo documentation = Gate failure.

## Full Validation — CURRENT RUN

Run now, do not inherit prior results:

```bash
dotnet restore
dotnet build
dotnet test
```

Frontend:

```bash
cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run build
```

Return to root:

```bash
git diff --check
git status --short --branch
```

All PostgreSQL/Testcontainers/MassTransit integration tests must run where supported by this environment.

## Source-of-Truth Reconciliation

Review:

```text
AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md
```

Ensure they agree on:

```text
P01 Gate in progress
last accepted task = TB-P01-T009
current gate = TB-P01-GATE
next phase after Architect ACCEPT = P02
```

Do NOT mark P01 complete before Architect ACCEPT.

## Mandatory Future UX Sequence

Verify SoT still preserves:

```text
Deep Shopeiva Study
Template reuse map
Design System extraction
Professional reusable Data Grid
Workspace interaction patterns
serious UI implementation
Visual evidence
Architect visual ACCEPT
```

Also preserve:

```text
Backend/module boundary != UI boundary
Weak UI/UX = product failure
```

## Carry-Forward Concerns

At minimum reconcile these:

```text
OpenTelemetry package version alignment
/__platform-* diagnostic endpoints before public deploy
config-backed tenant registry is not production control plane
Npgsql / MassTransit SQL Transport compatibility constraint
SQL Transport admin/runtime credential split
PlatformProbe disposable
durable Inbox/dedup deferred
delayed redelivery/scheduler deferred
T006 Outbox vs MassTransit EF Outbox future review
cache process-local until Redis
```

Classify each:

```text
BLOCKER
REPAIR_BEFORE_P02
DEFERRED_NON_BLOCKING
RESOLVED
```

Do not silently drop concerns.

## Gate Repairs

Only bounded repairs necessary for P01 coherence are allowed.

No new business capability.

If a material architectural conflict appears, return `BLOCKED`.

## Gate Documentation

Create:

```text
docs/evidence/TB-P01-GATE.md
```

Include:

```text
scope reviewed
validation evidence
package audit
architecture invariants
security/isolation checks
messaging/cache/persistence checks
documentation enforcement
concern classifications
final gate recommendation
```

## SoT Update

Before Architect review set:

```text
Last Architect Accepted Task: TB-P01-T009
Current Gate: TB-P01-GATE
Current Phase: P01 — Platform Foundation
Gate State: AWAITING_ARCHITECT_ACCEPT
```

Do not mark P01 COMPLETE yet.

## Save Gate Envelope VERBATIM

Save this complete envelope exactly to:

```text
docs/ai/tasks/TB-P01-GATE.gate.md
```

Do not summarize or condense.

## Git

If docs/evidence/SoT or bounded repairs changed:

```bash
git add .
git commit -m "chore run Tooba P01 platform foundation gate [TB-P01-GATE]"
git push origin main
git fetch origin
```

Require:

```text
HEAD == origin/main
```

No force push.

## Gate Result Contract

Return:

```text
BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Gate-ID: TB-P01-GATE
Phase: P01 — Platform Foundation
Status: PASS | REPAIR_REQUIRED | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- Repo-Root:
- Branch:
- Starting-HEAD:
- Starting-Origin-Main:
- Starting-Status:

Architecture-Invariants:
- ...

Package-Audit:
- ...

Security-Isolation:
- ...

Persistence-Boundaries:
- ...

Messaging:
- ...

Caching:
- ...

Persian-Documentation:
- ...

Validation:
- backend restore:
- backend build:
- backend tests:
- postgres/integration tests:
- frontend install:
- frontend typecheck:
- frontend lint:
- frontend build:
- git diff --check:

Concern-Classification:
- ...

Gate-Evidence:
- File:

Git:
- Commit:
- Push:
- Final-HEAD:
- Final-Origin-Main:
- Final-Status:
- Head-Matches-Origin:

Source-of-Truth:
- Last-Architect-Accepted-Task:
- Current-Gate:
- Current-Phase:
- Gate-State:
- Recovery-Ready:

Gate-Recommendation:
- P01_GATE_PASS | REPAIR_REQUIRED | BLOCKED

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1
```

## Pipeline Continuity — MANDATORY

After sending RESULT:

```text
Wait for the USER / Architect to provide the next valid task
in this SAME chat/session.
```

Do not leave PIPELINE mode.

Do not close the chat/session.

Do not treat the workflow as finished.

Do not invent, infer, prepare, or execute the next Task or Gate yourself.

Remain inside the Tooba Architect-controlled pipeline until the USER / Architect provides the next valid Envelope in this same chat.

Cursor PASS is not Architect ACCEPT.

Do not start P02 without a new Architect Envelope.

END_TOOBA_CURSOR_GATE_V1
