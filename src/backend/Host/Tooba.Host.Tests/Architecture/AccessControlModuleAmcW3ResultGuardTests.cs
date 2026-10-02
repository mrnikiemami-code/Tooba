using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-ACCESSCONTROL-AMC-001-W3 — Result&lt;T&gt; / ApiResponseFactory pipeline.</summary>
public sealed class AccessControlModuleAmcW3ResultGuardTests
{
    [Fact]
    public void AccessControl_handlers_return_Result_and_endpoints_use_ApiResponseFactory_From()
    {
        var root = Repo();
        var operation = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Composition/AccessControlOperation.cs"));
        Assert.Contains("AccessControlException", operation, StringComparison.Ordinal);
        Assert.Contains("Result.Failure", operation, StringComparison.Ordinal);
        Assert.Contains("NotFoundIfNull", operation, StringComparison.Ordinal);

        var createRole = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Roles/Commands/CreateRoleCommand.cs"));
        Assert.Contains("IRequest<Result<AccessRoleDto>>", createRole, StringComparison.Ordinal);
        Assert.Contains("AccessControlOperation.ExecuteAsync", createRole, StringComparison.Ordinal);

        var archive = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Roles/Commands/ArchiveRoleCommand.cs"));
        Assert.Contains("IRequest<Result>", archive, StringComparison.Ordinal);
        Assert.DoesNotContain("IRequest<Unit>", archive, StringComparison.Ordinal);

        var getRole = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Roles/Queries/GetRoleQuery.cs"));
        Assert.Contains("IRequest<Result<AccessRoleDto>>", getRole, StringComparison.Ordinal);
        Assert.Contains("NotFoundIfNull", getRole, StringComparison.Ordinal);

        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json(await sender.Send", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (AccessControlException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AccessControlHttpErrors.From", text, StringComparison.Ordinal);
        }

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"accessControlModuleAmc001W3\"", sot, StringComparison.Ordinal);
        Assert.Contains("ACCESSCONTROL_RESULT_PIPELINE_APPLIED", sot, StringComparison.Ordinal);
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
