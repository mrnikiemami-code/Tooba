using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Models;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Ports;

namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Commands;

/// <summary>Handler for <see cref="SaveCheckoutIdentitySettingsCommand"/>.</summary>
public sealed class SaveCheckoutIdentitySettingsHandler
    : IRequestHandler<SaveCheckoutIdentitySettingsCommand, Result<CheckoutIdentitySettingsView>>
{
    private readonly IStoreCheckoutIdentitySettingsDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public SaveCheckoutIdentitySettingsHandler(IStoreCheckoutIdentitySettingsDirectory directory) =>
        _directory = directory;

    /// <inheritdoc />
    public Task<Result<CheckoutIdentitySettingsView>> Handle(
        SaveCheckoutIdentitySettingsCommand request,
        CancellationToken cancellationToken)
        => _directory.SaveAsync(request.Policy, cancellationToken);
}
