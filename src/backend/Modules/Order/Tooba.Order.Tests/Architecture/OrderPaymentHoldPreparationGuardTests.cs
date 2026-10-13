using Xunit;

namespace Tooba.Order.Tests.Architecture;

/// <summary>
/// Durable guards for TB-P0-ORDER-PAYMENT-HOLD-COORDINATION-018: the payment-hold preparation seam must stay
/// an Order-owned, read-only-Order building block that takes its deadline from the configured policy and
/// calls the Inventory all-or-none extension. No endpoint, no PSP/wallet/payment attempt, no Order write.
/// </summary>
public sealed class OrderPaymentHoldPreparationGuardTests
{
    private const string ContractsFile =
        "src/backend/Modules/Order/Tooba.Order.Contracts/Payments/OrderPaymentHoldPreparationContracts.cs";

    private const string ServiceFile =
        "src/backend/Modules/Order/Tooba.Order.Application/Checkout/Process/OrderPaymentHoldPreparationService.cs";

    private const string ModuleFile =
        "src/backend/Modules/Order/Tooba.Order.Infrastructure/OrderModule.cs";

    [Fact]
    public void Seam_is_an_order_owned_port_without_a_caller_supplied_reservation_list()
    {
        var contracts = Read(ContractsFile);

        Assert.Contains("namespace Tooba.Order.Contracts.Payments;", contracts, StringComparison.Ordinal);
        Assert.Contains("public interface IOrderPaymentHoldPreparationPort", contracts, StringComparison.Ordinal);
        Assert.Contains("Task<OrderPaymentHoldPreparationResult> PrepareAsync(", contracts, StringComparison.Ordinal);
        // The request carries identity and a policy selector only.
        var request = contracts[contracts.IndexOf("record OrderPaymentHoldPreparationRequest", StringComparison.Ordinal)..];
        request = request[..request.IndexOf(';')];
        Assert.DoesNotContain("ReservationId", request, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpiresAt", request, StringComparison.Ordinal);
        Assert.DoesNotContain("Deadline", request, StringComparison.Ordinal);
        Assert.DoesNotContain("Amount", request, StringComparison.Ordinal);
    }

    [Fact]
    public void Seam_derives_reservations_from_persisted_lines_and_never_trusts_a_caller()
    {
        var service = Read(ServiceFile);

        Assert.Contains("snapshot.SellerOrders.SelectMany(order => order.Lines)", service, StringComparison.Ordinal);
        Assert.Contains("line.ReservationId", service, StringComparison.Ordinal);
        Assert.Contains("OrderPaymentHoldPreparationOutcome.ReservationSetIncomplete", service, StringComparison.Ordinal);
        // Uniqueness is enforced: one hold bound to two lines is ambiguity, not a set.
        Assert.Contains("distinctReservationIds.Length != reservationIds.Length", service, StringComparison.Ordinal);
        Assert.DoesNotContain("request.ReservationId", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Seam_takes_the_deadline_from_the_effective_policy_and_never_hardcodes_a_duration()
    {
        var service = Read(ServiceFile);

        Assert.Contains("_holdPolicy.ResolveUnpaidTimeoutAt(providerCode, now)", service, StringComparison.Ordinal);
        Assert.Contains("OrderPaymentHoldPreparationOutcome.PolicyUnavailable", service, StringComparison.Ordinal);
        Assert.DoesNotContain("AddHours", service, StringComparison.Ordinal);
        Assert.DoesNotContain("AddMinutes", service, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeSpan.From", service, StringComparison.Ordinal);
        Assert.DoesNotContain("120", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Seam_checks_order_payability_before_and_after_the_hold_extension()
    {
        var service = Read(ServiceFile);

        Assert.Contains("ReservationCyclePayabilityPolicy.IsActive(cycle)", service, StringComparison.Ordinal);
        Assert.Contains("ReservationCyclePayabilityPolicy.IsActive(afterCycle)", service, StringComparison.Ordinal);
        Assert.Contains("OrderPaymentHoldPreparationOutcome.CycleClosedAfterExtension", service, StringComparison.Ordinal);
        Assert.Contains("_holdExtension", service, StringComparison.Ordinal);
        Assert.Contains("ExtendHeldForPaymentAsync(distinctReservationIds, deadlineUtc", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Seam_makes_no_endpoint_psp_wallet_payment_attempt_or_order_write()
    {
        var service = Read(ServiceFile);

        Assert.DoesNotContain("DbContext", service, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", service, StringComparison.Ordinal);
        Assert.DoesNotContain("Psp", service, StringComparison.Ordinal);
        Assert.DoesNotContain("Gateway", service, StringComparison.Ordinal);
        Assert.DoesNotContain("Wallet", service, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentAttempt", service, StringComparison.Ordinal);
        Assert.DoesNotContain("InitiateAsync", service, StringComparison.Ordinal);
        Assert.DoesNotContain("Endpoint", service, StringComparison.Ordinal);
        Assert.DoesNotContain("CartConversion", service, StringComparison.Ordinal);
        Assert.DoesNotContain("TransactionScope", service, StringComparison.Ordinal);
        // Order reads only: no aggregate mutation API is reachable from this seam's source.
        Assert.DoesNotContain("Projection.ApplyVerifiedSuccess", service, StringComparison.Ordinal);
        Assert.DoesNotContain("CancelSellerOrderAsync", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Seam_is_registered_for_the_contracts_port()
    {
        var module = Read(ModuleFile);

        Assert.Contains(
            "services.AddScoped<IOrderPaymentHoldPreparationPort, Application.Checkout.Process.OrderPaymentHoldPreparationService>();",
            module,
            StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "src", "backend", "Tooba.slnx")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
