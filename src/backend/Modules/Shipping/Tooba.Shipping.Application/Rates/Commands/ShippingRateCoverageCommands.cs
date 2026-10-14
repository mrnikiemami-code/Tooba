using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Admin;

namespace Tooba.Shipping.Application.Rates.Commands;

/// <summary>
/// ایجاد سیاست نرخ ارسال برای یک کد خدمت. <paramref name="ServiceCode"/> از مسیر می‌آید و مرجع
/// قطعی است؛ هر کد خدمت دیگری در بدنه توسط اعتبارسنجی رد می‌شود.
/// </summary>
public sealed record CreateShippingRatePolicyCommand(string ServiceCode, ShippingRatePolicyWriteRequest Request)
    : IRequest<Result<ShippingRatePolicyAdminItem>>;

/// <summary>هندلر ایجاد سیاست نرخ.</summary>
public sealed class CreateShippingRatePolicyHandler(IShippingRateCoverageDirectory directory)
    : IRequestHandler<CreateShippingRatePolicyCommand, Result<ShippingRatePolicyAdminItem>>
{
    /// <inheritdoc />
    public Task<Result<ShippingRatePolicyAdminItem>> Handle(
        CreateShippingRatePolicyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingAdminOperation.ExecuteAsync(
            () => directory.CreateRateAsync(request.ServiceCode, request.Request, cancellationToken));
    }
}

/// <summary>ویرایش سیاست نرخ موجود.</summary>
public sealed record UpdateShippingRatePolicyCommand(
    Guid RatePolicyId,
    string ServiceCode,
    ShippingRatePolicyWriteRequest Request) : IRequest<Result<ShippingRatePolicyAdminItem>>;

/// <summary>هندلر ویرایش سیاست نرخ.</summary>
public sealed class UpdateShippingRatePolicyHandler(IShippingRateCoverageDirectory directory)
    : IRequestHandler<UpdateShippingRatePolicyCommand, Result<ShippingRatePolicyAdminItem>>
{
    /// <inheritdoc />
    public Task<Result<ShippingRatePolicyAdminItem>> Handle(
        UpdateShippingRatePolicyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingAdminOperation.ExecuteAsync(
            () => directory.UpdateRateAsync(
                request.RatePolicyId,
                request.ServiceCode,
                request.Request,
                cancellationToken));
    }
}

/// <summary>فعال/غیرفعال کردن سیاست نرخ.</summary>
public sealed record SetShippingRatePolicyActiveCommand(Guid RatePolicyId, string ServiceCode, bool IsActive)
    : IRequest<Result<ShippingRatePolicyAdminItem>>;

/// <summary>هندلر تغییر وضعیت سیاست نرخ.</summary>
public sealed class SetShippingRatePolicyActiveHandler(IShippingRateCoverageDirectory directory)
    : IRequestHandler<SetShippingRatePolicyActiveCommand, Result<ShippingRatePolicyAdminItem>>
{
    /// <inheritdoc />
    public Task<Result<ShippingRatePolicyAdminItem>> Handle(
        SetShippingRatePolicyActiveCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingAdminOperation.ExecuteAsync(
            () => directory.SetRateActiveAsync(
                request.RatePolicyId,
                request.ServiceCode,
                request.IsActive,
                cancellationToken));
    }
}

/// <summary>ایجاد مجوز پوشش صریح برای یک کد خدمت.</summary>
public sealed record CreateShippingCoverageGrantCommand(
    string ServiceCode,
    ShippingCoverageGrantWriteRequest Request) : IRequest<Result<ShippingCoverageGrantAdminItem>>;

/// <summary>هندلر ایجاد مجوز پوشش.</summary>
public sealed class CreateShippingCoverageGrantHandler(IShippingRateCoverageDirectory directory)
    : IRequestHandler<CreateShippingCoverageGrantCommand, Result<ShippingCoverageGrantAdminItem>>
{
    /// <inheritdoc />
    public Task<Result<ShippingCoverageGrantAdminItem>> Handle(
        CreateShippingCoverageGrantCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingAdminOperation.ExecuteAsync(
            () => directory.CreateGrantAsync(request.ServiceCode, request.Request, cancellationToken));
    }
}

/// <summary>فعال/غیرفعال کردن مجوز پوشش.</summary>
public sealed record SetShippingCoverageGrantActiveCommand(Guid CoverageGrantId, string ServiceCode, bool IsActive)
    : IRequest<Result<ShippingCoverageGrantAdminItem>>;

/// <summary>هندلر تغییر وضعیت مجوز پوشش.</summary>
public sealed class SetShippingCoverageGrantActiveHandler(IShippingRateCoverageDirectory directory)
    : IRequestHandler<SetShippingCoverageGrantActiveCommand, Result<ShippingCoverageGrantAdminItem>>
{
    /// <inheritdoc />
    public Task<Result<ShippingCoverageGrantAdminItem>> Handle(
        SetShippingCoverageGrantActiveCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingAdminOperation.ExecuteAsync(
            () => directory.SetGrantActiveAsync(
                request.CoverageGrantId,
                request.ServiceCode,
                request.IsActive,
                cancellationToken));
    }
}

/// <summary>خواندن نرخ‌ها و مجوزهای یک کد خدمت برای نویسندگی ادمین.</summary>
public sealed record GetShippingRatesAndCoverageQuery(string ServiceCode)
    : IRequest<Result<ShippingRatesAndCoverageView>>;

/// <summary>هندلر خواندن نرخ و پوشش یک خدمت.</summary>
public sealed class GetShippingRatesAndCoverageHandler(IShippingRateCoverageDirectory directory)
    : IRequestHandler<GetShippingRatesAndCoverageQuery, Result<ShippingRatesAndCoverageView>>
{
    /// <inheritdoc />
    public Task<Result<ShippingRatesAndCoverageView>> Handle(
        GetShippingRatesAndCoverageQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingAdminOperation.ExecuteAsync(() => directory.ListAsync(request.ServiceCode, cancellationToken));
    }
}
