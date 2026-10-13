using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Inventory.Tests.Architecture;

/// <summary>
/// Durable guards for the atomic hold extension: it must own exactly one local Inventory transaction,
/// never an ambient scope, never a network/provider call, never shorten a hold, never revive a
/// released/expired one, judge deadlines by the database clock and be registered in the module.
/// </summary>
public sealed class InventoryPaymentHoldExtensionGuardTests
{
    private const string ExtensionFile =
        "src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/Directories/InventoryDirectory.HoldExtension.cs";

    private const string ContractsFile =
        "src/backend/Modules/Inventory/Tooba.Inventory.Contracts/Orders/InventoryPaymentHoldExtensionContracts.cs";

    [Fact]
    public void Extension_owns_one_local_transaction_and_never_an_ambient_scope()
    {
        var extension = Read(ExtensionFile);

        Assert.Contains("BeginTransactionAsync", extension, StringComparison.Ordinal);
        Assert.Contains("CommitAsync", extension, StringComparison.Ordinal);
        Assert.Contains("RollbackAsync", extension, StringComparison.Ordinal);
        Assert.Contains("Transaction.Current is not null", extension, StringComparison.Ordinal);
        Assert.Contains(
            "InventoryPaymentHoldExtensionResult.AmbientTransactionUnsupported(requested.Length)",
            extension,
            StringComparison.Ordinal);
        Assert.DoesNotContain("new TransactionScope", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("TransactionScopeOption", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("EnlistTransaction", extension, StringComparison.Ordinal);
    }

    [Fact]
    public void Extension_is_all_or_none_with_deterministic_locking_and_database_clock_deadlines()
    {
        var extension = Read(ExtensionFile);

        Assert.Contains("ORDER BY r.reservation_id", extension, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", extension, StringComparison.Ordinal);
        Assert.Contains("clock_timestamp()", extension, StringComparison.Ordinal);
        Assert.Contains("AND r.status = 'Held'", extension, StringComparison.Ordinal);
        Assert.Contains("r.expires_at IS NULL OR r.expires_at > clock_timestamp()", extension, StringComparison.Ordinal);
        // All-or-none: a count mismatch must roll back instead of committing a partial set.
        Assert.Contains("if (liveReservationIds.Count != requested.Length)", extension, StringComparison.Ordinal);
        Assert.Contains("if (deadlineUtc <= databaseNowUtc)", extension, StringComparison.Ordinal);
        Assert.Matches(
            new Regex(@"liveReservationIds\.Count != requested\.Length[\s\S]{0,400}RollbackAsync"),
            extension);
        Assert.Matches(
            new Regex(@"deadlineUtc <= databaseNowUtc[\s\S]{0,400}RollbackAsync"),
            extension);
    }

    [Fact]
    public void Extension_never_shortens_or_revives_a_hold()
    {
        var extension = Read(ExtensionFile);

        Assert.Contains(
            "expires_at = CASE WHEN expires_at IS NULL THEN NULL ELSE GREATEST(expires_at, @deadline) END",
            extension,
            StringComparison.Ordinal);
        Assert.DoesNotContain("StockReservationStatus.Released", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("MoveTo(", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE", extension, StringComparison.Ordinal);
    }

    [Fact]
    public void Extension_makes_no_provider_or_foreign_module_call_and_no_state_write()
    {
        var extension = Read(ExtensionFile);

        Assert.DoesNotContain("_gateways", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("Wallet", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("Psp", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("_offers", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("_catalog", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChangesAsync", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("Guid.NewGuid()", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenConnectionAsync", extension, StringComparison.Ordinal);
        Assert.DoesNotContain("new InvalidOperationException", extension, StringComparison.Ordinal);
    }

    [Fact]
    public void Extension_contract_is_inventory_owned_and_registered_in_the_module()
    {
        var contracts = Read(ContractsFile);
        Assert.Contains("namespace Tooba.Inventory.Contracts.Orders;", contracts, StringComparison.Ordinal);
        Assert.Contains("public interface IInventoryPaymentHoldExtensionPort", contracts, StringComparison.Ordinal);
        Assert.Contains("ExtendHeldForPaymentAsync(", contracts, StringComparison.Ordinal);
        // Ownership of the ids is explicitly not claimed, and the clock authority is stated.
        Assert.Contains("مالکیت", contracts, StringComparison.Ordinal);
        Assert.Contains("ساعت دیتابیس", contracts, StringComparison.Ordinal);

        var module = Read("src/backend/Modules/Inventory/Tooba.Inventory.Infrastructure/DependencyInjection/InventoryModule.cs");
        Assert.Contains(
            "services.AddScoped<IInventoryPaymentHoldExtensionPort>(sp => (InventoryDirectory)sp.GetRequiredService<IInventoryDirectory>());",
            module,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Extension_regression_proof_runs_against_a_disposable_database()
    {
        var proof = Read("src/backend/Modules/Inventory/Tooba.Inventory.Tests/Behavior/InventoryHoldExtensionPostgresTests.cs");

        Assert.Contains("CREATE DATABASE", proof, StringComparison.Ordinal);
        Assert.Contains("DROP DATABASE IF EXISTS", proof, StringComparison.Ordinal);
        // The live Development database must never be opened by this proof.
        Assert.DoesNotContain("Database=tooba_alpha", proof, StringComparison.Ordinal);
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
