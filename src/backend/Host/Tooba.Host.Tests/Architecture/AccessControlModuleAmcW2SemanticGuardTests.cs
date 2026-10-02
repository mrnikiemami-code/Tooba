using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-ACCESSCONTROL-AMC-001-W2 — semantic/localization failure channel.</summary>
public sealed class AccessControlModuleAmcW2SemanticGuardTests
{
    [Fact]
    public void AccessControl_failures_use_stable_codes_catalog_and_no_code_contains_http_mapping()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Contracts/Errors/AccessControlErrorCodes.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Errors/AccessControlErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Resources/AccessControlErrors.fa.resx")));

        var exception = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Models/AccessControlContracts.cs"));
        Assert.Contains("AccessControlException(string code)", exception, StringComparison.Ordinal);
        Assert.DoesNotContain("AccessControlException(string code, string message)", exception, StringComparison.Ordinal);

        var directory = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Directories/AccessControlDirectory.cs"));
        Assert.DoesNotContain("کد نقش تکراری است", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("نقش یافت نشد", directory, StringComparison.Ordinal);

        var catalog = File.ReadAllText(Path.Combine(root,
            "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Permissions/PermissionCatalog.cs"));
        Assert.Contains("AccessControlErrorCodes.PermissionUnknown", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("مجوز ناشناخته", catalog, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.DoesNotContain("Code.Contains", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MapAccessError", text, StringComparison.Ordinal);
            Assert.Contains("AccessControlHttpErrors.From", text, StringComparison.Ordinal);
        }

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("AddAccessControlEndpointPresentation", program, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"accessControlModuleAmc001W2\"", sot, StringComparison.Ordinal);
        Assert.Contains("ACCESSCONTROL_SEMANTIC_LOCALIZATION_APPLIED", sot, StringComparison.Ordinal);
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
