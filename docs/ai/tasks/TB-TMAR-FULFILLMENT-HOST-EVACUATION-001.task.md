PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-FULFILLMENT-HOST-EVACUATION-001
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Mode: BACKEND_ONLY_UNTIL_EXPLICIT_RELEASE
Track: FULFILLMENT_HOST_EVACUATION
Title: Evacuate all Fulfillment-specific Host logic with semantic preservation
Backend-Only: YES

Architect verdict

TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001 is ARCHITECT-ACCEPTED at:
16062d45bde71476da35f9e20622f1b6b5637fa8

Accepted:

exact 15 / 10 / 5 request-validation inventory
exactly 10 transport validators
foundation DI discovery
transport-only validation boundaries

The removed FulfillmentReturnsGridAliases.cs contained aliases only, had zero production consumers, and the aliased underlying types remain. No semantic/business content was lost.

NON-NEGOTIABLE HOST EVACUATION RULE

No Host production file may be deleted merely because it is called dead/residue.

Before deletion:

create a Content Disposition Map
classify every type/member/constant/behavior
rehome every live responsibility to the correct owner
prove runtime/call-site parity
only then remove the evacuated Host shell

A single Host file may split into multiple destinations.

Zero-consumer syntax-only artifacts may be removed only with explicit proof that they contain no behavior/contract/state.

One objective

Remove all Fulfillment-specific runtime/business/security-policy ownership from Host before structure certification.

Current Fulfillment-specific Host files:

src/backend/Host/Tooba.Host/Admin/HostFulfillmentAdminAuthorizer.cs
src/backend/Host/Tooba.Host/Customer/HostFulfillmentCustomerAuthorizer.cs
src/backend/Host/Tooba.Host/Seller/HostFulfillmentSellerAuthorizer.cs

End state:
HOST_FULFILLMENT_SPECIFIC_FILES = ZERO

Do NOT structure-certify Fulfillment in this task.

A. Mandatory Content Disposition Map

Create first:
docs/evidence/TB-TMAR-FULFILLMENT-HOST-EVACUATION-001/content-disposition-map.md

Cover every responsibility.

HostFulfillmentAdminAuthorizer

Map:

CurrentAuthenticatedSession
ICurrentTenant
ControlPlaneRegistry
IAuthorizationGuard
IHostEnvironment
tenant-present admin authorization
Marketplace Development synthetic tenant fallback
MarketplacePlatformTenantId
actor resolution
AuthorizationCheck construction
Allow/Deny/Unavailable handling
HTTP status/error-code mapping
HostFulfillmentCustomerAuthorizer

Map:

authenticated actor resolution
Dev/Testing actor header
guest fallback actor
X-Tooba-Guest-Secret
checkout ownership lookup
CartAccess construction
guest cart ownership verification
missing/unauthorized semantics
Fulfillment error mapping
dependency on StorefrontCheckoutService.StorefrontGuestActorId
HostFulfillmentSellerAuthorizer

Map:

SellerPanel authorization
session/authorization/environment dependencies
actor + seller resolution
IAccessControlDirectory usage
effective access lookup
order.handle
GlobalWithinOwner projection
Category projection
DeniedByCeiling handling
SellerHandlePermissionInput construction

Do not remove any of these files before this map is complete.

B. Destination rule

Fulfillment-specific policy belongs to Fulfillment.

Generic platform mechanics may remain only behind generic reusable contracts that do NOT mention Fulfillment.

Preferred ownership:

Fulfillment-owned
IFulfillmentAdminAuthorizer implementation policy
IFulfillmentCustomerAuthorizer implementation policy
IFulfillmentSellerAuthorizer implementation policy
Fulfillment-specific error mapping
Fulfillment-specific permission projection
Fulfillment-specific customer/checkout authorization semantics
Generic platform seam may own
current authenticated actor/session
dev/testing actor seam
current tenant
generic admin/seller panel authorization primitive
generic authorization guard
generic environment detection
generic HTTP header access only if unavoidable

If an equivalent seam exists, reuse it.

If not, create the smallest generic abstraction in the canonical shared/platform contract location and implement it in Host.

Any new Host file MUST:

be generic/platform-named
contain zero Fulfillment references
contain zero Fulfillment error codes/types
be reusable by other modules
encode no Fulfillment business policy

Do NOT replace HostFulfillment* with another Fulfillment-specific Host file.

C. Admin evacuation

Remove HostFulfillmentAdminAuthorizer.cs only after rehome.

Preserve exactly:

tenant-present path
Marketplace development fallback
synthetic tenant semantics
actor resolution behavior
allow/deny/unavailable distinction
stable error/status semantics

Fulfillment must not reference Tooba.Host.

D. Customer evacuation

Remove HostFulfillmentCustomerAuthorizer.cs only after rehome.

Preserve:

authenticated customer
Dev/Testing actor header
guest actor
guest-secret header
checkout ownership lookup
actor ownership
guest cart verification
missing/unauthorized behavior

Remove direct dependency on:
Tooba.Order.Application.Storefront.Services.StorefrontCheckoutService.StorefrontGuestActorId

Use an existing contract/shared guest-actor authority if present.
If none exists, introduce the smallest stable shared contract/constant in the correct cross-module location.

Do NOT duplicate the guest actor Guid.
Do NOT introduce Fulfillment -> Order.Application.

E. Seller evacuation

Remove HostFulfillmentSellerAuthorizer.cs only after rehome.

Preserve:

seller actor resolution
seller party resolution
effective-access lookup
exact order.handle semantics
GlobalWithinOwner projection
Category scope projection
DeniedByCeiling exclusion
distinct category ids
SellerHandlePermissionInput output

Fulfillment must not depend on AccessControl.Application/Domain.

If the only current API is IAccessControlDirectory, introduce/use a Contracts-level read seam exposing only the effective-access data needed by Fulfillment.

Do NOT copy AccessControl business rules into Fulfillment.

F. Host final state

At PASS, absent:

HostFulfillmentAdminAuthorizer.cs
HostFulfillmentCustomerAuthorizer.cs
HostFulfillmentSellerAuthorizer.cs

Search entire Host production source.

Require:

zero HostFulfillment* types
zero Fulfillment-specific authorizer implementations in Host
zero Fulfillment-specific security/business policy in Host

Allowed Fulfillment mentions in Host:

generic composition registration
module assembly registration
migration/dev bootstrap only if non-policy composition

Any surviving behavior/policy => INCOMPLETE.

G. DI

Update registration so Fulfillment resolves module-owned authorizers.

Prefer module-owned registration extension methods.

Host may register only generic platform seams.

No service-locator expansion.

H. Semantic parity tests

Focused tests only.

Admin
tenant-present authorized
Marketplace development fallback authorized
deny
unavailable
missing tenant outside fallback
Customer
authenticated owner
wrong authenticated owner
valid guest secret
invalid/missing guest secret
Dev/Testing actor header
Seller
authorized seller
order.handle global scope
order.handle category scopes
denied-by-ceiling excluded
no matching permission
distinct category ids

Mocks/fakes only.
No DB.
No full web host.

I. Architecture guards

Prove:

Host Fulfillment-specific files = ZERO
Fulfillment -> Host = ZERO
cross-module dependencies = Contracts-only
no AccessControl.Application/Domain from Fulfillment
no Order.Application from Fulfillment
no duplicated guest actor constant
module-owned authorizer implementations exist
DI resolves all three interfaces
existing 15 / 10 / 5 validator state unchanged
Fulfillment remains NOT structure-certified
J. Protected state

Preserve:

routes
response/error semantics
MediatR 12.5
ISender-only endpoints
validator coverage 10/5
no schema/migration change
certified modules unchanged
Checkout PAUSED_AT_SAFE_W5_CHECKPOINT
frontendFrozen = true

Do NOT:

structure-certify Fulfillment
start another module
perform unrelated Host cleanup
resume Checkout
touch frontend
K. FAST-VALIDATION-BUDGET

Run ONLY:

focused authorizer parity tests
focused Fulfillment Host-evacuation guard
Fulfillment validator coverage guard
dotnet build src/backend/Modules/Fulfillment/Tooba.Fulfillment.Tests/Tooba.Fulfillment.Tests.csproj --no-restore
one Host project/test build only if required by a generic platform seam

Do NOT run:

full Fulfillment suite
full Host suite
broad TMAR suite
solution tests/build
Testcontainers
DB integration
retries

If a focused test hangs:
return INCOMPLETE and STOP.

L. Recovery

On PASS:

Fulfillment hostEvacuation = COMPLETE
Host Fulfillment-specific files = ZERO
semantic parity = PROVEN
validatorCoverage = COMPLETE_10_OF_10_REQUIRED_PRESENT_5_NO_VALIDATOR_REQUIRED
structure certification remains PENDING

Set:
nextTask = TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-STRUCTURE-001
nextTaskGate = NEXT_TMAR_WAVE_AFTER_FULFILLMENT_HOST_EVACUATION

M. Evidence

Create:
docs/evidence/TB-TMAR-FULFILLMENT-HOST-EVACUATION-001/fulfillment-host-evacuation.md

Include:

Content Disposition Map reference
old Host file -> destination mapping
generic platform seam changes
semantic parity proof
files removed only after rehome
cross-module boundary proof
DI proof
focused validation/build
structure certification still PENDING
PASS criteria

PASS only if:

Content Disposition Map covers every responsibility
all three Fulfillment-specific Host files are fully evacuated
no live semantic content is discarded
HostFulfillment* files/types = ZERO
Fulfillment-specific policy in Host = ZERO
Fulfillment -> Host = ZERO
AccessControl/Order boundaries are Contracts-only
guest actor authority is not duplicated
module-owned authorizers resolve through DI
admin/customer/seller behavior parity is proven
validator state remains 15 / 10 / 5
Fulfillment remains NOT structure-certified
Checkout/frontend unchanged
focused validations/builds pass
Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT
Task-ID: TB-TMAR-FULFILLMENT-HOST-EVACUATION-001
Parent-Task: TB-TMAR-FULFILLMENT-ARCH-COMPLETE-002-PRECERT-REPAIR-001
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT
Summary:
Parent-Precert-State:
Content-Disposition-Map-State:
Admin-Authorizer-Rehome-State:
Customer-Authorizer-Rehome-State:
Seller-Authorizer-Rehome-State:
Generic-Platform-Seam-State:
AccessControl-Boundary-State:
Order-Boundary-State:
Guest-Actor-Authority-State:
Host-Fulfillment-Specific-File-Count:
Host-Fulfillment-Specific-Type-Count:
Fulfillment-To-Host-Dependency-State:
CrossModule-Boundary-State:
DI-Resolution-State:
Admin-Parity-Tests:
Customer-Parity-Tests:
Seller-Parity-Tests:
Host-Evacuation-Guard:
Validator-Coverage-State:
Project-Builds:
Fulfillment-Structure-Certification-State:
Checkout-State:
Frontend-Production-Changes:
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
Do not structure-certify Fulfillment automatically.
Do not start another module.
Do not run broader tests.
Do not resume Checkout.
Do not touch frontend.
Do not poll.
Wait for Architect verification.

END_TOOBA_TASK