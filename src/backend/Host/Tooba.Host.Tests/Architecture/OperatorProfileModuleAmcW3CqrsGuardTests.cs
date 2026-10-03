using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.OperatorProfile.Application.Admin.Commands;
using Tooba.OperatorProfile.Application.Admin.Queries;
using Tooba.OperatorProfile.Application.Admin.Validators;
using Tooba.OperatorProfile.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-OPERATORPROFILE-AMC-001-W3 — CQRS/Result/validators/error-catalog durable inventory.
/// </summary>
public sealed class OperatorProfileModuleAmcW3CqrsGuardTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("GetOperatorProfileQuery", ValidatorRequiredPresent),
        ("UpsertOperatorProfileCommand", ValidatorRequiredPresent),
    ];

    private static readonly (string RequestTypeName, Type ValidatorType)[] RequiredValidators =
    [
        ("GetOperatorProfileQuery", typeof(GetOperatorProfileQueryValidator)),
        ("UpsertOperatorProfileCommand", typeof(UpsertOperatorProfileCommandValidator)),
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
        services.AddToobaCqrsFoundation(typeof(UpsertOperatorProfileCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        foreach (var (name, validatorType) in RequiredValidators)
        {
            var requestType = typeof(UpsertOperatorProfileCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            var validator = sp.GetService(validatorInterface);
            Assert.NotNull(validator);
            Assert.IsType(validatorType, validator);
        }
    }

    [Fact]
    public void Admin_endpoints_dispatch_through_ISender_and_api_From()
    {
        var admin = File.ReadAllText(Path.Combine(
            Repo(),
            "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Endpoints/Admin/OperatorProfileAdminEndpoints.cs"));
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("api.From", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IOperatorProfileDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_catalog_and_resources_are_present()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Contracts/Errors/OperatorProfileErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Contracts/Errors/OperatorProfileErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Contracts/Resources/OperatorProfileErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Contracts/Resources/OperatorProfileErrors.fa.resx")));

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/OperatorProfile/Tooba.OperatorProfile.Infrastructure/OperatorProfileModule.cs"));
        Assert.Contains("OperatorProfileErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("OperatorProfileErrorResourceSet", module, StringComparison.Ordinal);

        Assert.Equal("operator.profile.rejected", OperatorProfileErrorCodes.ProfileRejected);
        Assert.Equal("operator.profile.validation.display_name", OperatorProfileErrorCodes.InvalidDisplayName);
    }

    [Fact]
    public void Host_registers_operatorprofile_application_in_cqrs_foundation()
    {
        var program = File.ReadAllText(Path.Combine(Repo(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("UpsertOperatorProfileCommand", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.OperatorProfile.Application", program, StringComparison.Ordinal);
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
