using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Inventory.Tests.Architecture;

/// <summary>
/// Durable guards for the Inventory payment-boundary hold gate: it must stay read-only, must never
/// own a transaction, must never trust caller/app time, must lock rows in a deterministic order and
/// must fail closed when the ambient transaction guarantee is absent.
/// </summary>
public sealed class InventoryPaymentHoldGateGuardTests
{
    private const string GateFile =
        "src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.PaymentHoldGate.cs";

    private const string ContractsFile =
        "src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Orders/InventoryPaymentHoldGateContracts.cs";

    [Fact]
    public void Gate_fails_closed_without_a_caller_owned_ambient_transaction()
    {
        var gate = Read(GateFile);

        Assert.Contains("if (Transaction.Current is null)", gate, StringComparison.Ordinal);
        Assert.Contains(
            "InventoryPaymentHoldGateResult.AmbientTransactionRequired(requested.Length)",
            gate,
            StringComparison.Ordinal);
        Assert.Contains("ConnectionState.Open", gate, StringComparison.Ordinal);
        Assert.Contains("OpenConnectionAsync", gate, StringComparison.Ordinal);
        Assert.Contains("HasOpenTransactionBlockAsync", gate, StringComparison.Ordinal);
        Assert.Contains("txid_current_if_assigned()", gate, StringComparison.Ordinal);
        Assert.Contains("catch (PostgresException)", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_never_owns_a_transaction_and_never_writes()
    {
        var gate = Read(GateFile);

        Assert.DoesNotContain("BeginTransaction", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("CommitAsync", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("Rollback", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("TransactionScope", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteUpdate", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteDelete", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("MoveTo(", gate, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex(@"(?m)^\s*(UPDATE|INSERT|DELETE)\s"), gate);
    }

    [Fact]
    public void Gate_uses_row_locks_in_deterministic_order_and_the_database_clock()
    {
        var gate = Read(GateFile);

        Assert.Contains("FOR UPDATE", gate, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"ORDER BY r\.reservation_id\s+FOR UPDATE"), gate);
        Assert.Contains("ORDER BY r.reservation_id", gate, StringComparison.Ordinal);
        Assert.Contains("AND r.status = 'Held'", gate, StringComparison.Ordinal);
        Assert.Contains("r.expires_at IS NULL OR r.expires_at > clock_timestamp()", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("pg_locks", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_never_trusts_caller_or_application_time()
    {
        var gate = Read(GateFile);

        Assert.DoesNotContain("_clock", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.UtcNow", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("utcNow", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Gate_contract_is_owned_by_inventory_and_registered_in_the_module()
    {
        var contracts = Read(ContractsFile);
        Assert.Contains("namespace Tooba.Inventory.Contracts.Orders;", contracts, StringComparison.Ordinal);
        Assert.Contains("public interface IInventoryPaymentHoldGate", contracts, StringComparison.Ordinal);
        Assert.Contains("VerifyHeldForPaymentAsync(", contracts, StringComparison.Ordinal);
        Assert.Contains("تراکنش محیطی", contracts, StringComparison.Ordinal);

        var module = Read("src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs");
        Assert.Contains(
            "services.AddScoped<IInventoryPaymentHoldGate>(sp => (InventoryDirectory)sp.GetRequiredService<IInventoryDirectory>());",
            module,
            StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null
               && !File.Exists(Path.Combine(dir.FullName, "docs", "ai", "TOOBA-PIPELINE-PROTOCOL.md"))
               && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
