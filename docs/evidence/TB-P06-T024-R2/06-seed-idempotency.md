# 06 — Seed idempotency

Task: TB-P06-T024-R2

## Method

1. First Host Development start — seed creates missing entities / publishes snapshot.
2. Second Host restart (after member-tuple fix rebuild) — same logical IDs returned from `demo-preview`.

## Observed reuse

| Entity | Result |
|--------|--------|
| Employee user | Same `employeeActorId` |
| Categories موبایل/کتاب | Same category IDs |
| Offers | Same offer IDs |
| Orders | Same sellerOrderIds / order numbers (checkout idempotency keys) |
| Role `mobile-order-op` | Same role id |
| Assignments | No duplicate assignment errors; Ensure* paths skip existing |

## Auth tuples

InMemory authorization is empty after restart; seed **re-touches** employee `user#member@party` and capability sync so party#view + category scope remain valid after every Development start.
