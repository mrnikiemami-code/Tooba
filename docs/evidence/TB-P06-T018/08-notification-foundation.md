# 08 — Notification foundation (TB-P06-T018)

## Status: NOT SELECTED

Wave 1 did **not** implement a Notifications domain or inbox UI.

## Why deferred

| Reason | Detail |
|---|---|
| Scope size | Minimum real capability needs recipient, type, title, body, read/unread, createdAt, locale, tenant, optional target — Host module + migrations + APIs |
| Presentation priority | Panel honesty (hide fake nav / live settings subsets) restores demo integrity without a new foundation |
| No Host owner ready | Push/SignalR not in scope; claiming inbox without Host would force fake unread counts |
| Risk | Fake notification badges are explicitly forbidden by task rules |

## Honest customer/seller behavior instead

- Customer `/customer-panel/notifications` remains deep-link capability shell only; **hidden from primary nav**.
- Customer settings “notification preferences” section remains **honestly unavailable** (no fake save).
- No unread badge invented in panel chrome.

## Future wave prerequisites (not done here)

1. Host Notification module + persistence  
2. SpiceDB / ownership rules for recipient isolation  
3. List + mark-read APIs  
4. Exact Shopeiva inbox UI binding  
5. Integration tests (list/read/unauthorized/foreign/empty)

Do **not** claim push, SignalR, or live unread counts after Wave 1.
