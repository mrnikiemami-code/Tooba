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
                StoreLandingPageSectionRegistry.StoryRail => NormalizeStoryRail(document.RootElement),
                StoreLandingPageSectionRegistry.BannerShowcase => NormalizeBannerShowcase(document.RootElement),
                _ => throw new PlatformHttpException(400, "نوع بخش تأییدشده نیست.", "landing.section.type.invalid"),
            };
        }
    }

    public static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static string NormalizeHero(JsonElement root)
    {
        var slides = ReadHeroSlides(root);
        var title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength);
        if (string.IsNullOrEmpty(title) && slides.Count > 0)
        {
            title = slides[0].Title;
        }

        if (string.IsNullOrEmpty(title))
        {
            throw new PlatformHttpException(400, "عنوان بخش لازم است.", "landing.section.title.required");
        }

        var intervalSec = OptionalHeroSlideInterval(root);
        var slideCount = slides.Count > 0
            ? slides.Count
            : Math.Clamp(OptionalHeroSlideCount(root), 1, StoreLandingPageSectionRegistry.MaxHeroSlides);

        return JsonSerializer.Serialize(new
        {
            title,
            subtitle = OptionalString(root, "subtitle", StoreLandingPageSectionRegistry.TitleMaxLength),
            href = OptionalHref(root),
            mediaAssetId = OptionalGuid(root, "mediaAssetId"),
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root) ?? "Medium",
            slideIntervalSec = intervalSec,
            slideCount,
            autoplay = true,
            slides = slides.Select(s => new
            {
                mediaAssetId = s.MediaAssetId,
                imageUrl = s.ImageUrl,
                title = s.Title,
                alt = s.Alt,
                description = s.Description,
                ctaLabel = s.CtaLabel,
                destinationType = s.DestinationType,
                targetId = s.TargetId,
                targetSlug = s.TargetSlug,
                targetLabel = s.TargetLabel,
                customUrl = s.CustomUrl,
                href = s.Href,
                panelMediaAssetId = s.PanelMediaAssetId,
                panelImageUrl = s.PanelImageUrl,
                panelColor = s.PanelColor,
                panelSize = s.PanelSize,
                panelOpacity = s.PanelOpacity,
                panelSide = s.PanelSide,
            }).ToArray(),
        }, JsonOptions);
    }

    private sealed class HeroSlideNormalized
    {
        public Guid? MediaAssetId { get; init; }
        public string? ImageUrl { get; init; }
        public string Title { get; init; } = "";
        public string Alt { get; init; } = "";
        public string? Description { get; init; }
        public string? CtaLabel { get; init; }
        public string DestinationType { get; init; } = "none";
        public string? TargetId { get; init; }
        public string? TargetSlug { get; init; }
        public string? TargetLabel { get; init; }
        public string? CustomUrl { get; init; }
        public string? Href { get; init; }
        public Guid? PanelMediaAssetId { get; init; }
        public string? PanelImageUrl { get; init; }
        public string PanelColor { get; init; } = "#0f172a";
        public string PanelSize { get; init; } = "xlarge";
        public int PanelOpacity { get; init; } = 100;
        public string PanelSide { get; init; } = "left";
    }

    private static List<HeroSlideNormalized> ReadHeroSlides(JsonElement root)
    {
        if (!root.TryGetProperty("slides", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            throw new PlatformHttpException(400, "فهرست اسلاید معتبر نیست.", "landing.section.config.invalid");
        }

        var slides = new List<HeroSlideNormalized>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new PlatformHttpException(400, "آیتم اسلاید معتبر نیست.", "landing.section.config.invalid");
            }

            if (slides.Count >= StoreLandingPageSectionRegistry.MaxHeroSlides)
            {
                throw new PlatformHttpException(400, "تعداد اسلاید بیش از حد است.", "landing.section.ids.limit");
            }

            var destinationRaw = (OptionalString(item, "destinationType", 32) ?? "none").Trim().ToLowerInvariant();
            var destinationType = destinationRaw switch
            {
                "none" => "none",
                "all-products" => "all-products",
                "product" => "product",
                "category" => "category",
                "custom-url" => "custom-url",
                _ => "none",
            };

            var title = OptionalString(item, "title", StoreLandingPageSectionRegistry.TitleMaxLength) ?? "";
            var alt = OptionalString(item, "alt", StoreLandingPageSectionRegistry.TitleMaxLength) ?? "";
            var mediaAssetId = OptionalGuid(item, "mediaAssetId");
            var imageUrl = OptionalString(item, "imageUrl", 512);
            var href = OptionalHref(item) ?? OptionalString(item, "href", 256);
            var customUrl = OptionalString(item, "customUrl", 256);
            if (customUrl is not null
                && (customUrl.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
                    || customUrl.Contains("data:", StringComparison.OrdinalIgnoreCase)
                    || customUrl.Contains('<', StringComparison.Ordinal)))
            {
                throw new PlatformHttpException(400, "آدرس بخش معتبر نیست.", "landing.section.href.invalid");
            }

            var panelColorRaw = OptionalString(item, "panelColor", 16);
            var panelColor = IsHexColor(panelColorRaw) ? panelColorRaw! : "#0f172a";
            var panelSizeRaw = (OptionalString(item, "panelSize", 16) ?? "xlarge").Trim().ToLowerInvariant();
            var panelSize = panelSizeRaw switch
            {
                "small" => "small",
                "medium" => "medium",
                "large" => "large",
                "xlarge" => "xlarge",
                _ => "xlarge",
            };
            var panelSideRaw = (OptionalString(item, "panelSide", 16) ?? "left").Trim().ToLowerInvariant();
            var panelSide = panelSideRaw is "right" ? "right" : "left";
            var panelOpacity = 100;
            if (item.TryGetProperty("panelOpacity", out var opacityEl)
                && opacityEl.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                double opacityRaw = 100;
                if (opacityEl.ValueKind == JsonValueKind.Number && opacityEl.TryGetDouble(out opacityRaw))
                {
                    // ok
                }
                else if (opacityEl.ValueKind == JsonValueKind.String
                    && double.TryParse(opacityEl.GetString(), out opacityRaw))
                {
                    // ok
                }

                panelOpacity = (int)Math.Round(Math.Clamp(opacityRaw, 0, 100));
            }

            slides.Add(new HeroSlideNormalized
            {
                MediaAssetId = mediaAssetId,
                ImageUrl = imageUrl,
                Title = title,
                Alt = alt,
                Description = OptionalString(item, "description", 500),
                CtaLabel = OptionalString(item, "ctaLabel", 80),
                DestinationType = destinationType,
                TargetId = OptionalString(item, "targetId", 64),
                TargetSlug = OptionalString(item, "targetSlug", 200),
                TargetLabel = OptionalString(item, "targetLabel", StoreLandingPageSectionRegistry.TitleMaxLength),
                CustomUrl = customUrl,
                Href = href,
                PanelMediaAssetId = OptionalGuid(item, "panelMediaAssetId"),
                PanelImageUrl = OptionalString(item, "panelImageUrl", 512),
                PanelColor = panelColor,
                PanelSize = panelSize,
                PanelOpacity = panelOpacity,
                PanelSide = panelSide,
            });
        }

        return slides;
    }

    private static int OptionalHeroSlideInterval(JsonElement root)
    {
        if (!root.TryGetProperty("slideIntervalSec", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return StoreLandingPageSectionRegistry.DefaultHeroSlideIntervalSec;
        }

        double raw;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out raw))
        {
            // ok
        }
        else if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out raw))
        {
            // ok
        }
        else
        {
            throw new PlatformHttpException(400, "زمان تغییر اسلایدر معتبر نیست.", "landing.section.config.invalid");
        }

        var sec = (int)Math.Round(raw);
        if (sec < 1 || sec > 120)
        {
            throw new PlatformHttpException(400, "زمان تغییر اسلایدر خارج از محدوده است.", "landing.section.config.invalid");
        }

        return sec;
    }

    private static int OptionalHeroSlideCount(JsonElement root)
    {
        if (!root.TryGetProperty("slideCount", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return 1;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var count))
        {
            return count;
        }

        if (el.ValueKind == JsonValueKind.String && int.TryParse(el.GetString(), out count))
        {
            return count;
        }

        return 1;
    }

    private static string NormalizePromo(JsonElement root)
    {
        var title = RequiredTitle(root);
        return JsonSerializer.Serialize(new
        {
            title,
            href = OptionalHref(root),
            mediaAssetId = OptionalGuid(root, "mediaAssetId"),
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root),
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
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root),
            href = OptionalHref(root),
            bannerImageUrl = OptionalString(root, "bannerImageUrl", 512),
            bannerHref = OptionalHref(root, "bannerHref"),
            bannerMediaAssetId = OptionalGuid(root, "bannerMediaAssetId"),
        }, JsonOptions);
    }

    private static string NormalizeIdList(JsonElement root, string listName, bool allowEmpty)
    {
        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            ids = ReadGuids(root, listName, allowEmpty),
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root),
        }, JsonOptions);
    }

    private static string NormalizeArticleList(JsonElement root)
    {
        var source = root.TryGetProperty("source", out var sourceEl) ? sourceEl.GetString()?.Trim() ?? "Latest" : "Latest";
        if (string.Equals(source, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            var articleIds = ReadGuids(root, "articleIds", allowEmpty: true);
            return JsonSerializer.Serialize(new
            {
                title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
                source = "Manual",
                take = OptionalTake(root),
                articleIds,
                variantKey = OptionalVariantKey(root),
                heightPreset = OptionalHeightPreset(root),
            }, JsonOptions);
        }

        if (!string.Equals(source, "Latest", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(source, "LatestArticles", StringComparison.OrdinalIgnoreCase))
        {
            throw new PlatformHttpException(400, "فهرست مقاله فعلاً فقط Latest یا Manual است.", "landing.section.source.unsupported");
        }

        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            source = "Latest",
            take = OptionalTake(root),
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root),
        }, JsonOptions);
    }

    private static string NormalizeTitleOnly(JsonElement root) =>
        JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root),
        }, JsonOptions);

    private static string NormalizeNavigationMenu(JsonElement root)
    {
        var menuId = RequiredGuid(root, "menuId");
        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            menuId,
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root),
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
            variantKey = OptionalVariantKey(root),
            heightPreset = OptionalHeightPreset(root),
        }, JsonOptions);
    }

    private static string NormalizeStoryRail(JsonElement root)
    {
        // Builder display settings only — Story content comes from Story module.
        var take = OptionalTake(root);
        var enabled = !root.TryGetProperty("enabled", out var enabledEl)
            || enabledEl.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
            || (enabledEl.ValueKind == JsonValueKind.True)
            || (enabledEl.ValueKind == JsonValueKind.String && !string.Equals(enabledEl.GetString(), "false", StringComparison.OrdinalIgnoreCase));
        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            take,
            enabled,
            items = Array.Empty<object>(),
            variantKey = OptionalVariantKey(root) ?? "story.circle",
            heightPreset = OptionalHeightPreset(root),
        }, JsonOptions);
    }

    private static string NormalizeBannerShowcase(JsonElement root)
    {
        var variantKey = OptionalVariantKey(root) ?? "banner.single";
        var expected = ExpectedBannerSlotCount(variantKey);
        var items = ReadBannerItems(root);
        if (expected > 0 && items.Length > expected)
        {
            throw new PlatformHttpException(400, "تعداد جایگاه بنر با مدل چیدمان هم‌خوان نیست.", "landing.section.banner.slots");
        }

        if (items.Length > StoreLandingPageSectionRegistry.MaxBannerSlots)
        {
            throw new PlatformHttpException(400, "تعداد جایگاه بنر بیش از حد است.", "landing.section.banner.slots");
        }

        return JsonSerializer.Serialize(new
        {
            title = OptionalString(root, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
            heightPreset = OptionalHeightPreset(root),
            variantKey,
            items,
        }, JsonOptions);
    }

    private static int ExpectedBannerSlotCount(string variantKey) => variantKey switch
    {
        "banner.single" => 1,
        "banner.two-equal" or "banner.two-asymmetric" => 2,
        "banner.three" or "banner.one-large-two-small" => 3,
        "banner.four-grid" or "banner.mosaic-2x2" => 4,
        "banner.one-large-four-small" => 5,
        "banner.eight-compact" => 8,
        _ => 0,
    };

    private static object[] ReadStoryItems(JsonElement root)
    {
        if (!root.TryGetProperty("items", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            throw new PlatformHttpException(400, "فهرست استوری معتبر نیست.", "landing.section.config.invalid");
        }

        var items = new List<object>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new PlatformHttpException(400, "آیتم استوری معتبر نیست.", "landing.section.config.invalid");
            }

            if (items.Count >= StoreLandingPageSectionRegistry.MaxStoryItems)
            {
                throw new PlatformHttpException(400, "تعداد استوری بیش از حد است.", "landing.section.ids.limit");
            }

            var enabled = true;
            if (item.TryGetProperty("enabled", out var enabledEl) && enabledEl.ValueKind is JsonValueKind.False)
            {
                enabled = false;
            }

            items.Add(new
            {
                imageUrl = OptionalString(item, "imageUrl", 512),
                mediaAssetId = OptionalGuid(item, "mediaAssetId"),
                title = OptionalString(item, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
                href = OptionalHref(item) ?? OptionalString(item, "href", 256),
                enabled,
            });
        }

        return items.ToArray();
    }

    private static object[] ReadBannerItems(JsonElement root)
    {
        if (!root.TryGetProperty("items", out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (el.ValueKind != JsonValueKind.Array)
        {
            throw new PlatformHttpException(400, "فهرست بنر معتبر نیست.", "landing.section.config.invalid");
        }

        var items = new List<object>();
        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new PlatformHttpException(400, "آیتم بنر معتبر نیست.", "landing.section.config.invalid");
            }

            items.Add(new
            {
                mediaAssetId = OptionalGuid(item, "mediaAssetId"),
                imageUrl = OptionalString(item, "imageUrl", 512),
                alt = OptionalString(item, "alt", StoreLandingPageSectionRegistry.TitleMaxLength)
                    ?? OptionalString(item, "altText", StoreLandingPageSectionRegistry.TitleMaxLength),
                href = OptionalHref(item) ?? OptionalString(item, "href", 256),
                title = OptionalString(item, "title", StoreLandingPageSectionRegistry.TitleMaxLength),
                text = OptionalString(item, "text", 240),
                ctaLabel = OptionalString(item, "ctaLabel", 80),
                destinationType = OptionalString(item, "destinationType", 32),
                targetId = OptionalString(item, "targetId", 64),
                targetSlug = OptionalString(item, "targetSlug", 200),
                targetLabel = OptionalString(item, "targetLabel", StoreLandingPageSectionRegistry.TitleMaxLength),
                customUrl = OptionalString(item, "customUrl", 256),
            });
        }

        return items.ToArray();
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

    private static string? OptionalVariantKey(JsonElement root) =>
        OptionalString(root, "variantKey", 80);

    private static string? OptionalHeightPreset(JsonElement root)
    {
        var value = OptionalString(root, "heightPreset", 32);
        if (value is null)
        {
            return null;
        }

        if (!StoreLandingPageSectionRegistry.SizePresets.Contains(value))
        {
            throw new PlatformHttpException(400, "اندازه نمایش معتبر نیست.", "landing.section.height.invalid");
        }

        foreach (var preset in StoreLandingPageSectionRegistry.SizePresets)
        {
            if (string.Equals(preset, value, StringComparison.OrdinalIgnoreCase))
            {
                return preset;
            }
        }

        return value;
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

    private static string? OptionalHref(JsonElement root, string name = "href")
    {
        var href = OptionalString(root, name, 256);
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

        if (el.ValueKind == JsonValueKind.String)
        {
            var raw = el.GetString()?.Trim() ?? string.Empty;
            if (raw.Length == 0)
            {
                return null;
            }

            if (Guid.TryParse(raw, out var id))
            {
                return id;
            }
        }

        throw new PlatformHttpException(400, "شناسه معتبر نیست.", "landing.section.id.invalid");
    }

    private static bool IsHexColor(string? value)
    {
        if (value is null || value.Length != 7 || value[0] != '#')
        {
            return false;
        }

        for (var i = 1; i < 7; i++)
        {
            var c = value[i];
            var ok = (c >= '0' && c <= '9')
                || (c >= 'a' && c <= 'f')
                || (c >= 'A' && c <= 'F');
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }

    private static Guid RequiredGuid(JsonElement root, string name) =>
        OptionalGuid(root, name) ?? throw new PlatformHttpException(400, "شناسه منبع لازم است.", "landing.section.source.missing");

    private static Guid[] ReadGuids(JsonElement root, string name, bool allowEmpty)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            // Legacy CategoryGrid/BrandStrip may already store `ids`.
            if (!root.TryGetProperty("ids", out el) || el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                if (allowEmpty)
                {
                    return [];
                }

                throw new PlatformHttpException(400, "فهرست شناسه لازم است.", "landing.section.ids.required");
            }
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
