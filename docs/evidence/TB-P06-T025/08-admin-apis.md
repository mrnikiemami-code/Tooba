# 08 — Admin Support APIs

Task: TB-P06-T025

Base: `/v1/admin/support`

| Method | Path | Notes |
|--------|------|-------|
| GET | `/tickets` | filters: status, requesterKind, category, priority, q(subject) |
| GET | `/tickets/{id}` | includes internal notes |
| POST | `/tickets/{id}/replies` | `{ body, isInternalNote }` — public reply may notify requester |
| PATCH | `/tickets/{id}` | status / priority / assign |
| GET | `/demo-preview` | Development only — seed snapshot IDs for USER-PREVIEW |

Permissions: `support.view` (list/detail), `support.manage` (reply/patch). Admin notification inbox: skip if no recipient model.
