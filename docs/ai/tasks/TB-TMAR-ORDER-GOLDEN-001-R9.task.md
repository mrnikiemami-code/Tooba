PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R9
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R8
Channel: tooba-main
Claimed: bd496141-ae73-4154-8ef2-e1cb641c8fbd
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_9
Title: Move Customer Order list/detail/retry (+ dashboard Order summary) out of Host into Order CQRS
Backend-Only: YES

Architect decision

R8 is ACCEPTED at commit:
9f3c71e6301b340319db4b0508d27e5ecb820f98

Order remains:
INCOMPLETE_REFERENCE_REPAIR

Exact R9 scope

Move Customer Order list/detail/retry (+ dashboard Order summary) out of Host into Order CQRS.

Required:
1. Persist this task artifact + evidence bridge-claim.json
2. Move routes to Order.Endpoints via ISender:
   GET /v1/customer/orders
   GET /v1/customer/orders/{checkoutId}
   POST /v1/customer/orders/{checkoutId}/retry-unpaid
3. CQRS: ListCustomerOrdersQuery, GetCustomerOrderDetailQuery, GetCustomerOrderDashboardSummaryQuery, RetryCustomerUnpaidOrderCommand — real IRequestHandlers → Result<T>
4. Remove Host CustomerPanelEndpoints /orders* registrations; CustomerPanelComposer must have NO OrderDbContext after R9; remove ListOrders/GetOrder/RetryUnpaid business + Order-only helpers
5. Dashboard GET /v1/customer/dashboard may stay Host but Order counts/recent orders ONLY via GetCustomerOrderDashboardSummaryQuery (thin composition with Wishlist/AddressBook/profile)
6. Move CustomerOrder* DTOs to Order.Application/Customer/.../Models; leave profile models in Host
7. Order.Application: foreign Contracts only (Catalog/Party/Payment); no Payment/Catalog/Party App/Infra/Domain; use IReservationCycleCoordinator for retry; IClock; ApiResponseFactory; typed Result/SemanticError; no PlatformHttpException/ex.Message/UtcNow in Order Application
8. Narrow Host customer authorizer/actor adapter if needed; ownership by PlacedByUserId; no body-trusted customer ids
9. Guards for routes/ISender/no OrderDbContext in composer/no Host retry logic/Contracts-only/no Payment Domain/R4-R8 removals intact; update R7 inventory for R9 without erasing R7 history
10. Evidence host-delta.md + recovery-sot.md; SoT nextTask=TB-TMAR-ORDER-GOLDEN-001-R10; Order INCOMPLETE; Checkout PAUSED; TmarDurableGuard R10
11. Validate Order.Tests, customer order ownership/security/dashboard tests, arch guards, affected module guards, focused Host CustomerPanel + reverse-audit, full build
12. DO NOT commit/push/Bridge. Leave R2C/R5-R1 RESULT untracked.
    Do NOT start R10/SellerPanel/Admin/Checkout W6/frontend.

END_TOOBA_TASK
