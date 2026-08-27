# 05 — Admin API contract (TB-P06-T017)

Base: `/v1/admin/stories`  
Auth: `AdminPanelAccess.RequireAuthorizedAsync` on every admin route.

## Routes

| Method | Path | Purpose |
|---|---|---|
| GET | `/v1/admin/stories` | List tenant stories |
| GET | `/v1/admin/stories/{id}` | Get snapshot |
| POST | `/v1/admin/stories` | Create Draft (201) |
| PUT | `/v1/admin/stories/{id}` | Update fields |
| PUT | `/v1/admin/stories/reorder` | Body `{ storyIds: guid[] }` |
| POST | `/v1/admin/stories/{id}/enable` | Activate |
| POST | `/v1/admin/stories/{id}/disable` | Soft disable |
| POST | `/v1/admin/stories/{id}/schedule` | Body `{ startAt, endAt }` |
| POST | `/v1/admin/stories/{id}/items` | Add item |
| PUT | `/v1/admin/stories/{id}/items/{itemId}` | Update item |
| DELETE | `/v1/admin/stories/{id}/items/{itemId}` | Remove item |
| PUT | `/v1/admin/stories/{id}/items/reorder` | Body `{ itemIds: guid[] }` |

## Bodies (create / update / item)

- Create/Update: `title`, `locale`, `market`, `coverMediaAssetId`, `coverMediaUrl`, (`displayOrder` create), `ctaType`, `ctaTarget`
- Schedule: `startAt`, `endAt` → status becomes Scheduled / Active / Expired from `now`
- Item: `mediaType`, `mediaAssetId`, `mediaUrl`, `caption`, `durationMs`, `ctaType`, `ctaTarget`, (`displayOrder` add)

## Snapshot shape

`AdminStorySnapshot`: ids, tenant, locale/market, title, cover, displayOrder, schedule, status, CTA, versionToken, timestamps, nested `items[]`.

## Errors

| Condition | Status / code |
|---|---|
| Missing/unauthorized actor | 401 / 403 via AdminPanelAccess |
| Missing tenant | 400 `story.tenant.missing` |
| Not found | 404 `story.missing` |
| Unsafe CTA | 400 `story.cta.rejected` |
| Other domain reject | 400 `story.mutation.rejected` |
