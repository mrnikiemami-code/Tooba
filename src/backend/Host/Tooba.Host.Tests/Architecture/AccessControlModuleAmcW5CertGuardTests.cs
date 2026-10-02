using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-ACCESSCONTROL-AMC-001-W5-CERT — ARCH-COMPLETE-002 re-certification.</summary>
public sealed class AccessControlModuleAmcW5CertGuardTests
{
    [Fact]
    public void AccessControl_amc_complete_reference_pattern_certified()
    {
        var root = Repo();

        // Solution / VS grouping (W1)
        var slnx = File.ReadAllText(Path.Combine(root, "src/backend/Tooba.slnx"));
        Assert.Contains("/Modules/AccessControl/", slnx, StringComparison.Ordinal);
        Assert.Contains("Tooba.AccessControl.Endpoints", slnx, StringComparison.Ordinal);

        // Semantic / Result / Structure waves durable (W2–W4)
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Host/Tooba.Host.Tests/Architecture/AccessControlModuleAmcW2SemanticGuardTests.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Host/Tooba.Host.Tests/Architecture/AccessControlModuleAmcW3ResultGuardTests.cs")));
        Assert.True(File.Exists(Path.Combine(root,
            "src/backend/Host/Tooba.Host.Tests/Architecture/AccessControlModuleAmcW4StructureGuardTests.cs")));

        // Foreign App/Infra/Domain coupling ZERO — Contracts-only abroad
        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Tooba.AccessControl.Application.csproj",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Infrastructure/Tooba.AccessControl.Infrastructure.csproj",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Domain/Tooba.AccessControl.Domain.csproj",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Tooba.AccessControl.Endpoints.csproj",
                 })
        {
            var csproj = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.DoesNotContain("Identity.Application", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Identity.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Identity.Domain", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Catalog.Application", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Catalog.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Catalog.Domain", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Party.Application", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Party.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("Party.Domain", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("OperatorProfile.Application", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("OperatorProfile.Infrastructure", csproj, StringComparison.Ordinal);
            Assert.DoesNotContain("OperatorProfile.Domain", csproj, StringComparison.Ordinal);
        }

        // Endpoints Domain ZERO + Result path
        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Admin/AccessControlAdminSellerEndpoints.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Endpoints/Seller/AccessControlSellerEndpoints.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            Assert.DoesNotContain("using Tooba.AccessControl.Domain", text, StringComparison.Ordinal);
            Assert.Contains("api.From(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (AccessControlException", text, StringComparison.Ordinal);
        }

        // Validators 6/6 present
        foreach (var relative in new[]
                 {
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Validators/Role/CreateRoleCommandValidator.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Validators/Role/UpdateRoleCommandValidator.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Validators/Role/CloneRoleCommandValidator.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Validators/Assignment/AssignRoleCommandValidator.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Validators/Permissions/SetRolePermissionsCommandValidator.cs",
                     "src/backend/Modules/AccessControl/Tooba.AccessControl.Application/Validators/Ceiling/SetSellerCeilingCommandValidator.cs",
                 })
        {
            Assert.True(File.Exists(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar))));
        }

        var manifest = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-module-structure-manifests.json"));
        Assert.Contains("\"module\": \"AccessControl\"", manifest, StringComparison.Ordinal);
        Assert.Contains("Tooba.AccessControl.Domain", manifest, StringComparison.Ordinal);
        Assert.Contains("AccessControlDomain.cs", manifest, StringComparison.Ordinal);

        var sot = File.ReadAllText(Path.Combine(root, "docs/architecture/tmar-current-state.json"));
        Assert.Contains("\"accessControlModuleAmc001W5Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("ACCESSCONTROL_AMC_STRUCTURE_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"structureCertified\": true", sot, StringComparison.Ordinal);
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
