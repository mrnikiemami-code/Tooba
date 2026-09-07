# R1 note delete rule

Backend-authoritative:

- Soft-delete columns on `checkout_operational_notes`.
- `checkout_admin_view_acks` records Admin order views (detail GET + notes/history).
- Delete allowed only for author when no other user viewed after `CreatedAt`.
- Author’s own views do not lock.
- DELETE `/v1/admin/orders/{id}/notes/{noteId}`; FE shows «حذف» only when `canDelete`.

Runtime: author delete before foreign view → 200; after foreign view ack → 400 human FA.
