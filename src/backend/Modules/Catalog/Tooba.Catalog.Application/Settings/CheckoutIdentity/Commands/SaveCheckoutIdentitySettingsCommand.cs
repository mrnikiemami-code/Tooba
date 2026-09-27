using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Models;

namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Commands;

/// <summary>Admin PUT checkout-identity settings (nullable Policy matches Host transport).</summary>
public sealed record SaveCheckoutIdentitySettingsCommand(
    string? Policy,
    Guid ActorUserId) : IRequest<Result<CheckoutIdentitySettingsView>>;
