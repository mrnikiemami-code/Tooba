# Foundation adoption scan

FulfillmentDirectory / ReturnDirectory / ReturnEligibilityEvaluator / ShippingServiceDirectory:
- IClock injected (no UtcNow bypass in production sources)
- IIdGenerator injected; Domain Create accepts Guid / Func<Guid>
- EnsureDispatched(DateTimeOffset now)
- No Guid.NewGuid / UuidV7.New / DateTimeOffset.UtcNow in Fulfillment/Returns production (guards enforce)

Instrumentation remains ActivitySource pattern without raw StartActivity in module production sources.
