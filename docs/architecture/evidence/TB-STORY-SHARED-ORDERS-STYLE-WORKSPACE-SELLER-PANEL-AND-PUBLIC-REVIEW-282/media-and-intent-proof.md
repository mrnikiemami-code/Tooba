# Media and intent proof — TB-282

## Media upload scopes

| Role | Route | Client |
| --- | --- | --- |
| Seller | `POST /v1/seller/media` | `uploadSellerStoryMedia` |
| Admin for shop | `POST /v1/admin/media/upload-for-shop` | `uploadAdminStoryMediaForShop` |
| Platform admin | `POST /v1/admin/media/upload` | `uploadAdminStoryMedia` |

- Ready `mediaAssetId` stored on add-item; no seller raw-URL-only bypass in the new slide path
- Upload occurs only on explicit «افزودن اسلاید», not on open/cancel Dialog
- Live Host uploads: ZERO (user performs first live act)

## Durable intent

- Module: `app/stories/management/story-intent.ts` v1
- `storeStoryIntent` writes + verifies identical read-back before send
- `clearStoryIntentIfCurrent` clears only exact envelope
- Create/submit freeze occurrence + body before request
- Unit: `story-intent.test.ts` 2/2

## Occurrence key law

- `storyOccurrenceKey(existing)` requires a non-blank key; callers mint once with `newStoryOccurrenceKey` for a NEW act only
