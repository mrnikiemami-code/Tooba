# Error UX — TB-P09-T019

ReturnEndpoints no longer emit English `Bad Request`. `ReturnErrorMapper` + admin-error-map cover expired, non-returnable, not-delivered, quantity exceeded, stale, already approved/rejected, refund already started/completed, retry invalid.

Direct `request_return` when the kebab is hidden still runs eligibility (not generic `order.operation.invalid`).
