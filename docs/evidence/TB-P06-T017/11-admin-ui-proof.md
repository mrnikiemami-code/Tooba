# 11 — Admin UI proof (TB-P06-T017)

## Surface

| Item | Value |
|---|---|
| Route | `/admin/stories` (unprefixed panel) |
| Page | `app/admin/stories/page.tsx` → `AdminStoriesScreen` |
| Nav | Admin shell item «استوری‌ها» `live: true` |
| Marker | `data-testid="admin-stories"` |

## Capabilities (DataGrid + Host commands)

- List live admin snapshots
- Create story (Draft) then enable
- Enable / disable
- Schedule (`startAt` / `endAt` datetime-local → Host schedule)
- Manage items (media type/url, CTA)
- Uses `listAdminStories`, `enableAdminStory`, `scheduleAdminStory`, create/item helpers from `story-api.ts`

## Evidence-time

- `GET http://127.0.0.1:3000/admin/stories` → 200
- Capture: `captures/04-admin-stories.png` (via `scripts/prove-t06-t017-stories.mjs`)

Customer AddStory intentionally not present in storefront.
