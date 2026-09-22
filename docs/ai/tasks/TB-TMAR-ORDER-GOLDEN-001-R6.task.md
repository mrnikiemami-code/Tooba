PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R6
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R5-R1
Channel: tooba-main
Claimed: 79559bf1-fb82-4803-b1dd-eb6f616cdaca
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_6
Title: Move Admin Order Detail + AdminViewAck Out of Host into Order CQRS
Backend-Only: YES

Architect decision

R5-R1 is ACCEPTED at commit:
4b65a45633b900fc4b82d6addbbeb747e11b8fc0

Order remains:
INCOMPLETE_REFERENCE_REPAIR

Exact R6 scope

Move ONLY Admin Order Detail + AdminViewAck business authority out of Host.

Primary Host surfaces in scope:

- GET /v1/admin/orders/{checkoutId} from AdminPanelEndpoints
- AdminPanelComposer.GetOrderAsync + detail-only helpers
- Detail DTOs in AdminPanelModels
- CheckoutAdminViewAck persistence on the detail path (IClock, no UtcNow)

Required ownership:

Order.Endpoints -> ISender -> GetAdminOrderDetailQuery -> IRequestHandler -> Result&lt;AdminOrderDetailPage&gt;
Order.Application/Admin/Detail (+ Models, ports, composer)
Order.Infrastructure store for checkout load + AdminViewAck
Foreign Contracts only in Order.Application

Do NOT touch: OrdersGrid, CustomerPanel, SellerPanel, ReservationCycle ownership, Checkout W6, frontend, unrelated AdminPanel routes.
Do NOT restore R4/R5 Host removals.
Do NOT start R7.
Do NOT commit / push / Bridge Result from Worker unless Architect instructs.

END_TOOBA_TASK
