# R1 list state visibility

- PaymentState mapping now distinguishes Cancelled vs Paid vs PendingPayment.
- `formatAdminStatus`: Refunded → «بازگشت وجه»; Returned → «مرجوعی».
- SellerOrderStatus enum has no Returned/Refunded value; grid Status cannot surface مرجوعی/بازگشت وجه without a new composed DTO field/column.

**Architect gap (not auto-added):** optional list field for return/refund summary if desired later.
