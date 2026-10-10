using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-WALLET-AMC-001 — Host Wallet HOST_ZERO.</summary>
public sealed class HostWalletAmcGuardTests
{
    [Fact]
    public void Host_Wallet_folder_is_absent_and_module_owns_seed_bootstrap()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Wallet")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapWalletEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddWalletEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("WalletDevelopmentSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Wallet", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Wallet/Tooba.Wallet.Infrastructure/Adapters/WalletDevelopmentSeedBootstrap.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/WalletDevelopmentSeedHost.cs")));

        var composition = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/WalletDevelopmentSeedHost.cs"));
        Assert.Contains("WalletDevelopmentSeedBootstrap.ApplyAsync", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("WalletDbContext", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", composition, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostWalletAmc_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostWalletAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-WALLET-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_WALLET_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
