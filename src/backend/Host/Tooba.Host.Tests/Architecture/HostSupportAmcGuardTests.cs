using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>Durable guards for TB-TMAR-HOST-SUPPORT-AMC-001 / R1 — Host Support HOST_ZERO + AccessControl Contracts seam.</summary>
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
        Assert.Contains("IAccessControlDevelopmentSeedPrelude", composition, StringComparison.Ordinal);
        Assert.Contains("Tooba.AccessControl.Contracts.Development", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("SupportDbContext", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Database.MigrateAsync", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("Order.Application", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Application", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Domain", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Infrastructure", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Persistence", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("ISellerDevContextStore", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("IAccessControlDirectory", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessOwnerScope", composition, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessOwnerScopeKind", composition, StringComparison.Ordinal);
    }

    [Fact]
    public void AccessControl_development_seed_contract_is_neutral_and_path_exact()
    {
        var root = FindRepoRoot();
        var contractPath = Path.Combine(
            root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Development/AccessControlDevelopmentSeedContracts.cs");
        Assert.True(File.Exists(contractPath));
        var text = File.ReadAllText(contractPath);

        var nsMatch = Regex.Match(text, @"^namespace\s+([^\s;{]+)", RegexOptions.Multiline);
        Assert.True(nsMatch.Success);
        Assert.Equal("Tooba.AccessControl.Contracts.Development", nsMatch.Groups[1].Value);

        Assert.Contains("IAccessControlDevelopmentSeedPrelude", text, StringComparison.Ordinal);
        Assert.Contains("AccessControlDevelopmentSeedActors", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Application", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Domain", text, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControl.Infrastructure", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", text, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", text, StringComparison.Ordinal);
        Assert.DoesNotContain("object?", text, StringComparison.Ordinal);

        var implPath = Path.Combine(
            root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Development/AccessControlDevelopmentSeedPrelude.cs");
        Assert.True(File.Exists(implPath));
        var impl = File.ReadAllText(implPath);
        Assert.Contains("IAccessControlDevelopmentSeedPrelude", impl, StringComparison.Ordinal);
        Assert.Contains("namespace Tooba.AccessControl.Infrastructure.Development", impl, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host", impl, StringComparison.Ordinal);

        var module = File.ReadAllText(Path.Combine(
            root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/AccessControlModule.cs"));
        Assert.Contains("IAccessControlDevelopmentSeedPrelude, AccessControlDevelopmentSeedPrelude", module, StringComparison.Ordinal);
    }

    [Fact]
    public void SoT_hostSupportAmc_r1_present()
    {
        var sot = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"hostSupportAmc\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostSupportAmcR1\"", sot, StringComparison.Ordinal);
        Assert.Contains("TB-TMAR-HOST-SUPPORT-AMC-001-R1", sot, StringComparison.Ordinal);
        Assert.Contains("IAccessControlDevelopmentSeedPrelude", sot, StringComparison.Ordinal);
        Assert.Contains("CLOSED_HOST_ZERO_R1_ACCESSCONTROL_CONTRACTS_SEAM", sot, StringComparison.Ordinal);
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
