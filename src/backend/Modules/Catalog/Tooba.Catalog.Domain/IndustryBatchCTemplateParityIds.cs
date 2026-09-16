#pragma warning disable CS1591
namespace Tooba.Catalog.Domain;

/// <summary>شناسه‌های پایدار seed ساختاری Shoes برای Batch C.</summary>
public static class ShoesTemplateParityIds
{
    private const string Ns = "019022be";

    public static Guid Tag(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000a{index:000}");

    public static Guid AttributeDefinition(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000b{index:000}");

    public static Guid AttributeOption(int definition, int option) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c{definition:00}{option:00}");

    public static Guid LocalizedAttrName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000d{index:000}");

    public static Guid LocalizedTagName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000e{index:000}");

    public static Guid LocalizedOptionName(int definition, int option) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000f{definition:00}{option:00}");

    public static Guid ProductTagAssignment(int product, int tag) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000a{product:000}{tag:000}");

    public static Guid CategoryTagAssignment(int root, int tag) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000b{root:000}{tag:000}");

    public static Guid CategoryBinding(int root, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000c{root:000}{def:000}");

    public static Guid CategoryFacet(int root, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000d{root:000}{def:000}");

    public static Guid MegaMenuItem(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000002{root:000}");

    public static Guid MegaMenuTranslation(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000003{root:000}");

    public static Guid ProductAttrValue(int product, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000e{product:000}{def:000}");

    public static Guid VariantAxis(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000004{product:000}");

    public static Guid Variant(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000005{product:000}");

    public static Guid VariantAttrValue(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000006{product:000}");

    public static Guid ProductHistory(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000007{product:000}");

    public static Guid CategorySlugHistory(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000008{root:000}");
}

/// <summary>شناسه‌های پایدار seed ساختاری Plants برای Batch C.</summary>
public static class PlantsTemplateParityIds
{
    private const string Ns = "019022c0";

    public static Guid Tag(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000a{index:000}");

    public static Guid AttributeDefinition(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000b{index:000}");

    public static Guid AttributeOption(int definition, int option) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c{definition:00}{option:00}");

    public static Guid LocalizedAttrName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000d{index:000}");

    public static Guid LocalizedTagName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000e{index:000}");

    public static Guid LocalizedOptionName(int definition, int option) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000f{definition:00}{option:00}");

    public static Guid ProductTagAssignment(int product, int tag) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000a{product:000}{tag:000}");

    public static Guid CategoryTagAssignment(int root, int tag) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000b{root:000}{tag:000}");

    public static Guid CategoryBinding(int root, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000c{root:000}{def:000}");

    public static Guid CategoryFacet(int root, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000d{root:000}{def:000}");

    public static Guid MegaMenuItem(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000002{root:000}");

    public static Guid MegaMenuTranslation(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000003{root:000}");

    public static Guid ProductAttrValue(int product, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000e{product:000}{def:000}");

    public static Guid VariantAxis(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000004{product:000}");

    public static Guid Variant(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000005{product:000}");

    public static Guid VariantAttrValue(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000006{product:000}");

    public static Guid ProductHistory(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000007{product:000}");

    public static Guid CategorySlugHistory(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000008{root:000}");
}

/// <summary>شناسه‌های پایدار seed ساختاری Beauty برای Batch C.</summary>
public static class BeautyTemplateParityIds
{
    private const string Ns = "019022c2";

    public static Guid Tag(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000a{index:000}");

    public static Guid AttributeDefinition(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000b{index:000}");

    public static Guid AttributeOption(int definition, int option) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000c{definition:00}{option:00}");

    public static Guid LocalizedAttrName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000d{index:000}");

    public static Guid LocalizedTagName(int index) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000000e{index:000}");

    public static Guid LocalizedOptionName(int definition, int option) =>
        Guid.Parse($"{Ns}-0000-7000-8000-0000000f{definition:00}{option:00}");

    public static Guid ProductTagAssignment(int product, int tag) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000a{product:000}{tag:000}");

    public static Guid CategoryTagAssignment(int root, int tag) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000b{root:000}{tag:000}");

    public static Guid CategoryBinding(int root, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000c{root:000}{def:000}");

    public static Guid CategoryFacet(int root, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000d{root:000}{def:000}");

    public static Guid MegaMenuItem(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000002{root:000}");

    public static Guid MegaMenuTranslation(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000003{root:000}");

    public static Guid ProductAttrValue(int product, int def) =>
        Guid.Parse($"{Ns}-0000-7000-8000-00000e{product:000}{def:000}");

    public static Guid VariantAxis(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000004{product:000}");

    public static Guid Variant(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000005{product:000}");

    public static Guid VariantAttrValue(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000006{product:000}");

    public static Guid ProductHistory(int product) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000007{product:000}");

    public static Guid CategorySlugHistory(int root) =>
        Guid.Parse($"{Ns}-0000-7000-8000-000000008{root:000}");
}


