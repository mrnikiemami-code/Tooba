using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Localization.Application.Languages.Commands;
using Tooba.Localization.Application.Languages.Queries;
using Tooba.Localization.Application.Languages.Validators;
using Tooba.Localization.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-LOCALIZATION-AMC-001-W3 — CQRS/Result/validators/error-catalog durable inventory.
/// </summary>
public sealed class LocalizationModuleAmcW3CqrsGuardTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("ListLanguagesAdminQuery", ValidatorRequiredPresent),
        ("CreateLanguageCommand", ValidatorRequiredPresent),
        ("UpdateLanguageCommand", ValidatorRequiredPresent),
        ("PatchLanguageCommand", ValidatorRequiredPresent),
    ];

    private static readonly (string RequestTypeName, Type ValidatorType)[] RequiredValidators =
    [
        ("ListLanguagesAdminQuery", typeof(ListLanguagesAdminQueryValidator)),
        ("CreateLanguageCommand", typeof(CreateLanguageCommandValidator)),
        ("UpdateLanguageCommand", typeof(UpdateLanguageCommandValidator)),
        ("PatchLanguageCommand", typeof(PatchLanguageCommandValidator)),
    ];

    [Fact]
    public void Endpoint_reachable_requests_are_exhaustively_classified()
    {
        Assert.Equal(4, Manifest.Length);
        Assert.Equal(Manifest.Length, Manifest.Select(x => x.RequestTypeName).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Required_validators_are_registered_in_cqrs_foundation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddToobaCqrsFoundation(typeof(CreateLanguageCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        foreach (var (name, validatorType) in RequiredValidators)
        {
            var requestType = typeof(CreateLanguageCommand).Assembly
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
            "src/backend/Modules/Localization/Tooba.Localization.Endpoints/Admin/LocaleAdminEndpoints.cs"));
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("api.From", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("ILanguageDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("SemanticException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_json_api_endpoints_have_zero_direct_Results_Json()
    {
        var root = Repo();
        var admin = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Endpoints/Admin/LocaleAdminEndpoints.cs");
        var module = Path.Combine(root, "src/backend/Modules/Localization/Tooba.Localization.Endpoints/LocalizationEndpointModule.cs");
        foreach (var path in new[] { admin, module })
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("Results.Json", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Error_catalog_and_resources_are_present()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Contracts/Errors/LocalizationErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Contracts/Errors/LocalizationErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Contracts/Resources/LocalizationErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Contracts/Resources/LocalizationErrors.fa.resx")));

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Infrastructure/LocalizationModule.cs"));
        Assert.Contains("LocalizationErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("LocalizationErrorResourceSet", module, StringComparison.Ordinal);

        Assert.Equal("localization.language.not_found", LanguageErrorCodes.NotFound);
        Assert.Equal("localization.language.code_duplicate", LanguageErrorCodes.CodeDuplicate);
    }

    [Fact]
    public void Host_registers_localization_application_in_cqrs_foundation()
    {
        var program = File.ReadAllText(Path.Combine(Repo(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("CreateLanguageCommand", program, StringComparison.Ordinal);
        Assert.Contains("Tooba.Localization.Application", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Localization_operation_and_handlers_exist()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Application/Composition/LocalizationOperation.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Application/Languages/Commands/CreateLanguageCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Application/Languages/Commands/UpdateLanguageCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Application/Languages/Commands/PatchLanguageCommand.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Localization/Tooba.Localization.Application/Languages/Queries/ListLanguagesAdminQuery.cs")));
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
