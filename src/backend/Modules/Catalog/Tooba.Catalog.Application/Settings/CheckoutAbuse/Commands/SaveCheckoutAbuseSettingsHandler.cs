using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Models;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Ports;
using Tooba.Catalog.Contracts.Errors;

namespace Tooba.Catalog.Application.Settings.CheckoutAbuse.Commands;

/// <summary>Handler for <see cref="SaveCheckoutAbuseSettingsCommand"/>.</summary>
public sealed class SaveCheckoutAbuseSettingsHandler
    : IRequestHandler<SaveCheckoutAbuseSettingsCommand, Result<CheckoutAbuseSettingsView>>
{
    private readonly IStoreCheckoutAbuseSettingsDirectory _directory;

    /// <summary>Creates the handler.</summary>
    public SaveCheckoutAbuseSettingsHandler(IStoreCheckoutAbuseSettingsDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<CheckoutAbuseSettingsView>> Handle(
        SaveCheckoutAbuseSettingsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.MaxOpenUnpaidOrdersPerCustomer is null
            || request.ReservationCommitWindowMinutes is null
            || request.MaxCheckoutCommitsPerCustomerInWindow is null)
        {
            return Task.FromResult(Result.Failure<CheckoutAbuseSettingsView>(
                new SemanticError(CatalogErrorCodes.CheckoutAbuseInvalid)));
        }

        return _directory.SaveAsync(
            request.MaxOpenUnpaidOrdersPerCustomer.Value,
            request.ReservationCommitWindowMinutes.Value,
            request.MaxCheckoutCommitsPerCustomerInWindow.Value,
            request.ActorUserId,
            cancellationToken);
    }
}
