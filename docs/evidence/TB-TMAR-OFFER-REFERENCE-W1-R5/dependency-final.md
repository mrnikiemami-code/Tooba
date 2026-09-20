# dependency-final

## Tooba.Offer.Domain
- ../../../BuildingBlocks/Tooba.BuildingBlocks/Tooba.BuildingBlocks.csproj

## Tooba.Offer.Application
- ../../../BuildingBlocks/Tooba.BuildingBlocks/Tooba.BuildingBlocks.csproj
- ../../Catalog/Tooba.Catalog.Contracts/Tooba.Catalog.Contracts.csproj
- ../../Inventory/Tooba.Inventory.Contracts/Tooba.Inventory.Contracts.csproj
- ../../Party/Tooba.Party.Contracts/Tooba.Party.Contracts.csproj
- ../../Pricing/Tooba.Pricing.Contracts/Tooba.Pricing.Contracts.csproj
- ../Tooba.Offer.Domain/Tooba.Offer.Domain.csproj
- ../Tooba.Offer.Contracts/Tooba.Offer.Contracts.csproj

## Tooba.Offer.Contracts
- ../../../BuildingBlocks/Tooba.BuildingBlocks/Tooba.BuildingBlocks.csproj

## Tooba.Offer.Infrastructure
- ../Tooba.Offer.Application/Tooba.Offer.Application.csproj
- ../../Tooba.ModuleContracts/Tooba.ModuleContracts.csproj
- ../../../BuildingBlocks/Tooba.Persistence/Tooba.Persistence.csproj

## Tooba.Offer.Endpoints
- ../Tooba.Offer.Application/Tooba.Offer.Application.csproj
- ../Tooba.Offer.Contracts/Tooba.Offer.Contracts.csproj
- ../../Inventory/Tooba.Inventory.Contracts/Tooba.Inventory.Contracts.csproj
- ../../Pricing/Tooba.Pricing.Contracts/Tooba.Pricing.Contracts.csproj
- ../../../BuildingBlocks/Tooba.BuildingBlocks/Tooba.BuildingBlocks.csproj

Rules check: Domain has no Contracts/Application; Endpoints has no Infrastructure; Infrastructure has no foreign Application.
