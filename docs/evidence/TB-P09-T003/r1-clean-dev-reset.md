# R1 clean dev reset

Environment: LOCAL Development, Database `tooba_alpha` on `127.0.0.1:5432` (Docker `postgres-db`).

Destructive wipe of all test checkouts was **not** performed (FK/events risk). Instead:

- Applied additive schema migration for note soft-delete + view acks.
- Drove a fresh unused ReadyToFulfill checkout (`01a0429e-f137-7000-885a-d50dd2bc876d`) through real Host operations APIs.

Master data (users/sellers/offers/payment config) preserved.
