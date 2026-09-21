# Presentation / Error Audit

## Central path
- SemanticError + FulfillmentErrorCatalogContributor / ReturnsErrorCatalogContributor
- ApiResponseFactory / ISafeErrorMapper / IErrorMessageLocalizer

## Deleted / removed
- Host `ReturnErrorMapper.cs`
- Host Persian `"مقصد بازگشت وجه نامعتبر است."`
- Fulfillment endpoint service locator + OrderDbContext auth orchestration
- Work-queue `throw new PlatformHttpException(...)` (now ErrorCode on BulkResult)
- Manual Results.Json error envelopes on Fulfillment/Returns endpoints

## Expected failures covered
- fulfillment.missing, seller.order.handle.denied/scope_denied/missing
- return.missing, refund.destination.invalid, return.stale / eligibility codes via ReturnSemanticMapper
- work_queue.* bulk codes in FulfillmentErrorCodes + catalog
