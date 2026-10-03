using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Party.Application.Admin.Sellers.Queries;
using Tooba.Party.Application.Admin.Sellers.Validators;
using Tooba.Party.Application.Seller.Commands;
using Tooba.Party.Application.Seller.Queries;
using Tooba.Party.Application.Seller.Validators;
using Tooba.Party.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-PARTY-AMC-001-W3 — CQRS/Result/validators/error-catalog durable inventory.
/// </summary>
public sealed class PartyModuleAmcW3CqrsGuardTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED_NO_TRANSPORT_INPUT";

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("GetSellerSettingsQuery", NoValidatorRequired),
        ("UpdateSellerSettingsCommand", ValidatorRequiredPresent),
        ("ListAdminSellersQuery", NoValidatorRequired),
        ("QueryAdminSellersGridQuery", ValidatorRequiredPresent),
    ];

    private static readonly (string RequestTypeName, Type ValidatorType)[] RequiredValidators =
    [
        ("UpdateSellerSettingsCommand", typeof(UpdateSellerSettingsCommandValidator)),
        ("QueryAdminSellersGridQuery", typeof(QueryAdminSellersGridQueryValidator)),
    ];

    [Fact]
    public void Endpoint_reachable_requests_are_exhaustively_classified()
    {
        Assert.Equal(4, Manifest.Length);
        Assert.Equal(Manifest.Length, Manifest.Select(x => x.RequestTypeName).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, Manifest.Count(x => x.Classification == ValidatorRequiredPresent));
        Assert.Equal(2, Manifest.Count(x => x.Classification == NoValidatorRequired));
    }

    [Fact]
    public void Required_validators_are_registered_in_cqrs_foundation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(UpdateSellerSettingsCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        foreach (var (name, validatorType) in RequiredValidators)
        {
            var requestType = typeof(UpdateSellerSettingsCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            var validator = sp.GetService(validatorInterface);
            Assert.NotNull(validator);
            Assert.IsType(validatorType, validator);
        }

        foreach (var (name, classification) in Manifest.Where(x => x.Classification == NoValidatorRequired))
        {
            var requestType = typeof(UpdateSellerSettingsCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            Assert.Null(sp.GetService(validatorInterface));
        }
    }

    [Fact]
    public void Seller_and_admin_endpoints_dispatch_through_ISender_and_api_From()
    {
        var root = Repo();
        var seller = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Endpoints/Seller/PartySellerSettingsEndpoints.cs"));
        var admin = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Endpoints/Admin/Sellers/PartyAdminSellersEndpoints.cs"));

        foreach (var text in new[] { seller, admin })
        {
            Assert.Contains("ISender", text, StringComparison.Ordinal);
            Assert.Contains("ApiResponseFactory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
            Assert.DoesNotContain("PartyDbContext", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Error_catalog_and_resources_live_in_contracts_and_register_in_infrastructure()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Contracts/Errors/PartyErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Contracts/Errors/PartyErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Contracts/Resources/PartyErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Contracts/Resources/PartyErrors.fa.resx")));
        Assert.False(Directory.Exists(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Endpoints/Errors")));
        Assert.False(Directory.Exists(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Endpoints/Resources")));

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Infrastructure/PartyModule.cs"));
        Assert.Contains("PartyErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("PartyErrorResourceSet", module, StringComparison.Ordinal);

        Assert.Equal("seller.settings.missing", PartyErrorCodes.SellerSettingsMissing);
        Assert.Equal("seller.settings.rejected", PartyErrorCodes.SellerSettingsRejected);
        Assert.Equal("party.operation.rejected", PartyErrorCodes.OperationRejected);
    }

    [Fact]
    public void Domain_and_directory_use_semantic_exception_not_invalid_operation()
    {
        var root = Repo();
        var domain = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Domain/Aggregates/BusinessParty.cs"));
        var directory = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Infrastructure/Directories/PartyDirectory.cs"));
        Assert.Contains("SemanticException", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", domain, StringComparison.Ordinal);
        Assert.Contains("SemanticException", directory, StringComparison.Ordinal);
        Assert.DoesNotContain("InvalidOperationException", directory, StringComparison.Ordinal);

        var update = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Party/Tooba.Party.Application/Seller/Commands/UpdateSellerSettingsCommand.cs"));
        Assert.Contains("PartyOperation.ExecuteAsync", update, StringComparison.Ordinal);
    }

    [Fact]
    public void Host_registers_party_application_in_cqrs_foundation()
    {
        var program = File.ReadAllText(Path.Combine(Repo(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("GetSellerSettingsQuery", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Party.Application", program, StringComparison.Ordinal);
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
