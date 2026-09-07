# R1 Runtime — Return Deadline After Delivery (Scenario G)

Delivered returnable line `01a07a63-ae0e-7000-9acf-8176b7c333bf` (qty shipped=2):

| Field | Live API value |
| --- | --- |
| isReturnable | true |
| returnWindowDays | 7 |
| returnPolicyLabel | ۷ روز پس از تحویل |
| returnDeadlineDisplay | تا ۱۴۰۵/۰۶/۲۳ |
| returnRemainingDisplay | ۷ روز باقی‌مانده |
| returnStatusCode | eligible |

Computed from canonical delivered time + Order Line snapshot (not mutable Offer).

Expired-path covered by existing focused `ReturnEligibilityEvaluator` / snapshot tests (no clock skew harness in local Host).
