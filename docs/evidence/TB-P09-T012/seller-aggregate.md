# Seller-Aggregate — TB-P09-T012

Header badge uses `formatAdminStatus(fulfillmentStatus || seller.status)` and `PendingPayment` when locked. No seller-wide Packed unless every line packed.

Runtime D: after pack 1 of 2 on `01a07f1b-41de-7000-83ff-b2484761140e`, `fulfillmentStatus=Processing` (not Packed).
Runtime single: seller `fulfillmentStatus=Processing` while L1/L3 Packed and L2 Processing.
