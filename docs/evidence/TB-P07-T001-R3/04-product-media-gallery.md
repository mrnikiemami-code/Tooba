# TB-P07-T001-R3 — Product media gallery

## Presentation
- Gallery uses `storefrontMediaUrl(mediaAssetId)` → `GET /v1/storefront/media/{id}` SVG placeholder (never broken `<img>`).
- Workspace header leading shows primary (or first) media the same way.

## Model
Media row shape in `workspace-model.ts` / mapper:

`{ mediaAssetId, primary, displayOrder?, altText? }`

## Admin operations (Host)

| UI | Host |
| --- | --- |
| Attach by Guid | `POST /v1/admin/products/{id}/media` `{ mediaAssetId, altText? }` |
| Reorder up/down | `PUT .../media/order` `{ orderedMediaAssetIds }` |
| Set primary | `PUT .../media/{assetId}/primary` |
| Edit alt | `PATCH .../media/{assetId}` `{ altText }` |
| Remove | `DELETE .../media/{assetId}` |

## Honest deferral
Binary file upload is **DEFERRED**. UI shows note and accepts pasting/entering a Guid `MediaAssetId` only.

## Files
- `src/frontend/app/admin/workspace-model.ts`
- `src/frontend/app/admin/host-client.ts` (attach/reorder/primary/alt/remove helpers)
- `src/frontend/app/admin/product-workspace-screen.tsx` (media section grid)
