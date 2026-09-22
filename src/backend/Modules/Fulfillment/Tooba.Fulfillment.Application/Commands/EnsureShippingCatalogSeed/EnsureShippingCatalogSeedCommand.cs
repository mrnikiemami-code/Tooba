using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping;

namespace Tooba.Fulfillment.Application.Commands.EnsureShippingCatalogSeed;

public sealed record EnsureShippingCatalogSeedCommand : IRequest<Result>;

public sealed class EnsureShippingCatalogSeedHandler : IRequestHandler<EnsureShippingCatalogSeedCommand, Result>
{
    private readonly IShippingServiceDirectory _directory;
    public EnsureShippingCatalogSeedHandler(IShippingServiceDirectory directory) => _directory = directory;

    public async Task<Result> Handle(EnsureShippingCatalogSeedCommand request, CancellationToken cancellationToken)
    {
        await _directory.EnsureSeedAsync(cancellationToken);
        return Result.Success();
    }
}
