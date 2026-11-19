# Route → command → directory → version → occurrence (R4)

Reads/queries do not require occurrence. Every mutation below requires non-empty occurrence unless create-n/a noted; version required where stated.

| Route | Command | Directory | Version | Occurrence | Test |
| --- | --- | --- | --- | --- | --- |
| POST /v1/seller/stories | CreateSellerStoryDraft | SellerCreateDraftAsync | n/a create | required | R4 + Foundation |
| PUT /v1/seller/stories/{id} | UpdateSellerStory | SellerUpdateAsync | required | required | Domain/R4 |
| POST …/submit | SubmitSellerStory | SellerSubmitAsync | required | required | R3+R4 |
| POST …/stop | StopSellerStory | SellerStopAsync | required | required | R2+R4 |
| POST …/return-to-draft | ReturnSellerStoryToDraft | SellerReturnToDraftAsync | required | required | R2+R4 |
| POST …/items | AddSellerStoryItem | SellerAddItemAsync | required | required | R3+R4 |
| PUT …/items/{itemId} | UpdateSellerStoryItem | SellerUpdateItemAsync | required | required | R4 wire |
| DELETE …/items/{itemId} | RemoveSellerStoryItem | SellerRemoveItemAsync | required | required | R4 Transport |
| PUT …/items/reorder | ReorderSellerStoryItems | SellerReorderItemsAsync | required | required | R4 Transport |
| POST /v1/seller/stories/query | QuerySellerStories | SellerQueryAsync | n/a read | — | R2 |
| GET seller/admin/public lists | *Query | * | n/a read | — | retained |
| POST /v1/admin/stories | CreateAdminStory | AdminCreateAsync | n/a create | required | R4 |
| POST admin create-for-shop | CreateAdminStoryForShop | AdminCreateForShopAsync | n/a create | required | R4 |
| PUT /v1/admin/stories/{id} | UpdateAdminStory | AdminUpdateAsync | required | required | R3+R4 |
| PUT /v1/admin/stories/reorder | ReorderAdminStories | AdminReorderAsync | per-row ExpectedVersions | required | R4 Transport |
| POST …/enable | EnableAdminStory | AdminEnableAsync | required | required | R4 |
| POST …/disable | DisableAdminStory | AdminDisableAsync | required | required | R4 |
| POST …/schedule | ScheduleAdminStory | AdminSetScheduleAsync | required | required + content gate | R4 Transport |
| POST …/approve | ApproveAdminStory | AdminApproveAsync | required | required | R4 |
| POST …/approve-and-publish | ApproveAndPublish | AdminApproveAndPublishAsync | required | required | R4 |
| POST …/approve-and-schedule | ApproveAndSchedule | AdminApproveAndScheduleAsync | required | required | R4 |
| POST …/reject | RejectAdminStory | AdminRejectAsync | required | required | R4 |
| POST …/items | AddAdminStoryItem | AdminAddItemAsync | required | required | R3+R4 |
| PUT …/items/{itemId} | UpdateAdminStoryItem | AdminUpdateItemAsync | required | required | R3+R4 |
| DELETE …/items/{itemId} | RemoveAdminStoryItem | AdminRemoveItemAsync | required | required | R4 Transport |
| PUT …/items/reorder | ReorderAdminStoryItems | AdminReorderItemsAsync | required | required | R4 Transport |

OPEN cells: **none** for issued mutation routes.
