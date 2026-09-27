# Transaction / history — W12

## Replace transaction (preserved)

`BeginTransactionAsync` all-or-nothing across:

- Additional→Primary promotion
- old Primary removal / new Primary add
- orphan attribute value removal only
- invalid ProductVariantAxes removal
- variant Draft downgrade + default clearing (no hard delete)
- safety unpublish + EventUnpublished
- EventCategoryChanged with before/after summaries
- SaveChanges + Commit; Rollback on failure

## History

- `ProductHistoryRules.EventCategoryChanged` / `EventUnpublished`
- Actor from `ICatalogActorContext` via module binding
- Summaries via `ProductHistoryRules` / `ProductPublishRules` (authority not moved into validators)
