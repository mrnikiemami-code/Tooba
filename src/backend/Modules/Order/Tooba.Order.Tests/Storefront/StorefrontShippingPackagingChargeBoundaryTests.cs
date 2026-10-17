using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177 — مرز «مبلغ قابل پرداخت مشتری» در Order.
/// </summary>
/// <remarks>
/// <para>
/// این آزمون یک قاعدهٔ تجاری تازه نمی‌سازد و هیچ مبلغی را فعال نمی‌کند؛ فقط رفتار امروز را قفل
/// می‌کند: مبلغ ارسال مشتری از نرخ مالک Shipping می‌آید و مؤلفهٔ بسته‌بندی تنها از مسیر سایهٔ
/// عملیاتیِ پیش‌فرض‌خاموش دیده می‌شود که خروجی‌اش هیچ‌گاه به پیش‌نویس/پرداخت نوشته نمی‌شود.
/// </para>
/// </remarks>
public sealed class StorefrontShippingPackagingChargeBoundaryTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string OrderModuleRoot()
        => Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order");

    private static IEnumerable<string> ProductionCsFiles()
        => Directory.EnumerateFiles(OrderModuleRoot(), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Tooba.Order.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string Relative(string path)
        => Path.GetRelativePath(OrderModuleRoot(), path).Replace('\\', '/');

    [Fact]
    public void Packaging_is_confined_to_the_default_off_operational_shadow()
    {
        var referencing = ProductionCsFiles()
            .Where(path => File.ReadAllText(path).Contains("Packaging", StringComparison.Ordinal))
            .Select(Relative)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // سه فایل سایهٔ عملیاتی. هیچ مسیر cart/draft/payment در این فهرست نیست.
        Assert.Equal(
            [
                "Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowComparer.cs",
                "Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowContracts.cs",
                "Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowObserver.cs",
            ],
            referencing);
    }

    [Fact]
    public void Customer_shipping_charge_resolver_uses_the_rate_lookup_and_never_the_operational_quote()
    {
        var source = File.ReadAllText(Path.Combine(
            OrderModuleRoot(),
            "Tooba.Order.Application",
            "Storefront",
            "Shipping",
            "OwnedRate",
            "StorefrontShippingOwnedRateResolver.cs"));

        Assert.Contains("IShippingRateQuoteLookup", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IShippingOperationalQuoteLookup", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IShippingPackagingFeeLookup", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IShippingShipmentCostLookup", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Packaging", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Operational_quote_lookup_has_exactly_one_consumer_in_order()
    {
        var consumers = ProductionCsFiles()
            .Where(path => File.ReadAllText(path).Contains("IShippingOperationalQuoteLookup", StringComparison.Ordinal))
            .Select(Relative)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "Tooba.Order.Application/Storefront/Shipping/OperationalShadow/StorefrontShippingOperationalShadowObserver.cs",
                "Tooba.Order.Infrastructure/OrderModule.cs",
            ],
            consumers);
    }

    [Fact]
    public void Operational_shadow_gate_is_double_flagged_and_off_by_absence()
    {
        var options = File.ReadAllText(Path.Combine(
            OrderModuleRoot(),
            "Tooba.Order.Application",
            "Storefront",
            "Shipping",
            "OperationalShadow",
            "StorefrontShippingOperationalShadowOptions.cs"));

        // هر دو پرچم بدون مقدار اولیه‌اند: نبودِ پیکربندی یعنی خاموش، نه «فعال».
        Assert.Contains("public bool Enabled { get; set; }", options, StringComparison.Ordinal);
        Assert.Contains("public bool PrerequisitesConfirmed { get; set; }", options, StringComparison.Ordinal);
        Assert.DoesNotContain("= true", options, StringComparison.Ordinal);
    }

    [Fact]
    public void A_charged_packaging_amount_is_reported_as_not_reflected()
    {
        var comparer = File.ReadAllText(Path.Combine(
            OrderModuleRoot(),
            "Tooba.Order.Application",
            "Storefront",
            "Shipping",
            "OperationalShadow",
            "StorefrontShippingOperationalShadowComparer.cs"));

        // سیاست صریح مالک Order: مؤلفهٔ بسته‌بندی به‌طور خاموش داخل مبلغ پرداختی مشتری جمع نمی‌شود.
        Assert.Contains(
            "StorefrontShippingOperationalShadowOutcome.PackagingNotReflected",
            comparer,
            StringComparison.Ordinal);
        Assert.Contains("packaging > 0m", comparer, StringComparison.Ordinal);
    }
}
