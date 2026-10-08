using System.Xml.Linq;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTWORKSPACE-AMSC-001-W1 (Migrate) — durable Contracts-boundary locks:
/// the composing Admin surface reaches the Catalog write capability only through the
/// <c>ICatalogAdminProductWorkspaceMutationGateway</c> Contracts port, declares its own
/// <c>workspace.*</c> declared-code guard + bilingual resource set, maps typed faults by stable
/// code only, and never duplicates the descriptor ownership that belongs to Catalog.
/// </summary>
public sealed class ProductWorkspaceModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/ProductWorkspace";
    private const string App = ModuleRoot + "/Tooba.ProductWorkspace.Application";
    private const string Contracts = ModuleRoot + "/Tooba.ProductWorkspace.Contracts";
    private const string Endpoints = ModuleRoot + "/Tooba.ProductWorkspace.Endpoints";
    private const string Infra = ModuleRoot + "/Tooba.ProductWorkspace.Infrastructure";
    private const string Commands = App + "/Composition/Commands";
    private const string EndpointModule = Endpoints + "/ProductWorkspaceEndpointModule.cs";
    private const string CatalogContracts = "src/backend/Modules/Catalog/Tooba.Catalog.Contracts";

    [Fact]
    public void Endpoints_reach_Catalog_only_through_the_Contracts_mutation_gateway()
    {
        var root = Repo();
        var module = File.ReadAllText(Path.Combine(root, EndpointModule));

        Assert.Contains("ICatalogAdminProductWorkspaceMutationGateway", module, StringComparison.Ordinal);
        Assert.Contains("CatalogAdminProductWorkspaceActor", module, StringComparison.Ordinal);
        Assert.Contains("IActorDisplayLookup", module, StringComparison.Ordinal);
        Assert.Contains("ProductWorkspaceErrorCodes.WorkspacePermissionDenied", module, StringComparison.Ordinal);
        Assert.Contains("ProductWorkspaceErrorCodes.CategoryAssignmentStale", module, StringComparison.Ordinal);

        // No Catalog Application command, write model, actor context or namespace leaks into the surface.
        foreach (var forbidden in new[]
                 {
                     "using Tooba.Catalog.Application",
                     "ICatalogActorContext",
                     "WorkspaceProductCreateWriteModel",
                     "WorkspaceProductCatalogTitleWriteModel",
                     "WorkspaceProductCoreUpdateWriteModel",
                     "WorkspaceProductQuantityPolicyWriteModel",
                     "WorkspaceProductCategoryAssignWriteModel",
                     "WorkspaceProductAdditionalCategoryWriteModel",
                     "WorkspaceProductBrandAssignWriteModel",
                     "WorkspaceVariantCreateWriteModel",
                     "WorkspaceVariantPatchWriteModel",
                     "CatalogErrorCodes",
                 })
        {
            Assert.DoesNotContain(forbidden, module, StringComparison.Ordinal);
        }

        // The Endpoints project must not reference any foreign Application/Infrastructure/Domain project.
        var references = ProjectRefs(Path.Combine(
            root, Endpoints, "Tooba.ProductWorkspace.Endpoints.csproj"));
        Assert.NotEmpty(references);
        foreach (var reference in references)
        {
            Assert.True(
                reference.Contains("Tooba.BuildingBlocks.csproj", StringComparison.Ordinal)
                || reference.Contains("Tooba.ProductWorkspace.", StringComparison.Ordinal)
                || reference.Contains("Tooba.Catalog.Contracts.csproj", StringComparison.Ordinal)
                || reference.Contains("Tooba.OperatorProfile.Contracts.csproj", StringComparison.Ordinal),
                $"Endpoints must not depend on a foreign non-Contracts project: {reference}");
        }
    }

    [Fact]
    public void Workspace_mutations_are_dispatched_as_module_local_CQRS_commands()
    {
        var root = Repo();
        var commandsDirectory = Path.Combine(root, Commands);
        Assert.True(Directory.Exists(commandsDirectory));

        var expected = new[]
        {
            "CreateWorkspaceProductCommand.cs",
            "UpdateWorkspaceProductCatalogTitleCommand.cs",
            "UpdateWorkspaceProductCoreCommand.cs",
            "UpdateWorkspaceProductQuantityPolicyCommand.cs",
            "AssignWorkspaceProductCategoryCommand.cs",
            "AddWorkspaceProductAdditionalCategoryCommand.cs",
            "RemoveWorkspaceProductAdditionalCategoryCommand.cs",
            "AssignWorkspaceProductBrandCommand.cs",
            "PublishWorkspaceProductCommand.cs",
            "UnpublishWorkspaceProductCommand.cs",
            "ArchiveWorkspaceProductCommand.cs",
            "RestoreWorkspaceProductCommand.cs",
            "CreateWorkspaceProductVariantCommand.cs",
            "PatchWorkspaceProductVariantCommand.cs",
        };
        Assert.Equal(
            expected.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            Directory.EnumerateFiles(commandsDirectory, "*.cs").Select(Path.GetFileName!)
                .OrderBy(x => x, StringComparer.Ordinal).ToArray());

        foreach (var file in expected)
        {
            var text = File.ReadAllText(Path.Combine(commandsDirectory, file));
            Assert.Contains(": IRequest<Result", text, StringComparison.Ordinal);
            Assert.Contains("IRequestHandler<", text, StringComparison.Ordinal);
            Assert.Contains("ICatalogAdminProductWorkspaceMutationGateway", text, StringComparison.Ordinal);
            Assert.Contains("ProductWorkspaceOperation.ExecuteAsync(", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Tooba.Catalog.Application", text, StringComparison.Ordinal);
        }

        // Every dispatched mutation is a module-local command, never a Catalog Application command.
        var module = File.ReadAllText(Path.Combine(root, EndpointModule));
        foreach (var command in new[]
                 {
                     "CreateWorkspaceProductCommand",
                     "UpdateWorkspaceProductCatalogTitleCommand",
                     "UpdateWorkspaceProductCoreCommand",
                     "UpdateWorkspaceProductQuantityPolicyCommand",
                     "AssignWorkspaceProductCategoryCommand",
                     "AddWorkspaceProductAdditionalCategoryCommand",
                     "RemoveWorkspaceProductAdditionalCategoryCommand",
                     "AssignWorkspaceProductBrandCommand",
                     "PublishWorkspaceProductCommand",
                     "UnpublishWorkspaceProductCommand",
                     "ArchiveWorkspaceProductCommand",
                     "RestoreWorkspaceProductCommand",
                     "CreateWorkspaceProductVariantCommand",
                     "PatchWorkspaceProductVariantCommand",
                 })
        {
            Assert.Contains(command, module, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProductWorkspace_declares_and_localizes_workspace_codes_without_duplicating_descriptor_ownership()
    {
        var root = Repo();

        var codes = File.ReadAllText(Path.Combine(root, Contracts, "Errors", "ProductWorkspaceErrorCodes.cs"));
        Assert.Contains("public static bool IsKnown(string? code)", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.product.missing", codes, StringComparison.Ordinal);
        Assert.Contains("workspace.permission.denied", codes, StringComparison.Ordinal);
        Assert.Contains("catalog.category.assignment.stale", codes, StringComparison.Ordinal);

        // Bilingual resource pair exists for the consumed workspace.* keyspace.
        foreach (var culture in new[] { "ProductWorkspaceErrors.resx", "ProductWorkspaceErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, Contracts, "Resources", culture));
            Assert.Contains("workspace.product.missing", resx, StringComparison.Ordinal);
            Assert.Contains("workspace.permission.denied", resx, StringComparison.Ordinal);
        }

        // The module owns the localization set ...
        var resourceSet = File.ReadAllText(Path.Combine(
            root, Contracts, "Errors", "ProductWorkspaceErrorResourceSet.cs"));
        Assert.Contains("ProductWorkspaceErrorCodes.WorkspaceProductMissing", resourceSet, StringComparison.Ordinal);
        Assert.Contains("ProductWorkspaceErrorCodes.WorkspacePermissionDenied", resourceSet, StringComparison.Ordinal);

        // ... but NOT the descriptors: duplicate descriptor ownership breaks the fail-fast composed
        // ErrorDefinitionCatalog, so no ProductWorkspace catalog contributor may exist or be registered.
        Assert.False(File.Exists(Path.Combine(
            root, Contracts, "Errors", "ProductWorkspaceErrorCatalogContributor.cs")));
        var module = File.ReadAllText(Path.Combine(root, Infra, "ProductWorkspaceModule.cs"));
        Assert.Contains("IErrorResourceSet, ProductWorkspaceErrorResourceSet>", module, StringComparison.Ordinal);
        Assert.DoesNotContain("IErrorCatalogContributor", module, StringComparison.Ordinal);

        // Exactly one owner registers each consumed descriptor — the Catalog product write capability.
        var catalogContributor = File.ReadAllText(Path.Combine(
            root, CatalogContracts, "Errors", "CatalogErrorCatalogContributor.cs"));
        Assert.Contains("CatalogErrorCodes.WorkspaceProductMissing", catalogContributor, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes.WorkspacePermissionDenied", catalogContributor, StringComparison.Ordinal);
        Assert.Contains("CatalogErrorCodes.CategoryAssignmentStale", catalogContributor, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductWorkspace_operation_maps_typed_codes_only_and_declares_no_parallel_validators()
    {
        var root = Repo();
        var seam = File.ReadAllText(Path.Combine(root, App, "Composition", "ProductWorkspaceOperation.cs"));
        Assert.Contains(
            "catch (ContractOperationException ex) when (ProductWorkspaceErrorCodes.IsKnown(ex.Code))",
            seam,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", seam, StringComparison.Ordinal);

        // Transport shape is validated by the Catalog write capability, which returns the stable
        // workspace.* codes through the Contracts boundary; a module-local validator tree would fork
        // that canonical code set, so it must not exist.
        Assert.False(Directory.Exists(Path.Combine(root, App, "Validation")));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, App), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            Assert.DoesNotContain("AbstractValidator", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProductWorkspace_has_zero_foreign_Application_Infrastructure_Domain_coupling()
    {
        var root = Repo();

        var applicationRefs = ProjectRefs(Path.Combine(
            root, App, "Tooba.ProductWorkspace.Application.csproj"));
        Assert.NotEmpty(applicationRefs);
        foreach (var reference in applicationRefs)
        {
            if (reference.Contains("Tooba.ProductWorkspace.", StringComparison.Ordinal)
                || reference.Contains("Tooba.BuildingBlocks.csproj", StringComparison.Ordinal))
            {
                continue;
            }

            // Any foreign module reference must be a Contracts boundary — never Application/Infrastructure/Domain.
            Assert.EndsWith(".Contracts.csproj", reference);
        }

        var infrastructureRefs = ProjectRefs(Path.Combine(
            root, Infra, "Tooba.ProductWorkspace.Infrastructure.csproj"));
        foreach (var reference in infrastructureRefs)
        {
            foreach (var foreign in new[]
                     {
                         "Tooba.Catalog.Application", "Tooba.Catalog.Infrastructure", "Tooba.Catalog.Domain",
                         "Tooba.Offer.", "Tooba.Pricing.", "Tooba.Inventory.", "Tooba.Tax.", "Tooba.Party.",
                         "Tooba.Host",
                     })
            {
                Assert.DoesNotContain(foreign, reference, StringComparison.Ordinal);
            }
        }

        foreach (var project in new[]
                 {
                     "Tooba.ProductWorkspace.Contracts", "Tooba.ProductWorkspace.Domain",
                     "Tooba.ProductWorkspace.Application", "Tooba.ProductWorkspace.Infrastructure",
                     "Tooba.ProductWorkspace.Endpoints",
                 })
        {
            foreach (var reference in ProjectRefs(Path.Combine(root, ModuleRoot, project, project + ".csproj")))
            {
                Assert.DoesNotContain("Tooba.Host", reference, StringComparison.Ordinal);
            }
        }

        // No foreign DbContext / EF usage anywhere in the module.
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, ModuleRoot), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("CatalogDbContext", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("using Tooba.Host", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProductWorkspace_host_surface_stays_composition_only_and_routes_are_preserved()
    {
        var root = Repo();
        Assert.False(Directory.Exists(Path.Combine(root, "src/backend/Host/Tooba.Host/ProductWorkspace")));

        var module = File.ReadAllText(Path.Combine(root, EndpointModule));
        Assert.Equal(17, System.Text.RegularExpressions.Regex.Matches(
            module, @"\bMap(Get|Post|Put|Patch|Delete)\s*\(").Count);

        var program = File.ReadAllText(Path.Combine(root, "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("MapProductWorkspaceModuleEndpoints", program, StringComparison.Ordinal);
        Assert.Contains("AddProductWorkspaceEndpointPresentation", program, StringComparison.Ordinal);
        Assert.DoesNotContain("Tooba.Host.ProductWorkspace", program, StringComparison.Ordinal);
    }

    private static string[] ProjectRefs(string csprojPath) =>
        XDocument.Load(csprojPath)
            .Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .ToArray();

    private static string Repo()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
