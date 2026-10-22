using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Endpoints.Admin;

/// <summary>
/// خوانندهٔ بدنهٔ نوشتن نوع ارسال: تنها درز خواندن JSON در این capability.
/// </summary>
/// <remarks>
/// <para>
/// این درز آگاه به حضور است و هیچ‌چیز را بی‌صدا نرمال نمی‌کند: کلید غایب، مقدار null، فهرست null و عنصر null
/// دست‌نخورده تا اعتبارسنجی منتقل می‌شوند. JSON بدشکل یا نوع نامعتبر به کد پایدار
/// <c>shipping_type.body.malformed</c> نگاشت می‌شود، نه استخراج پیام استثنا.
/// </para>
/// <para>
/// فقط همین دو مسیر نوشتن از این درز استفاده می‌کنند؛ هیچ چارچوب بدنهٔ دوم یا جهانی ساخته نشده و هیچ
/// مسیر موجود دیگری تغییر نمی‌کند. مسیرها ابتدا مجوز می‌خواهند و تنها بعد بدنه خوانده می‌شود.
/// </para>
/// </remarks>
public static class ShippingTypeBodyReader
{
    /// <summary>بدنه را به مدل حضور‌آگاه می‌خواند.</summary>
    /// <param name="request">درخواست HTTP.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    /// <exception cref="SemanticException">بدنهٔ خالی/بدشکل/نوع نامعتبر؛ کد پایدار canonical.</exception>
    public static async Task<ShippingTypeWriteModel> ReadAsync(
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

            string? code = null;
            int? sortOrder = null;
            bool? isActive = null;
            long? expectedRevision = null;
            IReadOnlyList<ShippingTypeTranslationInput?>? translations = null;

            foreach (var property in root.EnumerateObject())
            {
                if (Matches(property, "code"))
                {
                    code = ReadOptionalString(property.Value);
                }
                else if (Matches(property, "sortOrder"))
                {
                    sortOrder = ReadOptionalInt32(property.Value);
                }
                else if (Matches(property, "isActive"))
                {
                    isActive = ReadOptionalBoolean(property.Value);
                }
                else if (Matches(property, "expectedRevision"))
                {
                    expectedRevision = ReadOptionalInt64(property.Value);
                }
                else if (Matches(property, "translations"))
                {
                    translations = ReadTranslations(property.Value);
                }
            }

            return new ShippingTypeWriteModel(code, sortOrder, isActive, expectedRevision, translations);
        }
    }

    private static IReadOnlyList<ShippingTypeTranslationInput?>? ReadTranslations(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Array)
        {
            throw Malformed();
        }

        var entries = new List<ShippingTypeTranslationInput?>(element.GetArrayLength());
        foreach (var item in element.EnumerateArray())
        {
            // عنصر null حفظ می‌شود تا کل درخواست رد شود؛ فیلتر بی‌صدا هرگز انجام نمی‌شود.
            if (item.ValueKind == JsonValueKind.Null)
            {
                entries.Add(null);
                continue;
            }

            if (item.ValueKind != JsonValueKind.Object)
            {
                throw Malformed();
            }

            Guid? languageId = null;
            string? name = null;
            string? description = null;

            foreach (var property in item.EnumerateObject())
            {
                if (Matches(property, "languageId"))
                {
                    languageId = ReadOptionalGuid(property.Value);
                }
                else if (Matches(property, "name"))
                {
                    name = ReadOptionalString(property.Value);
                }
                else if (Matches(property, "description"))
                {
                    description = ReadOptionalString(property.Value);
                }
            }

            entries.Add(new ShippingTypeTranslationInput(languageId, name, description));
        }

        return entries;
    }

    private static bool Matches(JsonProperty property, string name) =>
        string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase);

    private static string? ReadOptionalString(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => element.GetString(),
        _ => throw Malformed(),
    };

    private static int? ReadOptionalInt32(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw Malformed();
        }

        return value;
    }

    private static long? ReadOptionalInt64(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt64(out var value))
        {
            throw Malformed();
        }

        return value;
    }

    private static bool? ReadOptionalBoolean(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw Malformed(),
    };

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
        new(new SemanticError(FulfillmentErrorCodes.ShippingTypeBodyMalformed));
}
