# Outbox correlation — TB-TMAR-FND-OBSERR-001-R2

## Persisted today

`OutboxMessage.CorrelationId` (max 128) — already in schema across modules.

## Write path (`OutboxSaveChangesInterceptor`)

- Prefers `CorrelationIdContext.Current` / domain metadata via `MessagingCorrelation.ResolveForPublish`
- No longer uses `commerce.TraceId` as CorrelationId stand-in

## Dispatcher

- Restores `CorrelationIdContext.BeginScope` from persisted CorrelationId (or EventId for legacy null rows)
- Ensures deserialized `EventMetadata.CorrelationId` is filled when legacy null
- Publish inherits ambient correlation + Activity tags

## TraceParent columns

**Deferred.** Additive TraceParent/TraceState across every module outbox table would require broad migrations. CorrelationId remains the durable join key; W3C parent link is not faked when original Activity is gone. Documented residual for later additive migration if product requires cross-delay parent linking.
