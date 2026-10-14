using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Geography.Contracts.Admin;
using Tooba.Geography.Contracts.Places;

namespace Tooba.Geography.Endpoints.Admin;

/// <summary>
/// مجوزدهی ادمین برای خواندن فهرست مکان‌نگاری. درز انتزاعی است تا Host همان مجوزدهی موجود پنل را
/// تزریق کند (پیاده‌سازی پیش‌فرض: <see cref="GeographyAdminAuthorizer"/>).
/// </summary>
public interface IGeographyAdminAuthorizer
{
    /// <summary>دسترسی را بررسی می‌کند و شناسهٔ عامل را برمی‌گرداند.</summary>
    /// <param name="context">زمینهٔ HTTP.</param>
    /// <param name="permission">شناسهٔ مجوز موجود پلتفرم.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Guid> RequireAuthorizedAsync(HttpContext context, string permission, CancellationToken cancellationToken);
}

/// <summary>
/// شناسهٔ مجوز مصرفی این مسیرهای فقط-خواندنی. هیچ مجوز تازه‌ای ساخته نمی‌شود: این خواندن، ورودی
/// سطح ادمین «خدمات ارسال» است که از قبل با مجوزهای Fulfillment محافظت می‌شود، پس همان شناسهٔ
/// موجود دوباره استفاده می‌شود (نه یک قابلیت اختراعی و نه مسیر بی‌مجوز).
/// </summary>
public static class GeographyAdminPermissions
{
    /// <summary>مجوز خواندن (موجود در کاتالوگ پلتفرم).</summary>
    public const string Read = "fulfillment.view";
}

/// <summary>
/// مسیرهای فقط-خواندنی ادمین برای انتخاب شناسهٔ پایدار مکان. خروجی همیشه شناسهٔ GUID پایدار و کد را
/// برمی‌گرداند؛ برچسب از درز محلی‌سازی می‌آید و در نبود ترجمه به کد برمی‌گردد. هیچ نوشتنی وجود ندارد.
/// </summary>
public static class GeographyAdminEndpoints
{
    /// <summary>مسیرهای خواندن مکان‌نگاری را زیر گروه ادمین می‌نگارد.</summary>
    /// <param name="group">گروه مسیر ادمین.</param>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/geography/countries", ListCountriesAsync);
        group.MapGet("/geography/countries/{countryId:guid}/administrative-areas", ListAreasAsync);
        group.MapGet("/geography/countries/{countryId:guid}/cities", ListCitiesAsync);
        group.MapGet("/geography/cities/{cityId:guid}/service-areas", ListServiceAreasAsync);
    }

    private static async Task<IResult> ListCountriesAsync(
        IGeographyLookup lookup,
        IGeographyLabelLookup labels,
        IGeographyAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken,
        Guid? languageId = null)
    {
        await authorizer.RequireAuthorizedAsync(context, GeographyAdminPermissions.Read, cancellationToken);
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
        IGeographyAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken,
        Guid? languageId = null)
    {
        await authorizer.RequireAuthorizedAsync(context, GeographyAdminPermissions.Read, cancellationToken);
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

    private static async Task<IResult> ListCitiesAsync(
        Guid countryId,
        IGeographyLookup lookup,
        IGeographyLabelLookup labels,
        IGeographyAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken,
        Guid? administrativeAreaId = null,
        Guid? languageId = null)
    {
        await authorizer.RequireAuthorizedAsync(context, GeographyAdminPermissions.Read, cancellationToken);
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

    private static async Task<IResult> ListServiceAreasAsync(
        Guid cityId,
        IGeographyLookup lookup,
        IGeographyLabelLookup labels,
        IGeographyAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken,
        Guid? languageId = null)
    {
        await authorizer.RequireAuthorizedAsync(context, GeographyAdminPermissions.Read, cancellationToken);
        var rows = await lookup.ListServiceAreasAsync(cityId, cancellationToken);
        var texts = await labels.GetServiceAreaLabelsAsync(
            rows.Select(x => x.ServiceAreaId).ToList(),
            languageId,
            cancellationToken);
        return api.From(Result.Success(rows
            .Select(x => new GeographyPlaceOption(
                x.ServiceAreaId,
                x.Code,
                Label(texts, x.ServiceAreaId, x.Code),
                x.CityId,
                x.IsActive))
            .ToList()));
    }

    /// <summary>در نبود ترجمه، کد پایدار جانشین برچسب می‌شود؛ هیچ نام ساختگی تولید نمی‌شود.</summary>
    private static string Label(IReadOnlyDictionary<Guid, string> texts, Guid id, string code) =>
        texts.TryGetValue(id, out var label) && !string.IsNullOrWhiteSpace(label) ? label : code;
}

/// <summary>ترکیب نازک HTTP برای Geography: نگاشت مسیرهای فقط-خواندنی ادمین.</summary>
public static class GeographyEndpointModule
{
    /// <summary>مسیرهای ادمین Geography را زیر <c>/v1/admin</c> می‌نگارد.</summary>
    /// <param name="app">سازندهٔ مسیر.</param>
    public static IEndpointRouteBuilder MapGeographyEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var admin = app.MapGroup("/v1/admin");
        GeographyAdminEndpoints.Map(admin);
        return app;
    }
}
