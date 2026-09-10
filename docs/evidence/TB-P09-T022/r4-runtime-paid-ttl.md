# R4 Runtime — Paid TTL survival (Scenario A)

Host: `http://127.0.0.1:5088` (`Host: alpha.localhost`) — health 200 at `2026-09-10T12:25:44.215Z`.

Observability note: Cart `HoldTtl` was temporarily **90s** in the running Host for wall-clock expiry only; source reverted to **30m** after proof. No reservation rows were UPDATE'd by hand.

Script: `docs/evidence/TB-P09-T022/_r4_runtime.mjs` → `_r4_runtime_raw.json`.

## Checkout / reservation

| Field | Value |
| --- | --- |
| checkoutId | `01a08b47-f702-7000-a340-2b499de017d1` |
| reservationId | `01a08b47-f658-7000-bf19-87baa12f0014` |
| originalExpiresAt | `2026-09-10 12:27:14.2514+00` |
| payDelayMs (near expiry) | `77749` |
| confirm_deposit | 200 at `2026-09-10T12:27:02.355Z` |
| dispatch | 200 at `2026-09-10T12:27:26.627Z` |

## Before / after

| Phase | at (UTC) | status | ExpiresAt | qty | reserved (stock) |
| --- | --- | --- | --- | --- | --- |
| beforePay (cart) | 2026-09-10T12:25:44.413Z | Held | 2026-09-10 12:27:14.2514+00 | 1.000000 | 5.250000 |
| afterPay | 2026-09-10T12:27:02.599Z | Held | **null** | 1.000000 | 5.750000 |
| afterExpiryWindow (worker polled past original TTL) | 2026-09-10T12:27:25.483Z | Held | **null** | 1.000000 | 4.500000 |
| afterDispatch | 2026-09-10T12:27:26.850Z | **Consumed** | null | 1.000000 | 3.500000 |

Same reservation id throughout. OrderLine and Fulfillment refs aligned. Wall past original expiry: **true** (`waitMeta.waitedMs=22652` + worker pad).

## Verdict

**PASS** — paid commit clears ExpiresAt; expiry worker does not release; Dispatch Consume succeeds.
