# Process Manager ↔ Cart integration
- CheckoutProcessManager injects ICartConversionPort alongside ICheckoutInventoryReservationPort
- ConvertForCheckoutAsync called after MarkCartCommitting with CartConversionRequest(ProcessId, CorrelationId)
- Host ConvertCartAsync removed from ICheckoutSubmitHost
- Ordering preserved: reserve → persist order → cart convert → Complete
- PM does not implement Cart business rules
