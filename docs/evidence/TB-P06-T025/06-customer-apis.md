# 06 — Customer Support APIs

Task: TB-P06-T025

Base: `/v1/customer/support` (FE via `/api/customer/support/...` BFF + CSRF).

| Method | Path | Notes |
|--------|------|-------|
| GET | `/tickets` | own list; `status`, `page`, `pageSize` |
| POST | `/tickets` | create + first message; optional `Idempotency-Key` |
| GET | `/tickets/{id}` | detail + public messages only |
| POST | `/tickets/{id}/replies` | body ≤ 4000 |
| POST | `/tickets/{id}/close` | Open/Resolved only |
| POST | `/tickets/{id}/reopen` | Closed → Open |

Actor: session / Dev actor header. Foreign ticket → 404/403 (no leak).
