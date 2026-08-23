# Tooba — TB-P02-T002 — REPAIR — Wire Real SpiceDB Client & Integration Test

BEGIN_TOOBA_CURSOR_TASK_V1

Protocol-Version: 1
Task-ID: TB-P02-T002
Repair: YES
Phase: P02 — Identity / Authorization
Type: REPAIR / SpiceDB Real Integration
Repository: https://github.com/mrnikiemami-code/Tooba
Branch: main
Execution-Mode: PIPELINE
Architect-Decision-On-Previous-Result: REPAIR_REQUIRED

## Why This Repair Exists

The T002 foundation is structurally correct, but Architect ACCEPT is withheld because:

```text
Mode=SpiceDb
```

currently fails closed without a real SpiceDB client being wired.

The original task required an actual SpiceDB-compatible infrastructure adapter, not only an unavailable stub.

This repair is bounded to wiring the real client and proving it with an isolated real SpiceDB integration test.

Do NOT expand into Party/Organization/B2B or final permission matrices.

---

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
62523971e4e46c9915ebe38ff5f3928eabb49bf7
```

Require synchronized safe `main`.

Unsafe/ambiguous state => `RECOVERY_CONFLICT`.

No force push, history rewrite, destructive reset, silent stash, or unrelated work.

---

## Locked Versions

Use exactly:

```text
Authzed.Net = 1.6.0
SpiceDB integration-test image = authzed/spicedb:v1.56.0
```

Do not use:

```text
floating versions
preview builds
latest tag
community/unofficial .NET client
```

The official .NET client must remain Infrastructure-only.

---

## Real SpiceDB Adapter

Replace the current `Mode=SpiceDb -> Unavailable-only` behavior with a real adapter using the official client.

The adapter must implement the Tooba-owned abstractions already created for:

```text
permission checks
relationship writes
schema read/write/bootstrap where authorized
```

Do not leak `Authzed.Net` / generated gRPC types into:

```text
Domain
Application
ModuleContracts
```

---

## Authentication / Transport

Support:

```text
endpoint
preshared token / credential
TLS configuration
timeout
```

No real secret in repository.

Do not log token.

Development/Test may use a known local preshared key only inside isolated test configuration.

Production must not silently disable TLS verification.

---

## CheckPermission

Wire real permission checks.

Required mapping:

```text
SpiceDB HAS_PERMISSION -> AuthorizationDecision.Allow
SpiceDB NO_PERMISSION -> AuthorizationDecision.Deny
transport/server error -> AuthorizationDecision.Unavailable
```

Do not convert transport failure to DENY if the distinction would be lost internally.

Do not fail open.

---

## Relationship Writes

Wire the typed Tooba tuple writer to real SpiceDB relationship write APIs.

Do NOT accept arbitrary raw tuple strings from business modules.

Maintain typed validation for:

```text
subject
relation
resource
```

---

## Schema Apply

Wire real schema write/bootstrap support.

Rules:

```text
ApplySchemaOnStartup remains opt-in
production must not blindly overwrite schema on every startup
development/test may apply schema explicitly
schema version remains tracked/documented
```

Use the existing minimal foundation schema only.

Do not add Catalog/Order/Seller/Party permissions.

---

## Integration Test — MANDATORY

Add an isolated real SpiceDB integration test.

Preferred:

```text
Testcontainers
```

or equivalent isolated Docker process.

Use exact image:

```text
authzed/spicedb:v1.56.0
```

Start with a test-only preshared key.

Test must prove real network/gRPC behavior:

```text
start SpiceDB
apply schema
write relationship
check ALLOW
check DENY
verify Tenant A does not authorize Tenant B
delete/change relationship if useful
verify unavailable/fail-closed behavior when service is stopped
```

Do not report PASS from mocks/in-memory adapter as integration PASS.

---

## InMemory Adapter

Keep the in-memory adapter only as:

```text
test/dev double
```

Production mode must not use it.

No silent production fallback from SpiceDB to InMemory.

---

## Configuration Validation

If:

```text
Mode=SpiceDb
```

then validate required configuration.

Invalid/missing config must fail clearly.

Do not start protected production mode in silent allow-all state.

---

## Observability

Add safe tracing/metrics/logging around real SpiceDB calls.

May include:

```text
operation
resource type
permission
outcome
latency
edition
```

Do not use high-cardinality metric labels:

```text
UserId
TenantId
ResourceId
```

Never log token.

---

## Persian Documentation — MANDATORY

Every new/changed Tooba-owned:

```text
Class
Interface
Method
Function
Property
Constructor
Record
Struct
Enum
important internal member
```

must have strong Persian documentation.

Security-sensitive members must explain why:

```text
fail-closed
no SDK leakage
no token logging
no InMemory production fallback
```

Weak/name-echo comments = acceptance failure.

---

## Architecture Documentation

Update:

```text
docs/architecture/38-spicedb-authorization-foundation.md
```

Document:

```text
Authzed.Net 1.6.0
real gRPC client
SpiceDB v1.56.0 integration-test image
configuration/TLS/token model
check mapping
relationship writes
schema bootstrap
integration test strategy
fail-closed semantics
```

Remove any wording implying the SpiceDb adapter is still unwired.

---

## SoT

Keep:

```text
TB-P02-T002 = REPAIR IN PROGRESS / AWAITING_ARCHITECT_ACCEPT
P02 = IN_PROGRESS
```

Do NOT issue T003.

---

## Validation

Run current:

```bash
dotnet restore
dotnet build
dotnet test
```

SpiceDB integration test must actually run if Docker is available in this environment.

Frontend:

```bash
cd src/frontend
npm ci
npm run typecheck
npm run lint
npm run build
```

Then:

```bash
git diff --check
git status --short --branch
```

Manual checks:

```text
Authzed.Net exactly 1.6.0
real SpiceDB adapter wired
no SDK leakage to Domain/Application
real schema write
real relationship write
real permission check
real isolated SpiceDB integration test
tenant isolation
no fail-open
no token logging
strong Persian docs
```

---

## Git

Suggested commit:

```text
fix wire real SpiceDB client [TB-P02-T002]
```

Push `origin/main`, fetch, require:

```text
HEAD == origin/main
```

No force push.

---

## Result Contract

Return:

```text
BEGIN_TOOBA_CURSOR_RESULT_V1

Protocol-Version: 1
Task-ID: TB-P02-T002
Repair: YES
Phase: P02 — Identity / Authorization
Status: PASS | BLOCKED | RECOVERY_CONFLICT

Summary:
...

Repository-Recovery:
- Repo-Root:
- Branch:
- Starting-HEAD:
- Starting-Origin-Main:
- Starting-Status:

Packages:
- Authzed.Net:
- SpiceDB test image:

Real-SpiceDB-Adapter:
- ...

Permission-Check:
- ...

Relationship-Write:
- ...

Schema-Bootstrap:
- ...

Tenant-Isolation:
- ...

Integration-Test:
- SpiceDB process/container started:
- schema applied:
- relationship written:
- ALLOW verified:
- DENY verified:
- tenant isolation verified:
- service unavailable behavior verified:

Security-Observability:
- ...

Persian-Documentation:
- ...

Validation:
- backend restore:
- backend build:
- backend tests:
- spicedb integration tests:
- frontend install:
- frontend typecheck:
- frontend lint:
- frontend build:
- git diff --check:

Git:
- Commit:
- Push:
- Final-HEAD:
- Final-Origin-Main:
- Final-Status:
- Head-Matches-Origin:

Source-of-Truth:
- Current Task:
- Task State:
- Current Phase:
- T003 Issued:
- Recovery-Ready:

Architectural-Concerns:
- ...

Blockers:
- ...

END_TOOBA_CURSOR_RESULT_V1
```

---

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

Do not start `TB-P02-T003` without a new valid Architect Envelope.

END_TOOBA_CURSOR_TASK_V1
