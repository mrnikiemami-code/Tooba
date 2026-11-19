# TB-281-R3 COMPLETION LEDGER

Expected base: `4c086d32346ce2b6f695c254d7fa58db98c4f322` (docs issuance `7b3e105b1`).

| Remaining requirement | Implementation | Proving test | Final status |
| --- | --- | --- | --- |
| Shared admin/seller media+CTA gates | AdminUpdate/Add/UpdateItem + AdminCreate bind; EnsureStoredContentAuthorizedAsync | `StoryMarketplace281R3DirectoryTests.Admin_for_shop_update_and_item_use_same_media_and_cta_gates_as_seller` | LANDED |
| Submit version+occurrence | SellerSubmitAsync + command/endpoint/validator | `Submit_requires_version_and_replays_exact_occurrence` | LANDED |
| Item hash includes storyId + cross-target | HashItemIntent(storyId); TryReplay expectedStoryId | `Add_item_occurrence_same_key_on_other_story_conflicts` | LANDED |
| Catalog public seller id | ICatalogPublicSellerAttributionLookup + EnrichPublicAsync | `Public_enrich_uses_catalog_public_seller_id_when_available` | LANDED (controlled port) |
| Media recording-store suite | MediaDirectory.UploadForSeller + RecordingStore | `MediaStorySellerUpload281R3Tests` 5/5 | LANDED InMemory; PG/race injection OPEN |
| Occurrence tenant PK | StoryDbContext + migration Tb281R3… | Migration apply + model | LANDED schema; PG concurrency OPEN |
| stop→return→edit→resubmit | Directory cycle with versions | `Stop_return_edit_resubmit_cycle_with_versions` | LANDED |
| PG rollback G1–G7 | — | — | OPEN |
| Full admin composite occurrence wire | approve-and-* still no occurrence body | matrix | OPEN partial |
| Host skip-bootstrap refresh | process env | RECEIPTS | LANDED |
