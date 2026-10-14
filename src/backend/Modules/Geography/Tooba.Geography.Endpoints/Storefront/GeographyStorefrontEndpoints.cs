using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Admin;
using Tooba.Geography.Contracts.Places;

namespace Tooba.Geography.Endpoints.Storefront;

/// <summary>
/// مسیرهای فقط-خواندنی <b>عمومی</b> مکان‌نگاری برای مشتری فروشگاه.
/// </summary>
/// <remarks>
/// <para>
/// <b>چرا این مسیر لازم است.</b> صفحهٔ ارسال فروشگاه امروز فقط دو فهرست ثابت استان/شهر را از یک
/// کاتالوگ ایستا می‌خواند و هیچ شناسهٔ پایدار Geography ندارد؛ در نتیجه هویت مقصد مشتری هرگز از سطح
/// استان دقیق‌تر نمی‌شود. برای اینکه مشتری بتواند کشور/استان/شهر را با <em>شناسهٔ پایدار</em> انتخاب
/// کند، به یک خواندن عمومی و محدود نیاز است.
/// </para>
/// <para>
/// <b>چرا نسخهٔ ادمین بازاستفاده نشد.</b> مسیرهای <c>/v1/admin/geography/**</c> با مجوز
/// <c>fulfillment.view</c> محافظت می‌شوند. دادن آن مجوز به مشتری یعنی نشت قابلیت ادمین. پس این
/// مسیرها فقط همان <see cref="IGeographyLookup"/> فقط-خواندنی را مصرف می‌کنند و هیچ مجوزی نمی‌خواهند:
/// همان الگوی عمومی موجود در بقیهٔ مسیرهای <c>/v1/storefront</c>.
/// </para>
/// <para>
/// <b>مرز امنیت.</b> فقط <c>GET</c> است؛ هیچ نوشتنی، هیچ دسترسی مستقیم به لایهٔ ذخیره‌سازی و
/// هیچ پارامتر بی‌حدی وجود ندارد. خروجی فقط شناسهٔ پایدار، کد پایدار، برچسب و والد است؛ برچسب از درز
/// محلی‌سازی می‌آید و در نبود ترجمه به کد برمی‌گردد. ناحیهٔ خدمت عمداً ارائه نمی‌شود چون مسیر ارسال
/// مشتری به آن نیازی ندارد (کوچک‌ترین سطح لازم).
/// </para>
/// </remarks>
public static class GeographyStorefrontEndpoints
{
    /// <summary>مسیرهای عمومی خواندن مکان‌نگاری را زیر <c>/v1/storefront</c> می‌نگارد.</summary>
    /// <param name="app">سازندهٔ مسیر.</param>
    public static IEndpointRouteBuilder MapGeographyStorefrontEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var storefront = app.MapGroup("/v1/storefront");
        storefront.MapGet("/geography/places/countries", ListCountriesAsync);
        storefront.MapGet("/geography/places/countries/{countryId:guid}/administrative-areas", ListAreasAsync);
        storefront.MapGet("/geography/places/countries/{countryId:guid}/cities", ListCitiesAsync);
        return app;
    }

    private static async Task<IResult> ListCountriesAsync(
        IGeographyLookup lookup,
        IGeographyLabelLookup labels,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Guid? languageId = null)
    {
        var rows = await lookup.ListCountriesAsync(cancellationToken);
        var texts = await labels.GetCountryLabelsAsync(
            rows.Select(x => x.CountryId).ToList(),
            languageId,
            cancellationToken);
        return api.From(Result.Success(rows
            .Select(x => new GeographyPlaceOption(
                x.CountryId,
                x.CountryCode,
                Label(texts, x.CountryId, x.CountryCode),
                null,
                x.IsActive))
            .ToList()));
    }

    private static async Task<IResult> ListAreasAsync(
        Guid countryId,
        IGeographyLookup lookup,
        IGeographyLabelLookup labels,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Guid? languageId = null)
    {
        var rows = await lookup.ListAdministrativeAreasAsync(countryId, cancellationToken);
        var texts = await labels.GetAdministrativeAreaLabelsAsync(
            rows.Select(x => x.AdministrativeAreaId).ToList(),
            languageId,
            cancellationToken);
        return api.From(Result.Success(rows
            .Select(x => new GeographyPlaceOption(
                x.AdministrativeAreaId,
                x.Code,
                Label(texts, x.AdministrativeAreaId, x.Code),
                x.CountryId,
                x.IsActive))
            .ToList()));
    }

    /// <summary>
    /// شهرهای یک کشور، به‌صورت اختیاری محدود به یک ناحیهٔ اداری. پارامتر <paramref name="administrativeAreaId"/>
    /// همان قید «والد» است و سلسله‌مراتب را حفظ می‌کند: مشتری هرگز شهرِ استان دیگر را نمی‌بیند.
    /// </summary>
    private static async Task<IResult> ListCitiesAsync(
        Guid countryId,
        IGeographyLookup lookup,
        IGeographyLabelLookup labels,
        ApiResponseFactory api,
        CancellationToken cancellationToken,
        Guid? administrativeAreaId = null,
        Guid? languageId = null)
    {
        var rows = await lookup.ListCitiesAsync(countryId, administrativeAreaId, cancellationToken);
        var texts = await labels.GetCityLabelsAsync(
            rows.Select(x => x.CityId).ToList(),
            languageId,
            cancellationToken);
        return api.From(Result.Success(rows
            .Select(x => new GeographyPlaceOption(
                x.CityId,
                x.Code,
                Label(texts, x.CityId, x.Code),
                x.AdministrativeAreaId,
                x.IsActive))
            .ToList()));
    }

    /// <summary>در نبود ترجمه، کد پایدار جانشین برچسب می‌شود؛ هیچ نام ساختگی تولید نمی‌شود.</summary>
    private static string Label(IReadOnlyDictionary<Guid, string> texts, Guid id, string code) =>
        texts.TryGetValue(id, out var label) && !string.IsNullOrWhiteSpace(label) ? label : code;
}
