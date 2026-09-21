# Golden guards final — TB-TMAR-FND-OBSERR-001-R4

`OfferArchitectureGuardTests` and `OfferPhysicalStructureGuardTests` enforce:

- Domain does not reference Contracts, Application, Infrastructure, or Endpoints
- No `TypeForwardedTo`
- Contracts sources do not declare `namespace Tooba.Offer.Domain`
- Infrastructure does not reference Catalog.Application or Party.Application
- `OfferDbContext` is absent from Offer Application, Endpoints, Host production, and foreign modules
- Host seller composer does not map Offer routes or mutate Offer aggregates
- `OfferEndpointLocalizer` is absent; `.resx` and `OfferErrorCatalogContributor` exist
- Seller endpoints do not parse `AcceptLanguage`, switch on `StartsWith("en")`, catch locally, or map `SemanticException`
- `SafeErrorMapper` has no `ClassifySemanticCode` and no `.not_found` / `cannot_activate` substring classification
- Every public Offer error constant has a descriptor and English resource (catalog test)
- No manual en/fa switch in the endpoint
- No `exception.Message` / `ex.Message` response shaping in the endpoint
- `TracingBehavior` registered once; one `AddOpenTelemetry()`
- No raw `StartActivity` or local `AsyncLocal` in Offer Application/Endpoints
- No Offer production file over 800 lines
- New this task: Offer production sources must not call `DateTime.UtcNow`, `DateTimeOffset.UtcNow`, or `Guid.NewGuid()`

`Tooba.Host.Tests` error and correlation fixtures are in collection `PostgresSerial` so two `WebApplicationFactory` hosts cannot run `PostgresDatabaseMigrator.GrantAccess` at the same time.

Verdict: PASS (57 Offer tests, 0 failed).
