# Tooba — TB-P02-GATE — Identity / Authorization Acceptance Gate

BEGIN_TOOBA_CURSOR_GATE_V1

Protocol-Version: 1
Gate-ID: TB-P02-GATE
Phase: P02 — Identity / Authorization
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Depends-On: TB-P02-T005
Architect-Decision-On-Dependency: ACCEPTED

Objective

Perform the final evidence-based acceptance gate for P02.

Do NOT add new product features.

Review and validate:

TB-P02-T001 Identity / Authentication Foundation
TB-P02-T002 SpiceDB Authorization Foundation + Repair
TB-P02-T003 Party / Organization / Membership Foundation
TB-P02-T004 Session / Token / Credential Lifecycle
TB-P02-T005 Authentication HTTP Boundary + Repair

P02 must only pass if authentication, authorization, Party separation, tenant isolation, session lifecycle, and recovery evidence are coherent together.

Repository Recovery

Run:

git rev-parse --show-toplevel
git fetch origin
git branch --show-current
git rev-parse HEAD
git rev-parse origin/main
git status --short --branch

Expected predecessor:

120086689101670f4758ee1206940dce88da16a0

Require synchronized safe main.

Unsafe/ambiguous state => RECOVERY_CONFLICT.

Mandatory Architecture Invariants

Verify all remain true:

Identity/User != Party/Organization
Authentication != Authorization
Membership != Authorization
Tenant != Party
Role column != final authorization model

UserId = authentication principal identity
SpiceDB = authorization relationship/permission projection
Party DB = source of truth for Party/Membership business data

no Authzed.Net types in Domain/Application/ModuleContracts
no cross-module foreign keys
no shared mega DbContext
no Host parsing inside Identity/Party/Application code
Identity Checks

Verify:

multiple login identifiers
Username / Email / Phone normalization is type-specific
normalized identifier uniqueness
password uses proven hashing
plaintext password never persisted
disabled/locked accounts cannot authenticate
Identity remains free of Party/Seller/Agency/Customer business fields
SpiceDB Checks

Verify real SpiceDB integration remains wired:

Authzed.Net = 1.6.0
real schema write
real relationship write
real permission check
ALLOW / DENY / Unavailable distinction
fail-closed on outage
tenant isolation

Use the real isolated SpiceDB integration test if Docker is available.

Verify:

CONDITIONAL_PERMISSION -> Deny

is explicitly classified as deferred/non-blocking until caveats are introduced.

No fail-open behavior is allowed.

Party / Membership Checks

Verify:

Person and Organization are Party concepts
User link is Party-owned opaque UserId reference
one User may have multiple memberships
one Organization may have multiple users
membership rows do not contain final permission columns
organization capability model is extensible
SpiceDB projection occurs asynchronously via Outbox

No synchronous distributed transaction with SpiceDB.

Session / Credential Checks

Verify:

refresh secret stored hashed
rotation invalidates old refresh credential
reuse detection works
session revocation works
revoke-all works
SecurityStamp/CredentialVersion invalidates old sessions
password reset is expiring + single-use
identifier verification is expiring + single-use
OTP/reset challenges are PostgreSQL-backed
attempt limits exist
no plaintext bearer/reset/OTP secrets stored
HTTP Authentication Boundary

Verify current endpoints/contracts remain secure:

register
login
refresh
logout
logout-all
password reset request/complete
identifier verification request/complete
password change
current session/user

Verify:

no custom JWT crypto
opaque session boundary
enumeration-safe login/reset
Tenant authority only from trusted commerce context
X-Tenant-Id/query/body/cookie cannot override tenant
ProblemDetails contains no secrets
Authorization header / refresh / OTP / reset secrets are not logged
Authorization Boundary

Verify protected application use cases can consume:

stable UserId/session principal
IAuthorizationGuard / equivalent

without controllers directly embedding SpiceDB SDK calls.

Authentication middleware must not call SpiceDB merely to establish identity.

Security / Audit Separation

Verify:

Technical Log != Security Audit

and that security-event seams remain available for:

login success/failure
session created/revoked
password changed/reset
identifier verified
refresh reuse
permission denied
relationship changed

No full audit product is required in this gate.

Package Audit

Report exact versions for:

.NET
EF Core
Npgsql
MassTransit
MassTransit.SqlTransport.PostgreSQL
Authzed.Net
OpenTelemetry
Next.js
React
Tailwind

Do not upgrade packages in the Gate unless a current build is broken.

Verify absence of accidental:

RabbitMQ
MassTransit v9
Redis authorization cache
Keycloak/OIDC provider packages

unless explicitly already approved.

Persian Documentation Gate

Hard requirement:

required Tooba-owned Classes / Interfaces / Methods / Properties
must have strong meaningful Persian documentation

Verify:

CS1591 enforcement active
no blanket suppression
generated exclusions narrow

Weak/name-echo documentation = Gate failure.

Full CURRENT Validation

Run now; do not inherit previous results:

dotnet restore
dotnet build
dotnet test

Frontend:

cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run build

Return to root:

git diff --check
git status --short --branch

All PostgreSQL / SpiceDB / MassTransit integration tests should run where environment support exists.

Source-of-Truth Reconciliation

Review:

AGENTS.md
docs/PROJECT-STATE.md
docs/ROADMAP.md
docs/ai/TOOBA-PIPELINE-PROTOCOL.md
docs/ai/TOOBA-PIPELINE-CONTROLLER.md
docs/ai/TOOBA-RECOVERY-CONTEXT.md
docs/prompts/START-HERE-IF-CHATGPT-IS-LOST.md

They must agree on:

P01 = COMPLETE
P02 Gate = IN PROGRESS
Last accepted task = TB-P02-T005
Current gate = TB-P02-GATE
Next phase after Architect ACCEPT = P03 — Commerce Core

Do NOT mark P02 COMPLETE before Architect ACCEPT.

Concern Classification

At minimum reconcile:

OTel package split
/__platform-* diagnostics
config-backed tenant registry
Npgsql / MassTransit NodaTime constraint
SQL Transport admin/runtime credential split
durable Inbox/dedup
MassTransit delayed redelivery/scheduler
T006 Outbox vs MassTransit EF Outbox future review
process-local cache until Redis
Identity external delivery provider absent
Keycloak/OIDC deferred
WebAuthn/passkey deferred
rate-limit/anti-fraud product deferred
CONDITIONAL_PERMISSION caveats deferred
Redis authorization cache deferred

Classify each:

BLOCKER
REPAIR_BEFORE_P03
DEFERRED_NON_BLOCKING
RESOLVED

Do not silently drop concerns.

Mandatory Future UX Sequence

Verify SoT still preserves:

Deep Shopeiva Study
Template reuse map
Design System extraction
Professional reusable Data Grid
Workspace interaction patterns
serious UI implementation
Visual evidence
Architect visual ACCEPT

Also preserve:

Backend/module boundary != UI boundary
Weak UI/UX = product failure
Gate Evidence

Create:

docs/evidence/TB-P02-GATE.md

Include validation evidence, package audit, identity/authz invariants, tenant/security checks, concern classification, and final recommendation.

SoT State

Before Architect review:

Last Architect Accepted Task: TB-P02-T005
Current Gate: TB-P02-GATE
Current Phase: P02 — Identity / Authorization
Gate State: AWAITING_ARCHITECT_ACCEPT

Do NOT mark P02 COMPLETE yet.

Save Gate Envelope VERBATIM

Save exactly:

docs/ai/tasks/TB-P02-GATE.gate.md

Do not summarize or condense.

Gate Repairs

Only bounded repairs required for P02 coherence are allowed.

No Commerce Core implementation.

Material architecture conflict => BLOCKED.

Git

If evidence/SoT or bounded repairs changed:

git add .
git commit -m "chore run Tooba P02 identity authorization gate [TB-P02-GATE]"
git push origin main
git fetch origin

Require:

HEAD == origin/main

No force push.

Result Contract

Return:

BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Gate-ID: TB-P02-GATE
Phase: P02 — Identity / Authorization
Status: PASS | REPAIR_REQUIRED | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- ...

Identity:
- ...

SpiceDB-Authorization:
- ...

Party-Membership:
- ...

Session-Credential-Lifecycle:
- ...

Authentication-HTTP:
- ...

Tenant-Security:
- ...

Package-Audit:
- ...

Persian-Documentation:
- ...

Validation:
- backend restore:
- backend build:
- backend tests:
- postgres/spicedb/masstransit integration tests:
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
- P02_GATE_PASS | REPAIR_REQUIRED | BLOCKED

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1
CRITICAL — DO NOT LEAVE PIPELINE

After sending RESULT:

WAIT HERE for the USER / Architect to provide the next valid task
in this SAME chat/session.

You MUST remain inside the Tooba Architect-controlled pipeline.

Do NOT:

close this chat/session
end the agent workflow
leave PIPELINE mode
treat RESULT as the end of the work
move to another chat
wait outside this pipeline
invent the next task or gate
infer the next task
prepare P03 work
execute any P03 task

After RESULT, stay active in this SAME chat/session and wait here until the USER / Architect sends the next valid Envelope.

Only when a new valid Envelope is provided in this SAME chat/session may you execute the next task.

Cursor PASS is not Architect ACCEPT.

Leaving or ending the pipeline after RESULT is a protocol violation.

END_TOOBA_CURSOR_GATE_V1
