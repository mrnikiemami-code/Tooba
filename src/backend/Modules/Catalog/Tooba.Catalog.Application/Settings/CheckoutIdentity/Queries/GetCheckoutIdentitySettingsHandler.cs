using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Models;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Ports;

namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Queries;

/// <summary>Handler for <see cref="GetCheckoutIdentitySettingsQuery"/>.</summary>
public sealed class GetCheckoutIdentitySettingsHandler
    : IRequestHandler<GetCheckoutIdentitySettingsQuery, Result<CheckoutIdentitySettingsView>>
{
    private readonly IStoreCheckoutIdentitySettingsDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetCheckoutIdentitySettingsHandler(IStoreCheckoutIdentitySettingsDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public async Task<Result<CheckoutIdentitySettingsView>> Handle(
        GetCheckoutIdentitySettingsQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await _directory.GetAsync(cancellationToken));
}
