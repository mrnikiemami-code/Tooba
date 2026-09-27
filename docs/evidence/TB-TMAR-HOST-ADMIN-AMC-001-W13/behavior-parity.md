# Behavior parity — W13

| Route | Method | Success | Body notes |
|---|---|---|---|
| /v1/admin/products/{id}/media | GET | 200 list | Primary (not IsPrimary), DisplayOrder, AltText, MediaAssetId |
| .../media/readiness | GET | 200 | HasPrimaryImage, MediaCount, IsReady, MessageFa |
| .../media | POST | 201 list | no Location header (Host parity) |
| .../media/placeholder | POST | 201 list | no Location header |
| .../media/order | PUT | 200 list | OrderedMediaAssetIds |
| .../media/{assetId}/primary | PUT | 200 list | |
| .../media/{assetId} | PATCH | 200 list | AltText |
| .../media/{assetId} | DELETE | 200 list | unassign only |

Auth: ICatalogAdminAuthorizer replaces AdminPanelAccess; view-scope write deny preserved.
Ordering: primary first, then DisplayOrder.
Schema/frontend unchanged.
