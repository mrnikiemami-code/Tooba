using System.Text.Json;
using Tooba.Catalog.Domain;
using Tooba.BuildingBlocks;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>TB-P10-T022-R17 — PromotionCampaign Product Showcase source contract.</summary>
public sealed class PromotionCampaignSourceTests
{
    [Fact]
    public void Product_sources_registry_includes_PromotionCampaign()
    {
        Assert.Contains("PromotionCampaign", StoreLandingPageSectionRegistry.ProductSources);
        Assert.Contains("Manual", StoreLandingPageSectionRegistry.ProductSources);
        Assert.Contains("Newest", StoreLandingPageSectionRegistry.ProductSources);
    }

    [Fact]
    public void Normalize_persists_source_intent_without_member_snapshot()
    {
        var json = StoreLandingPageSectionConfig.ValidateAndNormalize(
            "ProductCollection",
            """{"title":"شگفت","source":"PromotionCampaign","take":6,"promotionTypeCode":"amazing"}""");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("PromotionCampaign", root.GetProperty("source").GetString());
        Assert.Equal("AMAZING", root.GetProperty("promotionTypeCode").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("campaignId").ValueKind);
        Assert.Equal(6, root.GetProperty("take").GetInt32());
        Assert.False(root.TryGetProperty("productIds", out var ids) && ids.GetArrayLength() > 0);
        Assert.False(root.TryGetProperty("price", out _));
        Assert.False(root.TryGetProperty("discountPercent", out _));
    }

    [Fact]
    public void Normalize_defaults_promotion_type_to_AMAZING()
    {
        var json = StoreLandingPageSectionConfig.ValidateAndNormalize(
            "ProductCollection",
            """{"source":"PromotionCampaign","take":8}""");
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("AMAZING", doc.RootElement.GetProperty("promotionTypeCode").GetString());
    }

    [Fact]
    public void Normalize_rejects_invalid_promotion_type_code()
    {
        var ex = Assert.Throws<PlatformHttpException>(() =>
            StoreLandingPageSectionConfig.ValidateAndNormalize(
                "ProductCollection",
                """{"source":"PromotionCampaign","promotionTypeCode":"AMA ZING"}"""));
        Assert.Equal("landing.section.promotionType.invalid", ex.ErrorCode);
    }

    [Fact]
    public void Existing_Newest_source_unchanged()
    {
        var json = StoreLandingPageSectionConfig.ValidateAndNormalize(
            "ProductCollection",
            """{"source":"Newest","take":4}""");
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Newest", doc.RootElement.GetProperty("source").GetString());
        Assert.Equal(4, doc.RootElement.GetProperty("take").GetInt32());
    }
}
