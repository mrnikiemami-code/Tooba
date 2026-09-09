# Reuse vs change — TB-P09-T019

Reuse: `IReturnDirectory`, `ReturnRequestStatus` vs `RefundAttemptStatus`, `IReturnEligibilityEvaluator`, Admin order ops `approve_return`/`reject_return`/`retry_refund`/`request_return`, Payment refund, settlement compensation, AppDataGrid.

Change: Admin work-queue projection (`AdminReturnWorkQueueRow`), DB-native human search, separate Return/Refund columns, kebab hide when empty, FA error mapper, nav wording, `NonReturnable` reason code, request_return honors `selections` quantity.
