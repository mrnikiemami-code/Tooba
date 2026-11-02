using Tooba.Order.Contracts.Shipping;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Carriers;

namespace Tooba.Shipping.Application.Carriers.Ports;

/// <summary>
/// Unified carrier execution readiness for capabilities GET and Create preflight (no outbound provider HTTP).
/// Order facts own payment/cancelled eligibility; Shipping owns account/operations/sender/package completion.
/// </summary>
public interface IShippingCarrierExecutionReadiness
{
    Task<ShippingGroupCarrierExecutionCapabilityView> BuildCapabilityAsync(
        ShippingAuthoringOwnerScope owner,
        OrderCommittedCarrierShipmentFacts facts,
        CancellationToken cancellationToken);

    /// <summary>
    /// Same authority as capabilities; returns failure code when Create must refuse before provider call.
    /// </summary>
    Task<(bool Allowed, string? FailureCode, ShippingCarrierShipmentPartyInput? Sender, ShippingCarrierShipmentPackageInput? Package)>
        EvaluateCreateAsync(
            ShippingAuthoringOwnerScope owner,
            OrderCommittedCarrierShipmentFacts facts,
            ShippingCarrierShipmentPartyInput? requestedSender,
            CancellationToken cancellationToken);
}
