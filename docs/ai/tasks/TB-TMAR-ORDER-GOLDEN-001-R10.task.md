PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R10
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R9
Channel: tooba-main
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_SELLER_PANEL_OWNERSHIP
Title: Move Seller Order List/Detail/Dashboard Authority Out of Host
Backend-Only: YES

(Persisted claim artifact — full Architect body received via Bridge GET /api/tasks/next at claim id 034fe351-f31d-4562-b978-df9874507fd4.)

Exact R10 scope: Move ONLY SellerPanel Order authority from Host to Order CQRS/Endpoints.
Routes: GET /v1/seller/orders, GET /v1/seller/orders/{sellerOrderId} → Order.Endpoints
Queries: ListSellerOrdersQuery, GetSellerOrderDetailQuery, GetSellerOrderDashboardSummaryQuery
Host SellerPanelComposer must have NO OrderDbContext after R10.
Dashboard may stay Host as thin composition (seller display + Order summary CQRS).
AccessControl/Catalog/Party: Contracts/ports only in Order Application.
nextTask after PASS: TB-TMAR-ORDER-GOLDEN-001-R11 (do NOT start).
Checkout remains PAUSED_AT_SAFE_W5_CHECKPOINT.

END_TOOBA_TASK
