# 07 — Seller Support APIs

Task: TB-P06-T025

Base: `/v1/seller/support`

Headers: `X-Tooba-Dev-Actor-User-Id`, `X-Tooba-Seller-Party-Id` (same as other seller panels).

| Method | Path | Notes |
|--------|------|-------|
| GET | `/tickets` | scoped to SellerPartyId |
| POST | `/tickets` | create as Seller requester |
| GET | `/tickets/{id}` | own seller scope; no internal notes |
| POST | `/tickets/{id}/replies` | |
| POST | `/tickets/{id}/close` | policy mirrors customer |
| POST | `/tickets/{id}/reopen` | |

Authz: `SellerPanelAccess.RequireAuthorizedAsync` + ticket ownership/scope. Foreign party → 403.
Nav capability: `support.view` (create/reply projected via `support.create` / `support.reply` where AccessControl UI lists them).
