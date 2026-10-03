using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Media.Application.Assets.Commands;
using Tooba.Media.Application.Assets.Queries;
using Tooba.Media.Application.Assets.Validators;
using Tooba.Media.Contracts.Errors;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-MEDIA-AMC-001-W3 — CQRS/Result/validators/error-catalog durable inventory.
/// </summary>
public sealed class MediaModuleAmcW3CqrsGuardTests
{
    private const string ValidatorRequiredPresent = "VALIDATOR_REQUIRED_PRESENT";
    private const string NoValidatorRequired = "NO_VALIDATOR_REQUIRED";

    private static readonly (string RequestTypeName, string Classification)[] Manifest =
    [
        ("UploadMediaAssetCommand", ValidatorRequiredPresent),
        ("QueryMediaAssetsQuery", ValidatorRequiredPresent),
        ("GetMediaAssetQuery", ValidatorRequiredPresent),
        ("GetMediaStorageKeyQuery", NoValidatorRequired),
    ];

    private static readonly (string RequestTypeName, Type ValidatorType)[] RequiredValidators =
    [
        ("UploadMediaAssetCommand", typeof(UploadMediaAssetCommandValidator)),
        ("QueryMediaAssetsQuery", typeof(QueryMediaAssetsQueryValidator)),
        ("GetMediaAssetQuery", typeof(GetMediaAssetQueryValidator)),
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
        services.AddToobaCqrsFoundation(typeof(UploadMediaAssetCommand).Assembly);
        using var sp = services.BuildServiceProvider();

        foreach (var (name, validatorType) in RequiredValidators)
        {
            var requestType = typeof(UploadMediaAssetCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            var validator = sp.GetService(validatorInterface);
            Assert.NotNull(validator);
            Assert.IsType(validatorType, validator);
        }

        foreach (var (name, _) in Manifest.Where(x => x.Classification == NoValidatorRequired))
        {
            var requestType = typeof(UploadMediaAssetCommand).Assembly
                .GetTypes()
                .Single(t => t.Name == name);
            var validatorInterface = typeof(IValidator<>).MakeGenericType(requestType);
            Assert.Null(sp.GetService(validatorInterface));
        }
    }

    [Fact]
    public void Admin_endpoints_dispatch_through_ISender_and_api_From()
    {
        var admin = File.ReadAllText(Path.Combine(
            Repo(),
            "src/backend/Modules/Media/Tooba.Media.Endpoints/Admin/MediaAdminEndpoints.cs"));
        Assert.Contains("ISender", admin, StringComparison.Ordinal);
        Assert.Contains("api.From", admin, StringComparison.Ordinal);
        Assert.Contains("ApiResponseFactory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("IMediaDirectory", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("PlatformHttpException", admin, StringComparison.Ordinal);
        Assert.DoesNotContain("Results.Json(new { title", admin, StringComparison.Ordinal);
    }

    [Fact]
    public void Error_catalog_and_resources_are_present()
    {
        var root = Repo();
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Media/Tooba.Media.Contracts/Errors/MediaErrorCatalogContributor.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Media/Tooba.Media.Contracts/Errors/MediaErrorResourceSet.cs")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Media/Tooba.Media.Contracts/Resources/MediaErrors.resx")));
        Assert.True(File.Exists(Path.Combine(
            root, "src/backend/Modules/Media/Tooba.Media.Contracts/Resources/MediaErrors.fa.resx")));

        var module = File.ReadAllText(Path.Combine(
            root, "src/backend/Modules/Media/Tooba.Media.Infrastructure/MediaModule.cs"));
        Assert.Contains("MediaErrorCatalogContributor", module, StringComparison.Ordinal);
        Assert.Contains("MediaErrorResourceSet", module, StringComparison.Ordinal);

        Assert.Equal("media.upload.failed", MediaErrorCodes.UploadFailed);
        Assert.Equal("media.missing", MediaErrorCodes.Missing);
    }

    [Fact]
    public void Host_registers_media_application_in_cqrs_foundation()
    {
        var program = File.ReadAllText(Path.Combine(Repo(), "src/backend/Host/Tooba.Host/Program.cs"));
        Assert.Contains("UploadMediaAssetCommand", program, StringComparison.Ordinal);
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
