using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.CheckoutAbuse.Models;

namespace Tooba.Catalog.Application.Settings.CheckoutAbuse.Commands;

/// <summary>Admin PUT checkout-abuse settings (nullable ints match Host transport).</summary>
public sealed record SaveCheckoutAbuseSettingsCommand(
    int? MaxOpenUnpaidOrdersPerCustomer,
    int? ReservationCommitWindowMinutes,
    int? MaxCheckoutCommitsPerCustomerInWindow,
    Guid ActorUserId) : IRequest<Result<CheckoutAbuseSettingsView>>;
