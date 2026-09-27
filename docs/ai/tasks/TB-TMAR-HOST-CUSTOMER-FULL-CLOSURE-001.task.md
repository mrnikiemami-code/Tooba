PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK
Task-ID: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001
Parent-Task: TB-TMAR-HOST-CUSTOMER-EVACUATION-001
Parent-Accepted-Commit: 877e43ba802ab951bab52a029d9854c1788f757d
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: HOST_FOLDER_FULL_CLOSURE
Target-Host-Folder: src/backend/Host/Tooba.Host/Customer
Title: Full professional closure of Host/Customer without creating a Customer god-module
Architecture-Decision:
- DO NOT create Tooba.Customer / Modules/Customer.
- Customer in Host is an audience/presentation surface, not a bounded context.
- Business/use-case ownership must remain with natural modules.
- /v1/customer/profile belongs to CustomerProfile presentation/application ownership.
- Wishlist behavior/routes belong to Wishlist.
- Order summary remains Order-owned.
- Address data/count remains AddressBook-owned.
- Identity contact remains Identity-owned.
- /v1/customer/dashboard may be owned by CustomerProfile.Endpoints ONLY as a thin customer-account presentation/BFF composition surface; this does NOT transfer Order/Wishlist/AddressBook/Identity business ownership to CustomerProfile.
- Any cross-module dashboard composition must be Contracts-only, read-only, policy-free, persistence-free, and must not introduce a shared/god contract or new generic orchestration module.
- Host/Customer final target = ZERO production files.
- Do not relocate residue to another Host folder merely to make Host/Customer empty.
Current-State:
- Prior task moved five module-specific customer authorizers to Order/Notification/Returns/Support/Wallet Endpoints.
- Remaining Host files:
  1. Customer/CustomerPanelComposer.cs
  2. Customer/CustomerPanelEndpoints.cs
  3. Customer/CustomerPanelModels.cs
- Known current illegal/temporary dependencies include:
  - Tooba.AddressBook.Application.Ports
  - Tooba.Wishlist.Application
  - Tooba.Order.Application.Customer.Models
  - Tooba.Order.Application.Customer.Queries.GetCustomerOrderDashboardSummary
- Current routes to preserve exactly:
  - GET /v1/customer/dev-context
  - GET /v1/customer/dashboard
  - GET /v1/customer/profile
  - PUT /v1/customer/profile
- Current unauthorized semantic:
  - machine code customer.session.required
  - canonical composed catalog owner = Foundation
- Guest actor authority:
  - Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId
- Frontend remains frozen.
- Checkout remains paused at safe W5 checkpoint.
Required-Skills:
1. .cursor/skills/tooba-architecture-analyze/SKILL.md
2. .cursor/skills/tooba-architecture-migrate/SKILL.md
3. .cursor/skills/tooba-architecture-certify/SKILL.md
Mandatory-Repository-SoT:
Read before planning/execution:
- AGENTS.md
- docs/architecture/TMAR-HOST-EVACUATION-PROTOCOL.md
- docs/architecture/TMAR-COMPLETE-REFERENCE-STRUCTURE-STANDARD.md
- docs/architecture/TMAR-architecture-locks.md
- docs/architecture/TOOBA-REFERENCE-MODULE-PATTERN.md
- docs/architecture/tmar-current-state.json
- docs/architecture/tmar-module-structure-manifests.json
- Evidence/task from TB-TMAR-HOST-CUSTOMER-EVACUATION-001
  Repository reality wins over assumptions in this task.
Execution-Model:
This is one coherent Analyze → Migrate → Certify task.
First produce a bounded internal plan from actual repository state, then execute it.
Do not ask for approval between phases unless a real architecture/product/behavior blocker is discovered.
If a mandatory decision cannot be proven from repository locks/current behavior, return RECOVERY_CONFLICT and STOP instead of inventing architecture.
Hard-Timebox:
- Preferred: 10–12 minutes
- Hard max: 15 minutes
- If the full safe closure cannot complete within the hard max, return INCOMPLETE with exact completed state, remaining files, exact next bounded continuation, commit/push state, and STOP.
- Do not silently broaden, loop, or auto-start another Host folder.
PRIMARY OBJECTIVE
Close Host/Customer completely and professionally:
- Host/Customer production file count = ZERO.
- No module-specific customer panel business endpoint remains Host-owned.
- No Host→foreign Application/Infrastructure/Domain dependency remains from this surface.
- Preserve all existing route paths/methods and shipped success DTO JSON shapes.
- Preserve auth/dev/testing semantics exactly.
- No new Modules/Customer.
- No new shared/god architecture.
- CustomerProfile and Wishlist receive only the minimum foundation needed for this closure.
- Dashboard composition becomes Contracts-only and presentation-only.
- Re-run focused validation and certify the touched surface.
PHASE A — ANALYZE / PLAN
A1. Re-read all 3 remaining Host/Customer files completely.
A2. Inventory every type/member/route/DTO/dependency/DI registration/call site/test.
A3. Re-read directly affected CustomerProfile, Wishlist, Order, AddressBook, Identity projects and existing endpoint/composition conventions.
A4. Build an exact Content Disposition Map for:
- CustomerPanelEndpoints
- CustomerPanelComposer
- CustomerPanelModels
- CustomerProfileWriteRequest
- profile read/write behavior
- dashboard read composition
- dev-context behavior
- actor/session/dev-header resolution
- display-name fallback behavior
- Order dashboard summary use
- Wishlist count use
- AddressBook count use
- Identity contact use
A5. Determine the minimum lawful destination foundations:
- CustomerProfile:
  - create/reuse Tooba.CustomerProfile.Endpoints if absent;
  - real MediatR 12.5 Commands/Queries in CustomerProfile.Application for HTTP-owned profile read/write;
  - Endpoints dispatch via ISender;
  - no endpoint direct Directory/DbContext call;
  - transport validator classification exhaustive.
- Wishlist:
  - create/reuse narrow Contracts ports/DTOs needed by customer dashboard if absent;
  - create/reuse Wishlist Endpoints/CQRS only to the minimum required to remove Host-owned Wishlist HTTP/Application coupling;
  - do not redesign Wishlist domain.
- AddressBook:
  - use existing Contracts boundary if present;
  - if the needed read/count capability exists only in Application today, create the narrowest lawful AddressBook.Contracts read port/DTO and adapt behind it; do NOT re-open general AddressBook recovery.
- Order:
  - expose/reuse Contracts-safe dashboard summary surface if Host/Customer currently depends on Order.Application models/query types;
  - do NOT move Order business logic or duplicate Order read models.
- Identity:
  - use existing Contracts only; do not create another identity abstraction.
A6. Dashboard ownership decision:
Default required destination is CustomerProfile.Endpoints as a CUSTOMER-ACCOUNT PRESENTATION COMPOSITION surface, not CustomerProfile business ownership.
It may aggregate only contracts-safe read snapshots/counts from CustomerProfile, Order, Wishlist, AddressBook, Identity.
It must contain ZERO business policy, ZERO persistence, ZERO cross-module Application/Infrastructure/Domain dependency.
If repository architecture proves an already-existing better neutral presentation owner, use that existing owner instead and document the proof.
Do NOT create a new Customer, CustomerPortal, CustomerBff, or generic orchestration module in this task.
A7. If analysis finds that moving /dashboard to CustomerProfile.Endpoints would force business ownership transfer or a new god-contract, return RECOVERY_CONFLICT with exact evidence and STOP; do not invent a new module.
Required Analyze-State fields in evidence:
- Foundation-State per CustomerProfile/Wishlist/AddressBook/Order/Identity
- Ownership-State
- Dashboard-Presentation-Owner
- Contracts-Boundary-State
- Cross-Module-Coupling-State
- Endpoint-Ownership-State
- Validation-Classification-State
- Localization-State
- API-Result-Pattern-State
- Schema-Migration-State
- Final-Disposition
PHASE B — MIGRATE
B1. CustomerProfile foundation / profile routes
- Create/reuse Tooba.CustomerProfile.Endpoints.
- Add it to solution grouping consistently with repo convention.
- Add module endpoint composition entry.
- Move GET /v1/customer/profile and PUT /v1/customer/profile from Host to CustomerProfile.Endpoints.
- Preserve exact route/method.
- Application HTTP use-cases must be real MediatR 12.5 requests/handlers.
- Endpoint must use ISender.
- Use ApiResponseFactory for expected failures.
- Preserve shipped success DTO field names/shape.
- Preserve actor ownership semantics.
- Use FluentValidation only for transport/input shape.
- Do not move email/mobile credential authority into CustomerProfile.
B2. Dashboard presentation composition
- Move GET /v1/customer/dashboard out of Host.
- Default destination: CustomerProfile.Endpoints under a clearly presentation-oriented capability folder such as CustomerDashboard/ or repository-consistent equivalent.
- Do NOT place foreign business logic there.
- Build the response from narrow Contracts-safe read boundaries only.
- Preserve current response JSON shape:
  - ActorUserId
  - DisplayName
  - TotalOrders
  - PendingOrders
  - PaidOrders
  - WishlistAvailable
  - WishlistCount
  - AddressBookAvailable
  - AddressBookCount
  - RecentOrders
- Order recent-order DTOs must cross via Order.Contracts or another already-approved stable contract boundary, not Order.Application.
- Wishlist count must cross via Wishlist.Contracts.
- AddressBook count must cross via AddressBook.Contracts.
- Customer profile snapshot via CustomerProfile-owned CQRS/contracts.
- Identity contact via Identity.Contracts.
- No direct foreign directories from Endpoints.
B3. Wishlist boundary cleanup
- Remove Host/Customer dependence on Tooba.Wishlist.Application.
- Create/reuse the minimum Wishlist.Contracts read port/DTO needed by dashboard.
- If Host currently owns Wishlist HTTP outside Host/Customer, do NOT broaden into unrelated Wishlist route migration unless it is strictly necessary for the dashboard boundary.
- Do not claim full Wishlist module certification unless the full module certification surface is actually completed.
B4. AddressBook boundary cleanup
- Remove Host/Customer dependence on Tooba.AddressBook.Application.Ports.
- Prefer/reuse AddressBook.Contracts.
- If a count/read port must be added, keep it narrow and module-owned; implementation remains AddressBook-owned.
- Do not re-open AddressBook endpoints/structure beyond this required Contracts seam.
B5. Order boundary cleanup
- Remove Customer surface dependence on:
  - Tooba.Order.Application.Customer.Models
  - Tooba.Order.Application.Customer.Queries.*
- Reuse or add the narrowest Order.Contracts read model/port/query-facing contract necessary.
- Order remains the owner of order lifecycle and dashboard summary calculation.
- Do not duplicate calculation in CustomerProfile.
B6. Actor/session/dev-context
- Preserve exact existing semantics:
  - authenticated session first;
  - Development/Testing dev actor header behavior exactly as currently shipped;
  - fallback to StorefrontGuestActor.ActorId only where current route semantics allow it.
- GET /v1/customer/dev-context route must remain available with same environment gating and response shape.
- It may move to the same customer-account presentation endpoint module; do not leave it in Host/Customer just for convenience.
- Do not create a second guest actor constant.
B7. Error ownership / localization
- Continue consuming customer.session.required; do not re-register its descriptor outside its canonical owner.
- Preserve composed-catalog uniqueness.
- No duplicate descriptor registration.
- Remove hard-coded Persian display fallback text from touched production surface using existing canonical localization/presentation resources.
- Do not create a second localization mechanism.
- Preserve machine codes and HTTP semantics.
- If localized display fallback is not an error code concern, use the existing presentation localization infrastructure or an owning module resource set; do not put user-facing Persian literal in Application/Domain/Endpoints.
B8. Host cleanup
After all routes/models/composition have real destinations:
- delete:
  - Host/Customer/CustomerPanelComposer.cs
  - Host/Customer/CustomerPanelEndpoints.cs
  - Host/Customer/CustomerPanelModels.cs
- remove Host registrations/usings/call sites specific to them.
- re-scan src/backend/Host/Tooba.Host/Customer.
  Success requires ZERO production .cs files in this folder.
  Do not move them to another Host folder.
B9. Structure / namespace / solution
For every created/touched module project:
- capability-oriented physical folders;
- exact path↔namespace;
- no root dump;
- no stale/duplicate physical copy;
- preserve/add Solution Folder grouping where repo convention requires it;
- no compatibility aliases/type forwarding to hide structure debt.
B10. No schema behavior change
- No new DB migration unless strictly required by an already-existing persisted feature contract. This task should normally require NONE.
- Do not regenerate migrations.
- Do not change table/column/index semantics.
- Routes and success payloads unchanged.
PHASE C — CERTIFY TOUCHED SURFACE
C1. Re-read every touched production file before PASS.
C2. Prove:
- Host/Customer production file count = 0.
- Customer profile routes are module-owned.
- dashboard route is module-owned presentation composition.
- dev-context route no longer requires Host/Customer.
- Host→CustomerProfile/Wishlist/AddressBook/Order Application dependency from this surface = ZERO.
- Cross-module dependencies = Contracts-only.
- No foreign DbContext/DbSet/SQL join.
- No new shared/god module.
- No Modules/Customer.
- MediatR 12.5 + ISender for HTTP use-cases.
- exhaustive validator classification for newly HTTP-reachable requests.
- ApiResponseFactory canonical failure mapping.
- composed error catalog duplicate descriptors = ZERO.
- hard-coded touched user-facing Persian/English display strings = ZERO unless an explicit canonical exception already exists and is documented.
- path↔namespace exact.
- no stale copies.
- no new oversized/god file.
- no schema/migration change.
- no frontend change.
C3. Do not over-certify
- CustomerProfile may be promoted to COMPLETE_REFERENCE_PATTERN only if all ARCH-COMPLETE-002 requirements are actually satisfied.
- Wishlist may be promoted only if its full required surface is actually completed.
- Otherwise record them as minimum foundation/touched-surface repair only.
- Host/Customer folder closure can PASS independently from full certification of unrelated untouched module areas if all moved responsibilities are correctly owned and no illegal Host residue remains.
MANDATORY DURABLE GUARDS
Add/strengthen focused guards that fail if:
1. any production file reappears under Host/Customer;
2. /v1/customer/profile is mapped by Host;
3. /v1/customer/dashboard is mapped by Host;
4. touched customer-account presentation references any foreign .Application, .Infrastructure, or .Domain;
5. a Modules/Customer project/folder is introduced;
6. customer dashboard composition calls foreign DbContext/DbSet;
7. duplicate error descriptor registration reappears for customer.session.required;
8. path↔namespace or stale-copy state regresses for newly created CustomerProfile/Wishlist surfaces.
FOCUSED VALIDATION ONLY
Build only changed/relevant projects:
- CustomerProfile Contracts/Application/Infrastructure/Endpoints as applicable
- Wishlist Contracts/Application/Endpoints as applicable
- AddressBook Contracts + directly affected implementation project if seam added
- Order Contracts + directly affected Order project if seam added
- Host
- test project(s) containing the new guards
Run only focused tests for:
- customer profile route behavior
- dashboard response parity
- dev-context behavior
- session/guest/dev actor semantics
- validator coverage for new HTTP requests
- Contracts-only boundary
- Host/Customer ZERO
- error catalog uniqueness
- path/namespace/physical structure
- directly affected module architecture guards
TESTS ARE EVIDENCE, NOT NAVIGATION.
No full solution test suite.
No open-ended fix/test loop.
For one deterministic local failure, one bounded repair + one affected rerun only.
If failure persists or requires broader speculative work: STOP and return INCOMPLETE/RECOVERY_CONFLICT.
PROTECTED STATE
Must not alter:
- frontend
- Checkout W5 paused state
- unrelated Host folders
- Golden/reference modules except minimum Contracts seam required by this task
- machine error codes
- published route paths/methods
- shipped success JSON shape
- DB schema/migrations unless explicitly proven unavoidable
- shared ErrorDefinitionCatalog fail-fast behavior
- current canonical error descriptor ownership
- Offer/reference module implementation
- unrelated architecture baselines/guards merely to make tests pass
NO-GO / ANTI-PATTERNS
Do NOT:
- create Tooba.Customer;
- create CustomerPortal, CustomerBff, or another new generic orchestration module;
- make CustomerProfile own Order/Wishlist/AddressBook business rules;
- expose foreign DbContext;
- add Application→Application or Infrastructure→Infrastructure references;
- preserve cross-module SQL/EF joins;
- relocate Host residue to Host/Composition or another Host folder;
- duplicate StorefrontGuestActor;
- use Results.Problem / local ProblemDetails mapper where ApiResponseFactory applies;
- parse ex.Message;
- hard-code new Persian/English API messages;
- duplicate error descriptors;
- weaken guards/baselines;
- change response envelopes;
- broaden into full independent recovery of unrelated modules.
SUCCESS CRITERIA
PASS only if all are true:
- Analyze plan completed from actual repo.
- Host/Customer = ZERO production files.
- No Modules/Customer created.
- GET /v1/customer/profile and PUT /v1/customer/profile module-owned.
- GET /v1/customer/dashboard module-owned as contracts-only presentation composition.
- GET /v1/customer/dev-context preserved with same semantics outside Host/Customer.
- Host customer surface no longer references foreign Application/Infrastructure/Domain.
- Order/Wishlist/AddressBook/Identity remain owners of their own semantics.
- CustomerProfile does not become a god-module.
- CQRS/ISender canonical where HTTP use-cases moved.
- validator coverage classified/guarded.
- canonical ApiResponseFactory/errors/localization used.
- duplicate error descriptors = ZERO.
- hard-coded touched user-facing display fallbacks removed/canonicalized.
- route/method/success shape behavior preserved.
- schema/migrations unchanged.
- focused validations PASS.
- SoT/evidence updated honestly.
- next Host folder NOT started.
- commit pushed; HEAD == origin/main.
- working tree clean except explicitly reported pre-existing untracked build artifacts.
EVIDENCE REQUIREMENTS
Create:
docs/evidence/TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001/
Must include at least:
- analyze.md
- content-disposition.md
- boundary-map.md
- validation.md
- closure.md
Evidence must show:
1. before/after Host/Customer tree
2. route ownership before/after
3. exact destination of every old type/member
4. CustomerProfile/Wishlist foundation state
5. cross-module dependency inventory
6. proof of no foreign Application/Infrastructure/Domain
7. dashboard Contracts-only dependency diagram
8. request→handler→validator matrix
9. localization/error ownership proof
10. path↔namespace/physical tree proof
11. schema/migration proof
12. focused build/test results
13. residual non-blocking debt
14. exact PASS/INCOMPLETE/RECOVERY_CONFLICT verdict
RECOVERY SOT
Update:
docs/architecture/tmar-current-state.json
Add/update a bounded hostCustomerFullClosure state containing:
- task
- parent task/commit
- state
- hostCustomerProductionFileCount
- profileRouteOwner
- dashboardRouteOwner
- devContextRouteOwner
- crossModuleBoundary
- customerModuleCreated = false
- applicationLeakage = ZERO
- schemaMigration = NONE
- validatorCoverage
- apiResultPattern
- localizationState
- errorCatalogDuplicateCount
- focusedValidations
- residualDebt
- nextHostFolderStarted = false
- workflowStop = USER_REVIEW_HOST_CUSTOMER_FULL_CLOSURE
Do not rewrite unrelated history.
CANONICAL WORKER RESULT
Return ONLY:
PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-HOST-CUSTOMER-FULL-CLOSURE-001
Parent-Task: TB-TMAR-HOST-CUSTOMER-EVACUATION-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Analyze-State:
Host-Customer-Production-File-Count-Before:
Host-Customer-Production-File-Count-After:
Customer-Module-Created:
Profile-Route-Owner:
Dashboard-Route-Owner:
DevContext-Route-Owner:
CustomerProfile-Foundation-State:
Wishlist-Foundation-State:
Order-Boundary-State:
AddressBook-Boundary-State:
Identity-Boundary-State:
Cross-Module-Boundary-State:
Foreign-Application-Dependency-Count:
Foreign-Infrastructure-Dependency-Count:
Foreign-Domain-Dependency-Count:
CQRS-State:
Validator-Coverage-State:
API-Result-Pattern-State:
Localization-State:
Error-Catalog-Duplicate-Count:
Schema-Migration-State:
Behavior-Parity-State:
Route-Parity-State:
Success-Payload-Parity-State:
Focused-Validation:
Known-PreExisting-Failures:
Remaining-Blockers:
Residual-Debt:
SoT-State:
Evidence-Path:
Commit-SHA:
Push-State:
HEAD-Equals-Origin-Main:
Working-Tree:
User-Work-Preserved:
Next-Host-Folder-Started: false
Recovery-Next-Task:
Next-Recommended-Task:
STOP
END_TOOBA_WORKER_RESULT
STOP RULE:
After returning the worker result, stop completely.
Do not start the next Host folder.
Do not poll.
Do not issue a self-generated follow-up task.
END_TOOBA_TASK
