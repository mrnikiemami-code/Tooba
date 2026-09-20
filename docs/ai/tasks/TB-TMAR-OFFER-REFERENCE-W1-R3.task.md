PIPELINE-PROTOCOL: BRIDGE-WAKE-V1

BEGIN_TOOBA_REPAIR_TASK

Task-ID:
TB-TMAR-OFFER-REFERENCE-W1-R3

Parent-Task:
TB-TMAR-OFFER-REFERENCE-W1-R2

Channel:
tooba-main

WorkerId:
tooba-worker-01

AgentType:
cursor

Status:
REPAIR

Program:
TMAR — Tooba Microservice-Ready Architecture Recovery

Track:
OFFER_REFERENCE_MODULE

Title:
Offer Golden Architecture Reset — CQRS/MediatR, Namespace Ownership, Contract Purity, and Use-Case Placement

Backend-Only:
YES

TMAR-Execution-Mode:
BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE

Architect Decision

The previous Offer COMPLETE state is REVOKED.

The Architect directly inspected current main on GitHub at:
12abe981d28fa616a253380364924687d55fa53d

The repository proves the module is physically grouped but is NOT yet a valid Golden Reference Module.

Do not switch modules.
Do not resume Checkout.
Do not touch frontend.

Architect-verified defects

Tooba.Offer.Application currently contains only Ports/IOfferDirectory.cs, Ports/IOfferSellerPanel.cs, and the csproj. There are no Commands, Queries, MediatR Handlers, Validators, or UseCases.

Tooba.Offer.Application.csproj does not reference BuildingBlocks/MediatR.

Tooba.Offer.Contracts/Dtos/SalesChannel.cs and OfferStatus.cs physically live in Contracts but declare namespace Tooba.Offer.Domain;.

Tooba.Offer.Domain.csproj references Offer.Contracts.

Tooba.Offer.Domain/Aggregates/TypeForwarders.cs forwards SalesChannel and OfferStatus.

Infrastructure/Adapters/OfferDirectory.cs owns application use-case orchestration and references Catalog.Application + Party.Application.

OfferSellerEndpoints.cs injects IOfferSellerPanel, not ISender.

Host/Tooba.Host/Seller/SellerPanelComposer.cs is ~939 LOC and owns Offer mutation, direct DbContext queries, cross-module orchestration, and localized errors.

Current Offer architecture guards pass despite these violations, so guard coverage is insufficient.

Target flow — locked

HTTP Endpoint
→ ISender
→ Command / Query
→ Application Handler
→ Domain + Application Ports
→ Infrastructure implementations
→ Offer-owned persistence

Cross-module reads:
Application Handler
→ foreign module Contract/Gate
→ owning adapter

No foreign module DbContext from Offer.
No foreign Application project reference from Offer.

Git safety

Repository:
D:\Users\User\source\repos\SarvNewVer

Verify:

branch main

HEAD == origin/main

clean task-owned worktree

staged = 0

18ca10c9 remains ancestor

stashes untouched

user work preserved

Forbidden:
git reset
git clean
destructive checkout/restore
unsafe rebase
stash pop/drop
broad git add .

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/recovery-start.md

1. Fix type ownership / namespace leakage

OfferStatus and SalesChannel must have one explicit ownership model.

Required:

Domain owns Domain state/value concepts needed by SellerOffer.

Contracts owns public cross-module contract representations.

every Contracts source namespace starts with Tooba.Offer.Contracts...

Domain must not reference Contracts merely to obtain its own types.

use explicit mapping if Domain and Contract representations differ.

Do not preserve misleading same-name cross-assembly types without explicit mapping.

Expected final Domain references:

BuildingBlocks

no Offer.Contracts

no Application/Infrastructure/Endpoints

no foreign implementation assembly

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/type-ownership.md

2. Remove TypeForwarders unless a real binary consumer is proven

Audit source consumers of the forwarded Offer types.

Default expected outcome for this source-built solution:
TypeForwarders.cs removed.

If a real external binary consumer exists, STOP removal and report exact evidence.
Do not keep forwarding "just in case".

3. Add real CQRS/MediatR to Offer.Application

Use the existing BuildingBlocks/MediatR foundation. Do not install another MediatR version.

Target Application folders:

Commands/CreateOffer/

Commands/UpdateOffer/

Commands/ActivateOffer/

Commands/SuspendOffer/

Commands/ArchiveOffer/

Commands/SetReturnPolicy/

Commands/SetOrderQuantityLimits/

Queries/GetOffer/

Queries/ListSellerOffers/

Ports/

Only create folders backed by real use cases.

Create Commands/Queries and IRequestHandler implementations for the real existing behaviors.

Handlers must:

live in Application

own orchestration

depend on Application ports

use IClock

use IIdGenerator

call Domain behavior

produce semantic errors

never use DbContext directly

never depend on HTTP/Host types

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/cqrs-map.md

4. Retire IOfferDirectory as a use-case façade

IOfferDirectory currently hides use cases whose implementation is in Infrastructure.

Refactor so MediatR handlers are the use cases.

If a persistence abstraction remains necessary, rename/narrow it to a true repository/store/reader port matching its responsibility.

Infrastructure must not decide:

Catalog Variant validity

Seller Party validity

Aggregate creation flow

lifecycle transition orchestration

use-case authorization

use-case sequencing

Those belong to Application/Domain.

5. Fix foreign-module boundaries

Offer must not permanently reference:

Catalog.Application

Party.Application

foreign DbContexts

foreign implementation details

Use stable Contracts/Gates from owning modules.

If a minimal missing contract is required, add it narrowly to the owning Contracts project.

Expected:

Offer.Application has no foreign Application project reference

Offer.Infrastructure has no Catalog.Application / Party.Application reference

cross-module calls go through Contracts

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/dependency-boundary.md

6. Endpoints must use ISender

Refactor Offer-owned HTTP routes so:

Endpoint
→ auth/authorization transport concern
→ command/query
→ ISender.Send
→ HTTP mapping

Offer endpoints must not inject IOfferSellerPanel for Offer-owned CRUD/lifecycle operations.

Endpoints must not inject:

OfferDbContext

repository

Infrastructure implementation

use-case directory

Keep routes behavior-compatible.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/endpoint-flow.md

7. SellerPanelComposer — remove Offer-native responsibilities now

Do not rewrite every seller concern in this wave.

But after R3, SellerPanelComposer must NOT:

create Offer

mutate Offer lifecycle/status

set Offer return policy

set Offer quantity limits

directly save Offer aggregate

act as handler for Offer-owned commands/queries

Price/Inventory/Tax/Order/Dashboard cross-module composition may remain temporarily only if clearly recorded for R4.

The Offer endpoint path must no longer depend on Host for Offer-native use cases.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/host-extraction.md

8. Language / code standardization

For all production source modified by R3:

identifiers English

namespaces ownership-correct and English

XML docs/comments English

stable machine error codes English

no Persian prose in Domain/Application/Infrastructure

user-facing localized text only at localization/boundary layer

Do not mass-edit unrelated modules.

Scan untouched Offer production files and list remaining Persian source literals/comments for later Offer cleanup.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/language-standardization.md

9. MediatR registration

Verify current global/composition MediatR registration.

Ensure Offer handlers are actually discoverable.
Do not duplicate global registration blindly.
No service locator.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/di-registration.md

10. Strengthen architecture guards

Add guards that fail if future Offer code reintroduces:

Contracts source declaring namespace Tooba.Offer.Domain

Domain → Offer.Contracts reference

TypeForwardedTo in Offer

Application with no CQRS/MediatR path

Offer.Infrastructure → foreign Application reference

Offer endpoint → IOfferSellerPanel for Offer-owned operations

direct OfferDbContext usage outside Offer.Infrastructure persistence

Offer business writes in Host

localized prose in Offer Domain/Application

Offer production file >800 LOC

Do not weaken existing baselines.

11. Tests / validation

Required:

SellerOffer Domain tests

Application handler tests for Create/Update/Lifecycle/Get/List as applicable

missing Catalog Variant

missing Seller Party

deterministic IClock/IIdGenerator

Endpoint tests proving ISender path

architecture guard tests

all Offer projects build

Tooba.Offer.Tests green

focused affected Host tests green

backend/solution build green as appropriate

No skipped/fake assertions.
No suppressions to force green.

Evidence:
docs/evidence/TB-TMAR-OFFER-REFERENCE-W1-R3/validation.md

12. Anti-pattern gate

Scan final diff and Offer for:

polling

magic sleeps/timeouts

catch-ignore

first-item/first-seller shortcuts

direct system clock

direct random ID

service locator

DbContext in Application/Endpoints

foreign Application coupling

hardcoded localized business text below boundary

test-only production branches

baseline widening

Expected:
AntiPattern-Gate: CLEAN

13. Recovery truthfulness

Update narrowly:

Master Recovery

Architect Bootstrap

Capability Map only if ownership/capability facts changed

R3 recovery-sot

Do NOT mark Offer COMPLETE after this wave.

Expected:
Module-Recovery-State: IN_PROGRESS_REFERENCE_REPAIR

Expected next:
TB-TMAR-OFFER-REFERENCE-W1-R4

R4 will finish Seller BFF decomposition, cross-module Price/Inventory/Tax/Order boundaries, residual language/namespace/design cleanup, and then continue toward final Golden completion.

14. Acceptance criteria

PASS only if all:

Contracts namespace leak fixed

Domain no longer references Offer.Contracts

TypeForwarders removed unless exact binary blocker proven

real MediatR Commands/Queries/Handlers exist for Offer-owned use cases

Application owns use-case orchestration

Infrastructure no longer owns Offer use-case orchestration

Offer.Infrastructure no longer references Catalog.Application/Party.Application

endpoints use ISender for Offer-owned operations

Offer-native mutation removed from SellerPanelComposer

IClock/IIdGenerator preserved

modified production code standardized to English

no localized prose in Domain/Application

strengthened architecture guards green

focused behavior tests green

Offer build green

frontend untouched

user work preserved

recovery docs truthful

Result Contract

Return ONLY canonical BRIDGE-WAKE-V1 Result with:

Summary
Program-Name
Track
Recovery-Start
Architect-Verified-Baseline
TMAR-Execution-Mode
Frontend-Production-Changes
Type-Ownership
TypeForwarders
Domain-References
CQRS-Map
MediatR-Registration
UseCase-Placement
Infrastructure-Role
Foreign-Module-Boundaries
Endpoint-Flow
Host-Extraction
Language-Standardization
Architecture-Guards
Focused-Validation
AntiPattern-Gate
Residual-Offer-Debt
Capability-Map
Bootstrap
Master-Recovery-State
Recovery-SoT
Git
Architectural-Concerns
Blockers
Product-Resume-Safety
Module-Recovery-State
Next-Recommended-Task

Expected:
Module-Recovery-State: IN_PROGRESS_REFERENCE_REPAIR
Next-Recommended-Task: TB-TMAR-OFFER-REFERENCE-W1-R4

After canonical Result:
STOP completely.
Do NOT poll.
Do NOT fetch next task.
Do NOT write Worker IDLE.

END_TOOBA_REPAIR_TASK
