# UI / route-command wire handoff — TB-282

## Surfaces

| Surface | URL | Owner |
| --- | --- | --- |
| Admin list/editor | `/fa/admin/stories` | `StoryManagementScreen` + `ADMIN_STORY_CAPABILITIES` |
| Seller list/editor | `/fa/vendor-panel/stories` | same screen + `SELLER_STORY_CAPABILITIES` inside existing VendorShell |
| Public rail/player | home composition + `StoryModal` | storefront Story feature |

## Key commands (existing owning endpoints)

- Seller create `POST /v1/seller/stories`
- Seller add item `POST /v1/seller/stories/{id}/items`
- Seller submit `POST /v1/seller/stories/{id}/submit`
- Seller query `POST/GET query` via `querySellerStories`
- Admin create/get/query/approve-and-publish/reject/schedule/items
- Media seller/admin upload as in media-and-intent-proof

## FE modules

- `app/stories/management/StoryManagementScreen.tsx`
- `app/stories/management/story-intent.ts`
- `app/stories/story-api.ts`
- `app/storefront/stories/{home-stories,story-modal}.tsx`
