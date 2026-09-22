# Behavior Preservation Audit

Preserved exact URLs/verbs:
- GET/POST /v1/customer/returns, GET /v1/customer/returns/{id}
- GET /v1/seller/returns, GET .../{id}, POST .../approve|reject
- GET /v1/admin/returns, POST /returns/query, GET .../{id}, POST .../retry-refund

Seller scope: Application GetSellerReturn / Approve / Reject enforce SellerPartyId
Customer get: RequestedByUserId ownership
Refund destination EffectiveRefundDestination = RefundDestination ?? Destination
Idempotency/domain transitions unchanged in ReturnDirectory/Domain
