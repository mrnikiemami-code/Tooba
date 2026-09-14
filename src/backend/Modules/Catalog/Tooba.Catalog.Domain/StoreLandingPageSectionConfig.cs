#pragma warning disable CS1591
using System.Text.Json;
using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>اعتبارسنجی typed برای پیکربندی Section. SQL/HTML/JS پذیرفته نمی‌شود.</summary>
public static class StoreLandingPageSectionConfig
{
    private static readonly HashSet<string> Forbidden = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "css", "js", "javascript", "script", "sql", "query", "filter", "expression", "raw",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string ValidateAndNormalize(string sectionType, string? configurationJson)
    {
        if (!StoreLandingPageSectionRegistry.IsApproved(sectionType))
        {
            throw new PlatformHttpException(400, "نوع بخش تأییدشده نیست.", "landing.section.type.invalid");
        }

        if (string.IsNullOrWhiteSpace(configurationJson))
        {
            configurationJson = "{}";
        }

        if (configurationJson.Length > StoreLandingPageSectionRegistry.ConfigMaxLength)
        {
            throw new PlatformHttpException(400, "پیکربندی بخش بیش از حد بزرگ است.", "landing.section.config.too_large");
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(configurationJson);
        }
        catch (JsonException)
        {
            throw new PlatformHttpException(400, "پیکربندی بخش معتبر نیست.", "landing.section.config.invalid");
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new PlatformHttpException(400, "پیکربندی بخش باید شیء باشد.", "landing.section.config.invalid");
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (Forbidden.Contains(property.Name))
                {
                    throw new PlatformHttpException(400, "پیکربندی اجرایی یا پرس‌وجوی آزاد مجاز نیست.", "landing.section.config.forbidden");
                }
            }

            return sectionType switch
            {
                StoreLandingPageSectionRegistry.Hero => NormalizeHero(document.RootElement),
                StoreLandingPageSectionRegistry.ProductCollection => NormalizeProductCollection(document.RootElement),
                StoreLandingPageSectionRegistry.CategoryGrid => NormalizeIdList(document.RootElement, "categoryIds", allowEmpty: true),
                StoreLandingPageSectionRegistry.BrandStrip => NormalizeIdList(document.RootElement, "brandIds", allowEmpty: true),
                StoreLandingPageSectionRegistry.PromoBanner => NormalizePromo(document.RootElement),
                StoreLandingPageSectionRegistry.ArticleList => NormalizeArticleList(document.RootElement),
                StoreLandingPageSectionRegistry.Reviews => NormalizeTitleOnly(document.RootElement),
                StoreLandingPageSectionRegistry.RichText => NormalizeRichText(document.RootElement),
                StoreLandingPageSectionRegistry.NavigationMenu => NormalizeNavigationMenu(document.RootElement),
                _ => throw new PlatformHttpException(400, "نوع بخش تأییدشده نیست.", "landing.section.type.invalid"),
            };
        }
    }

    public static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static string NormalizeHero(JsonElement root)
    {
        var title = RequiredTitle(root);
        return JsonSerializer.Serialize(new
        {
            title,
            subtitle = OptionalString(root, "subtitle", StoreLandingPageSectionRegistry.TitleMaxLength),
            href = OptionalHref(root),
            mediaAssetId = OptionalGuid(root, "mediaAssetId"),
        }, JsonOptions);
    }

    private static string NormalizePromo(JsonElement root)
    {
        var title = RequiredTitle(root);
        return JsonSerializer.Serialize(new
        {
            title,
            href = OptionalHref(root),
            mediaAssetId = OptionalGuid(root, "mediaAssetId"),
        }, JsonOptions);
    }

    private static string NormalizeProductCollection(JsonElement root)
    {
        var source = root.TryGetProperty("source", out var sourceEl) ? sourceEl.GetString()?.Trim() ?? "" : "";
        if (StoreLandingPageSectionRegistry.UnsupportedProductSources.Contains(source))
        {
            throw new PlatformHttpException(400, "این منبع محصول هنوز پشتیبانی نمی‌شود.", "landing.section.source.unsupported");
        }

        if (!StoreLandingPageSectionRegistry.ProductSources.Contains(source))
        {
            throw new PlatformHttpException(400, "منبع محصول معتبر نیست.", "landing.section.source.invalid");
        }

        var take = OptionalTake(root);
        Guid? categoryId = null;
        Guid? brandId = null;
        var productIds = Array.Empty<Guid>();

        if (string.Equals(source, "Category", StringComparison.OrdinalIgnoreCase))
        {
            categoryId = RequiredGuid(root, "categoryId");
            source = "Category";
        }
        else if (string.Equals(source, "Brand", StringComparison.OrdinalIgnoreCase))
        {
            brandId = RequiredGuid(root, "brandId");
            source = "Brand";
        }
        else if (string.Equals(source, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            productIds = ReadGuids(root, "productIds", allowEmpty: false);
            source = "Manual";
        }
        else
        {
            source = "Newest";
        }

        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            source,
            take,
            categoryId,
            brandId,
            productIds,
        }, JsonOptions);
    }

    private static string NormalizeIdList(JsonElement root, string listName, bool allowEmpty)
    {
        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            ids = ReadGuids(root, listName, allowEmpty),
        }, JsonOptions);
    }

    private static string NormalizeArticleList(JsonElement root)
    {
        var source = root.TryGetProperty("source", out var sourceEl) ? sourceEl.GetString()?.Trim() ?? "Latest" : "Latest";
        if (!string.Equals(source, "Latest", StringComparison.OrdinalIgnoreCase))
        {
            throw new PlatformHttpException(400, "فهرست مقاله فعلاً فقط Latest است.", "landing.section.source.unsupported");
        }

        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            source = "Latest",
            take = OptionalTake(root),
        }, JsonOptions);
    }

    private static string NormalizeTitleOnly(JsonElement root) =>
        JsonSerializer.Serialize(new { title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength) }, JsonOptions);

    private static string NormalizeNavigationMenu(JsonElement root)
    {
        var menuId = RequiredGuid(root, "menuId");
        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            menuId,
        }, JsonOptions);
    }

    private static string NormalizeRichText(JsonElement root)
    {
        var text = OptionalString(root, "text", StoreLandingPageSectionRegistry.TextMaxLength)
            ?? throw new PlatformHttpException(400, "متن بخش لازم است.", "landing.section.text.required");
        if (text.Contains('<', StringComparison.Ordinal) || text.Contains('>', StringComparison.Ordinal))
        {
            throw new PlatformHttpException(400, "HTML در متن مجاز نیست.", "landing.section.text.html");
        }

        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            text,
        }, JsonOptions);
    }

    private static string RequiredTitle(JsonElement root)
    {
        var title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength);
        if (string.IsNullOrEmpty(title))
        {
            throw new PlatformHttpException(400, "عنوان بخش لازم است.", "landing.section.title.required");
        }

        return title;
    }

    private static string? OptionalString(JsonElement root, string name, int max)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (el.ValueKind != JsonValueKind.String)
        {
            throw new PlatformHttpException(400, "مقدار متنی معتبر نیست.", "landing.section.config.invalid");
        }

        var value = el.GetString()?.Trim() ?? string.Empty;
        if (value.Length == 0)
        {
            return null;
        }

        return value.Length > max ? value[..max] : value;
    }

    private static string? OptionalHref(JsonElement root)
    {
        var href = OptionalString(root, "href", 256);
        if (href is null)
        {
            return null;
        }

        if (href.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            || href.Contains("data:", StringComparison.OrdinalIgnoreCase)
            || href.Contains('<', StringComparison.Ordinal))
        {
            throw new PlatformHttpException(400, "آدرس بخش معتبر نیست.", "landing.section.href.invalid");
        }

        return href;
    }

    private static int OptionalTake(JsonElement root)
    {
        if (!root.TryGetProperty("take", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return StoreLandingPageSectionRegistry.DefaultTake;
        }

        if (!el.TryGetInt32(out var take) || take < 1 || take > StoreLandingPageSectionRegistry.MaxTake)
        {
            throw new PlatformHttpException(400, "تعداد آیتم خارج از محدوده است.", "landing.section.take.invalid");
        }

        return take;
    }

    private static Guid? OptionalGuid(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.String && Guid.TryParse(el.GetString(), out var id))
        {
            return id;
        }

        throw new PlatformHttpException(400, "شناسه معتبر نیست.", "landing.section.id.invalid");
    }

    private static Guid RequiredGuid(JsonElement root, string name) =>
        OptionalGuid(root, name) ?? throw new PlatformHttpException(400, "شناسه منبع لازم است.", "landing.section.source.missing");

    private static Guid[] ReadGuids(JsonElement root, string name, bool allowEmpty)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            if (allowEmpty)
            {
                return [];
            }

            throw new PlatformHttpException(400, "فهرست شناسه لازم است.", "landing.section.ids.required");
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            throw new PlatformHttpException(400, "فهرست شناسه معتبر نیست.", "landing.section.ids.invalid");
        }

        var ids = new List<Guid>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || !Guid.TryParse(item.GetString(), out var id))
            {
                throw new PlatformHttpException(400, "شناسه معتبر نیست.", "landing.section.id.invalid");
            }

            if (!ids.Contains(id))
            {
                ids.Add(id);
            }
        }

        if (!allowEmpty && ids.Count == 0)
        {
            throw new PlatformHttpException(400, "فهرست شناسه خالی است.", "landing.section.ids.required");
        }

        if (ids.Count > StoreLandingPageSectionRegistry.MaxTake)
        {
            throw new PlatformHttpException(400, "تعداد شناسه بیش از حد است.", "landing.section.ids.limit");
        }

        return ids.ToArray();
    }
}
