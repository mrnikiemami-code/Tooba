PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: FULFILLMENT_ARCH_COMPLETE_002_AUDIT_CLASSIFICATION_REPAIR
Title: Correct Fulfillment validator classification before implementation
Backend-Only: YES
Audit-Repair-Only: YES

Architect verdict

Parent audit commit:
fd08f1a90bbc5a7784f84b3c5c049af5ea2319ab

Parent audit is NOT YET ARCHITECT-ACCEPTED.

The static inventory / structure / Host / cross-module findings are acceptable, but the validator classification contains one material error and two inaccurate reason labels.

Material classification defect
ListCustomerCheckoutFulfillmentsQuery

Current request:
ListCustomerCheckoutFulfillmentsQuery(Guid CheckoutId)

Endpoint source:
GET /v1/customer/orders/{checkoutId:guid}/fulfillments

The checkoutId comes from the HTTP route. It is therefore untrusted transport input.

The customer authorizer verifies whether the current actor may view that specific checkout. That authorization check does NOT turn the route value into an authorization-derived identity.

Therefore:

ListCustomerCheckoutFulfillmentsQuery = VALIDATOR_REQUIRED

Required transport rule:

CheckoutId != Guid.Empty

Do NOT validate ownership/existence/business access in FluentValidation.

This changes Fulfillment totals from 9/6 to:

endpoint-reachable = 15
VALIDATOR_REQUIRED = 10
validators present = 0
validators missing = 10
NO_VALIDATOR_REQUIRED = 5
Reason-label repairs

These two requests are NOT zero-input:

ListShippingServicesQuery(string? Language)
ListEnabledShippingMethodsTreeQuery(string? Language)

Language is an optional HTTP query parameter.

No validator is required because there is no transport-shape constraint owned by Fulfillment here; semantic locale resolution is delegated to the existing localization path.

Classify both as:

NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE

Do NOT add ceremonial validators.

Correct NO_VALIDATOR_REQUIRED set

Exactly 5:

ListAdminFulfillmentsQuery

NO_VALIDATOR_REQUIRED_NO_INPUT

EnsureShippingCatalogSeedCommand

NO_VALIDATOR_REQUIRED_NO_INPUT

ListSellerFulfillmentsQuery

NO_VALIDATOR_REQUIRED_AUTH_SCOPED_QUERY
SellerPartyId is produced by IFulfillmentSellerAuthorizer

ListShippingServicesQuery

NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE

ListEnabledShippingMethodsTreeQuery

NO_VALIDATOR_REQUIRED_OPTIONAL_PRESENTATION_LOCALE
Correct VALIDATOR_REQUIRED set

Exactly 10:

SellerMutateFulfillmentCommand
CreateShippingServiceCommand
UpdateShippingServiceCommand
DeactivateShippingServiceCommand
GetShippingServiceQuery
GetAdminFulfillmentQuery
GetSellerFulfillmentQuery
ExecuteAdminFulfillmentBulkCommand
QueryAdminFulfillmentWorkQueueQuery
ListCustomerCheckoutFulfillmentsQuery
Host alias finding remains valid

Keep the parent finding:

src/backend/Host/Tooba.Host/FulfillmentReturnsGridAliases.cs

is dead Host alias residue and is classified:
REMOVE_DEAD_RESIDUE

Do NOT remove it in this audit-repair task.

The next pre-cert repair task must include:

exactly 10 Fulfillment transport validators
removal of this dead Host alias residue
focused guard/allowlist repair needed only for that deletion

Then structure certification follows.

One objective only

Correct audit evidence + SoT classification.

Do NOT:

add validators
change Fulfillment production code
remove alias file
change endpoints/handlers
change namespaces/folders
strengthen Fulfillment architecture guards
certify Fulfillment
alter certified modules
resume Checkout
touch frontend
Required updates

Update only the audit/recovery truth:

docs/evidence/TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001/fulfillment-arch-complete-002-audit.md
docs/architecture/tmar-current-state.json
docs/architecture/TOOBA-TMAR-MASTER-RECOVERY.md
docs/architecture/TOOBA-ARCHITECT-BOOTSTRAP.md
src/backend/Host/Tooba.Host.Tests/TmarDurableGuardTests.cs

Correct to:

validatorRequiredCount = 10
validatorsPresentCount = 0
validatorsMissingCount = 10
noValidatorRequiredCount = 5
noInputNoValidatorCount = 2
authScopedNoValidatorCount = 1
optionalPresentationLocaleNoValidatorCount = 2
validatorCoverageState = 0_OF_10_REQUIRED_PRESENT_5_NO_VALIDATOR_REQUIRED

Required validator requests must include:
ListCustomerCheckoutFulfillmentsQuery

Repair scope must state:
ADD_10_TRANSPORT_VALIDATORS_AND_REMOVE_DEAD_HOST_FULFILLMENT_RETURNS_GRID_ALIASES_THEN_STRUCTURE

Keep:

auditDecision = NEEDS_PRECERT_REPAIR_THEN_STRUCTURE
Fulfillment NOT certified
nextTask = TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
FAST-VALIDATION-BUDGET

Run ONLY:

dotnet test src/backend/Host/Tooba.Host.Tests/Tooba.Host.Tests.csproj --no-restore --filter FullyQualifiedName~TmarDurableGuardTests

Do NOT run:

Fulfillment tests
full Host suite
structure gate
full build
solution build
Testcontainers
DB tests
retries

If the focused guard hangs:
return INCOMPLETE and STOP.

PASS criteria

PASS only if:

endpoint inventory remains 15
totals are 10 required / 0 present / 10 missing / 5 no-validator-required
ListCustomerCheckoutFulfillmentsQuery is VALIDATOR_REQUIRED because CheckoutId is route input
its future validator rule is only CheckoutId != Guid.Empty
two language queries are classified OPTIONAL_PRESENTATION_LOCALE, not NO_INPUT
exactly two true NO_INPUT requests remain
exactly one AUTH_SCOPED_QUERY remains
Host dead alias residue remains explicitly recorded for next repair
no production code changed
Fulfillment remains uncertified
certified modules unchanged
Checkout/frontend unchanged
focused durable guard passes
Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001-R1
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-AUDIT-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Audit-Acceptance-State:
Endpoint-Reachable-Request-Count:
Validator-Required-Count:
Validators-Present-Count:
Validators-Missing-Count:
No-Validator-Required-Count:
No-Input-No-Validator-Requests:
Auth-Scoped-No-Validator-Requests:
Optional-Presentation-Locale-No-Validator-Requests:
Required-Validator-Requests:
Customer-Checkout-Classification-State:
Host-Dead-Alias-Residue-State:
Audit-Decision:
Production-Code-Changed:
Fulfillment-Structure-Certification-State:
Certified-Modules-State:
Focused-Durable-Guard:
Recovery-State:
Git:
User-Work-Preserved:
Recovery-Next-Task:
Next-Recommended-Task:
END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.
Do not add Fulfillment validators automatically.
Do not remove Host alias residue automatically.
Do not structure-certify Fulfillment.
Do not start another module.
Do not run broader tests.
Do not resume Checkout.
Do not touch frontend.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK