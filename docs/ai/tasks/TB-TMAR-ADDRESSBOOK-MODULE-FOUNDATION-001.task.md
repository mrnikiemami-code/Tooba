PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001
Parent-Task: TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook module foundation shell only
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001

ACCEPTED-PARENT-COMMIT:
b22e73fd12182556c712af234555c90383e0a542

RECOVERY-SOT-CHECKPOINT:
8624574a66dd60f5236932c0c61095649dec5109

TIMEBOX:
Target <= 6 minutes.
Hard maximum 8 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONLY TWO LOGICAL CHANGES:

Create the module-owned Tooba.AddressBook.Endpoints project shell in ARCH-COMPLETE-002-compatible shape.
Register the AddressBook Application assembly in the existing AddToobaCqrsFoundation assembly list.

Nothing else.

CHANGE 1 — ENDPOINTS PROJECT SHELL:

Create:

src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj

Requirements:

TargetFramework net8.0
nullable + implicit usings consistent with sibling endpoint projects
FrameworkReference Microsoft.AspNetCore.App
ProjectReference only:
Tooba.AddressBook.Application
Tooba.BuildingBlocks
NO Host reference
NO Infrastructure reference
NO foreign Application/Domain/Infrastructure reference

Create root composition file:

src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/AddressBookEndpointModule.cs

Namespace:
Tooba.AddressBook.Endpoints

This is FOUNDATION SHELL ONLY.

It may expose the standard endpoint-presentation registration and module-map extension signatures needed by the later migration, but MUST NOT map any AddressBook route yet.

If a no-op MapAddressBookModuleEndpoints extension is introduced:

it must be explicitly documented as temporary FOUNDATION_ONLY_NO_ROUTES
it must contain no route mappings
Program.cs MUST NOT switch to it in this task

Do NOT create Customer endpoint files yet.
Do NOT move request DTOs.
Do NOT add authorizer/actor seams yet.
Do NOT add handlers.
Do NOT add validators.

If the repo uses a solution/project list that must include the new project for normal tooling, add only the minimum project inclusion required.

CHANGE 2 — CQRS FOUNDATION ASSEMBLY REGISTRATION:

In Host Program.cs existing AddToobaCqrsFoundation(...) call:

add the AddressBook Application assembly using a stable AddressBook Application type.

Preferred current type:
typeof(Tooba.AddressBook.Application.IAddressBookDirectory).Assembly

If using an interface type is incompatible with existing style, use another stable current AddressBook.Application type and document it.

Purpose:

make future AddressBook MediatR handlers/validators discoverable through the existing shared foundation
NO second MediatR registration
NO second validator pipeline
NO AddMediatR/AddValidatorsFromAssembly duplicate registration outside the existing foundation

Do NOT add any IRequest/handler in this task.

PROTECTED CURRENT HOST STATE:

Do NOT modify:

Host/AddressBook/AddressBookEndpoints.cs
Host/AddressBook/AddressBookDevelopmentSeed.cs
app.MapAddressBookEndpoints()
ProductWorkspaceDevelopmentBootstrap AddressBook seed calls
AddressBook route behavior
Order guest actor dependency
AddressBook Application/Domain/Infrastructure root structure
DB/schema/migrations

This task is foundation-only.

BOUNDARY RESULT REQUIRED:

After task:

Tooba.AddressBook.Endpoints -> Tooba.Host = ZERO
Tooba.AddressBook.Endpoints -> Tooba.AddressBook.Infrastructure = ZERO
Tooba.AddressBook.Endpoints -> foreign Application/Domain/Infrastructure = ZERO

Allowed:
Tooba.AddressBook.Endpoints -> Tooba.AddressBook.Application
Tooba.AddressBook.Endpoints -> Tooba.BuildingBlocks

Host still owns all six AddressBook routes after this task.
That is expected.

FOCUSED VALIDATION ONLY:

Mandatory:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore

Then:
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

No tests unless compilation reveals an exact existing project-registration guard that must be updated.
No solution build.
No broad architecture suite.
No integration tests.
No retries.

CANONICAL TASK ARTIFACT:

Commit this exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001/addressbook-module-foundation.md

Evidence must include:

new Endpoints project refs
root file state
endpoint route count in new project = ZERO
Program CQRS foundation assembly registration
proof no duplicate MediatR/FluentValidation pipeline added
Host six-route ownership unchanged
Host AddressBook two files unchanged
boundary audit
focused build results
exact recommended next slice

RECOVERY HONESTY:

AddressBook remains IN_PROGRESS.
NOT COMPLETE_REFERENCE_PATTERN.
NOT STRUCTURE_CERTIFIED.

Expected next work should be a small CQRS/route slice, not a big-bang migration.

PASS ONLY IF:

Endpoints project exists and builds
root contains only AddressBookEndpointModule.cs
new project references only Application + BuildingBlocks
AddressBook Application assembly is added to existing CQRS foundation
no duplicate MediatR/validator registration introduced
new Endpoints project maps ZERO routes
Host still owns all six routes
Host AddressBook files unchanged
focused builds pass
canonical task/evidence committed
hard timebox respected

If safe completion exceeds 8 minutes:
Status = INCOMPLETE
STOP.
Do not start CQRS work.
Do not migrate routes.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-MODULE-FOUNDATION-001
Parent-Task: TB-TMAR-HOST-ADDRESSBOOK-INVENTORY-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Endpoints-Project-State:
Endpoints-Root-State:
Endpoints-Project-References:
New-Endpoint-Route-Count:
Cqrs-Foundation-Registration-State:
Duplicate-Pipeline-State:
Host-Route-Ownership-State:
Host-AddressBook-Files-State:
Boundary-Audit-State:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start the next CQRS slice.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
