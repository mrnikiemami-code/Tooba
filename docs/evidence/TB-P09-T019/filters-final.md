# Filters final — TB-P09-T019

Backend-backed: returnStatus, refundStatus, seller, order number, customer, created/updated, search.

Quick views: all, pending_review, approved, refund_needed, refund_pending, refund_failed, completed, rejected.

`queueFilter` is EF-translatable (inline status predicates; `Matches()` is in-memory only).

No Received state invented. «دریافت‌شده» maps to existing Approved (refund_needed) in this domain.
