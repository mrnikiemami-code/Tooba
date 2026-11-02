using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;

namespace Tooba.Shipping.Application.Carriers.Commands;

public sealed record UpsertShippingCarrierAccountCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid? AccountId,
    ShippingCarrierAccountUpsertRequest Request)
    : IRequest<Result<ShippingCarrierAccountView>>;

public sealed class UpsertShippingCarrierAccountHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<UpsertShippingCarrierAccountCommand, Result<ShippingCarrierAccountView>>
{
    public async Task<Result<ShippingCarrierAccountView>> Handle(
        UpsertShippingCarrierAccountCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);
        try
        {
            return await directory.UpsertAccountAsync(
                request.Owner,
                request.AccountId,
                request.Request,
                cancellationToken);
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingCarrierAccountView>(exception.Error);
        }
    }
}

public sealed record TestShippingCarrierConnectionCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid AccountId) : IRequest<Result<ShippingCarrierConnectionTestOutcome>>;

public sealed class TestShippingCarrierConnectionHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<TestShippingCarrierConnectionCommand, Result<ShippingCarrierConnectionTestOutcome>>
{
    public Task<Result<ShippingCarrierConnectionTestOutcome>> Handle(
        TestShippingCarrierConnectionCommand request,
        CancellationToken cancellationToken)
        => directory.TestConnectionAsync(request.Owner, request.AccountId, cancellationToken);
}

public sealed record SyncShippingCarrierReferencesCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid AccountId) : IRequest<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>>;

public sealed class SyncShippingCarrierReferencesHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<SyncShippingCarrierReferencesCommand, Result<IReadOnlyList<ShippingCarrierReferenceValueView>>>
{
    public Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> Handle(
        SyncShippingCarrierReferencesCommand request,
        CancellationToken cancellationToken)
        => directory.SyncReferencesAsync(request.Owner, request.AccountId, cancellationToken);
}

public sealed record UpsertShippingCarrierDestinationMappingCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid AccountId,
    ShippingCarrierDestinationMappingUpsertRequest Request)
    : IRequest<Result<ShippingCarrierDestinationMappingView>>;

public sealed class UpsertShippingCarrierDestinationMappingHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<UpsertShippingCarrierDestinationMappingCommand, Result<ShippingCarrierDestinationMappingView>>
{
    public Task<Result<ShippingCarrierDestinationMappingView>> Handle(
        UpsertShippingCarrierDestinationMappingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Request);
        return directory.UpsertDestinationMappingAsync(
            request.Owner,
            request.AccountId,
            request.Request,
            cancellationToken);
    }
}

public sealed record ReplaceShippingCarrierMethodBindingCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingCarrierMethodBindingReplaceRequest Request)
    : IRequest<Result<ShippingCarrierMethodBindingView>>;

public sealed class ReplaceShippingCarrierMethodBindingHandler(IShippingCarrierAccountDirectory directory)
    : IRequestHandler<ReplaceShippingCarrierMethodBindingCommand, Result<ShippingCarrierMethodBindingView>>
{
    public Task<Result<ShippingCarrierMethodBindingView>> Handle(
        ReplaceShippingCarrierMethodBindingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Request);
        return directory.ReplaceMethodBindingAsync(
            request.Owner,
            request.DraftId,
            request.Request,
            cancellationToken);
    }
}
