using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Places;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Infrastructure.Rates;

/// <summary>
/// اعتبارسنجی دامنهٔ مقصد روی درز فقط-خواندنی Geography (<see cref="IGeographyLookup.GetByIdsAsync"/>).
/// هیچ DbContext جغرافیایی و هیچ FK میان‌طرح‌واره‌ای اینجا نیست؛ فقط Contracts خوانده می‌شود.
/// </summary>
/// <remarks>
/// <b>TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1.</b> هر فراخوانی واقعی Geography با canonical
/// <see cref="IModuleCallTracer"/> به‌صورت یک span فرزند <c>module.Shipping.Geography.*</c> پوشش
/// داده می‌شود: مالکیت span با MediatR نیست، بنابراین span والدِ درخواست جای آن را نمی‌گیرد.
/// هیچ شناسهٔ مقصد/مختصات/PII به‌عنوان tag نوشته نمی‌شود و behavior/exception/cancellation عیناً
/// حفظ می‌شود.
/// </remarks>
public sealed class GeographyDestinationScopeValidator(
    IGeographyLookup geography,
    IModuleCallTracer moduleCallTracer)
    : IShippingDestinationScopeValidator
{
    private const string SourceModule = "Shipping";
    private const string TargetModule = "Geography";
    private const string ValidateOperation = "ValidateDestinationIdentifiers";

    /// <inheritdoc />
    public async Task<Result<ShippingDestinationScopeWrite>> ValidateAsync(
        ShippingDestinationScopeWrite scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

        using var moduleCall = moduleCallTracer.Begin(SourceModule, TargetModule, ValidateOperation);
        try
        {
            var validated = await geography.GetByIdsAsync(
                new DestinationIdentifiers(
                    scope.CountryId,
                    scope.AdministrativeAreaId,
                    scope.CityId,
                    scope.ServiceAreaId),
                cancellationToken);

            // null یعنی Geography این مقصد را نپذیرفت: شناسهٔ ناشناخته، غیرفعال، یا سلسله‌مراتب ناهم‌بسته.
            if (validated is null)
            {
                moduleCall.SetBusinessFailure(ShippingErrorCodes.DestinationScopeInvalid);
                return Result.Failure<ShippingDestinationScopeWrite>(
                    new SemanticError(ShippingErrorCodes.DestinationScopeInvalid));
            }

            moduleCall.SetOk();
            return Result.Success(scope);
        }
        catch (Exception exception)
        {
            // فقط نوع استثنا ثبت می‌شود؛ همان استثنا بدون تبدیل دوباره پرتاب می‌شود.
            moduleCall.SetError(exception);
            throw;
        }
    }
}
