using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;

namespace Tooba.Shipping.Application.Carriers.Commands;

public sealed record CreateShippingCarrierShipmentCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid ActorUserId,
    ShippingCarrierShipmentExecuteRequest Request)
    : IRequest<Result<ShippingCarrierShipmentOperationView>>;

public sealed class CreateShippingCarrierShipmentHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<CreateShippingCarrierShipmentCommand, Result<ShippingCarrierShipmentOperationView>>
{
    public Task<Result<ShippingCarrierShipmentOperationView>> Handle(
        CreateShippingCarrierShipmentCommand request,
        CancellationToken cancellationToken)
        => operations.CreateAsync(request.Owner, request.ActorUserId, request.Request, cancellationToken);
}

public sealed record GetShippingCarrierShipmentCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid OperationId) : IRequest<Result<ShippingCarrierShipmentOperationView>>;

public sealed class GetShippingCarrierShipmentHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<GetShippingCarrierShipmentCommand, Result<ShippingCarrierShipmentOperationView>>
{
    public Task<Result<ShippingCarrierShipmentOperationView>> Handle(
        GetShippingCarrierShipmentCommand request,
        CancellationToken cancellationToken)
        => operations.GetAsync(request.Owner, request.OperationId, cancellationToken);
}

public sealed record TrackShippingCarrierShipmentCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid OperationId) : IRequest<Result<ShippingCarrierShipmentOperationView>>;

public sealed class TrackShippingCarrierShipmentHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<TrackShippingCarrierShipmentCommand, Result<ShippingCarrierShipmentOperationView>>
{
    public Task<Result<ShippingCarrierShipmentOperationView>> Handle(
        TrackShippingCarrierShipmentCommand request,
        CancellationToken cancellationToken)
        => operations.TrackAsync(request.Owner, request.OperationId, cancellationToken);
}

public sealed record CancelShippingCarrierShipmentCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid OperationId) : IRequest<Result<ShippingCarrierShipmentOperationView>>;

public sealed class CancelShippingCarrierShipmentHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<CancelShippingCarrierShipmentCommand, Result<ShippingCarrierShipmentOperationView>>
{
    public Task<Result<ShippingCarrierShipmentOperationView>> Handle(
        CancelShippingCarrierShipmentCommand request,
        CancellationToken cancellationToken)
        => operations.CancelAsync(request.Owner, request.OperationId, cancellationToken);
}

public sealed record LabelShippingCarrierShipmentCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid OperationId) : IRequest<Result<ShippingCarrierShipmentOperationView>>;

public sealed class LabelShippingCarrierShipmentHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<LabelShippingCarrierShipmentCommand, Result<ShippingCarrierShipmentOperationView>>
{
    public Task<Result<ShippingCarrierShipmentOperationView>> Handle(
        LabelShippingCarrierShipmentCommand request,
        CancellationToken cancellationToken)
        => operations.LabelAsync(request.Owner, request.OperationId, cancellationToken);
}

public sealed record SaveShippingCarrierShipmentPackageCompletionCommand(
    ShippingAuthoringOwnerScope Owner,
    ShippingCarrierShipmentPackageCompletionRequest Request)
    : IRequest<Result<ShippingGroupCarrierExecutionCapabilityView>>;

public sealed class SaveShippingCarrierShipmentPackageCompletionHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<SaveShippingCarrierShipmentPackageCompletionCommand, Result<ShippingGroupCarrierExecutionCapabilityView>>
{
    public Task<Result<ShippingGroupCarrierExecutionCapabilityView>> Handle(
        SaveShippingCarrierShipmentPackageCompletionCommand request,
        CancellationToken cancellationToken)
        => operations.SavePackageCompletionAsync(request.Owner, request.Request, cancellationToken);
}

public sealed record SaveShippingCarrierShipmentSenderCompletionCommand(
    ShippingAuthoringOwnerScope Owner,
    ShippingCarrierShipmentSenderCompletionRequest Request)
    : IRequest<Result<ShippingGroupCarrierExecutionCapabilityView>>;

public sealed class SaveShippingCarrierShipmentSenderCompletionHandler(IShippingCarrierShipmentOperations operations)
    : IRequestHandler<SaveShippingCarrierShipmentSenderCompletionCommand, Result<ShippingGroupCarrierExecutionCapabilityView>>
{
    public Task<Result<ShippingGroupCarrierExecutionCapabilityView>> Handle(
        SaveShippingCarrierShipmentSenderCompletionCommand request,
        CancellationToken cancellationToken)
        => operations.SaveSenderCompletionAsync(request.Owner, request.Request, cancellationToken);
}
