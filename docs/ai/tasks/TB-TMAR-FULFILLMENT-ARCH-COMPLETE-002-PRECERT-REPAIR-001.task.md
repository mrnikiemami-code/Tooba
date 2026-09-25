PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: FULFILLMENT_ARCH_COMPLETE_002_PRECERT_REPAIR
Title: Add Fulfillment transport validators and remove dead Host alias residue
Backend-Only: YES

Architect verdict

TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001-R1 is ARCHITECT-ACCEPTED at:
ac6156bcf796147910e5b5ea8e85c5bc9084027f

Accepted audit state:

Fulfillment = COMPLETE_REFERENCE_PATTERN / HTTP_OWNING / MODULE_ENDPOINTS / MEDIATR_12_5
Fulfillment is NOT ARCH-COMPLETE-002 certified yet
endpoint-reachable requests = exactly 15
VALIDATOR_REQUIRED = 10
validators present = 0
NO_VALIDATOR_REQUIRED = 5
2 NO_INPUT
1 AUTH_SCOPED_QUERY
2 OPTIONAL_PRESENTATION_LOCALE
Fulfillment -> Host = ZERO
Host business residue absent
exactly three thin Host security adapters remain
dead Host alias residue exists:
src/backend/Host/Tooba.Host/FulfillmentReturnsGridAliases.cs
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true
One objective only

Before structure certification:

add exactly the 10 missing Fulfillment transport/input validators
remove the dead Host alias residue file
add only the focused validator-coverage / alias-residue guards needed to lock this repair

Do NOT structure-certify Fulfillment in this task.

A. Validator folders

Create capability folders under:

src/backend/Modules/Fulfillment/Tooba.Fulfillment.Application/Validators/

Expected subfolders:

Seller
Customer
Admin
Shipping

Do not create Common/Helpers/Utils/Managers dumping grounds.

B. Exactly 10 validators

Create exactly:

Seller
SellerMutateFulfillmentCommandValidator
GetSellerFulfillmentQueryValidator
Customer
ListCustomerCheckoutFulfillmentsQueryValidator
Admin
GetAdminFulfillmentQueryValidator
ExecuteAdminFulfillmentBulkCommandValidator
QueryAdminFulfillmentWorkQueueQueryValidator
Shipping
CreateShippingServiceCommandValidator
UpdateShippingServiceCommandValidator
DeactivateShippingServiceCommandValidator
GetShippingServiceQueryValidator

Do NOT create validators for the five NO_VALIDATOR_REQUIRED requests.

C. Transport/input validation rules

Validation must remain primitive transport/input shape only.

SellerMutateFulfillmentCommandValidator

Validate:

FulfillmentId != Guid.Empty
Permission is not null
if ShipmentId is supplied, it must not be Guid.Empty
if CarrierDisplayName is supplied, it must not be whitespace-only
if TrackingReference is supplied, it must not be whitespace-only
if ShippingMethodCode is supplied, it must not be whitespace-only
if ShipmentLines is supplied:
collection must not contain null items
each OrderLineId != Guid.Empty
each Quantity > 0

Do NOT validate:

ActorUserId
SellerPartyId
seller ownership
fulfillment existence/state
whether a given Kind requires/forbids a specific optional field
mutation eligibility
shipping-method existence
tracking/provider business semantics

Reason:
ActorUserId/SellerPartyId/Permission originate from the trusted seller authorization boundary, except Permission must still be non-null as a command-shape invariant. Mutation-kind-specific requirements remain Application/Domain business semantics.

Do NOT invent max lengths unless a canonical existing constant for the exact same field already exists.

GetSellerFulfillmentQueryValidator

Validate only:

FulfillmentId != Guid.Empty

Do NOT validate SellerPartyId; it is authorizer-derived.

ListCustomerCheckoutFulfillmentsQueryValidator

Validate only:

CheckoutId != Guid.Empty

Do NOT validate ownership/existence/access.

GetAdminFulfillmentQueryValidator

Validate only:

FulfillmentId != Guid.Empty
ExecuteAdminFulfillmentBulkCommandValidator

Validate only:

Request is not null

Do NOT validate ActorUserId.
Do NOT duplicate:

action-code allowlist
empty bulk semantics
cross-seller rules
row identity matching
action compatibility
shipment resolution
These remain existing Application/business behavior.
QueryAdminFulfillmentWorkQueueQueryValidator

Validate only:

Request is not null

Do NOT duplicate AdminFulfillmentGridQueryPolicy:

paging normalization
field allowlist
operator allowlist
sort normalization
filter semantics
advanced-filter connectors
search semantics
CreateShippingServiceCommandValidator

Validate only:

Model is not null

Do NOT duplicate ShippingServiceSemantic / directory/domain rules.

UpdateShippingServiceCommandValidator

Validate only:

ServiceId != Guid.Empty
Model is not null
DeactivateShippingServiceCommandValidator

Validate only:

ServiceId != Guid.Empty
GetShippingServiceQueryValidator

Validate only:

ServiceId != Guid.Empty
D. Explicit NO_VALIDATOR_REQUIRED requests

Guard exactly these five as validator-free:

NO_VALIDATOR_REQUIRED_NO_INPUT
ListAdminFulfillmentsQuery
EnsureShippingCatalogSeedCommand
NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY
ListSellerFulfillmentsQuery
SellerPartyId produced by IFulfillmentSellerAuthorizer
NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE
ListShippingServicesQuery
ListEnabledShippingMethodsTreeQuery

Do not create ceremonial validators.

E. Discovery / MediatR

Use only existing:
AddToobaCqrsFoundation / AddValidatorsFromAssembly

MediatR remains 12.5.0.

Do NOT:

manually invoke validators in endpoints
manually invoke validators in handlers
add a second validation pipeline
change MediatR version

If foundation discovery cannot resolve all ten:
return RECOVERY_CONFLICT and STOP.

F. Remove dead Host alias residue

Delete exactly:

src/backend/Host/Tooba.Host/FulfillmentReturnsGridAliases.cs

Parent audit established:

7 global aliases
zero production consumers
dead residue
REMOVE_DEAD_RESIDUE

Before deleting, verify again that no production consumer exists.

After deleting:

remove only the focused Host test/allowlist expectation that explicitly permits/requires this file
do not change unrelated Host aliases
do not rename/move any Fulfillment type
do not create compatibility aliases

This deletion must not change runtime behavior.

G. Exhaustive validator coverage guard

Add one focused Fulfillment coverage guard proving:

endpoint-reachable request inventory = exactly 15
VALIDATOR_REQUIRED = exactly 10
required validators = exactly the ten listed above
all ten concrete validators resolve through foundation DI
NO_VALIDATOR_REQUIRED = exactly 5
no validator registered for those five
all 15 requests are real MediatR requests
endpoints dispatch through ISender
no direct IValidator/ValidateAsync invocation in endpoints/handlers
MediatR = 12.5.0
dead Host alias file absent
Fulfillment remains NOT structure-certified
Fulfillment remains in uncertifiedHttpOwningModules

No representative sampling.

H. Focused validator tests

Direct in-memory only.

Prove at minimum:

all route Guid rules reject Guid.Empty and accept non-empty
ListCustomerCheckoutFulfillmentsQuery rejects Guid.Empty only
SellerMutate:
rejects empty FulfillmentId
rejects null Permission
rejects supplied empty ShipmentId
rejects supplied whitespace optional strings
rejects supplied shipment lines with empty OrderLineId or Quantity <= 0
does NOT reject empty ActorUserId/SellerPartyId solely as validator rules
ExecuteAdminFulfillmentBulk only rejects null Request; business-hostile but non-null request reaches Application behavior
QueryAdminFulfillmentWorkQueue only rejects null Request; policy-hostile but non-null grid envelope passes FluentValidation
Create/Update shipping validators do not duplicate shipping semantic/business policy

No web host.
No database.

I. Protected state

Preserve:

ARCH-COMPLETE-002
HOST-MODULE-ENDPOINT-001
ARCH-CQRS-001/002
certified modules Order/Cart/StoreContext/Offer/Payment/Settlement
Fulfillment -> Host = ZERO
exactly three thin Host security adapters
no schema/migration change
existing routes/behavior
Checkout = PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true

Do NOT:

structure-certify Fulfillment
edit tmar-module-structure-manifests.json to certify Fulfillment
add Fulfillment to certifiedModules
remove Fulfillment from uncertifiedHttpOwningModules
strengthen full exact namespace/root-allowlist guard yet
touch certified modules
resume Checkout
touch frontend
J. FAST-VALIDATION-BUDGET

Run ONLY:

direct in-memory Fulfillment validator tests
Fulfillment validator coverage guard
one focused Host guard covering dead alias removal, only if an existing guard must change
build Fulfillment test project:
dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj --no-restore

Do NOT run:

full Fulfillment tests
full Host tests
broad TMAR suite
structure gate
solution tests
solution build
Testcontainers
DB integration
retries

If any focused test hangs:
return INCOMPLETE and STOP.

K. Recovery

On PASS, update minimum SoT:

parent audit + R1 = ARCHITECT_ACCEPTED
validator coverage =
COMPLETE_10_OF_10_REQUIRED_PRESENT_5_NO_VALIDATOR_REQUIRED
Host dead alias residue = REMOVED
Fulfillment structure certification remains PENDING

Set:
nextTask = TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001
nextTaskGate = NEXT_TMAR_WAVE_AFTER_FULFILLMENT_PRECERT_REPAIR

Update only:

docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
focused durable guard expectations if needed

Do NOT certify Fulfillment.

L. Evidence

Create:
docs/evidence/TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001/fulfillment-precert-repair.md

Record:

exact 15 / 10 / 5 inventory
exact ten validators
exact five no-validator-required requests/reasons
exact validator rules
central discovery proof
dead Host alias deletion proof
focused tests/build
explicit structure certification PENDING
PASS criteria

PASS only if:

exactly ten validators are added
exactly five no-validator-required requests remain validator-free
validation is transport/input shape only
trusted authorizer values are not policed as untrusted input
no grid policy duplication
no bulk business-rule duplication
no shipping business-rule duplication
foundation discovery resolves all ten
endpoint inventory remains exactly 15
MediatR remains 12.5.0
endpoints remain ISender-only
no direct validator invocation exists
FulfillmentReturnsGridAliases.cs is removed after zero-consumer proof
no compatibility alias replaces it
Fulfillment -> Host remains ZERO
three thin Host security adapters remain
Fulfillment remains NOT structure-certified
Checkout/frontend unchanged
focused tests pass
Fulfillment test project builds
Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Audit-R1-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Added:
No-Validator-Required-Count:
No-Validator-Required-State:
Validation-Scope-State:
Seller-Validation-Boundary-State:
Customer-Checkout-Validation-State:
Admin-Bulk-Validation-Boundary-State:
Grid-Validation-Boundary-State:
Shipping-Validation-Boundary-State:
FluentValidation-Discovery-State:
MediatR-State:
ISender-State:
Direct-Validator-Invocation-State:
Host-Dead-Alias-Residue-State:
Host-Fulfillment-Residue-State:
Fulfillment-To-Host-Dependency-State:
Fulfillment-Structure-Certification-State:
Focused-Validator-Tests:
Focused-Coverage-Guard:
Focused-Host-Alias-Guard:
Project-Build:
Recovery-State:
Residual-Defects:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not start Fulfillment structure certification automatically.
Do not run broader tests.
Do not start another module.
Do not resume Checkout.
Do not touch frontend.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK