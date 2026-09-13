# TB-P10-T004-R24-R1-R4 — Anti-pattern scan

| Pattern | Result |
| --- | --- |
| setInterval / timer auth polling | CLEAN (identity, account menu, header, cart, login) |
| timer Cart polling | CLEAN |
| auth/me fetch in multiple header copies | CLEAN — shared cache |
| retry-on-401 loop | CLEAN — 401 cached as known anonymous |
| global refetch storm on logout | CLEAN — mark anonymous then notify |
| auth provider remount per route | N/A — module cache survives client nav; full goto is one resolve |
| merge on render/mount / every /me / every nav | CLEAN — transition lock |
| route-based Cart reset / optimistic badge zero as SoT | CLEAN — AUTH_CHANGED still hides pointer only |
| localStorage Cart SoT | CLEAN — sessionStorage pointer only |
| timing sleeps to hide races | CLEAN in product code (runtime waits are proof-only) |

Home hero / pending-payment / stories timers are unrelated visual timers and do not fetch auth or cart.

CLEAN.
