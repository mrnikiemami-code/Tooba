# TB-P10-T004-R23 — Observability

- Settings audit: `ReservationPolicyAuditEvent` on Admin PUT field changes
- Block audit: `CheckoutAbuseBlockEvent` (`open_unpaid` / `reservation_commit`) written after aborted TX via `RecordBlockAsync`
- Churn commits inspectable in `order.checkout_reservation_commits`

No new dashboard. Customer UX does not show raw machine codes.
