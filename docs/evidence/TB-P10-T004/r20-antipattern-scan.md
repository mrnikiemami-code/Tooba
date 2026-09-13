# R20 anti-pattern scan

CLEAN for new work:

- No FE policy precedence; Settings/effective from Host.
- No hardcoded timer constants in pending UI.
- No polling (`setInterval` ticks locally only).
- No CSS overflow hide for layout bugs.
- Pending cards reuse storefront card primitives.
- Retry busy flag; no optimistic cycle increment.
- Raw exception names hidden.

Notes (not new locks):

- Admin payment history can still show `GATEWAY_REJECTED` (existing operations list).
- Screenshot capture mirrors RTL; live `dir=rtl`.
