using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Contracts.Authoring;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Application.Authoring.Commands;

/// <summary>
/// جایگزینی تنظیمات روش یک پیش‌نویس مالک با بازبینی ریشهٔ مورد انتظار: دو انتخاب مستقل.
/// </summary>
/// <param name="Owner">مالک مجاز همین درخواست.</param>
/// <param name="DraftId">شناسهٔ پیش‌نویس از مسیر.</param>
/// <param name="Request">بدنهٔ زمان پرداخت کرایه و حالت محاسبه.</param>
public sealed record ReplaceShippingAuthoringMethodSettingsCommand(
    ShippingAuthoringOwnerScope Owner,
    Guid DraftId,
    ShippingAuthoringMethodSettingsReplaceRequest Request)
    : IRequest<Result<ShippingAuthoringMethodSettingsItem>>;

/// <summary>
/// هندلر PUT تنظیمات روش. هر دو انتخاب پیش از هر نوشتن در دامنه پیش‌سنجی می‌شوند و سپس تنظیمات +
/// بازبینی ریشه در <b>یک</b> SaveChanges ضمنی نوشته می‌شوند.
/// </summary>
/// <remarks>
/// هیچ مقداری از transport به پیش‌فرض تبدیل نمی‌شود: غیبت زمان پرداخت یا حالت محاسبه یک خطای پایدار
/// است، نه <c>Manual</c>/<c>Prepaid</c>. این نوشتن هیچ ردیف دستی، ارز، متن، جغرافیا یا برنامه را
/// تغییر نمی‌دهد و هیچ کرایه‌ای محاسبه نمی‌کند.
/// </remarks>
public sealed class ReplaceShippingAuthoringMethodSettingsHandler(IShippingAuthoringDraftDirectory directory)
    : IRequestHandler<ReplaceShippingAuthoringMethodSettingsCommand, Result<ShippingAuthoringMethodSettingsItem>>
{
    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringMethodSettingsItem>> Handle(
        ReplaceShippingAuthoringMethodSettingsCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Owner);
        ArgumentNullException.ThrowIfNull(request.Request);

        // Defense in depth for direct (non-HTTP) invocation: an absent choice is never "the default".
        if (request.Request.FreightPaymentMode is null || request.Request.CalculationMode is null)
        {
            return Result.Failure<ShippingAuthoringMethodSettingsItem>(
                new SemanticError(ShippingErrorCodes.AuthoringMethodSettingsInvalid));
        }

        try
        {
            var replaced = await directory.ReplaceMethodSettingsAsync(
                request.Owner,
                request.DraftId,
                request.Request.ExpectedRevision,
                request.Request.FreightPaymentMode,
                request.Request.CalculationMode,
                cancellationToken);
            return replaced.IsFailure
                ? Result.Failure<ShippingAuthoringMethodSettingsItem>(replaced.Errors)
                : Result.Success(ShippingAuthoringMethodSettingsWorkflow.Map(replaced.Value));
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringMethodSettingsItem>(exception.Error);
        }
    }
}
