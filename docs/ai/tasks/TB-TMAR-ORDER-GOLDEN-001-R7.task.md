PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R7
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R6
Channel: tooba-main
Claimed: 730cc9a8-3f41-486a-91fe-b4bc8b4c20b1
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_7
Title: Host Order reverse audit (audit-only)
Backend-Only: YES

Architect decision

R6 is ACCEPTED at commit:
6e9980a40c06a8ced0f4a32aefa004b700a99003

Order remains:
INCOMPLETE_REFERENCE_REPAIR

Exact R7 scope

AUDIT ONLY — reverse-audit every Host production surface that still references
OrderDbContext / Order Application|Infrastructure|Domain / Order-owned routes.

Do NOT migrate CustomerPanel / SellerPanel / Admin dashboard / ReservationCycle /
Checkout W6 / frontend.
Do NOT start R8.
Do NOT commit / push / Bridge Result from Worker unless Architect instructs.

Required evidence + durable Host.Tests inventory guard + SoT sync.

END_TOOBA_TASK
