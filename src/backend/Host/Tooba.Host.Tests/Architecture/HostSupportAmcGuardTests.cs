using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-SUPPORT-AMC-001 — Host Support HOST_ZERO.</summary>
public sealed class HostSupportAmcGuardTests
{
    [Fact]
    public void Host_Support_folder_is_absent_and_module_owns_seed_bootstrap()
    {
        var root = FindRepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/Support")));

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapSupportEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddSupportEndpointPresentation", program, StringComparison.Ordinal);
        Assert.Contains("SupportDevelopmentSeedHost.ApplyAsync", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.Support", program, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Support/Tooba.Support.Infrastructure/Development/SupportDevelopmentSeedBootstrap.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/SupportDevelopmentSeedHost.cs")));

        var composition = File.ReadAllText(Path.Combine(
            root, "src/backend/Host/Tooba.Host/Composition/SupportDevelopmentSeedHost.cs"));
        Assert.Contains("SupportDevelopmentSeedBootstrap.ApplyAsync", composition, StringComparison.Ordinal);
        Assert.Contains("StorefrontGuestActor.ActorId", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("SupportDbContext", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Order.Application", composition, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostSupportAmc_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostSupportAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-SUPPORT-AMC-001", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_SUPPORT_AMC_001_CLOSED_HOST_ZERO", sot, StringComparison.Ordinal);
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
