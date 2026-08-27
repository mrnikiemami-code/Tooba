# 13 — Notification UI (TB-P06-T020)

Date: 2026-08-27  
Decision reference: `12-notification-decision.md` → **DEFERRED_WITH_REASON**

## Customer

| Item | Status |
|---|---|
| Route `/customer-panel/notifications` | Honest `CustomerCapabilityShell` stub only |
| Nav | In `CUSTOMER_DEFERRED_NAV_HREFS` — **hidden** from live menu |
| Settings prefs | Existing honest unavailable section (no fake save) |

No port of Shopeiva `notifications.jsx` inbox (unread pulse, mark-read, delete) — would be fake without Host.

## Seller

| Item | Status |
|---|---|
| `/vendor-panel/notifications` | **Not created** |
| Vendor shell Bell / unread badge | **Not added** |
| Closest Shopeiva sources (for a future Task) | Customer Account inbox; Vendor settings notification toggles |

## Explicit non-deliverables

- No fake notification rows
- No unread count badge
- No push / realtime wiring
- No mark-read API

## Verdict

**NOTIFICATION_UI = DEFERRED_WITH_REASON** (nav hidden / deferred shells only)
