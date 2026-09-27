# Transaction / history — W10

## Bulk SetProductAttributes

- `BeginTransactionAsync` before applying values
- Each value via `ApplyProductAttributeValueAsync` (Result); on failure → Rollback + Failure
- On success: queue `ProductHistoryRules.EventAttributesChanged` / SectionAttributes / SummaryAttributesFa
- Single `SaveChangesAsync` then `CommitAsync`
- Catch → Rollback + rethrow unexpected

All-or-nothing preserved. No partial commits.

## Actor on history

`QueueProductHistory` uses scoped `ICatalogActorContext` populated by `CatalogActorRequestBinding`.

## Single set

No history event (same as pre-W10 Host `SetProductAttributeAsync`).
