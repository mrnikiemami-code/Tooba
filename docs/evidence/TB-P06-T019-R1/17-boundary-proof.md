# 17 — Boundary proof (TB-P06-T019-R1)

## Story module only (backend)

| Area | Location |
|---|---|
| Domain | `Tooba.Story.Domain` |
| Application contracts | `Tooba.Story.Application` |
| Persistence / directory / seed / migration `AddStoryReviewOwnership` | `Tooba.Story.Infrastructure` |
| Host HTTP | `Tooba.Host/Story/StoryEndpoints.cs` (+ existing panel composers) |
| Tests | `Tooba.Host.Tests/StoryFoundationTests.cs` |

No cross-module SQL JOINs into Catalog/Cart/etc. Seller identity uses shared BuildingBlocks authorization + Host `SellerPanelAccess` (same pattern as other seller panels).

## Frontend ownership

| Area | Path |
|---|---|
| Shared management | `app/stories/management/*` |
| API client | `app/stories/story-api.ts` |
| Admin thin wrapper | `app/admin/admin-screens.tsx` + `admin/stories` |
| Seller thin wrapper | `app/vendor-panel/stories` |
| Storefront | **unchanged** |

## Migration

`20260827080717_AddStoryReviewOwnership` — Story schema columns only (`origin`, `review_status`, `seller_party_id`, review actors/timestamps, `rejection_reason` + indexes).
