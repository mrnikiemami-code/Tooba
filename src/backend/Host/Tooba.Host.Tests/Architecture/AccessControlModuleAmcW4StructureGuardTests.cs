using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-ACCESSCONTROL-AMC-001-W4 — Domain Aggregates, Models cohesion, Endpoints Domain ZERO.</summary>
public sealed class AccessControlModuleAmcW4StructureGuardTests
{
    [Fact]
    public void AccessControl_domain_foldering_models_split_and_endpoints_domain_zero()
    {
        var root = Repo();
        Assert.False(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Domain/AccessControlDomain.cs")));
        Assert.True(Directory.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Domain/Aggregates")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Enums/AccessOwnerScopeKind.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Enums/AccessScopeKind.cs")));

        Assert.False(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Models/AccessControlContracts.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Models/AccessControlDtos.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Ports/IAccessControlDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Exceptions/AccessControlException.cs")));

        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.DoesNotContain("using Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
            Assert.Contains("using Tooba.AccessControl.Contracts.Enums", text, StringComparison.Ordinal);
        }

        var endpointsCsproj = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj"));
        Assert.DoesNotContain("Tooba.AccessControl.Domain.csproj", endpointsCsproj, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"accessControlModuleAmc001W4\"", sot, StringComparison.Ordinal);
        Assert.Contains("ACCESSCONTROL_STRUCTURE_POLISH_APPLIED", sot, StringComparison.Ordinal);
    }

    private static string Repo()
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
