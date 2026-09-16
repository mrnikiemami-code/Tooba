#pragma warning disable CS1591
namespace Tooba.Catalog.Domain;

/// <summary>شناسه‌های پایدار قالب Auto Parts برای seed idempotent (Batch A).</summary>
public static class AutoPartsTemplateCatalogIds
{
    public const string Key = "auto-parts";
    private const string Ns = "019022b1";

    public static readonly Guid TemplateId = Guid.Parse($"{Ns}-0000-7000-8000-00000000f001");
    public static readonly Guid LandingPageId = Guid.Parse($"{Ns}-0000-7000-8000-00000000f010");

    public static Guid MediaAsset(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000a{index:000}");

    public static Guid BrandId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000b{index:000}");

    public static Guid CategoryRoot(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c10{index:00}");

    public static Guid CategoryMid(int root, int mid) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c2{root:00}{mid}");

    public static Guid CategoryLeaf(int root, int mid, int leaf) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000c30{root:00}{mid}{leaf}");

    public static Guid ProductId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000d{index:000}");

    public static Guid ProductMediaRef(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000e{index:000}");

    public static Guid ProductCategoryAssignment(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000002b{index:000}");

    public static Guid LocalizedProductName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001a{index:000}");

    public static Guid LocalizedBrandName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001b{index:000}");

    public static Guid CategoryTranslation(int root, int mid, int leaf) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000001c{root:00}{mid}{leaf}");

    public static Guid BannerSectionId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001d{index:000}");
}

/// <summary>شناسه‌های پایدار قالب Building Materials برای seed idempotent (Batch A).</summary>
public static class BuildingMaterialsTemplateCatalogIds
{
    public const string Key = "building-materials";
    private const string Ns = "019022b3";

    public static readonly Guid TemplateId = Guid.Parse($"{Ns}-0000-7000-8000-00000000f001");
    public static readonly Guid LandingPageId = Guid.Parse($"{Ns}-0000-7000-8000-00000000f010");

    public static Guid MediaAsset(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000a{index:000}");

    public static Guid BrandId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000b{index:000}");

    public static Guid CategoryRoot(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c10{index:00}");

    public static Guid CategoryMid(int root, int mid) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c2{root:00}{mid}");

    public static Guid CategoryLeaf(int root, int mid, int leaf) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000c30{root:00}{mid}{leaf}");

    public static Guid ProductId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000d{index:000}");

    public static Guid ProductMediaRef(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000e{index:000}");

    public static Guid ProductCategoryAssignment(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000002b{index:000}");

    public static Guid LocalizedProductName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001a{index:000}");

    public static Guid LocalizedBrandName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001b{index:000}");

    public static Guid CategoryTranslation(int root, int mid, int leaf) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000001c{root:00}{mid}{leaf}");

    public static Guid BannerSectionId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001d{index:000}");
}

/// <summary>شناسه‌های پایدار قالب Tools &amp; Hardware برای seed idempotent (Batch A).</summary>
public static class ToolsHardwareTemplateCatalogIds
{
    public const string Key = "tools-hardware";
    private const string Ns = "019022b5";

    public static readonly Guid TemplateId = Guid.Parse($"{Ns}-0000-7000-8000-00000000f001");
    public static readonly Guid LandingPageId = Guid.Parse($"{Ns}-0000-7000-8000-00000000f010");

    public static Guid MediaAsset(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000a{index:000}");

    public static Guid BrandId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000b{index:000}");

    public static Guid CategoryRoot(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c10{index:00}");

    public static Guid CategoryMid(int root, int mid) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c2{root:00}{mid}");

    public static Guid CategoryLeaf(int root, int mid, int leaf) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000c30{root:00}{mid}{leaf}");

    public static Guid ProductId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000d{index:000}");

    public static Guid ProductMediaRef(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000e{index:000}");

    public static Guid ProductCategoryAssignment(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000002b{index:000}");

    public static Guid LocalizedProductName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001a{index:000}");

    public static Guid LocalizedBrandName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001b{index:000}");

    public static Guid CategoryTranslation(int root, int mid, int leaf) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000001c{root:00}{mid}{leaf}");

    public static Guid BannerSectionId(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000001d{index:000}");
}
