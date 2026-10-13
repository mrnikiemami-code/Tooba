using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>
/// TB-P0-CHECKOUT-START-PAYMENT-COMPOSER-019 — گاردهای منبع ترکیب‌کنندهٔ کلیک نهایی:
/// بدون endpoint، بدون ویرایش Host، بدون کد خطای ساختگی/سقف زمانی ثابت و بدون بازپیاده‌سازی commit.
/// </summary>
public sealed class StorefrontStartPaymentComposerGuardTests
{
    [Fact]
    public void Composer_publishes_no_http_endpoint_and_stays_inert()
    {
        var composer = ComposerSource();

        Assert.DoesNotContain("MapPost", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IEndpointRouteBuilder", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("ISender", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequestHandler", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("MediatR", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void No_module_or_host_route_publishes_start_payment()
    {
        foreach (var root in new[]
                 {
                     Path.Combine(OrderRoot(), "Tooba.Order.Endpoints"),
                     Path.Combine(RepoRoot(), "src", "backend", "Host", "Tooba.Host"),
                 })
        {
            foreach (var path in Sources(root))
            {
                var text = File.ReadAllText(path);
                Assert.DoesNotContain("start-payment", text, StringComparison.Ordinal);
                Assert.DoesNotContain("StorefrontStartPaymentComposer", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Composer_reuses_the_canonical_seams_and_reproduces_no_commit_mechanics()
    {
        var composer = ComposerSource();

        // مسیرهای متعارف: commit سفارش، آماده‌سازی ۰۱۸، درز Payment.
        Assert.Contains("CommitAsync", composer, StringComparison.Ordinal);
        Assert.Contains("PrepareAsync", composer, StringComparison.Ordinal);
        Assert.Contains("InitiateForCheckoutAsync", composer, StringComparison.Ordinal);

        // هیچ بازتولید موجودی/تراکنش/سهمیه/تبدیل سبد/چرخه در این لایه.
        foreach (var forbidden in new[]
                 {
                     "TransactionScope",
                     "ReserveForCheckout",
                     "ConvertForCheckout",
                     "EnsureCanStartInitialReservation",
                     "ReleaseAsync",
                     "SaveChanges",
                     "OrderDbContext",
                 })
        {
            Assert.DoesNotContain(forbidden, composer, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Composer_invents_no_error_code_and_forges_no_payment_code()
    {
        var composer = ComposerSource();

        Assert.DoesNotContain("\"checkout.", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"shipping.", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("\"payment.", composer, StringComparison.Ordinal);
        Assert.Contains("StorefrontOrderErrors.", composer, StringComparison.Ordinal);
        Assert.Contains("exception.Code", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Composer_accepts_no_reservation_amount_expiry_or_checkout_identity_from_the_caller()
    {
        var request = Slice(
            ComposerSource(),
            "public sealed record StorefrontStartPaymentRequest(",
            "public enum StorefrontStartPaymentFault");

        Assert.DoesNotContain("ReservationId", request, StringComparison.Ordinal);
        Assert.DoesNotContain("CheckoutId", request, StringComparison.Ordinal);
        Assert.DoesNotContain("Amount", request, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpiresAt", request, StringComparison.Ordinal);
        Assert.DoesNotContain("Deadline", request, StringComparison.Ordinal);
        Assert.Contains("CartId", request, StringComparison.Ordinal);
        Assert.Contains("ProviderCode", request, StringComparison.Ordinal);
    }

    [Fact]
    public void Composer_introduces_no_wall_clock_and_no_fixed_hold_duration()
    {
        var composer = ComposerSource();

        Assert.DoesNotContain("DateTimeOffset.UtcNow", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.Now", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHours(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AddMinutes(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("AddSeconds(", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeSpan.From", composer, StringComparison.Ordinal);

        // مهلت فقط از درز ۰۱۸ می‌آید (سیاست پویا) و بازاستفاده می‌شود.
        Assert.Contains("preparation.RequestedDeadlineUtc", composer, StringComparison.Ordinal);
        Assert.Contains("preparation.GuaranteedUntilUtc", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Composer_stays_contracts_only_for_foreign_modules()
    {
        var composer = ComposerSource();

        Assert.DoesNotContain("Tooba.Payment.Application", composer, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Payment.Infrastructure", composer, StringComparison.Ordinal);
        Assert.Contains("Tooba.Payment.Contracts.Storefront", composer, StringComparison.Ordinal);
        Assert.Contains("Tooba.Order.Contracts.Payments", composer, StringComparison.Ordinal);
    }

    [Fact]
    public void Order_module_registers_the_inert_composer()
    {
        var module = File.ReadAllText(Path.Combine(
            OrderRoot(), "Tooba.Order.Infrastructure", "OrderModule.cs"));

        Assert.Contains(
            "services.AddScoped<Application.Storefront.Services.StorefrontStartPaymentComposer>();",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Same_idempotency_key_is_namespaced_for_commit_and_payment()
    {
        var composer = ComposerSource();

        Assert.Contains("CommitKeyPrefix", composer, StringComparison.Ordinal);
        Assert.Contains("PaymentKeyPrefix", composer, StringComparison.Ordinal);
        Assert.Contains("public const string CommitKeyPrefix = \"start-payment:commit:\"", composer, StringComparison.Ordinal);
        Assert.Contains("public const string PaymentKeyPrefix = \"start-payment:payment:\"", composer, StringComparison.Ordinal);
        Assert.Contains("SharedIdempotencyKeyMaxLength", composer, StringComparison.Ordinal);
    }

    private static string ComposerSource() => File.ReadAllText(Path.Combine(
        OrderRoot(),
        "Tooba.Order.Application",
        "Storefront",
        "Services",
        "StorefrontStartPaymentComposer.cs"));

    private static string Slice(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"marker not found: {start}");
        var to = source.IndexOf(end, from, StringComparison.Ordinal);
        Assert.True(to > from, $"marker not found: {end}");
        return source[from..to];
    }

    private static IEnumerable<string> Sources(string root)
    {
        Assert.True(Directory.Exists(root), root);
        return Directory
            .GetFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }

    private static string OrderRoot() =>
        Path.Combine(RepoRoot(), "src", "backend", "Modules", "Order");
}
