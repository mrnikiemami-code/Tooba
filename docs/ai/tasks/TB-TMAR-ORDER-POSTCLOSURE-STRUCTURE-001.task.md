PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_ENDPOINTS_STRUCTURE_HARDENING
Title: Reorganize Order.Endpoints by Capability + Namespace Alignment
Backend-Only: YES

Architect decision

Order business architecture remains:
COMPLETE_REFERENCE_PATTERN

Validation/foldering hardening for Application is accepted.

Current remaining structural issue:
Tooba.Order.Endpoints still keeps capability-specific endpoint files at project root.

This task fixes ONLY Endpoints structure.

Do NOT touch Infrastructure foldering in this task.
Do NOT touch business behavior.
Do NOT resume Checkout W6.
Do NOT touch frontend.

Reference pattern

Capability-driven organization.

Target:

capability is visible from Solution Explorer

path ↔ namespace alignment

root contains composition/shared infrastructure only

OrderEndpointModule is allowed at root.
Errors/Resources remain shared folders.

Current root files to relocate

Audit and move:

AdminCustomersEndpoints.cs

AdminOrderCompletenessEndpoints.cs

AdminOrderDetailEndpoints.cs

AdminOrderInventoryRecoverySupplyEndpoints.cs

AdminOrderOperationsEndpoints.cs

AdminOrdersGridEndpoints.cs

CustomerOrderEndpoints.cs

SellerOrderEndpoints.cs

StorefrontOrderEndpoints.cs

Keep at root:

OrderEndpointModule.cs

Keep shared folders:

Errors/

Resources/

Required target shape
Tooba.Order.Endpoints
├─ Admin
│  ├─ Customers
│  │  └─ AdminCustomersEndpoints.cs
│  ├─ Completeness
│  │  └─ AdminOrderCompletenessEndpoints.cs
│  ├─ Detail
│  │  └─ AdminOrderDetailEndpoints.cs
│  ├─ InventoryRecovery
│  │  └─ AdminOrderInventoryRecoverySupplyEndpoints.cs
│  ├─ Operations
│  │  └─ AdminOrderOperationsEndpoints.cs
│  └─ OrdersGrid
│     └─ AdminOrdersGridEndpoints.cs
├─ Customer
│  └─ CustomerOrderEndpoints.cs
├─ Seller
│  └─ SellerOrderEndpoints.cs
├─ Storefront
│  └─ StorefrontOrderEndpoints.cs
├─ Errors
├─ Resources
└─ OrderEndpointModule.cs

If actual responsibility proves a slightly better capability folder name, use it only if:

it is explicit

it matches Application capability naming

evidence explains the deviation

Namespace alignment

Every moved file MUST use a namespace matching its physical path.

Examples:

Admin/Operations/AdminOrderOperationsEndpoints.cs
-> namespace Tooba.Order.Endpoints.Admin.Operations;

Customer/CustomerOrderEndpoints.cs
-> namespace Tooba.Order.Endpoints.Customer;

Storefront/StorefrontOrderEndpoints.cs
-> namespace Tooba.Order.Endpoints.Storefront;

Forbidden:

file moved but namespace left at Tooba.Order.Endpoints

alias workaround to avoid updating references

wildcard/global using introduced only to hide namespace debt

OrderEndpointModule

Keep OrderEndpointModule.cs at root.

Update its using/imports and mapping calls cleanly.

Do not duplicate route registration.

All current route families must remain mapped exactly once.

Preserve exactly

all route paths

HTTP methods

authorizer usage

ISender usage

ApiResponseFactory behavior

Result/SemanticError behavior

endpoint names/tags if present

request/response contracts

validation pipeline behavior

Host endpoint ownership state

module registration

all existing tests

No behavior redesign.

Endpoint ownership

Order remains the owner of all current Order HTTP surfaces.

Host must NOT regain any Order endpoint.

No duplicate mapping.

Architecture guard

Add a durable guard under Order.Tests proving:

Tooba.Order.Endpoints root contains no capability-specific endpoint files.

Allowed root .cs files are explicitly allowlisted:

OrderEndpointModule.cs

Errors/ and Resources/ are shared folders.

Every capability endpoint file path matches its namespace.

No moved endpoint file keeps namespace Tooba.Order.Endpoints;.

OrderEndpointModule still maps every capability exactly once.

No duplicate /orders route ownership appears in Host.

Suggested test:
OrderEndpointOrganizationGuardTests

The test must fail if a future developer adds a capability-specific *Endpoints.cs file back to project root.

Evidence

Create:

docs/evidence/TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001/endpoints-organization.md

Include:

Before

list root endpoint files

After

actual folder tree

Namespace alignment

path -> namespace table

Allowed root files

exact allowlist + reason

Route preservation

summary proving all previous route families remain registered

Validation

Run:

full Tooba.Order.Tests

endpoint presentation tests

endpoint ownership guards

Host reverse-audit guards

Tmar durable guards

dotnet build src/backend/Tooba.slnx

SoT

On PASS:

Order remains:
COMPLETE_REFERENCE_PATTERN

Checkout remains:
PAUSED_AT_SAFE_W5_CHECKPOINT

Next task:
TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-002

Do NOT start it.

PASS criteria

PASS only if:

All listed capability endpoint files moved out of project root.

Target capability folders are coherent.

Path and namespace align.

OrderEndpointModule.cs remains root composition entry.

Route behavior/ownership unchanged.

No duplicate Host routes introduced.

Durable guard prevents future root endpoint clutter.

Full current-head backend build passes.

Frontend unchanged.

Infrastructure structure untouched except compile-only using updates if absolutely required.

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-POSTCLOSURE-STRUCTURE-001
Parent-Task: TB-TMAR-ORDER-POSTCLOSURE-QUALITY-001-R1
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Endpoint-Folder-State:
Root-Files-Before:
Root-Files-After:
Admin-Capability-Folders:
Customer-Folder-State:
Seller-Folder-State:
Storefront-Folder-State:
Namespace-Alignment-State:
OrderEndpointModule-State:
Route-Preservation-State:
Duplicate-Route-State:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Authority-State:
Order-Final-State:
Checkout-State:
Frontend-Production-Changes:
Residual-Defects:
Git:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start STRUCTURE-002.
Do not reorganize Infrastructure yet.
Do not resume Checkout W6.
Do not start another module.
Do not poll.

END_TOOBA_TASK