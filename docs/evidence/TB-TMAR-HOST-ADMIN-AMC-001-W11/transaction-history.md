# Transaction / history — W11

## Apply atomicity

`ProductVariantDirectory.ApplyMatrixAsync` uses `BeginTransactionAsync` covering:

- axis replacement
- variant create / reactivate (archived→Draft) / archive
- patches
- default selection / single-default enforce
- ProductHistory EventVariantsChanged
- SaveChanges

Rollback on failure; no weakened atomicity.

## History

`QueueProductHistory(..., EventVariantsChanged, SectionVariants, SummaryVariantsFa, ...)` with actor from `ICatalogActorContext`.
