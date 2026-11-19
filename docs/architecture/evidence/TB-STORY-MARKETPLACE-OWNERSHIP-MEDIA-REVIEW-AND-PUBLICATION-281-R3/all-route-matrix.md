# Route → command → directory → version → occurrence (R3)

| Route | Command | Directory | Version | Occurrence | Test |
| --- | --- | --- | --- | --- | --- |
| POST /v1/seller/stories | CreateSellerStoryDraft | SellerCreateDraftAsync | n/a create | optional | R2 create replay |
| PUT /v1/seller/stories/{id} | UpdateSellerStory | SellerUpdateAsync | required | — | domain |
| POST …/submit | SubmitSellerStory | SellerSubmitAsync | required | optional+replay | R3 submit |
| POST …/stop | StopSellerStory | SellerStopAsync | required | optional+replay | R2/R3 cycle |
| POST …/return-to-draft | ReturnSellerStoryToDraft | SellerReturnToDraftAsync | required | optional+replay | R2/R3 cycle |
| POST …/items | AddSellerStoryItem | SellerAddItemAsync | required | optional+storyId hash | R3 cross-target |
| PUT …/items/{itemId} | UpdateSellerStoryItem | SellerUpdateItemAsync | required | — | |
| DELETE …/items/{itemId} | RemoveSellerStoryItem | SellerRemoveItemAsync | required (opt param) | — | |
| POST /v1/seller/stories/query | QuerySellerStories | SellerQueryAsync | n/a read | — | R2 |
| Admin update/items | Admin* | bind+CTA+version | required | — | R3 admin gates |
| Admin approve / approve-and-* / enable | Admin* | EnsureStoredContentAuthorized | required | OPEN on wire for composites | R3 directory |
| Admin create-for-shop | AdminCreateForShop | cover+CTA+occ | n/a create | optional | source |

OPEN cells: admin approve-and-* / enable occurrence transport; remove/reorder full occurrence protocol; PG matrix.
