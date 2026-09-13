# R19 admin UX

- Orders/Payments grids: batched `GetProjectionsAsync`; compact FA labels; no per-row `GetProjectionAsync` / `ListEventsAsync`
- Order Detail: current cycle + immutable history + stored policy snapshot (not live Settings)
- Reacquire fail: business copy + shortage lines; Admin does not parse `Status.ToString()`
- Retry limit visible; `CanExtendTimer` / `CanRetryReservation` always false; no extend/reset action
- Store Settings مهلت‌ها inherit/override/effective; Category tab سیاست رزرو; Offer publication panel; Seller read-only / PUT 403
- No raw enum/GUID in normal mapped UX (`admin-reservation-cycle.test.ts`, `reservation-policy-admin.test.ts`)
