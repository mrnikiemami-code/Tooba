# project-layout

## Offer projects on disk

| Project | Path | .csproj |
| --- | --- | --- |
| Domain | `src/backend/Modules/Offer/Tooba.Offer.Domain/` | yes |
| Application | `src/backend/Modules/Offer/Tooba.Offer.Application/` | yes |
| Contracts | `src/backend/Modules/Offer/Tooba.Offer.Contracts/` | yes |
| Infrastructure | `src/backend/Modules/Offer/Tooba.Offer.Infrastructure/` | yes |
| Endpoints | `src/backend/Modules/Offer/Tooba.Offer.Endpoints/` | yes |
| Tests | `src/backend/Modules/Offer/Tooba.Offer.Tests/` | yes |

## Physical responsibility folders (non-empty only)

- Domain: `Aggregates/`, `Events/` (+ `Aggregates/TypeForwarders.cs`)
- Application: `Ports/`
- Contracts: `Ports/`, `Dtos/`
- Infrastructure: `Persistence/` (+ Configurations/Migrations), `Adapters/`, `Outbox/`, `Events/`, `DependencyInjection/`
- Endpoints: `Seller/` + root `OfferEndpointModule.cs`
- Tests: `Domain/`, `Contracts/`, `Infrastructure/`, `Endpoints/`, `Architecture/`

Empty ceremonial folders removed (Application/UseCases and artifacts).
