# namespace-alignment

| Path | Namespace |
| --- | --- |
| Domain/Aggregates/SellerOffer.cs | Tooba.Offer.Domain.Aggregates |
| Domain/Events/OfferDomainEvents.cs | Tooba.Offer.Domain.Events |
| Application/Ports/* | Tooba.Offer.Application.Ports |
| Contracts/Ports/* | Tooba.Offer.Contracts.Ports |
| Contracts/Dtos/OfferReference.cs, SellerOfferPanelDtos.cs | Tooba.Offer.Contracts.Dtos |
| Contracts/Dtos/OfferStatus.cs, SalesChannel.cs | Tooba.Offer.Domain (owned contract type-forward targets) |
| Infrastructure/Adapters/* | Tooba.Offer.Infrastructure.Adapters |
| Infrastructure/Outbox/* | Tooba.Offer.Infrastructure.Outbox |
| Infrastructure/DependencyInjection/* | Tooba.Offer.Infrastructure.DependencyInjection |
| Infrastructure/Persistence/* | Tooba.Offer.Infrastructure.Persistence(.Configurations/.Migrations) |
| Infrastructure/Events/* | Tooba.Offer.Infrastructure.Events |
| Endpoints/Seller/* | Tooba.Offer.Endpoints.Seller |
| Endpoints/OfferEndpointModule.cs | Tooba.Offer.Endpoints |

Consumer usings updated across Host, Cart, Order, Inventory, Pricing, Promotion.
