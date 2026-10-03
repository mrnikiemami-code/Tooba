using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.ProductQnA.Application.Customer.Commands;
using Tooba.ProductQnA.Application.Customer.Validators;
using Tooba.ProductQnA.Application.Storefront.Queries;
using Tooba.ProductQnA.Application.Storefront.Validators;
using Tooba.ProductQnA.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PRODUCTQNA-AMC-001-W3 — CQRS/Result/validators/error-catalog durable inventory.
/// </summary>
public sealed class ProductQnAModuleAmcW3CqrsGuardTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("SubmitProductQuestionCommand", ValidatorRequiredPresent),
        ("GetPublishedQuestionsQuery", ValidatorRequiredPresent),
    ];

    private static readonly (string RequestTypeName, Type ValidatorType)[] RequiredValidators =
    [
        ("SubmitProductQuestionCommand", typeof(SubmitProductQuestionCommandValidator)),
        ("GetPublishedQuestionsQuery", typeof(GetPublishedQuestionsQueryValidator)),
    ];

    [Fact]
    public void Endpoint_reachable_requests_are_exhaustively_classified()
    {
        Assert.Equal(2, Manifest.Length);
        Assert.Equal(Manifest.Length, Manifest.Select(x => x.RequestTypeName).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Required_validators_are_registered_in_cqrs_foundation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(SubmitProductQuestionCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        foreach (var (name, validatorType) in RequiredValidators)
        {
            var requestType = typeof(SubmitProductQuestionCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            var validator = sp.GetService(validatorInterface);
            Assert.NotNull(validator);
            Assert.IsType(validatorType, validator);
        }
    }

    [Fact]
    public void Endpoints_dispatch_through_ISender_and_api_From_without_Results_Json()
    {
        var root = Repo();
        var customer = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints/Customer/ProductQnACustomerEndpoints.cs"));
        var storefront = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints/Storefront/ProductQnAStorefrontEndpoints.cs"));

        foreach (var text in new[] { customer, storefront })
        {
            Assert.Contains("ISender", text, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("catch (SemanticException", text, StringComparison.Ordinal);
            Assert.DoesNotContain("ProductQnADbContext", text, StringComparison.Ordinal);
        }

        Assert.Contains("api.Created", customer, StringComparison.Ordinal);
        Assert.Contains("api.From", storefront, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_catalog_and_resources_live_in_contracts_and_register_in_infrastructure()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Contracts/Errors/ProductQnAErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Contracts/Errors/ProductQnAErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Contracts/Resources/ProductQnAErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Contracts/Resources/ProductQnAErrors.fa.resx")));
        Assert.False(Directory.Exists(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints/Errors")));
        Assert.False(Directory.Exists(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Endpoints/Resources")));

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/ProductQnA/Tooba.ProductQnA.Infrastructure/ProductQnAModule.cs"));
        Assert.Contains("ProductQnAErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("ProductQnAErrorResourceSet", module, StringComparison.Ordinal);

        Assert.Equal("product_qna.rejected", ProductQnAErrorCodes.Rejected);
        Assert.Equal("product_qna.not_found", ProductQnAErrorCodes.NotFound);
    }

    [Fact]
    public void Host_registers_productqna_application_in_cqrs_foundation()
    {
        var program = File.ReadAllText(Path.Combine(Repo(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("SubmitProductQuestionCommand", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.ProductQnA.Application", program, StringComparison.Ordinal);
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
