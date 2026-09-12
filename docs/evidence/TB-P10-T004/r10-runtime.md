# TB-P10-T004-R10 — Runtime
Host :5088 (`Host: alpha.localhost`). Raw: r10-runtime-raw.json ok=true.

| Scenario | Result |
| --- | --- |
| A Online unpaid timeout | PASS — Expired, hold 1→0, history kept (orders=1, attempts=1) |
| B Expired retry + stock | PASS — same Order, new reservation, old Released unchanged, attempts=2 |
| C Expired retry + no stock | PASS — 409 `payment.unpaid.supply_unavailable` + «قابل تأمین نیست» |
| D Manual initial timeout | PASS — Expired, hold released |
| E Evidence protected | PASS — Pending, hold remains |
| F Succeeded untouched | PASS — Succeeded, hold=1 |
| G Timeout then late sandbox success | PASS — Succeeded, hold=1, orders=1 |
| G2 Late success + no stock | PASS — Succeeded + SupplyStatus Unavailable |
| H Admin Settings | PASS — fake method override 5h; store online effective 3h |
| I Admin grids | PASS — payment Expired vs order PendingPayment vs Supply AvailableForReacquire |
| J Customer | PASS — PaymentExpired + retry 200 → PendingPayment |
