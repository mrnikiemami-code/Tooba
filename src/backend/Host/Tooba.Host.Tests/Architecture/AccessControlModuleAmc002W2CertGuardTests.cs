using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-ACCESSCONTROL-AMC-002-W2-CERT — re-cert after SellerDev Result closure.</summary>
public sealed class AccessControlModuleAmc002W2CertGuardTests
{
    [Fact]
    public void AccessControl_amc002_endpoints_results_json_zero_and_foreign_coupling_zero()
    {
        var root = Repo();
        var endpointsRoot = Path.Combine(root,
            "src", "backend", "Modules", "AccessControl", "Tooba.AccessControl.Endpoints");
        foreach (var path in Directory.EnumerateFiles(endpointsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (AccessControlException", text, StringComparison.Ordinal);
        }

        var query = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Development/Seller/GetSellerDevContextsQuery.cs"));
        Assert.Contains("IRequest<Result<SellerDevContextsView>>", query, StringComparison.Ordinal);
        Assert.Contains("AccessControlErrorCodes.SellerDevNotReady", query, StringComparison.Ordinal);

        var codes = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Errors/AccessControlErrorCodes.cs"));
        Assert.Contains("SellerDevUnavailable", codes, StringComparison.Ordinal);
        Assert.Contains("SellerDevNotReady", codes, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj",
                 })
        {
            var csproj = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.DoesNotContain("Identity.Application", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Catalog.Domain", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Party.Infrastructure", csproj, StringComparison.Ordinal);
        }

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"accessControlModuleAmc002W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("ACCESSCONTROL_AMC002_STRUCTURE_CERTIFIED", sot, StringComparison.Ordinal);
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
