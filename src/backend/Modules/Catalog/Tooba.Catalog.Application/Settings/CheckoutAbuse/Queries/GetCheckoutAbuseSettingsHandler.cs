using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Models;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Ports;

namespace Tooba.Catalog.Application.Settings.CheckoutAbuse.Queries;

/// <summary>Handler for <see cref="GetCheckoutAbuseSettingsQuery"/>.</summary>
public sealed class GetCheckoutAbuseSettingsHandler
    : IRequestHandler<GetCheckoutAbuseSettingsQuery, Result<CheckoutAbuseSettingsView>>
{
    private readonly IStoreCheckoutAbuseSettingsDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public GetCheckoutAbuseSettingsHandler(IStoreCheckoutAbuseSettingsDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result<CheckoutAbuseSettingsView>> Handle(
        GetCheckoutAbuseSettingsQuery request,
        CancellationToken cancellationToken)
        => Result.Success(await _directory.GetAsync(cancellationToken));
}
