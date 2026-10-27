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
public sealed record OfferFulfillmentAssignmentBody(
    bool ResponsibilitySupplied,
    OfferFulfillmentResponsibility? Responsibility,
    Guid? ShippingTypeId);

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
            }

            return new OfferFulfillmentAssignmentBody(supplied, responsibility, shippingTypeId);
        }
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
    }

    private static async Task<IResult> AssignAsync(
        Guid offerId,
        ISender sender,
        IOfferFulfillmentAdminAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        await authorizer.RequireManageAsync(context, cancellationToken);

        var body = await OfferFulfillmentBodyReader.ReadAsync(context.Request, cancellationToken);
        if (!body.ResponsibilitySupplied)
        {
            // The responsibility key is the decision itself: an absent key is not "clear", it is an unreadable
            // request. Only an explicit null clears the assignment.
            throw new SemanticException(new SemanticError(OfferErrorCodes.FulfillmentBodyMalformed));
        }

        var result = await sender.Send(
            new AssignOfferFulfillmentCommand(offerId, body.Responsibility, body.ShippingTypeId),
            cancellationToken);
        return api.From(result);
    }
}

