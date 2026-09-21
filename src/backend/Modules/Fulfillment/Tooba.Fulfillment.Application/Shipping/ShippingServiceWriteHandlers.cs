using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Shipping;

/// <summary>فرمان ایجاد سرویس ارسال.</summary>
/// <param name="Model">مدل.</param>
public sealed record CreateShippingServiceCommand(ShippingServiceWriteModel Model)
    : IRequest<Result<ShippingServiceDetailDto>>;

/// <summary>فرمان ویرایش سرویس ارسال.</summary>
/// <param name="ServiceId">شناسه.</param>
/// <param name="Model">مدل.</param>
public sealed record UpdateShippingServiceCommand(Guid ServiceId, ShippingServiceWriteModel Model)
    : IRequest<Result<ShippingServiceDetailDto>>;

/// <summary>فرمان غیرفعال‌سازی سرویس ارسال.</summary>
/// <param name="ServiceId">شناسه.</param>
public sealed record DeactivateShippingServiceCommand(Guid ServiceId) : IRequest<Result>;

/// <summary>فرمان seed کاتالوگ ارسال.</summary>
public sealed record EnsureShippingCatalogSeedCommand : IRequest<Result>;

/// <summary>Handler ایجاد سرویس ارسال.</summary>
public sealed class CreateShippingServiceHandler
    : IRequestHandler<CreateShippingServiceCommand, Result<ShippingServiceDetailDto>>
{
    private readonly IShippingServiceDirectory _directory;
    private readonly IShippingCatalogReader _catalog;

    /// <summary>Handler را می‌سازد.</summary>
    public CreateShippingServiceHandler(IShippingServiceDirectory directory, IShippingCatalogReader catalog)
    {
        _directory = directory;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Result<ShippingServiceDetailDto>> Handle(
        CreateShippingServiceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await _directory.CreateAsync(request.Model, cancellationToken);
            var detail = await _catalog.GetAsync(id, cancellationToken);
            return detail is null
                ? ShippingServiceSemantic.Failure<ShippingServiceDetailDto>(FulfillmentErrorCodes.ShippingServiceNotFound)
                : Result.Success(ShippingServiceSemantic.ToDetail(detail));
        }
        catch (InvalidOperationException ex) when (ShippingServiceSemantic.IsShippingSemantic(ex.Message))
        {
            return ShippingServiceSemantic.Failure<ShippingServiceDetailDto>(ex.Message);
        }
    }
}

/// <summary>Handler ویرایش سرویس ارسال.</summary>
public sealed class UpdateShippingServiceHandler
    : IRequestHandler<UpdateShippingServiceCommand, Result<ShippingServiceDetailDto>>
{
    private readonly IShippingServiceDirectory _directory;
    private readonly IShippingCatalogReader _catalog;

    /// <summary>Handler را می‌سازد.</summary>
    public UpdateShippingServiceHandler(IShippingServiceDirectory directory, IShippingCatalogReader catalog)
    {
        _directory = directory;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Result<ShippingServiceDetailDto>> Handle(
        UpdateShippingServiceCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _directory.UpdateAsync(request.ServiceId, request.Model, cancellationToken);
            var detail = await _catalog.GetAsync(request.ServiceId, cancellationToken);
            return detail is null
                ? ShippingServiceSemantic.Failure<ShippingServiceDetailDto>(FulfillmentErrorCodes.ShippingServiceNotFound)
                : Result.Success(ShippingServiceSemantic.ToDetail(detail));
        }
        catch (InvalidOperationException ex) when (ShippingServiceSemantic.IsShippingSemantic(ex.Message))
        {
            return ShippingServiceSemantic.Failure<ShippingServiceDetailDto>(ex.Message);
        }
    }
}

/// <summary>Handler غیرفعال‌سازی سرویس ارسال.</summary>
public sealed class DeactivateShippingServiceHandler : IRequestHandler<DeactivateShippingServiceCommand, Result>
{
    private readonly IShippingServiceDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public DeactivateShippingServiceHandler(IShippingServiceDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result> Handle(DeactivateShippingServiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _directory.DeactivateAsync(request.ServiceId, cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex) when (ShippingServiceSemantic.IsShippingSemantic(ex.Message))
        {
            return ShippingServiceSemantic.Failure(ex.Message);
        }
    }
}

/// <summary>Handler seed کاتالوگ ارسال.</summary>
public sealed class EnsureShippingCatalogSeedHandler : IRequestHandler<EnsureShippingCatalogSeedCommand, Result>
{
    private readonly IShippingServiceDirectory _directory;

    /// <summary>Handler را می‌سازد.</summary>
    public EnsureShippingCatalogSeedHandler(IShippingServiceDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public async Task<Result> Handle(EnsureShippingCatalogSeedCommand request, CancellationToken cancellationToken)
    {
        await _directory.EnsureSeedAsync(cancellationToken);
        return Result.Success();
    }
}
