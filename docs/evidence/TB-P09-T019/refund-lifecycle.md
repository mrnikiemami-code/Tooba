# Refund lifecycle — TB-P09-T019

Refund statuses on queue: none / pending / failed / completed. Independent of composed Return status (Requested/Approved/Rejected/Completed/Cancelled).

Approve may start Payment refund; Return does not become Completed merely because refund pending. Retry reuses existing `RetryRefundAsync`.
