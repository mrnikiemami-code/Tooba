using Xunit;

namespace Tooba.Shipping.Tests.Packaging;

/// <summary>
/// TB-SHIPPING-PACKAGING-CUSTOMER-CHARGE-177 — مرز مالکیتی «هزینهٔ بسته‌بندی» در برابر «کرایهٔ مشتری».
/// </summary>
/// <remarks>
/// <para>
/// این آزمون‌ها یک تصمیم تجاری نمی‌سازند؛ فقط رفتار امروز را به‌صورت اجراشدنی قفل می‌کنند تا هیچ
/// Task بعدی نتواند بدون تصمیم صریح معماری، مبلغ بسته‌بندی را بی‌سروصدا داخل مبلغ مشتری جمع کند.
/// </para>
/// <para>
/// واقعیت ردیابی‌شده: سیاست بسته‌بندی فقط از مسیر ارزیابی <em>عملیاتی</em> خوانده می‌شود
/// (<see cref="Tooba.Shipping.Domain.Operations.ShippingOperationalQuoteEvaluator"/>) که خودش
/// فقط-خواندنی و ادمین‌محور است. مسیر نرخ مشتری
/// (<see cref="Tooba.Shipping.Domain.Rates.ShippingRatePolicyEngine"/>) هیچ ورودی بسته‌بندی ندارد.
/// </para>
/// </remarks>
public sealed class ShippingPackagingCustomerChargeBoundaryTests
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

    private static string ModuleRoot(params string[] segments)
        => Path.Combine([RepoRoot(), "src", "backend", "Modules", "Shipping", .. segments]);

    private static string ReadModuleFile(params string[] segments)
        => File.ReadAllText(ModuleRoot(segments));

    private static IEnumerable<string> ProductionCsFiles(params string[] segments)
        => Directory.EnumerateFiles(ModuleRoot(segments), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Tooba.Shipping.Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    [Fact]
    public void Customer_rate_quote_engine_has_no_packaging_input()
    {
        var source = ReadModuleFile("Tooba.Shipping.Domain", "Rates", "ShippingRatePolicyEngine.cs");

        // امضای هر دو overload فقط نرخ/پوشش/اعلام حالت روش را می‌گیرد؛ هیچ مؤلفهٔ بسته‌بندی وارد نمی‌شود.
        Assert.Contains("public static ShippingQuote Quote(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Packaging", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ShippingPackagingPolicy", source, StringComparison.Ordinal);

        // ورودی نرخ هم هیچ فیلد بسته‌بندی ندارد (نوع مشترک مسیر مشتری و مسیر عملیاتی).
        var request = ReadModuleFile("Tooba.Shipping.Contracts", "Rates", "ShippingRatePolicyContracts.cs");
        Assert.Contains("public sealed record ShippingQuoteRequest(", request, StringComparison.Ordinal);
        Assert.DoesNotContain("Packaging", request, StringComparison.Ordinal);
    }

    [Fact]
    public void Rates_contracts_never_expose_packaging()
    {
        var offenders = ProductionCsFiles("Tooba.Shipping.Contracts", "Rates")
            .Where(path => File.ReadAllText(path).Contains("Packaging", StringComparison.Ordinal))
            .Select(path => Path.GetFileName(path))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Packaging_fee_is_reachable_only_through_the_read_only_operational_quote()
    {
        var consumers = ProductionCsFiles()
            .Where(path => Path.GetFileName(path) != "ShippingPackagingFeeResolver.cs")
            .Where(path => File.ReadAllText(path).Contains("ShippingPackagingFeeResolver", StringComparison.Ordinal))
            .Select(path => Path.GetFileName(path))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // تنها مصرف‌کنندهٔ resolver، ارزیاب عملیاتی است (به‌همراه پیاده‌سازی درز فقط-خواندنی).
        Assert.Equal(
            ["ShippingOperationalQuoteEvaluator.cs", "ShippingPackagingFeeLookup.cs"],
            consumers);
    }

    [Fact]
    public void Operational_quote_surface_is_authorized_read_only_and_never_mutates_a_policy()
    {
        var source = ReadModuleFile(
            "Tooba.Shipping.Endpoints",
            "Admin",
            "ShippingOperationalQuoteEndpoints.cs");

        Assert.Contains("MapPost(\"/shipping-services/{serviceCode}/operational-quote\"", source, StringComparison.Ordinal);
        Assert.Contains("ShippingAdminPermissions.View", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ShippingAdminPermissions.Manage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", source, StringComparison.Ordinal);

        // هیچ نوشتن/تغییر سیاستی در این مسیر وجود ندارد.
        Assert.DoesNotContain("CreateShippingPackagingPolicyCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UpdateShippingPackagingPolicyCommand", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SetShippingPackagingPolicyActiveCommand", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Packaging_payer_contract_still_has_no_tax_or_on_delivery_variant()
    {
        var contracts = ReadModuleFile("Tooba.Shipping.Contracts", "Packaging", "ShippingPackagingContracts.cs");

        // امروز فقط «هنگام تسویه» مجاز است؛ افزودن طرف دیگر یک تصمیم قراردادی جدید است، نه فلگ.
        var payerBlock = Slice(contracts, "public enum ShippingPackagingChargePayer", "}");
        Assert.Contains("Checkout = 0", payerBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("CollectOnDelivery", payerBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("Seller", payerBlock, StringComparison.Ordinal);
        Assert.DoesNotContain("Marketplace", payerBlock, StringComparison.Ordinal);

        // هیچ مفهوم مالیاتی/سهم/تسویه‌ای در قرارداد بسته‌بندی وجود ندارد.
        Assert.DoesNotContain("Tax", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Settlement", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Payout", contracts, StringComparison.Ordinal);
    }

    private static string Slice(string source, string startMarker, string endMarker)
    {
        var start = source.IndexOf(startMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"marker not found: {startMarker}");
        var end = source.IndexOf(endMarker, start, StringComparison.Ordinal);
        Assert.True(end > start, $"end marker not found after: {startMarker}");
        return source[start..(end + endMarker.Length)];
    }
}
