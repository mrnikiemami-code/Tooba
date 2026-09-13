# R17 Order Detail

Section `رزرو موجودی` (`admin-order-reservation-cycle`) after `OrderSupplyCard`.

Shows human status/reason (FA/EN from server mapper), cycle #, used/max, StartedAt, ExpiresAt, remaining when Active, SupplyStatus label, retry remaining.

History + events under the same section. Shortage lines when reacquire failed. Retry-limit copy when remaining is 0.

`CanRetryReservation=false`, `CanExtendTimer=false`. No GUID cycle ids. Payment attempt shown as last-8 human ref only.

Included on existing `GET` order (`AdminOrderDetailPage.ReservationCycle`) — one projection + events + supply for the page.
