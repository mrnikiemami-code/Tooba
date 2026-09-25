PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ADDRESSBOOK-ACTOR-SEAM-001
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FIRST_ADDRESSBOOK
Title: AddressBook neutral actor-authority seam before route migration
Backend-Only: YES

ARCHITECT-ACCEPTED-PARENT:
TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1

ACCEPTED-PARENT-COMMIT:
5a083e2a516f2c6b10f79872107a00fbb6059c72

RECOVERY-SOT-CHECKPOINT:
2520f2ddcf5fd25c6a4a103a7ed73f3b6e791c63

TIMEBOX:
Target <= 5 minutes.
Hard maximum 7 minutes.
If safe completion exceeds hard limit, return INCOMPLETE and STOP.
No retry loop.
No scope expansion.

ONE OBJECTIVE:

Create the actor-authority seam needed so AddressBook routes can later move out of Host without referencing Host session types or Order.Application.

NO ROUTE MIGRATION in this task.

CURRENT BEHAVIOR TO PRESERVE LATER:

Host ResolveActor precedence:

1. authenticated session -> session.UserId
2. Production/non-Dev/non-Testing unauthenticated -> null / later 401
3. Development/Testing X-Tooba-Dev-Actor-User-Id valid non-empty Guid -> that actor
4. Development/Testing fallback -> Storefront guest actor

Current guest actor source is an architectural leak:
Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId

Canonical contract authority already exists:
Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId

FOUNDATION AVAILABLE:

Tooba.BuildingBlocks.Security.ICurrentAuthenticatedUser
- IsAuthenticated
- Guid? UserId

Host already has:
HostCurrentAuthenticatedUser(CurrentAuthenticatedSession) : ICurrentAuthenticatedUser

TASK DECISION:

Prefer a module-owned AddressBook endpoint-layer actor resolver that consumes the neutral shared ICurrentAuthenticatedUser seam.

Do NOT add a new generic BuildingBlocks abstraction unless repository precedent makes it strictly necessary.

Create in AddressBook.Endpoints, preferably under:
Customer/AddressBookCustomerActorResolver.cs

Expected form:
- small interface + implementation local to AddressBook.Endpoints, OR one sealed service if interface is unnecessary
- consumes ICurrentAuthenticatedUser
- consumes IHostEnvironment
- accepts HttpRequest when resolving
- preserves exact session/dev-header/environment/fallback precedence
- guest fallback MUST use:
  Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId
- NO reference to Order.Application
- NO reference to Host types
- NO CurrentAuthenticatedSession reference

If Order.Contracts project reference is needed:
add exactly that Contracts reference to Tooba.AddressBook.Endpoints.csproj.
Foreign Contracts edge is allowed.
Do not reference Order.Application/Domain/Infrastructure.

DI:
Register the resolver in AddAddressBookEndpointPresentation().
If ICurrentAuthenticatedUser is already registered in Host, do not duplicate it.
If it is not registered, add only the minimal existing HostCurrentAuthenticatedUser -> ICurrentAuthenticatedUser registration needed.
Do not add a second auth/session system.

BEHAVIOR:
Resolver should return Guid?:
- authenticated valid user -> user id
- unauthenticated in non-Development/non-Testing -> null
- Development/Testing valid X-Tooba-Dev-Actor-User-Id -> parsed non-empty Guid
- Development/Testing otherwise -> StorefrontGuestActor.ActorId

Keep header name exactly:
X-Tooba-Dev-Actor-User-Id
Do not invent new headers.
Do not change unauthorized response here.
Do not map any route here.

NO HOST MIGRATION:

Do NOT modify:
- Host/AddressBook/AddressBookEndpoints.cs
- Host/AddressBook/AddressBookDevelopmentSeed.cs
- Program app.MapAddressBookEndpoints()
- six Host route mappings

It is acceptable to add DI registration in Program only if required for ICurrentAuthenticatedUser and currently missing.

NO CQRS CHANGES:

Do NOT modify the six existing requests/handlers/validators.
Current inventory is already:
6 use cases
5 validator-required / 5 present
1 no-validator-required
gap ZERO

BOUNDARIES AFTER TASK:

AddressBook.Endpoints -> Host = ZERO
AddressBook.Endpoints -> Order.Application = ZERO
AddressBook.Endpoints -> AddressBook.Infrastructure = ZERO

Allowed:
AddressBook.Endpoints -> AddressBook.Application
AddressBook.Endpoints -> BuildingBlocks
AddressBook.Endpoints -> Order.Contracts

FOCUSED VALIDATION:

Build:
dotnet build src/backend/Modules/AddressBook/Tooba.AddressBook.Endpoints/Tooba.AddressBook.Endpoints.csproj --no-restore
dotnet build src/backend/Host/Tooba.Host/Tooba.Host.csproj --no-restore

If an existing tiny test location can directly test actor precedence without broad setup, add only focused tests.
Otherwise skip tests and document.

No solution build.
No integration suite.
No broad architecture suite.
No retries.

CANONICAL TASK ARTIFACT:

Commit exact task at:
docs/ai/tasks/TB-TMAR-ADDRESSBOOK-ACTOR-SEAM-001.task.md

EVIDENCE:

Create:
docs/evidence/TB-TMAR-ADDRESSBOOK-ACTOR-SEAM-001/addressbook-actor-seam.md

Evidence must include:

- exact resolver type/path
- exact precedence
- exact header
- ICurrentAuthenticatedUser usage
- guest actor authority = Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId
- zero Order.Application reference in AddressBook.Endpoints
- zero Host reference in AddressBook.Endpoints
- DI registration
- Host route ownership unchanged
- Endpoints route count still ZERO
- focused builds/tests
- exact next recommended route slice

RECOVERY HONESTY:

AddressBook remains IN_PROGRESS.
Host still owns all six routes.
NOT COMPLETE_REFERENCE_PATTERN.
NOT STRUCTURE_CERTIFIED.

Expected next task after PASS:
TB-TMAR-ADDRESSBOOK-ENDPOINT-READ-MIGRATION-001
for List + Get only.

PASS ONLY IF:

- actor resolver exists in module-owned endpoint layer
- exact old precedence preserved
- ICurrentAuthenticatedUser used
- fallback uses Order.Contracts constant
- no Order.Application/Host dependency in Endpoints
- no route moved
- Endpoints still maps ZERO routes
- focused builds pass
- canonical task/evidence committed
- hard timebox respected

If safe completion exceeds 7 minutes:
Status = INCOMPLETE
STOP.
Do not migrate routes.
Do not loop.

CANONICAL RESULT — RETURN ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-ADDRESSBOOK-ACTOR-SEAM-001
Parent-Task: TB-TMAR-ADDRESSBOOK-CQRS-WRITE-SLICE-002-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-State:
Actor-Resolver-State:
Actor-Resolver-Path:
Actor-Precedence-State:
Dev-Header-State:
Authenticated-User-Seam-State:
Guest-Actor-Authority-State:
Endpoints-Project-References:
Endpoint-Boundary-State:
DI-Registration-State:
Host-Route-Ownership-State:
New-Endpoint-Route-Count:
Focused-Builds:
Focused-Tests:
Canonical-Task-State:
Evidence:
Production-Code-Scope:
Test-Code-Scope:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP
Do not auto-start route migration.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK
