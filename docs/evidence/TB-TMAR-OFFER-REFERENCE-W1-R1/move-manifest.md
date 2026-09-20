# move-manifest

## Physical moves (this repair)
- REMOVE_EMPTY src/backend/Modules/Offer/Tooba.Offer.Application/UseCases
- REMOVE_EMPTY artifacts folders under Offer Application/Domain/Infrastructure
- OLD: src/backend/Modules/Offer/Tooba.Offer.Domain/TypeForwarders.cs
  NEW: src/backend/Modules/Offer/Tooba.Offer.Domain/Aggregates/TypeForwarders.cs

## Namespace alignment (files already in correct folders; namespaces corrected)
- Domain/Aggregates/SellerOffer.cs: Tooba.Offer.Domain -> Tooba.Offer.Domain.Aggregates
- Domain/Events/OfferDomainEvents.cs: Tooba.Offer.Domain -> Tooba.Offer.Domain.Events
- Application/Ports/*: Tooba.Offer.Application -> Tooba.Offer.Application.Ports
- Contracts/Ports/*: Tooba.Offer.Contracts -> Tooba.Offer.Contracts.Ports
- Contracts/Dtos/OfferReference.cs, SellerOfferPanelDtos.cs: Tooba.Offer.Contracts -> Tooba.Offer.Contracts.Dtos
- Infrastructure/Adapters/OfferDirectory.cs: Tooba.Offer.Infrastructure -> Tooba.Offer.Infrastructure.Adapters
- Infrastructure/DependencyInjection/OfferModule.cs: Tooba.Offer.Infrastructure -> Tooba.Offer.Infrastructure.DependencyInjection
- Infrastructure/Outbox/OfferOutboxRegistration.cs: Tooba.Offer.Infrastructure -> Tooba.Offer.Infrastructure.Outbox
- Endpoints/Seller/*: Tooba.Offer.Endpoints -> Tooba.Offer.Endpoints.Seller
- Consumer usings updated across Host and dependent modules

Prior W1 already placed files into Aggregates/Ports/Adapters/etc.; R1 made namespaces and guards match disk.
