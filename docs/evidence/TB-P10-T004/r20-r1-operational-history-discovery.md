# R20-R1 operational history discovery

Renderer: `OperationalHistoryTimeline` in `admin-order-detail-screen.tsx`.
API map: `mapHistoryEntry` in `admin-order-completeness.ts` (passthrough of Host labels/summaries).

Host projection (`AdminOrderCompletenessComposer`):

- Most payment/order/shipment labels are already human FA/EN.
- Raw fall-throughs found:
  - `payment_failed.summaryFa/En` = `payment.LastFailureCode`
  - `payment_refund_failed.summaryFa/En` = `payment.LastFailureCode`

Live Host GET for checkout `01a098eb-7669-7000-9fc2-1654986b455b` still returns `summaryFa=GATEWAY_REJECTED` (audit/projection unchanged).

Known LastFailureCode values that can appear:

- GATEWAY_REJECTED (observed)
- GATEWAY_TIMEOUT / GATEWAY_UNAVAILABLE / GATEWAY_RATE_LIMITED / GATEWAY_PENDING / GATEWAY_UNKNOWN / GATEWAY_MISCONFIGURED
- MANUAL_DEPOSIT_REJECTED / MANUAL_DEPOSIT_PENDING

Presentation mapper (`admin-operational-history-presentation.ts`) maps those codes and uses a human fallback for other `SCREAMING_SNAKE` tokens. Amounts, Persian sentences, and order numbers pass through.

No Status.ToString() parsing for business logic. Stored payment/reservation rows were not rewritten.
