# TB-P10-T001 — Applicable locks

Read from `docs/architecture/TOOBA-LOCKS.md`:

- LOCK-QTY-001 / 002 / 003 — decimal quantity + Product policy + Offer min/max
- LOCK-ROUND-001 — GlobalRoundingMode for business normalization
- LOCK-OPS-024..028 — cart TTL vs paid reservation (do not regress)
- LOCK-SF-001 — Shopeiva UI contract for storefront checkout
- LOCK-SF-002 — checkout price backend-authoritative
- LOCK-SF-003 — do not trust card/display price
- LOCK-SF-004 — shipping/payment options Store-enabled (T002/T003)
- LOCK-SF-005 — delivery slot never earlier than minimum (T002)

Reuse: existing Cart Host APIs, guest secret, checkout coupon preview, home merchandising feed.
Change: wire product-card ATC, mini-cart drawer, cart recommendations; no redesign.
