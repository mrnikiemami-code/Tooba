PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R11
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R10
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_ADMIN_RESIDUAL_OWNERSHIP
Title: Remove Final Admin Order Db Authority from Host
Backend-Only: YES

Architect decision

R10 is ACCEPTED at commit:
f8e5e92bfbbfbe798709d1e1a2de6c6a36578606

Order remains:
INCOMPLETE_REFERENCE_REPAIR

R7 reverse-audit identifies R11 as the final known migration slice before closure audit.

Reference pattern

Endpoint ownership/foldering:
Fulfillment-style

CQRS/MediatR/Result:
Offer-style

Host rule:
Host may keep genuine cross-module admin composition.
Host must NOT own OrderDbContext, Order business filtering/projection, Order-owned routes, or Order state classification.

Exact R11 scope

Remove the remaining Admin Order authority from Host.

Current residuals:

Host/Admin/AdminPanelComposer.cs

Host/Admin/AdminPanelEndpoints.cs

Host/Grid/AdminCustomersGridQueryEngine.cs

Host/Grid/AdminSellersGridQueryEngine.cs

Order-backed DTOs in Host/Admin/AdminPanelModels.cs

Host/Admin/AdminReservationCycleMapper.cs if no legitimate thin presentation use remains

Do NOT blindly move the whole AdminPanel.

1. Legacy Admin Orders list

Move:

GET /v1/admin/orders

to Order.Endpoints.

Create explicit query:
ListAdminOrdersQuery

Preserve current behavior:

latest 200 Checkout groups

seller display names

Returns projection

existing AdminOrdersGridProjection.MapOrderListItem

current output shape

Use:
Endpoint -> ISender -> Query -> Handler -> Result<T>

Host route and Host ListOrdersAsync/LoadOrderGroupsAsync/MapOrderListItemAsync must be removed.

Do not replace this compatibility route with the POST grid route unless response/behavior parity is exact.

2. Admin Customers list + grid

These are derived entirely from Order Checkouts, so move them to Order ownership.

Routes:

GET /v1/admin/customers

POST /v1/admin/customers/query

Create explicit queries:

ListAdminCustomersQuery

QueryAdminCustomersGridQuery

Move Order-owned models:

AdminCustomerListItem

Preserve grid behavior exactly:

grouping by PlacedByUserId

order count

last activity

search by recipient/contact

typed filters

advanced filters

sort

pagination

Status = Active semantics

latest checkout recipient/contact projection

Order.Infrastructure owns DB-native grid query.

Host AdminCustomersGridQueryEngine must be removed.

3. Admin Dashboard Order metrics

GET /v1/admin/dashboard is cross-module:

Catalog published product count

Offer active/seller counts

Order open/paid/pending/customer counts

Dashboard route MAY remain Host.

But create an Order-owned query:
GetAdminOrderDashboardMetricsQuery

It should return only Order metrics:

OpenOrders

PaidOrders

PendingOrders

Customers

Host may combine those with:

Catalog published products

Offer active offers

Offer seller count

After R11:

AdminPanelComposer must not inject OrderDbContext

no SellerOrderStatus classification in Host

no checkout customer counting in Host

4. Admin Sellers list + grid Order metric

Seller list/grid are cross-module:

Party identity/status

Offer counts

Order count

They MAY remain Host as cross-module composition.

But Order order-count metric must be obtained from an Order-owned boundary.

Create a narrow Order Application query/port, for example:

GetSellerOrderCountsQuery(IReadOnlyList<Guid> SellerPartyIds)
or equivalent stable read boundary.

Host seller list/grid may combine:

Party data

Offer metrics

Order count map from Order query/boundary

Forbidden after R11:

OrderDbContext in AdminPanelComposer

OrderDbContext in AdminSellersGridQueryEngine

Preserve all existing seller grid:

search

Party status/name filter

active offer count filter/sort

order count filter/sort

advanced filter

paging

Do not move Party/Offer business ownership into Order.

5. AdminReservationCycleMapper cleanup

Audit all production callers.

If no legitimate production caller remains:

delete Host/Admin/AdminReservationCycleMapper.cs

move/delete leftover Host DTOs only used by it

If a legitimate Host presentation caller remains:

keep only a demonstrably thin mapping facade

no Order business decisions

no duplicate policy/status logic

document exact caller and reason in host-delta

Do not keep dead facade "just in case".

AdminPanelComposer target

After R11 AdminPanelComposer may remain only for legitimate cross-module surfaces.

It must NOT contain:

OrderDbContext

CheckoutGroup/SellerOrder DB reads

SellerOrderStatus business classification

legacy Order list projection

Admin customer derivation from Order persistence

Order count SQL

Allowed examples:

combining Catalog/Offer/Party values with typed Order query results

non-Order admin surfaces

Endpoint semantics

New Order-owned routes:

GET /v1/admin/orders

GET /v1/admin/customers

POST /v1/admin/customers/query

Use:

IOrderAdminAuthorizer

ISender

ApiResponseFactory

Result/SemanticError

Do not use manual business Results.Json.

Dashboard/sellers Host endpoints may remain if they are thin cross-module composition.

Foreign boundaries

Order Application:

Party via Party.Contracts

Returns via Returns.Contracts

no Party.Infrastructure

no Offer Application/Infrastructure for R11 Order queries

no foreign DbContext

For seller order counts, Order owns its own data and exposes the metric outward through a narrow Application query/port.

Host cross-module composer may consume ISender or a stable Order read boundary.

Do not create Host -> Order.Infrastructure coupling.

Grid policy

Professional grid behavior must remain unchanged.

For Admin Customers grid:

policy/normalization should be Order-owned or shared BuildingBlocks, not Host business logic.

For Admin Sellers grid:

Host may keep the cross-module grid engine, but its Order metric input must come from Order boundary, never OrderDbContext.

No cross-schema SQL JOIN.

Error/time semantics

Use stable Result/SemanticError.

Forbidden in moved Order slice:

PlatformHttpException

ex.Message classification

manual localized error identity

DateTimeOffset.UtcNow / DateTime.UtcNow if time is needed

Unknown exceptions propagate.

Do NOT touch

SellerPanel R10 completed

CustomerPanel R9 completed

Storefront

Admin Order Detail R6

Admin OrdersGrid POST route R3

Checkout W6

frontend

unrelated Catalog/Offer/Party admin routes

Required Host result after R11

Host should have ZERO production OrderDbContext consumers except explicitly allowed development/migration bootstrap files already classified in R7.

Production Host must have no illegal Order authority.

Allowed residual examples:

HostOrderAdminAuthorizer

HostOrderAdminEffectiveAccessReader

HostOrderCustomerAuthorizer

HostOrderSellerAuthorizer

HostSellerOrderViewAccessReader

HostOrderStorefrontActor / identity gate

UnpaidOrderExpiryHostedService execution shell

Program/module composition

development migration/seed files explicitly classified

Guards

Add/strengthen durable guards proving:

Host /v1/admin/orders route absent

Host /v1/admin/customers and /customers/query routes absent

Order.Endpoints owns those routes via ISender

Host AdminCustomersGridQueryEngine absent

Host AdminPanelComposer has no OrderDbContext

Host AdminSellersGridQueryEngine has no OrderDbContext

no production Host OrderDbContext consumer remains outside explicit dev/migration allowlist

Admin customers grid is DB-native under Order Infrastructure

seller order counts come through Order boundary

no foreign Application/Infrastructure leak in Order R11 slice

R4–R10 Host removals remain intact

Reverse audit

After implementation, run the R7 inventory discovery again.

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R11/order-host-post-r11-audit.md

Classify every remaining Host Order reference as:

ALLOWED_THIN_HOST_ADAPTER

NON_ORDER_HOST_CONCERN

ILLEGAL_ORDER_AUTHORITY

PASS requires:
ILLEGAL_ORDER_AUTHORITY = 0

If any illegal authority remains:
Status = INCOMPLETE
and list exact residual. Do NOT call Order complete.

Host delta evidence

Create:
docs/evidence/TB-TMAR-ORDER-GOLDEN-001-R11/host-delta.md

Include:

Removed from Host

exact routes/files/methods/dependencies

Cross-module Admin surfaces retained

dashboard/seller list/grid details and why allowed

Remaining Host Order references

exact file-by-file list + classification

Closure readiness

READY_FOR_ORDER_FINAL_CLOSURE_AUDIT
or
NOT_READY_FOR_CLOSURE

Validation

Run:

Order.Tests

Admin legacy order list tests

Admin customers list/grid tests

Admin dashboard metric tests

Admin sellers grid regression tests

Order architecture guards

Host reverse-audit guards

Tmar durable guards

focused AdminPanel tests

full dotnet build src/backend/Tooba.slnx

Recovery

On PASS:

record R11

Order remains INCOMPLETE_REFERENCE_REPAIR until Architect performs independent final reverse-audit

nextTask = TB-TMAR-ORDER-GOLDEN-001-FINAL-CLOSURE

do NOT start closure task

Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT

PASS criteria

PASS only if:

GET /v1/admin/orders owned by Order.Endpoints.

Admin customer list/grid owned by Order.

Host AdminCustomersGridQueryEngine removed.

Host AdminPanelComposer has no OrderDbContext.

Host AdminSellersGridQueryEngine has no OrderDbContext.

Admin dashboard Order metrics are Order-owned.

Seller order-count metrics come from Order-owned boundary.

No production Host illegal OrderDbContext/business authority remains.

Cross-module dashboard/seller composition is thin and documented.

Result/CQRS semantics match reference.

Behavior/grid parity preserved.

Tests/build pass.

Post-R11 reverse audit says ILLEGAL_ORDER_AUTHORITY = 0.

Checkout W6 not started.

Frontend unchanged.

Otherwise:
Status = INCOMPLETE

Canonical Result

Return ONLY:

PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_WORKER_RESULT

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R11
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R10
Status: PASS | INCOMPLETE | RECOVERY_CONFLICT

Summary:
Legacy-Admin-Orders-State:
Admin-Customers-State:
Admin-Customers-Grid-State:
Admin-Dashboard-Order-Metrics-State:
Admin-Sellers-Order-Metrics-State:
AdminPanel-OrderDbAuthority:
AdminSellersGrid-OrderDbAuthority:
AdminReservationCycleMapper-State:
Order-Endpoint-State:
Order-CQRS-State:
Order-Foreign-Boundary:
Grid-Parity:
Behavior-Parity:
Architecture-Guard-Validation:
Focused-Validation:
Full-Validation:
Host-Removed-This-Task:
Cross-Module-Host-Remainder:
Remaining-Host-Order-References:
Post-R11-Illegal-Authority-Count:
Order-Closure-Readiness:
Residual-Defects:
Order-Overall-State:
Checkout-State:
Frontend-Production-Changes:
Recovery-State:
Recovery-Next-Task:
Git:
Blockers:
User-Work-Preserved:
Next-Recommended-Task:

END_TOOBA_WORKER_RESULT

STOP

After Result:
STOP completely.

Do not start final closure.
Do not resume Checkout W6.
Do not poll.

END_TOOBA_TASK

