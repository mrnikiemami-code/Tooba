using Xunit;

using Tooba.Order.Application.Checkout.Abuse;
using Tooba.Order.Application.Checkout.Contracts;
using Tooba.Order.Application.Checkout.Policies;
using Tooba.Order.Application.Checkout.Process;
using Tooba.Order.Application.PurchaseVerification;
using Tooba.Order.Application.ReservationCycle.Contracts;
using Tooba.Order.Application.ReservationCycle.Policies;
using Tooba.Order.Application.ReservationCycle.Services;
using Tooba.Order.Application.Seller.Policies;

namespace Tooba.Host.Tests;

/// <summary>TB-TMAR-CHECKOUT-IMPL-W5 — Promotion checkout seam via Contracts.</summary>
public sealed class CheckoutImplW5PromotionContractTests
{
    private static string Root => FindRepoRoot();
    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));

    [Fact]
    public void Order_Application_no_longer_references_Promotion_Application()
    {
        var app = Read("src/backend/Modules/Order/Tooba.Order.Application/Tooba.Order.Application.csproj");
        Assert.DoesNotContain("Tooba.Promotion.Application", app, StringComparison.Ordinal);
        var infra = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/Tooba.Order.Infrastructure.csproj");
        Assert.Contains("Tooba.Promotion.Contracts", infra, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Application", infra, StringComparison.Ordinal);
    }

    [Fact]
    public void Checkout_directory_uses_checkout_promotion_port()
    {
        var checkout = Read("src/backend/Modules/Order/Tooba.Order.Infrastructure/Checkout/Persistence/CheckoutDirectory.cs");
        Assert.Contains("ICheckoutPromotionPort", checkout, StringComparison.Ordinal);
        Assert.Contains("EvaluateForCheckoutAsync", checkout, StringComparison.Ordinal);
        Assert.Contains("CheckoutPromotionEvaluationRequest", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("using Tooba.Promotion.Application", checkout, StringComparison.Ordinal);
        Assert.DoesNotContain("IPromotionEvaluator", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Promotion_contracts_and_adapter_are_promotion_owned()
    {
        var contracts = Read("src/backend/Modules/Promotion/Tooba.Promotion.Contracts/Checkout/CheckoutPromotionContracts.cs");
        Assert.Contains("interface ICheckoutPromotionPort", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Promotion.Domain", contracts, StringComparison.Ordinal);
        var adapter = Read("src/backend/Modules/Promotion/Tooba.Promotion.Application/Checkout/CheckoutPromotionAdapter.cs");
        Assert.Contains("class CheckoutPromotionAdapter : ICheckoutPromotionPort", adapter, StringComparison.Ordinal);
        var module = Read("src/backend/Modules/Promotion/Tooba.Promotion.Infrastructure/DependencyInjection/PromotionModule.cs");
        Assert.Contains("ICheckoutPromotionPort, CheckoutPromotionAdapter", module, StringComparison.Ordinal);
    }

    [Fact]
    public void App_to_app_baseline_removed_Order_Promotion_edge()
    {
        var baseline = Read("src/backend/Host/Tooba.Host.Tests/Baselines/tmar-app-to-app-edges.json");
        Assert.DoesNotContain("Tooba.Order.Application -> Tooba.Promotion.Application", baseline, StringComparison.Ordinal);
    }

    [Fact]
    public void Shared_TransactionScope_remains_in_process_manager()
    {
        var pm = Read("src/backend/Modules/Order/Tooba.Order.Application/Checkout/Process/CheckoutProcessManager.cs");
        Assert.Contains("TransactionScope", pm, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
