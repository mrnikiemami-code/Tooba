using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.ProductQnA.Application.Customer.Commands;
using Tooba.ProductQnA.Application.Storefront.Queries;
using Tooba.ProductQnA.Application.Validation;
using Tooba.ProductQnA.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTQNA-AMSC-001-W1 (Migrate) — durable canonicalization locks:
/// typed code-carrying Domain/Infrastructure faults, a known-code filtered composition seam,
/// Application-owned transport validation codes, and a Contracts catalog that registers only
/// ProductQnA-owned semantic descriptors.
/// </summary>
public sealed class ProductQnAModuleAmsc001W1MigrateGuardTests
{
    private const string ModuleRoot = "src/backend/Modules/ProductQnA";
    private const string App = ModuleRoot + "/Tooba.ProductQnA.Application";
    private const string Domain = ModuleRoot + "/Tooba.ProductQnA.Domain";
    private const string Infra = ModuleRoot + "/Tooba.ProductQnA.Infrastructure";
    private const string Contracts = ModuleRoot + "/Tooba.ProductQnA.Contracts";

    [Fact]
    public void ProductQnA_error_codes_declare_known_guard_and_no_transport_validation_codes()
    {
        Assert.Contains("public static bool IsKnown(string? code)", File.ReadAllText(
            Path.Combine(Repo(), Contracts, "Errors", "ProductQnAErrorCodes.cs")), StringComparison.Ordinal);

        Assert.True(ProductQnAErrorCodes.IsKnown(ProductQnAErrorCodes.Rejected));
        Assert.True(ProductQnAErrorCodes.IsKnown(ProductQnAErrorCodes.NotFound));
        Assert.False(ProductQnAErrorCodes.IsKnown("product_qna.validation.actor_required"));
        Assert.False(ProductQnAErrorCodes.IsKnown("bulk_inquiry.rejected"));
        Assert.False(ProductQnAErrorCodes.IsKnown(null));
        Assert.False(ProductQnAErrorCodes.IsKnown(" "));

        var codes = File.ReadAllText(Path.Combine(Repo(), Contracts, "Errors", "ProductQnAErrorCodes.cs"));
        Assert.DoesNotContain("product_qna.validation.", codes, StringComparison.Ordinal);
        Assert.Contains("product_qna.rejected", codes, StringComparison.Ordinal);
        Assert.Contains("product_qna.not_found", codes, StringComparison.Ordinal);
        Assert.Contains("customer.session.required", codes, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductQnA_validation_codes_are_application_owned_and_not_catalogued()
    {
        var root = Repo();

        var validationCodes = File.ReadAllText(Path.Combine(root, App, "Validation", "ProductQnAValidationCodes.cs"));
        foreach (var code in new[]
                 {
                     "product_qna.validation.actor_required",
                     "product_qna.validation.body_required",
                     "product_qna.validation.product_required",
                     "product_qna.validation.question_body_required",
                     "product_qna.validation.slug_required",
                     "product_qna.validation.page_invalid",
                     "product_qna.validation.page_size_invalid",
                 })
        {
            Assert.Contains(code, validationCodes, StringComparison.Ordinal);
        }

        // The catalog registration body must not declare any transport validation code.
        var catalog = File.ReadAllText(Path.Combine(root, Contracts, "Errors", "ProductQnAErrorCatalogContributor.cs"));
        var registrations = catalog[catalog.IndexOf("Contribute()", StringComparison.Ordinal)..];
        Assert.DoesNotContain("ProductQnAValidationCodes", registrations, StringComparison.Ordinal);
        Assert.DoesNotContain("product_qna.validation.", registrations, StringComparison.Ordinal);
        Assert.Contains("ProductQnAErrorCodes.Rejected", registrations, StringComparison.Ordinal);
        Assert.Contains("ProductQnAErrorCodes.NotFound", registrations, StringComparison.Ordinal);

        // Both cultures still carry every localization key (unchanged values).
        foreach (var culture in new[] { "ProductQnAErrors.resx", "ProductQnAErrors.fa.resx" })
        {
            var resx = File.ReadAllText(Path.Combine(root, Contracts, "Resources", culture));
            Assert.Contains("product_qna.rejected", resx, StringComparison.Ordinal);
            Assert.Contains("product_qna.not_found", resx, StringComparison.Ordinal);
            Assert.Contains("product_qna.validation.page_size_invalid", resx, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ProductQnA_validators_live_in_one_shared_validation_folder_and_are_discoverable()
    {
        var root = Repo();

        Assert.False(Directory.Exists(Path.Combine(root, App, "Customer", "Validators")));
        Assert.False(Directory.Exists(Path.Combine(root, App, "Storefront", "Validators")));
        Assert.True(File.Exists(Path.Combine(root, App, "Validation", "SubmitProductQuestionCommandValidator.cs")));
        Assert.True(File.Exists(Path.Combine(root, App, "Validation", "GetPublishedQuestionsQueryValidator.cs")));

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(SubmitProductQuestionCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        Assert.IsType<SubmitProductQuestionCommandValidator>(
            sp.GetRequiredService<IValidator<SubmitProductQuestionCommand>>());
        Assert.IsType<GetPublishedQuestionsQueryValidator>(
            sp.GetRequiredService<IValidator<GetPublishedQuestionsQuery>>());
    }

    [Fact]
    public void ProductQnA_domain_and_directory_throw_typed_code_carrying_faults()
    {
        var root = Repo();

        foreach (var relative in new[]
                 {
                     "Tooba.ProductQnA.Domain/Aggregates/ProductQuestion.cs",
                     "Tooba.ProductQnA.Domain/Aggregates/ProductAnswer.cs",
                     "Tooba.ProductQnA.Infrastructure/Directories/ProductQaDirectory.cs",
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, ModuleRoot, relative));
            Assert.Contains("ContractOperationException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SemanticException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("new SemanticError(", text, StringComparison.Ordinal);
        }

        Assert.DoesNotMatch(
            new Regex(@"throw new ContractOperationException\((""[^""]+""|ProductQnAErrorCodes\.(?!Rejected))"),
            File.ReadAllText(Path.Combine(root, Domain, "Aggregates", "ProductQuestion.cs")));
    }

    [Fact]
    public void ProductQnA_operation_maps_known_contract_codes_plus_semantic_faults_only()
    {
        var seam = File.ReadAllText(Path.Combine(Repo(), App, "Composition", "ProductQnAOperation.cs"));
        Assert.Contains(
            "catch (ContractOperationException ex) when (ProductQnAErrorCodes.IsKnown(ex.Code))",
            seam,
            StringComparison.Ordinal);
        Assert.Contains("catch (SemanticException ex)", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", seam, StringComparison.Ordinal);
        Assert.DoesNotContain("exception.Message", seam, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductQnA_domain_and_application_reference_only_foundation_and_own_projects()
    {
        var root = Repo();

        foreach (var project in new[] { "Tooba.ProductQnA.Domain", "Tooba.ProductQnA.Application" })
        {
            var csproj = File.ReadAllText(Path.Combine(root, ModuleRoot, project, project + ".csproj"));
            var references = csproj
                .Split('<', '>')
                .Where(token => token.StartsWith("ProjectReference Include=", StringComparison.Ordinal))
                .ToArray();
            Assert.NotEmpty(references);

            foreach (var reference in references)
            {
                Assert.True(
                    reference.Contains("Tooba.BuildingBlocks.csproj", StringComparison.Ordinal)
                    || reference.Contains("Tooba.ProductQnA.", StringComparison.Ordinal),
                    $"{project} must not depend on a non-foundation foreign project: {reference}");
            }
        }

        // Infrastructure keeps the Contracts-only Catalog seam and nothing foreign.
        var infraCsproj = File.ReadAllText(Path.Combine(root, Infra, "Tooba.ProductQnA.Infrastructure.csproj"));
        Assert.Contains("Tooba.Catalog.Contracts", infraCsproj, StringComparison.Ordinal);
        foreach (var forbidden in new[]
                 {
                     "Tooba.Catalog.Application", "Tooba.Catalog.Domain", "Tooba.Catalog.Infrastructure",
                     "Tooba.BulkInquiry.", "Tooba.Order.", "Tooba.Cart.",
                 })
        {
            Assert.DoesNotContain(forbidden, infraCsproj, StringComparison.Ordinal);
        }
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
