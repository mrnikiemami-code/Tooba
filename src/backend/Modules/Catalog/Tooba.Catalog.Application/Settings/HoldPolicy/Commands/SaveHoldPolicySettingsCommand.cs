using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.HoldPolicy.Models;

namespace Tooba.Catalog.Application.Settings.HoldPolicy.Commands;

/// <summary>Admin PUT hold-policy settings aggregate (nullable ints match Host transport).</summary>
public sealed record SaveHoldPolicySettingsCommand(
    int? CartPersistenceHours,
    int? OnlinePaymentHoldHours,
    int? ManualPaymentInitialHoldHours,
    int? ManualPaymentReviewHoldHours,
    IReadOnlyList<PaymentMethodHoldView>? Methods,
    int? InitialReservationHoldMinutes,
    int? RetryReservationHoldMinutes,
    int? MaxReservationCycles,
    Guid ActorUserId) : IRequest<Result<HoldPolicySettingsView>>;
