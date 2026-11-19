# TB-281-R4 COMPLETION LEDGER

Expected base: `14b2395eea3585b7e12af71d135c84c77ce7f9c2` (docs issuance `dbb612d6d`).

| Remaining requirement (from R3 OPEN) | Implementation | Proving test | Final status |
| --- | --- | --- | --- |
| Remove/reorder version+occurrence wire | Endpoint body → command/validator/composer/directory | `StoryMarketplace281R4TransportTests.Remove_and_reorder…` | LANDED |
| Admin schedule content gate | AdminSetScheduleAsync → EnsureStoredContentAuthorizedAsync | `Admin_set_schedule_requires_stored_content_authorization` | LANDED |
| Platform product CTA | EnsurePublishedExistsAsync vs shop ownership | `Platform_product_cta_uses_catalog_validity…` | LANDED |
| Non-empty occurrence on mutations | RequireOccurrenceKey + composites | Transport + Lifecycle | LANDED |
| FE version/occurrence transport | story-api + StoryManagementScreen | tsc 0 | LANDED transport (UI NOT ISSUED) |
| all-route-matrix no OPEN | updated matrix | all-route-matrix.md | LANDED |
| Media SaveChanges fail injection | RecordingStore + interceptor tests | `MediaStorySellerUpload281R4Tests` | LANDED |
| Exact PG unique identity | SqlState+Schema+Table+Constraint | MediaDirectory.Story + R4 | LANDED |
| Ambient tx refuse before store | CurrentTransaction check | C3 relational + InMemory note | LANDED |
| PG C1 items/visibility/isolation | StoryMarketplace281R4RelationalTests.C1 | rollback-only | LANDED |
| PG C2 occurrence PK + stale | C2 | rollback-only | LANDED |
| PG C3 media relational | C3 | rollback-only | LANDED |
| Host skip-bootstrap refresh | process-only | RECEIPTS | LANDED |
