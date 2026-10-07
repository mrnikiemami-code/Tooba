using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>TB-TMAR-PRODUCTQNA-AMC-001-W2 — Domain/Application/Infra layer structure without root dumps.</summary>
public sealed class ProductQnAModuleAmcW2StructureGuardTests
{
    [Fact]
    public void ProductQnA_layers_are_capability_cohesive_without_root_dumps()
    {
        var root = Repo();
        var domain = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Domain");
        var app = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Application");
        var infra = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Infrastructure");
        var contracts = Path.Combine(root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Contracts");

        Assert.False(File.Exists(Path.Combine(domain, "ProductQuestion.cs")));
        Assert.False(File.Exists(Path.Combine(app, "ProductQaContracts.cs")));
        Assert.False(Directory.Exists(Path.Combine(app, "Commands")));
        Assert.False(Directory.Exists(Path.Combine(app, "Queries")));
        Assert.False(Directory.Exists(Path.Combine(infra, "Migrations")));
        Assert.False(File.Exists(Path.Combine(infra, "ProductQaDirectory.cs")));

        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "ProductQuestion.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Aggregates", "ProductAnswer.cs")));
        Assert.True(File.Exists(Path.Combine(domain, "Enums", "ProductQuestionStatus.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Ports", "IProductQaDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Models", "ProductQaModels.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Customer", "Commands", "SubmitProductQuestionCommand.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Validation", "SubmitProductQuestionCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(app, "Storefront", "Queries", "GetPublishedQuestionsQuery.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Directories", "ProductQaDirectory.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Development", "ProductQnADevelopmentSeed.cs")));
        Assert.True(File.Exists(Path.Combine(infra, "Persistence", "Migrations", "20260826120000_InitialProductQnA.cs")));
        Assert.True(File.Exists(Path.Combine(contracts, "Errors", "ProductQnAErrorCodes.cs")));

        Assert.Empty(Directory.EnumerateFiles(domain, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(app, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Empty(Directory.EnumerateFiles(contracts, "*.cs", SearchOption.TopDirectoryOnly));
        Assert.Equal(
            new[] { "ProductQnAModule.cs" },
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
