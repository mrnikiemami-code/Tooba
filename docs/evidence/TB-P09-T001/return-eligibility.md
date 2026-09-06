# Return eligibility (TB-P09-T001)

SoT: `IReturnEligibilityEvaluator` / `ReturnEligibilityEvaluator`.

ReasonCodes: `not_paid`, `not_delivered`, `window_expired`, `nothing_returnable`, `eligible`, `order_missing`, `fulfillment_missing`.

Window = 30 days from `LastDeliveredAt`. Settlement is never consulted.
`ReturnDirectory.CreateAsync` / `CreateAdminInitiatedAsync` call the evaluator.
GET `/v1/admin/orders/{checkoutId}/return-eligibility` exposes results.
