# TB-P09-T003 — Permissions

- Ops projection uses effective-access (`order.cancel` / `order.handle` / `fulfillment.manage` / `return.manage` / …).
- Unauthorized actor (`00000000-…0099`) POST operations → **403**.
- View-only path: list/detail/notes/history require `order.view`; mutations require handle/cancel/return permissions (existing architecture; no new permission families).
- FE menu renders only API-returned actions (`AdminOrderOperationsMenu`).
