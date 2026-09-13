# TB-P10-T004-R24 — Network / performance

- No new per-second API polling. Checkout source has no `setInterval(ms)` fetch loop. Pending-card timer is local countdown from `holdEndsAt`; test asserts `setInterval` does not `fetch`.
- Open-unpaid is one indexed SellerOrder+Checkout count, not N+1 pending cards.
- Churn is one window query on `ix_checkout_reservation_commits_customer_occurred`.
- Settings loaded once per submit (`LoadSettingsAsync` outside TX).
- Customer lock is a single PK row per customer; sequential checkout is one UPDATE.
- Countdown local only.

Y-fe-cart 200; Y-no-new-polling PASS.
