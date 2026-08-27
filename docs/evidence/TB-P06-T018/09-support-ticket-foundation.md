# 09 — Support / ticket foundation (TB-P06-T018)

## Status: NOT SELECTED

Wave 1 did **not** implement Support/Tickets.

## Why deferred

| Reason | Detail |
|---|---|
| Scope size | Minimum real capability needs Ticket, actor (customer/seller), subject, message thread, status, timestamps, optional category, admin/support reply |
| No Host owner | Tickets would require a new module, permissions, and thread APIs |
| Presentation priority | Hiding tickets from primary nav closes the honesty gap without inventing a chatbot/SLA surface |
| Forbidden fakes | Fake ticket lists, static demo threads, or chatbot stubs are disallowed |

## Honest panel behavior instead

- Customer `/customer-panel/tickets` — deep-link capability shell; **hidden from primary nav**.
- Seller `/vendor-panel/tickets` — deep-link capability shell; **hidden from primary nav**.
- No fake SLA timers or chatbot UI.

## Future wave prerequisites (not done here)

1. Host Support/Tickets module  
2. Customer/Seller/Admin reply permissions  
3. Thread APIs + status machine  
4. Exact Shopeiva tickets UI binding  
5. Isolation + unauthorized tests  

Do **not** claim support inbox readiness after Wave 1.
