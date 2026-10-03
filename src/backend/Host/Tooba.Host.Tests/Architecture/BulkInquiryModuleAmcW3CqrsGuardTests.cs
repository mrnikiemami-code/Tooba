using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-BULKINQUIRY-AMC-001-W3 — Result pipeline, Contracts catalog, thin endpoints.</summary>
public sealed class BulkInquiryModuleAmcW3CqrsGuardTests
{
    [Fact]
    public void BulkInquiry_w3_owns_catalog_in_contracts_and_result_pipeline()
    {
        var root = Repo();
        var contracts = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Contracts");
        var app = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Application");
        var endpoints = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Endpoints");
        var infra = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Infrastructure");

        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "BulkInquiryErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "BulkInquiryErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Resources", "BulkInquiryErrors.resx")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Errors")));
        Assert.False(Directory.Exists(Path.Combine(endpoints, "Resources")));
        Assert.True(File.Exists(Path.Combine(app, "Composition", "BulkInquiryOperation.cs")));

        var module = File.ReadAllText(Path.Combine(infra, "BulkInquiryModule.cs"));
        Assert.Contains("BulkInquiryErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("BulkInquiryErrorResourceSet", module, StringComparison.Ordinal);

        foreach (var file in Directory.EnumerateFiles(endpoints, "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                                 && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (SemanticException", text, StringComparison.Ordinal);
        }

        var validator = File.ReadAllText(Path.Combine(
            app, "Storefront", "Validators", "SubmitBulkInquiryCommandValidator.cs"));
        Assert.Contains("BulkInquiryErrorCodes.", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("\"bulk_inquiry.validation.", validator, StringComparison.Ordinal);

        var command = File.ReadAllText(Path.Combine(
            app, "Storefront", "Commands", "SubmitBulkInquiryCommand.cs"));
        Assert.Contains("IRequest<Result<", command, StringComparison.Ordinal);
        Assert.Contains("BulkInquiryOperation.ExecuteAsync", command, StringComparison.Ordinal);
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
