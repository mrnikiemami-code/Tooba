# Offer Golden reverify — TB-TMAR-FND-OBSERR-001-R4

Physical: six projects (`Domain`, `Application`, `Contracts`, `Infrastructure`, `Endpoints`, `Tests`). Physical-structure guard checks approved folders and rejects root dumping. No `OfferEndpointLocalizer`. No second seller endpoint mapper.

Domain: no Contracts project reference, no `PlatformHttpException`, semantic errors only, no Persian prose, no HTTP or persistence.

Application: MediatR handlers in per-use-case command/query folders. No `OfferDbContext`, no Host reference, seller scope on queries. Time and ids go through `IClock` and `IIdGenerator`.

Contracts: `Tooba.Offer.Contracts` namespace. No `namespace Tooba.Offer.Domain`. `OfferStatus` and `SalesChannel` in Contracts are the public DTOs. Domain owns `Tooba.Offer.Domain.ValueObjects` copies. No `TypeForwardedTo`. Call sites alias them (`DomainStatus`, `ContractStatus`). Ownership is split by layer, not ambiguous.

Infrastructure: Offer persistence and adapters only. No foreign Application project references. Development seed mutations stay here. Repair this task: `OfferDevelopmentSeedGateway` now takes `IClock` instead of `DateTimeOffset.UtcNow`.

Endpoints: thin `ISender` for Offer use cases. Price and inventory aliases call owner contracts, then `GetOfferQuery`. No local error or localization switch. No Host BFF.

Host: seller panel composer and extracted surfaces do not use `OfferDbContext` or mutate Offer aggregates. Guard `Host_production_sources_do_not_reference_offer_persistence` passed.

Source: Offer production handwritten files are at or under 800 lines (guard `No_offer_production_source_exceeds_800_loc`).

Verdict: COMPLETE_REFERENCE_PATTERN after the clock repair. Not a ceremonial pass: the seed bypass was a real Golden defect and was fixed before tests were accepted.
