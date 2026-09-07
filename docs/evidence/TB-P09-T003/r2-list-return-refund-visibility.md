# R2 list return/refund visibility

`AdminOrdersGridQueryEngine.ComposeOperationalStatus` overwrites وضعیت cell (not پرداخت) with:

- ReturnRequested → مرجوعی در انتظار بررسی
- ReturnApproved → مرجوعی تأیید شده
- RefundPending → بازگشت وجه در انتظار
- RefundCompleted → بازگشت وجه انجام شد
- RefundFailed → شکست بازگشت وجه

Backend-composed; FE `formatAdminStatus` maps labels. No new column/redesign.
