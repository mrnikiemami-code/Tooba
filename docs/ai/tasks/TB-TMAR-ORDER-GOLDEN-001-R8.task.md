PIPELINE-PROTOCOL: BRIDGE-WAKE-V1
BEGIN_TOOBA_TASK

Task-ID: TB-TMAR-ORDER-GOLDEN-001-R8
Parent-Task: TB-TMAR-ORDER-GOLDEN-001-R7
Channel: tooba-main
Claimed: 82d1a199-12fd-4907-ba10-796408944766
WorkerId: tooba-worker-01
AgentType: cursor
Program: TMAR — Tooba Microservice-Ready Architecture Recovery
Track: ORDER_GOLDEN_IMPLEMENTATION_SLICE_8
Title: Move reservation cycle policy/retry/expiry business authority out of Host into Order
Backend-Only: YES

Architect decision

R7 is ACCEPTED at commit:
8baca44a84bb5964030ce605aefc4f6cf4ef2eba

Order remains:
INCOMPLETE_REFERENCE_REPAIR

Exact R8 scope

Move reservation cycle policy / retry-after-expiry / unpaid expiry reconciliation
business authority out of Host into Order.

Required:
1. Persist this task artifact + evidence bridge-claim.json
2. DELETE Host/ReservationCycleCoordinator.cs — Order owns retry-after-expiry
   orchestration, max-cycle, reacquire, cycle events (no OrderDbContext in Application)
3. DELETE Host/ReservationCyclePolicyResolver.cs — Order owns precedence merge
   (platform>store>category>offer, strictest multi-line); Catalog.Contracts for
   store/category/offer hold overrides; no Catalog App/Infra/Domain in Order Application;
   no Host CatalogDbContext in resolver
4. Split UnpaidOrderExpiryHostedService: Host keeps BackgroundService shell only;
   Order owns IUnpaidOrderExpiryReconciler.ReconcileAsync(batchSize,ct)
5. Preserve IReservationCyclePolicyResolver; remove concrete Host cast in
   ReservationPolicyAdminEndpoints; do NOT migrate ReservationPolicyAdmin beyond
   compile adaptation
6. IClock everywhere in moved business logic; typed/stable errors; no ex.Message
7. Guards + update R7 reverse-audit inventory for R8 removals without erasing
   historical R7 evidence
8. Evidence host-delta.md + recovery-sot.md; SoT nextTask=R9; Order INCOMPLETE;
   Checkout PAUSED
9. Validate Order.Tests, reservation/retry/expiry/policy tests, arch guards,
   Catalog guards if Contracts changed, focused Host worker/admin policy +
   reverse-audit, full build
10. DO NOT commit/push/Bridge. Leave R2C/R5-R1 RESULT untracked.
    Do NOT start R9/CustomerPanel/SellerPanel/Admin grids/Checkout W6/frontend.

END_TOOBA_TASK
