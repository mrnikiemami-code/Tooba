using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Models;

namespace Tooba.Catalog.Application.Settings.CheckoutAbuse.Queries;

/// <summary>Admin GET checkout-abuse settings.</summary>
public sealed record GetCheckoutAbuseSettingsQuery : IRequest<Result<CheckoutAbuseSettingsView>>;
