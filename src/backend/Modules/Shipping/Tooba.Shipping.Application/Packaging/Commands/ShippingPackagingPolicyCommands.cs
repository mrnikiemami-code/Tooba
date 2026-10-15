using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Packaging.Ports;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Packaging;

namespace Tooba.Shipping.Application.Packaging.Commands;

/// <summary>
/// ایجاد سیاست هزینهٔ بسته‌بندی برای یک کد خدمت. <paramref name="ServiceCode"/> از مسیر می‌آید و
/// مرجع قطعی است؛ هر کد خدمت دیگری در بدنه توسط اعتبارسنجی رد می‌شود.
/// </summary>
public sealed record CreateShippingPackagingPolicyCommand(
    string ServiceCode,
    ShippingPackagingPolicyWriteRequest Request)
    : IRequest<Result<ShippingPackagingPolicyAdminItem>>;

/// <summary>هندلر ایجاد سیاست بسته‌بندی.</summary>
public sealed class CreateShippingPackagingPolicyHandler(
    IShippingPackagingPolicyDirectory directory,
    IShippingDestinationScopeValidator scopeValidator,
    IShippingMethodOptionValidator optionValidator)
    : IRequestHandler<CreateShippingPackagingPolicyCommand, Result<ShippingPackagingPolicyAdminItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingPackagingPolicyAdminItem>> Handle(
        CreateShippingPackagingPolicyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // هویت روش (خدمت + گزینهٔ اختیاری) پیش از هر نوشتن با کاتالوگ واقعی Fulfillment سنجیده
        // می‌شود؛ یک ترکیب ساختگی هرگز به‌عنوان سیاست تجاری ثبت نمی‌شود.
        var methodValid = await optionValidator.ValidateAsync(
            request.ServiceCode,
            request.Request.OptionCode,
            cancellationToken);
        if (methodValid.IsFailure)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(methodValid.Errors);
        }

        // دامنهٔ مقصد پیش از هر نوشتن با فهرست مرجع Geography سنجیده می‌شود.
        var validated = await scopeValidator.ValidateAsync(request.Request.Scope, cancellationToken);
        if (validated.IsFailure)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(validated.Errors);
        }

        return await ShippingPackagingOperation.ExecuteAsync(
            () => directory.CreateAsync(request.ServiceCode, request.Request, cancellationToken));
    }
}

/// <summary>ویرایش سیاست هزینهٔ بسته‌بندی موجود.</summary>
public sealed record UpdateShippingPackagingPolicyCommand(
    Guid PackagingPolicyId,
    string ServiceCode,
    ShippingPackagingPolicyWriteRequest Request)
    : IRequest<Result<ShippingPackagingPolicyAdminItem>>;

/// <summary>هندلر ویرایش سیاست بسته‌بندی.</summary>
public sealed class UpdateShippingPackagingPolicyHandler(
    IShippingPackagingPolicyDirectory directory,
    IShippingMethodOptionValidator optionValidator)
    : IRequestHandler<UpdateShippingPackagingPolicyCommand, Result<ShippingPackagingPolicyAdminItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingPackagingPolicyAdminItem>> Handle(
        UpdateShippingPackagingPolicyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var methodValid = await optionValidator.ValidateAsync(
            request.ServiceCode,
            request.Request.OptionCode,
            cancellationToken);
        if (methodValid.IsFailure)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(methodValid.Errors);
        }

        return await ShippingPackagingOperation.ExecuteAsync(
            () => directory.UpdateAsync(
                request.PackagingPolicyId,
                request.ServiceCode,
                request.Request,
                cancellationToken));
    }
}

/// <summary>فعال/غیرفعال کردن سیاست هزینهٔ بسته‌بندی.</summary>
public sealed record SetShippingPackagingPolicyActiveCommand(
    Guid PackagingPolicyId,
    string ServiceCode,
    bool IsActive)
    : IRequest<Result<ShippingPackagingPolicyAdminItem>>;

/// <summary>هندلر تغییر وضعیت سیاست بسته‌بندی.</summary>
public sealed class SetShippingPackagingPolicyActiveHandler(IShippingPackagingPolicyDirectory directory)
    : IRequestHandler<SetShippingPackagingPolicyActiveCommand, Result<ShippingPackagingPolicyAdminItem>>
{
    /// <inheritdoc />
    public Task<Result<ShippingPackagingPolicyAdminItem>> Handle(
        SetShippingPackagingPolicyActiveCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingPackagingOperation.ExecuteAsync(
            () => directory.SetActiveAsync(
                request.PackagingPolicyId,
                request.ServiceCode,
                request.IsActive,
                cancellationToken));
    }
}

/// <summary>خواندن سیاست‌های بسته‌بندی یک کد خدمت برای نویسندگی ادمین.</summary>
public sealed record GetShippingPackagingPoliciesQuery(string ServiceCode)
    : IRequest<Result<ShippingPackagingPoliciesView>>;

/// <summary>هندلر خواندن سیاست‌های بسته‌بندی یک خدمت.</summary>
public sealed class GetShippingPackagingPoliciesHandler(IShippingPackagingPolicyDirectory directory)
    : IRequestHandler<GetShippingPackagingPoliciesQuery, Result<ShippingPackagingPoliciesView>>
{
    /// <inheritdoc />
    public Task<Result<ShippingPackagingPoliciesView>> Handle(
        GetShippingPackagingPoliciesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return ShippingPackagingOperation.ExecuteAsync(
            () => directory.ListAsync(request.ServiceCode, cancellationToken));
    }
}
