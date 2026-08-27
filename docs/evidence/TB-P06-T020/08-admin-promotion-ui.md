# 08 — Admin promotion UI (TB-P06-T020)

## Route

`/admin/promotions` → `AdminPromotionsScreen` (DataGrid, same shell pattern as reviews/stories).

## Capabilities

- List all promotions (optional seller filter via API)
- Show code, name, discount, seller party snippet, status, expiry
- Deactivate active promotions (oversight POST)

## Client

`admin-api.ts`: `loadAdminPromotions`, `deactivateAdminPromotion`.

## Nav

`admin-shell.tsx` moderation group: **پروموشن‌ها** → `/admin/promotions` (`live: true`).
