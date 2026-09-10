# R4 Runtime — Error UX (stale / non-active path)

Supported Admin ops path: pay → cancel → attempt `mark_processing` (and related forward ops) while cancelled.

| Field | Value |
| --- | --- |
| checkoutId | `01a08b4c-b653-7000-84c6-3c07553560d9` |
| reservation before cancel | `01a08b4c-b56b-7000-b72d-0dc38e74a7dc` Held @ `2026-09-10T12:30:56.194Z` |
| after cancel | same id **Released** @ `2026-09-10T12:30:57.585Z` |
| probeStatus | **400** |
| probeCode | `order.cancelled.blocks_action` |
| probeDetail | `سفارش لغوشده است؛ این عملیات مجاز نیست.` |

Leak check on user-facing payload:

- Held: no
- Released: no
- GUID in detail: no
- SQL: no

Capability gate correctly blocks forward fulfillment after cancel with stable localized mapping (does not expose inventory enum/raw/SQL/GUID). Exact `inventory.reservation.not_active` consume path remains covered by focused `PaidOrderReservationLifecycleTests` (R3).

## Boundary (payment near expiry)

Scenario A delayed confirm ~12s before original cart TTL (`payDelayMs=77749`, `ttlMs=89749`). Safe outcome: committed reservation survived. Exact concurrency race remains R3 focused tests — not faked here.

## Verdict

**PASS**
