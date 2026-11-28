using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Results;
using Tooba.BuildingBlocks.Security;
using Tooba.Offer.Application.Offers.Commands.AssignOfferFulfillment;
using Tooba.Offer.Application.Offers.Queries.GetOfferShippingMethods;
using Tooba.Offer.Contracts.Errors;
using Tooba.Offer.Contracts.Dtos;

namespace Tooba.Offer.Endpoints.Admin;

/// <summary>
/// گیت مجوز انتساب انجام ارسال به آفر؛ همان گیت پنل مدیر به‌همراه مجوز موجود fulfillment.
/// </summary>
/// <remarks>
/// هیچ فهرست مجوز تازه‌ای ساخته نمی‌شود: انتساب مسئولیت ارسال دقیقاً یک عمل fulfillment است، پس همان
/// <c>fulfillment.manage</c> موجود دوباره استفاده می‌شود. درز مجوز فقط-مدیر است و هیچ مسیر فروشنده‌ای به آن
/// دسترسی ندارد؛ بنابراین فروشنده حق تازه‌ای برای اعلام مسئولیت Platform نمی‌گیرد.
/// </remarks>
public interface IOfferFulfillmentAdminAuthorizer
{
    /// <summary>مجوز نوشتن پیش از خواندن بدنه و پیش از هر dispatch.</summary>
    Task<Guid> RequireManageAsync(HttpContext context, CancellationToken cancellationToken);
}

/// <summary>پیاده‌سازی روی درزهای عمومی پلتفرم، بدون وابستگی به Host.</summary>
public sealed class OfferFulfillmentAdminAuthorizer(
    IAdminPanelAccess adminAccess,
    IAuthorizationService authorization,
    ICurrentEdition edition,
    ICurrentTenant tenant) : IOfferFulfillmentAdminAuthorizer
{
    /// <summary>مجوز موجود و دوباره‌استفاده‌شدهٔ نوشتن fulfillment.</summary>
    public const string Manage = "fulfillment.manage";

    /// <inheritdoc />
    public async Task<Guid> RequireManageAsync(HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var actor = await adminAccess.RequireAuthorizedAsync(context.Request, cancellationToken);
        var decision = await authorization.CanAsync(
            new AuthorizationCheck
            {
                Subject = AuthorizationSubject.ForUser(actor),
                Resource = new AuthorizationResource
                {
                    Type = AuthorizationObjectTypes.Permission,
                    Id = Manage,
                },
                Permission = AuthorizationRelations.Check,
                CallContext = ResolveCallContext(edition, tenant),
            },
            cancellationToken);

        if (decision.Kind == AuthorizationDecisionKind.Allow)
        {
            return actor;
        }

        if (decision.Kind == AuthorizationDecisionKind.Unavailable)
        {
            throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
        }

        throw new SemanticException(new SemanticError(FoundationErrorCodes.AdminAuthorizationDenied));
    }

    /// <summary>زمینهٔ مجوز فقط از نصب trusted؛ هیچ Edition/Tenant از کلاینت پذیرفته نمی‌شود.</summary>
    private static AuthorizationCallContext ResolveCallContext(ICurrentEdition edition, ICurrentTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(edition);
        ArgumentNullException.ThrowIfNull(tenant);

        return edition.Current?.Edition switch
        {
            ToobaEdition.SingleStore => SingleStore(tenant),
            ToobaEdition.Marketplace => Marketplace(tenant),
            _ => throw Unavailable(),
        };
    }

    private static AuthorizationCallContext SingleStore(ICurrentTenant tenant)
    {
        var current = tenant.Current;
        var tenantId = current?.TenantId.Value;
        if (current is null
            || current.Status != TenantStatus.Active
            || string.IsNullOrWhiteSpace(tenantId))
        {
            throw Unavailable();
        }

        return new AuthorizationCallContext
        {
            Edition = ToobaEdition.SingleStore,
            TenantId = tenantId.Trim(),
        };
    }

    private static AuthorizationCallContext Marketplace(ICurrentTenant tenant)
    {
        if (tenant.Current is not null)
        {
            throw Unavailable();
        }

        return new AuthorizationCallContext
        {
            Edition = ToobaEdition.Marketplace,
            TenantId = null,
        };
    }

    private static SemanticException Unavailable() =>
        new(new SemanticError(FoundationErrorCodes.AdminAuthorizationUnavailable));
}

/// <summary>بدنهٔ حضور‌آگاه انتساب انجام ارسال؛ کلید غایب با null صریح یکی نیست.</summary>
/// <param name="ResponsibilitySupplied">آیا کلید مسئولیت در بدنه آمده است.</param>
/// <param name="Responsibility">مسئولیت یا تهی.</param>
/// <param name="ShippingTypeId">شناسهٔ نوع ارسال یا تهی.</param>
/// <param name="ShippingModeSupplied">آیا کلید حالت انتخاب روش ارسال آمده است.</param>
/// <param name="ShippingMode">حالت انتخاب روش ارسال یا تهی.</param>
/// <param name="ShippingMethodIds">شناسه‌های پایدار روش‌های انتخاب‌شده یا تهی.</param>
public sealed record OfferFulfillmentAssignmentBody(
    bool ResponsibilitySupplied,
    OfferFulfillmentResponsibility? Responsibility,
    Guid? ShippingTypeId,
    string? ExpectedVersion = null,
    bool ShippingModeSupplied = false,
    OfferShippingSelectionMode? ShippingMode = null,
    IReadOnlyList<Guid>? ShippingMethodIds = null);

/// <summary>خوانندهٔ بدنهٔ انتساب؛ بدشکلی به کد پایدار canonical نگاشت می‌شود.</summary>
public static class OfferFulfillmentBodyReader
{
    /// <summary>بدنه را بی‌آنکه بی‌صدا نرمال کند می‌خواند.</summary>
    /// <param name="request">درخواست HTTP.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <exception cref="SemanticException">بدنهٔ خالی/بدشکل/نوع نامعتبر.</exception>
    public static async Task<OfferFulfillmentAssignmentBody> ReadAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        JsonDocument document;
        try
        {
            document = await JsonDocument.ParseAsync(request.Body, default, cancellationToken);
        }
        catch (JsonException)
        {
            throw Malformed();
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw Malformed();
            }

            var supplied = false;
            OfferFulfillmentResponsibility? responsibility = null;
            Guid? shippingTypeId = null;
            string? expectedVersion = null;
            var modeSupplied = false;
            OfferShippingSelectionMode? mode = null;
            IReadOnlyList<Guid>? methodIds = null;

            foreach (var property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, "responsibility", StringComparison.OrdinalIgnoreCase))
                {
                    supplied = true;
                    responsibility = ReadOptionalResponsibility(property.Value);
                }
                else if (string.Equals(property.Name, "shippingTypeId", StringComparison.OrdinalIgnoreCase))
                {
                    shippingTypeId = ReadOptionalGuid(property.Value);
                }
                else if (string.Equals(property.Name, "expectedVersion", StringComparison.OrdinalIgnoreCase))
                {
                    expectedVersion = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()
                        : throw Malformed();
                }
                else if (string.Equals(property.Name, "shippingMode", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(property.Name, "mode", StringComparison.OrdinalIgnoreCase))
                {
                    modeSupplied = true;
                    mode = ReadOptionalShippingMode(property.Value);
                }
                else if (string.Equals(property.Name, "shippingMethodIds", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(property.Name, "methodIds", StringComparison.OrdinalIgnoreCase))
                {
                    methodIds = ReadGuidArray(property.Value);
                }
            }

            return new OfferFulfillmentAssignmentBody(
                supplied, responsibility, shippingTypeId, expectedVersion, modeSupplied, mode, methodIds);
        }
    }

    private static OfferShippingSelectionMode? ReadOptionalShippingMode(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String
            || !Enum.TryParse<OfferShippingSelectionMode>(element.GetString(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw Malformed();
        }

        return parsed;
    }

    private static IReadOnlyList<Guid>? ReadGuidArray(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Malformed();
        }

        var values = new List<Guid>(element.GetArrayLength());
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !item.TryGetGuid(out var value))
            {
                throw Malformed();
            }

            values.Add(value);
        }

        return values;
    }

    private static OfferFulfillmentResponsibility? ReadOptionalResponsibility(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String
            || !Enum.TryParse<OfferFulfillmentResponsibility>(element.GetString(), ignoreCase: true, out var parsed)
            || !Enum.IsDefined(parsed))
        {
            throw Malformed();
        }

        return parsed;
    }

    private static Guid? ReadOptionalGuid(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.String || !element.TryGetGuid(out var value))
        {
            throw Malformed();
        }

        return value;
    }

    private static SemanticException Malformed() =>
        new(new SemanticError(OfferErrorCodes.FulfillmentBodyMalformed));
}

/// <summary>مسیر فقط-مدیر انتساب انجام ارسال روی یک آفر موجود.</summary>
/// <remarks>
/// ترتیب ثابت: مجوز پیش از خواندن بدنه، سپس بدنه، سپس ISender. هیچ مدل complex از binding خودکار عبور نمی‌کند.
/// </remarks>
public static class OfferFulfillmentAdminEndpoints
{
    /// <summary>ثبت مسیر روی گروه ادمین.</summary>
    /// <param name="admin">گروه <c>/v1/admin</c>.</param>
    public static void Map(RouteGroupBuilder admin)
    {
        ArgumentNullException.ThrowIfNull(admin);
        admin.MapPut("/offers/{offerId:guid}/fulfillment", AssignAsync);
        admin.MapGet("/offers/{offerId:guid}/shipping-methods", GetMethodsAsync);
        admin.MapGet("/offers/shipping-methods", ListMethodsAsync);
    }

    private static async Task<IResult> GetMethodsAsync(
        Guid offerId,
        ISender sender,
        IOfferFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireManageAsync(context, cancellationToken);
        var result = await sender.Send(
            new Tooba.Offer.Application.Offers.Queries.GetOfferShippingMethods.GetOfferShippingMethodsQuery(
                offerId, RequestLanguage(context)),
            cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> ListMethodsAsync(
        ISender sender,
        IOfferFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireManageAsync(context, cancellationToken);
        var query = context.Request.Query;
        OfferFulfillmentResponsibility? responsibility = null;
        var responsibilityText = query["responsibility"].ToString();
        if (!string.IsNullOrWhiteSpace(responsibilityText))
        {
            if (!Enum.TryParse<OfferFulfillmentResponsibility>(responsibilityText, ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                throw new SemanticException(new SemanticError(OfferErrorCodes.FulfillmentBodyMalformed));
            }

            responsibility = parsed;
        }

        Guid? shippingTypeId = ParseOptionalGuid(query["shippingTypeId"].ToString());
        Guid? sellerPartyId = ParseOptionalGuid(query["sellerPartyId"].ToString());

        var result = await sender.Send(
            new Tooba.Offer.Application.Offers.Queries.GetOfferShippingMethods.ListOfferShippingMethodOptionsQuery(
                responsibility, shippingTypeId, sellerPartyId, RequestLanguage(context)),
            cancellationToken);
        return api.From(result);
    }

    private static string? RequestLanguage(HttpContext context)
    {
        var explicitLanguage = context.Request.Query["language"].ToString();
        if (!string.IsNullOrWhiteSpace(explicitLanguage))
        {
            return explicitLanguage.Trim();
        }

        var header = context.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return null;
        }

        var first = header.Split(',', 2, StringSplitOptions.TrimEntries)[0];
        return first.Length == 0 ? null : first;
    }

    private static Guid? ParseOptionalGuid(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return Guid.TryParse(text, out var value) && value != Guid.Empty
            ? value
            : throw new SemanticException(new SemanticError(OfferErrorCodes.FulfillmentBodyMalformed));
    }

    private static async Task<IResult> AssignAsync(
        Guid offerId,
        ISender sender,
        IOfferFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var actorUserId = await authorizer.RequireManageAsync(context, cancellationToken);

        var body = await OfferFulfillmentBodyReader.ReadAsync(context.Request, cancellationToken);
        if (!body.ResponsibilitySupplied)
        {
            // The responsibility key is the decision itself: an absent key is not "clear", it is an unreadable
            // request. Only an explicit null clears the assignment.
            throw new SemanticException(new SemanticError(OfferErrorCodes.FulfillmentBodyMalformed));
        }

        var result = await sender.Send(
            new AssignOfferFulfillmentCommand(
                offerId,
                body.Responsibility,
                body.ShippingTypeId,
                body.ExpectedVersion ?? string.Empty,
                actorUserId,
                body.ShippingModeSupplied,
                body.ShippingMode,
                body.ShippingMethodIds),
            cancellationToken);
        return api.From(result);
    }
}

