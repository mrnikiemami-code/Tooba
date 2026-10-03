using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-BULKINQUIRY-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class BulkInquiryModuleAmcW2StructureGuardTests
{
    [Fact]
    public void BulkInquiry_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Domain");
        var app = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Application");
        var infra = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/BulkInquiry/Tooba.BulkInquiry.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "BulkPurchaseInquiry.cs")));
        Assert.False(File.Exists(Path.Combine(app, "BulkInquiryContracts.cs")));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.False(File.Exists(Path.Combine(infra, "BulkInquiryDirectory.cs")));

        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "BulkPurchaseInquiry.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Enums", "BulkInquiryStatus.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IBulkInquiryDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "BulkInquiryModels.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Storefront", "Commands", "SubmitBulkInquiryCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Storefront", "Validators", "SubmitBulkInquiryCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "BulkInquiryDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "Migrations", "20260826120000_InitialBulkInquiry.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "BulkInquiryOutboxRegistration.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "BulkInquiryErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "BulkInquiryModule.cs" },
            Directory.EnumerateFiles(infra, "*.cs", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray());
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
