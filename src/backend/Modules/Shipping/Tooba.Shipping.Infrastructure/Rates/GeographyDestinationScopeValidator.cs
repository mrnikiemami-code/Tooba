using Tooba.BuildingBlocks;
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
public sealed class GeographyDestinationScopeValidator(IGeographyLookup geography)
    : IShippingDestinationScopeValidator
{
    /// <inheritdoc />
    public async Task<Result<ShippingDestinationScopeWrite>> ValidateAsync(
        ShippingDestinationScopeWrite scope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);

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
            return Result.Failure<ShippingDestinationScopeWrite>(
                new SemanticError(ShippingErrorCodes.DestinationScopeInvalid));
        }

        return Result.Success(scope);
    }
}
