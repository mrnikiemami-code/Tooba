using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutIdentity.Models;

namespace Tooba.Catalog.Application.Settings.CheckoutIdentity.Queries;

/// <summary>Admin GET checkout-identity settings.</summary>
public sealed record GetCheckoutIdentitySettingsQuery : IRequest<Result<CheckoutIdentitySettingsView>>;
